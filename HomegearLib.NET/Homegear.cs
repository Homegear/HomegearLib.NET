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
        public delegate void DataReloadedEventHandler(Homegear sender);
        public delegate void DeviceVariableUpdatedEventHandler(Homegear sender, Device device, Channel channel, Variable variable);

        #region "Events"
        public event ConnectErrorEventHandler OnConnectError;
        public event DataReloadedEventHandler OnReloaded;
        public event DeviceVariableUpdatedEventHandler OnDeviceVariableUpdated;
        #endregion

        RPCController _rpc = null;
        volatile bool _disposing = false;
        volatile bool _stopConnectThread = false;
        Thread _connectThread = null;
        
        Families _families = null;
        public Families Families { get { return _families; } }

        Devices _devices = null;
        public Devices Devices { get { return _devices; } }

        public Homegear(RPCController rpc)
        {
            if (rpc == null) throw new NullReferenceException("RPC object is null.");
            _rpc = rpc;
            _families = new Families(_rpc, new Dictionary<Int32, Family>());
            _devices = new Devices(_rpc, new Dictionary<Int32, Device>());
            _rpc.Disconnected += _rpc_Disconnected;
            _rpc.InitCompleted += _rpc_InitCompleted;
            _rpc.OnRPCEvent += _rpc_OnRPCEvent;
            _stopConnectThread = false;
            _connectThread = new Thread(Connect);
            _connectThread.Start();
            while (!_connectThread.IsAlive) ;
        }

        void _rpc_OnRPCEvent(RPCController sender, Variable value)
        {
            if(value.PeerID == 0) return; //System variable
            if (!Devices.ContainsKey(value.PeerID)) return;
            Device device = Devices[value.PeerID];
            if (!device.Channels.ContainsKey(value.Channel)) return;
            Channel deviceChannel = device.Channels[value.Channel];
            if (!deviceChannel.Variables.ContainsKey(value.Name)) return;
            Variable variable = deviceChannel.Variables[value.Name];
            variable.SetValue(value);
            if (OnDeviceVariableUpdated != null) OnDeviceVariableUpdated(this, device, deviceChannel, variable);
        }

        void _rpc_InitCompleted(RPCController sender)
        {
            _families.Dispose();
            _families = new Families(_rpc, _rpc.Families);
            _devices.Dispose();
            _devices = new Devices(_rpc, _rpc.Devices);
            if(OnReloaded != null) OnReloaded(this);
        }

        ~Homegear()
        {
            _stopConnectThread = true;
            if (_connectThread.IsAlive) _connectThread.Join();
        }

        public void Dispose()
        {
            _disposing = true;
            _stopConnectThread = true;
            if (_connectThread.IsAlive) _connectThread.Join();
            _rpc.Disconnect();
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
                    if (OnConnectError != null) OnConnectError(this, ex.Message, ex.StackTrace);
                    Thread.Sleep(10000);
                }
            }
        }

        void _rpc_Disconnected(RPCController sender)
        {
            if (_disposing) return;
            _stopConnectThread = true;
            if (_connectThread.IsAlive) _connectThread.Join();
            _stopConnectThread = false;
            _connectThread = new Thread(Connect);
            _connectThread.Start();
            while (!_connectThread.IsAlive) ;
        }
    }
}
