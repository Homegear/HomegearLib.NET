using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace HomegearLib
{
    public class Homegear : IDisposable
    {
        public delegate void ConnectErrorEventHandler(Homegear sender, string message);

        #region "Events"
        public event ConnectErrorEventHandler OnConnectError;
        #endregion

        RPC _rpc = null;
        volatile bool _disposing = false;
        volatile bool _stopConnectThread = false;
        Thread _connectThread = null;

        public Homegear(RPC rpc)
        {
            if (rpc == null) throw new NullReferenceException("RPC object is null.");
            _rpc = rpc;
            _rpc.Disconnected += _rpc_Disconnected;
            _stopConnectThread = false;
            _connectThread = new Thread(Connect);
            _connectThread.Start();
            while (!_connectThread.IsAlive) ;
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
                    if (OnConnectError != null) OnConnectError(this, ex.Message);
                    Thread.Sleep(10000);
                }
            }
        }

        void _rpc_Disconnected(RPC sender)
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
