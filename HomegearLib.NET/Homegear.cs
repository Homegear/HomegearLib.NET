using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using HomegearLib.RPC;

namespace HomegearLib
{
    public enum ReloadType
    {
        Full = 1,
        SystemVariables = 2
    }

    public enum DeviceReloadType
    {
        Full = 1,
        Metadata = 2,
        Channel = 4,
        Links = 8
    }

    public enum UpdateResultCode
    {
        OK = 0,
        UnknownError = 1,
        NoVersionFileFound = 2,
        NoFirmwareFound = 3,
        CouldNotOpenFirmwareFile = 4,
        WrongFileFormat = 5,
        NoResponse = 6,
        NoUpdateRequest = 7,
        TooManyCommunicationErrors = 8,
        PhysicalInterfaceError = 9
    }

    public class UpdateResult
    {
        UpdateResultCode _code = -1;
        public UpdateResultCode Code { get { return _code; } }

        String _description = "";
        public String Description { get { return _description; } }

        public UpdateResult(UpdateResultCode code, String description)
        {
            _code = code;
            _description = description;
        }
    }

    public class UpdateStatus
    {
        Int32 _currentDevice = -1;
        public Int32 CurrentDevice { get { return _currentDevice; } }

        Int32 _currentDeviceProgress = -1;
        public Int32 CurrentDeviceProgress { get { return _currentDeviceProgress; } }

        Int32 _deviceCount = -1;
        public Int32 DeviceCount { get { return _deviceCount; } }

        Int32 _currentUpdate = 0;
        public Int32 CurrentUpdate { get { return _currentUpdate; } }

        ReadOnlyDictionary<Int32, UpdateResult> _results = new ReadOnlyDictionary<Int32,UpdateResult>();
        public ReadOnlyDictionary<Int32, UpdateResult> Results { get { return _results; } }

        public UpdateStatus(Int32 currentDevice, Int32 currentDeviceProgress, Int32 deviceCount, Int32 currentUpdate, Dictionary<Int32, UpdateResult> results)
        {
            _currentDevice = currentDevice;
            _currentDeviceProgress = currentDeviceProgress;
            _deviceCount = deviceCount;
            _currentUpdate = currentUpdate;
            _results = new ReadOnlyDictionary<Int32, UpdateResult>(results);
        }
    }

    public class Homegear : IDisposable
    {
        public delegate void ConnectErrorEventHandler(Homegear sender, String message, String stackTrace);
        public delegate void ConnectedEventHandler(Homegear sender);
        public delegate void DisconnectedEventHandler(Homegear sender);
        public delegate void DataReloadEventHandler(Homegear sender);
        public delegate void SystemVariableUpdatedEventHandler(Homegear sender, SystemVariable variable);
        public delegate void MetadataUpdatedEventHandler(Homegear sender, Device device, MetadataVariable variable);
        public delegate void DeviceVariableUpdatedEventHandler(Homegear sender, Device device, Channel channel, Variable variable);
        public delegate void DeviceConfigParameterUpdatedEventHandler(Homegear sender, Device device, Channel channel, ConfigParameter parameter);
        public delegate void DeviceLinkConfigParameterUpdatedEventHandler(Homegear sender, Device device, Channel channel, Link link, ConfigParameter parameter);
        public delegate void ReloadRequiredEventHandler(Homegear sender, ReloadType reloadType);
        public delegate void DeviceReloadRequiredEventHandler(Homegear sender, Device device, Channel channel, DeviceReloadType reloadType);

        #region "Events"
        public event ConnectErrorEventHandler ConnectError;
        public event DataReloadEventHandler Reloaded;
        public event SystemVariableUpdatedEventHandler SystemVariableUpdated;
        public event MetadataUpdatedEventHandler MetadataUpdated;
        public event DeviceVariableUpdatedEventHandler DeviceVariableUpdated;
        public event DeviceConfigParameterUpdatedEventHandler DeviceConfigParameterUpdated;
        public event DeviceLinkConfigParameterUpdatedEventHandler DeviceLinkConfigParameterUpdated;
        public event ReloadRequiredEventHandler ReloadRequired;
        public event DeviceReloadRequiredEventHandler DeviceReloadRequired;
        public event ConnectedEventHandler Connected;
        public event DisconnectedEventHandler Disconnected;
        #endregion

        volatile bool _connecting = false;
        RPCController _rpc = null;
        volatile bool _disposing = false;
        volatile bool _stopConnectThread = false;
        Thread _connectThread = null;
        
        Families _families = null;
        public Families Families { get { return _families; } }

        Devices _devices = null;
        public Devices Devices { get { return _devices; } }

        SystemVariables _systemVariables = null;
        public SystemVariables SystemVariables { get { return _systemVariables; } }

        Interfaces _interfaces = null;
        public Interfaces Interfaces
        {
            get
            {
                if (_interfaces == null || _interfaces.Count == 0) _interfaces = new Interfaces(_rpc, _rpc.Interfaces);
                bool interfacesAdded = false;
                bool interfacesRemoved = false;
                _interfaces.Update(out interfacesRemoved, out interfacesAdded);
                if ((interfacesAdded || interfacesRemoved) && ReloadRequired != null) ReloadRequired(this, ReloadType.Full);
                return _interfaces;
            }
        }

        String _version = "";
        public String Version
        {
            get
            {
                if (_version.Length == 0) _version = _rpc.GetVersion();
                return _version;
            }
        }

        public Int32 LogLevel { get { return _rpc.LogLevel(); } set { _rpc.LogLevel(value); } }

        public List<ServiceMessage> ServiceMessages { get { return _rpc.GetServiceMessages(); } }

        public Homegear(RPCController rpc)
        {
            if (rpc == null) throw new NullReferenceException("RPC object is null.");
            _rpc = rpc;
            _families = new Families(_rpc, new Dictionary<Int32, Family>());
            _devices = new Devices(_rpc, new Dictionary<Int32, Device>());
            _systemVariables = new SystemVariables(_rpc, new Dictionary<String, SystemVariable>());
            _rpc.Connected += _rpc_Connected;
            _rpc.Disconnected += _rpc_Disconnected;
            _rpc.InitCompleted += _rpc_InitCompleted;
            _rpc.DeviceVariableUpdated += _rpc_OnDeviceVariableUpdated;
            _rpc.SystemVariableUpdated += _rpc_OnSystemVariableUpdated;
            _rpc.SystemVariableDeleted += _rpc_OnSystemVariableDeleted;
            _rpc.MetadataUpdated += _rpc_OnMetadataUpdated;
            _rpc.MetadataDeleted += _rpc_OnMetadataDeleted;
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
            if (ReloadRequired != null) ReloadRequired(this, ReloadType.Full);
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
                if (DeviceReloadRequired != null) DeviceReloadRequired(this, device, channel, DeviceReloadType.Links);
            }
        }

        void _rpc_OnDevicesDeleted(RPCController sender)
        {
            if (ReloadRequired != null) ReloadRequired(this, ReloadType.Full);
        }

        void _rpc_OnDeviceVariableUpdated(RPCController sender, Variable value)
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

        void _rpc_OnSystemVariableUpdated(RPCController sender, SystemVariable value)
        {
            if (_disposing) return;
            if (!SystemVariables.ContainsKey(value.Name))
            {
                System.Diagnostics.Debug.Write("Position 1");
                if (ReloadRequired != null) ReloadRequired(this, ReloadType.SystemVariables);
                return;
            }
            SystemVariable variable = SystemVariables[value.Name];
            variable.SetValue(value);
            if (SystemVariableUpdated != null) SystemVariableUpdated(this, variable);
        }

        void _rpc_OnSystemVariableDeleted(RPCController sender)
        {
            if (_disposing) return;
            System.Diagnostics.Debug.Write("Position 2");
            if (ReloadRequired != null) ReloadRequired(this, ReloadType.SystemVariables);
        }

        void _rpc_OnMetadataUpdated(RPCController sender, Int32 peerID, MetadataVariable value)
        {
            if (_disposing) return;
            if(!Devices.ContainsKey(peerID))
            {
                if (ReloadRequired != null) ReloadRequired(this, ReloadType.Full);
                return;
            }
            Device device = Devices[peerID];
            if (!device.Metadata.ContainsKey(value.Name))
            {
                if (DeviceReloadRequired != null) DeviceReloadRequired(this, device, null, DeviceReloadType.Metadata);
                return;
            }
            MetadataVariable variable = device.Metadata[value.Name];
            variable.SetValue(value);
            if (MetadataUpdated != null) MetadataUpdated(this, device, variable);
        }

        void _rpc_OnMetadataDeleted(RPCController sender, Int32 peerID)
        {
            if (_disposing) return;
            if (!Devices.ContainsKey(peerID)) return;
            Device device = Devices[peerID];
            if (DeviceReloadRequired != null) DeviceReloadRequired(this, device, null, DeviceReloadType.Metadata);
        }

        void _rpc_InitCompleted(RPCController sender)
        {
            if (_disposing) return;
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
                bool systemVariablesAdded = false;
                bool systemVariablesDeleted = false;
                List<SystemVariable> updatedSystemVariables = SystemVariables.Update(out systemVariablesDeleted, out systemVariablesAdded);
                foreach(SystemVariable variable in updatedSystemVariables)
                {
                    if (SystemVariableUpdated != null) SystemVariableUpdated(this, variable);
                }
                if ((devicesDeleted || newDevices) && ReloadRequired != null) ReloadRequired(this, ReloadType.Full);
                else
                {
                    if ((systemVariablesAdded || systemVariablesDeleted) && ReloadRequired != null)
                    {
                        System.Diagnostics.Debug.Write("Position 3");
                        ReloadRequired(this, ReloadType.SystemVariables);
                    }
                    foreach (KeyValuePair<Int32, Device> devicePair in Devices)
                    {
                        if (devicePair.Value.MetadataRequested)
                        {
                            bool variablesAdded = false;
                            bool variablesDeleted = false;
                            List<MetadataVariable> updatedMetadata = devicePair.Value.Metadata.Update(out variablesDeleted, out variablesAdded);
                            foreach (MetadataVariable variable in updatedMetadata)
                            {
                                if (MetadataUpdated != null) MetadataUpdated(this, devicePair.Value, variable);
                            }
                            if ((variablesAdded || variablesDeleted) && DeviceReloadRequired != null) DeviceReloadRequired(this, devicePair.Value, null, DeviceReloadType.Metadata);
                        }
                    }
                }
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
            _rpc.Connected -= _rpc_Connected;
            _rpc.Disconnected -= _rpc_Disconnected;
            _rpc.InitCompleted -= _rpc_InitCompleted;
            _rpc.DeviceVariableUpdated -= _rpc_OnDeviceVariableUpdated;
            _rpc.SystemVariableUpdated -= _rpc_OnSystemVariableUpdated;
            _rpc.SystemVariableDeleted -= _rpc_OnSystemVariableDeleted;
            _rpc.MetadataUpdated -= _rpc_OnMetadataUpdated;
            _rpc.MetadataDeleted -= _rpc_OnMetadataDeleted;
            _rpc.NewDevices -= _rpc_OnNewDevices;
            _rpc.DevicesDeleted -= _rpc_OnDevicesDeleted;
            _rpc.UpdateDevice -= _rpc_OnUpdateDevice;
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
            if (_systemVariables != null) _systemVariables.Dispose();
            _systemVariables = new SystemVariables(_rpc, _rpc.SystemVariables);
            if (Reloaded != null) Reloaded(this);
        }

        void Connect()
        {
            if (_disposing || _connecting) return;
            _connecting = true;
            try
            {
                while (!_stopConnectThread && !_disposing)
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
            catch (Exception ex)
            {
                _connecting = false;
                throw ex;
            }
            _connecting = false;
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
            if (_connectThread.IsAlive)
            {
                if (!_connectThread.Join(20000))
                {
                    try
                    {
                        _connectThread.Abort();
                    }
                    catch (Exception) { }
                }
            }
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

        public UpdateStatus GetUpdateStatus()
        {
            return _rpc.GetUpdateStatus();
        }

        public Int32 SearchDevices()
        {
            return _rpc.SearchDevices();
        }
    }
}
