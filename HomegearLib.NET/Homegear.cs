using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using HomegearLib.RPC;

namespace HomegearLib
{
    public class Homegear : IDisposable
    {
        public delegate void ConnectErrorEventHandler(Homegear sender, String message, String stackTrace);
        public delegate void ConnectedEventHandler(Homegear sender);
        public delegate void DisconnectedEventHandler(Homegear sender);
        public delegate void DataReloadEventHandler(Homegear sender);
        public delegate void DeviceVariableUpdatedEventHandler(Homegear sender, Device device, Channel channel, Variable variable);
        public delegate void DeviceConfigParameterUpdatedEventHandler(Homegear sender, Device device, Channel channel, ConfigParameter parameter);
        public delegate void DeviceLinkConfigParameterUpdatedEventHandler(Homegear sender, Device device, Channel channel, Link link, ConfigParameter parameter);
        public delegate void DeviceLinksUpdatedEventHandler(Homegear sender, Device device, Channel channel);
        public delegate void ReloadRequiredEventHandler(Homegear sender);

        #region "Events"
        public event ConnectErrorEventHandler ConnectError;
        public event DataReloadEventHandler Reloaded;
        public event DeviceVariableUpdatedEventHandler DeviceVariableUpdated;
        public event DeviceConfigParameterUpdatedEventHandler DeviceConfigParameterUpdated;
        public event DeviceLinkConfigParameterUpdatedEventHandler DeviceLinkConfigParameterUpdated;
        public event DeviceLinksUpdatedEventHandler DeviceLinksUpdated;
        public event ReloadRequiredEventHandler ReloadRequired;
        public event ConnectedEventHandler Connected;
        public event DisconnectedEventHandler Disconnected;
        #endregion

        RPCController _rpc = null;
        volatile bool _disposing = false;
        volatile bool _stopConnectThread = false;
        Thread _connectThread = null;
        
        Families _families = null;
        public Families Families { get { return _families; } }

        Devices _devices = null;
        public Devices Devices { get { return _devices; } }

        Interfaces _interfaces = null;
        public Interfaces Interfaces
        {
            get
            {
                if (_interfaces == null || _interfaces.Count == 0) _interfaces = new Interfaces(_rpc, _rpc.Interfaces);
                bool interfacesAdded = false;
                bool interfacesRemoved = false;
                _interfaces.Update(out interfacesRemoved, out interfacesAdded);
                if ((interfacesAdded || interfacesRemoved) && ReloadRequired != null) ReloadRequired(this);
                return _interfaces;
            }
        }

        public Homegear(RPCController rpc)
        {
            if (rpc == null) throw new NullReferenceException("RPC object is null.");
            _rpc = rpc;
            _families = new Families(_rpc, new Dictionary<Int32, Family>());
            _devices = new Devices(_rpc, new Dictionary<Int32, Device>());
            _rpc.Connected += _rpc_Connected;
            _rpc.Disconnected += _rpc_Disconnected;
            _rpc.InitCompleted += _rpc_InitCompleted;
            _rpc.RPCEvent += _rpc_OnRPCEvent;
            _rpc.NewDevices += _rpc_OnNewDevices;
            _rpc.DevicesDeleted += _rpc_OnDevicesDeleted;
            _rpc.UpdateDevice += _rpc_OnUpdateDevice;
            _stopConnectThread = false;
            _connectThread = new Thread(Connect);
            _connectThread.Start();
            while (!_connectThread.IsAlive) ;
        }

        private void _rpc_OnNewDevices(RPCController sender)
        {
            if (ReloadRequired != null) ReloadRequired(this);
        }

        void _rpc_OnUpdateDevice(RPCController sender, int peerID, int channelIndex, RPCUpdateDeviceFlags flags)
        {
            if (!Devices.ContainsKey(peerID)) return;
            Device device = Devices[peerID];
            if (!device.Channels.ContainsKey(channelIndex)) return;
            Channel channel = device.Channels[channelIndex];
            if(flags == RPCUpdateDeviceFlags.rpcConfig)
            {
                List<ConfigParameter> changedParameters = channel.Config.Reload();
                foreach(ConfigParameter parameter in changedParameters)
                {
                    if (DeviceConfigParameterUpdated != null) DeviceConfigParameterUpdated(this, device, channel, parameter);
                }
                foreach (KeyValuePair<Int32, ReadOnlyDictionary<Int32, Link>> remotePeer in channel.Links)
                {
                    foreach (KeyValuePair<Int32, Link> linkPair in remotePeer.Value)
                    {
                        changedParameters = linkPair.Value.Config.Reload();
                        foreach (ConfigParameter parameter in changedParameters)
                        {
                            if (DeviceLinkConfigParameterUpdated != null) DeviceLinkConfigParameterUpdated(this, device, channel, linkPair.Value, parameter);
                        }
                    }
                }
            }
            else
            {
                channel.Links = null;
                if (DeviceLinksUpdated != null) DeviceLinksUpdated(this, device, channel);
            }
        }

        void _rpc_OnDevicesDeleted(RPCController sender)
        {
            if (ReloadRequired != null) ReloadRequired(this);
        }

        void _rpc_OnRPCEvent(RPCController sender, Variable value)
        {
            if (_disposing) return;
            if(value.PeerID == 0) return; //System variable
            if (!Devices.ContainsKey(value.PeerID)) return;
            Device device = Devices[value.PeerID];
            if (!device.Channels.ContainsKey(value.Channel)) return;
            Channel deviceChannel = device.Channels[value.Channel];
            if (!deviceChannel.Variables.ContainsKey(value.Name)) return;
            Variable variable = deviceChannel.Variables[value.Name];
            variable.SetValue(value);
            if (DeviceVariableUpdated != null) DeviceVariableUpdated(this, device, deviceChannel, variable);
        }

        void _rpc_InitCompleted(RPCController sender)
        {
            if (Devices.Count == 0) Reload();
            else
            {
                bool devicesDeleted = false;
                bool newDevices = false;
                List<Variable> updatedVariables = Devices.UpdateVariables(_rpc.GetAllValues(), out devicesDeleted, out newDevices);
                foreach(Variable variable in updatedVariables)
                {
                    if(!Devices.ContainsKey(variable.PeerID)) continue;
                    Device device = Devices[variable.PeerID];
                    if(!device.Channels.ContainsKey(variable.Channel)) continue;
                    if (DeviceVariableUpdated != null) DeviceVariableUpdated(this, device, device.Channels[variable.Channel], variable);
                }
                if ((devicesDeleted || newDevices) && ReloadRequired != null) ReloadRequired(this);
            }
        }

        ~Homegear()
        {
            _stopConnectThread = true;
            if (_connectThread.IsAlive)
            {
                if(!_connectThread.Join(20000))
                {
                    try
                    {
                        _connectThread.Abort();
                    }
                    catch (Exception) { }
                }
            }
        }

        public void Dispose()
        {
            if (_disposing) return;
            _disposing = true;
            _stopConnectThread = true;
            if (_connectThread.IsAlive) _connectThread.Join();
            _rpc.Disconnect();
        }

        public void Reload()
        {
            if (_disposing) return;
            _rpc.Clear();
            if(_families != null) _families.Dispose();
            _families = new Families(_rpc, _rpc.Families);
            if(_devices != null) _devices.Dispose();
            _devices = new Devices(_rpc, _rpc.Devices);
            if(_interfaces != null) _interfaces.Dispose();
            _interfaces = null;
            if (Reloaded != null) Reloaded(this);
        }

        void Connect()
        {
            if (_disposing) return;
            while (!_stopConnectThread)
            {
                try
                {
                    if (!_rpc.IsConnected) _rpc.Connect();
                    break;
                }
                catch (Exception ex)
                {
                    if (ConnectError != null) ConnectError(this, ex.Message, ex.StackTrace);
                    Thread.Sleep(10000);
                }
            }
        }

        void _rpc_Connected(RPCController sender)
        {
            if (_disposing) return;
            if(Connected != null) Connected(this);
        }

        void _rpc_Disconnected(RPCController sender)
        {
            if (_disposing) return;
            if(Disconnected != null) Disconnected(this);
            _stopConnectThread = true;
            if (_connectThread.IsAlive) _connectThread.Join();
            _stopConnectThread = false;
            _connectThread = new Thread(Connect);
            _connectThread.Start();
        }

        public void EnablePairingMode(bool value)
        {
            _rpc.SetInstallMode(value);
        }

        public void EnablePairingMode(bool value, Int32 duration)
        {
            _rpc.SetInstallMode(value, duration);
        }

        public Int32 TimeLeftInPairingMode()
        {
            return _rpc.GetInstallMode();
        }

        public Int32 SearchDevices()
        {
            return _rpc.SearchDevices();
        }
    }
}
