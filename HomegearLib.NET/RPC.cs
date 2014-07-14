using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomegearLib
{
    public class RPC : IDisposable
    {
        public delegate void RPCEventEventHandler(RPC sender, uint peerID, int channel, string parameterName, RPCVariable value);
        public delegate void ConnectedEventHandler(RPC sender);
        public delegate void DisconnectedEventHandler(RPC sender);

        #region "Events"
        public event RPCEventEventHandler OnRPCEvent;
        public event ConnectedEventHandler Connected;
        public event DisconnectedEventHandler Disconnected;
        #endregion

        public bool IsConnected { get { return _client != null && _client.IsConnected; } }

        RPCClient _client;
        RPCServer _server;
        System.Timers.Timer _keepAliveTimer;

        /// <summary>
        /// Creates a new RPC object
        /// </summary>
        /// <param name="homegearHostname">The hostname or IP address of the Homegear server to connect to.</param>
        /// <param name="homegearPort">The port Homegear is listening on.</param>
        /// <param name="callbackHostname">The hostname of the the computer running this library. Needed by Homegear for certificate verification.</param>
        /// <param name="callbackListenIP">The IP address to bind the callback server to. Not "0.0.0.0", "::", "127.0.0.1" or "::1" Homegear sends events to the callback server.</param>
        /// <param name="callbackListenPort">The port of the callback server.</param>
        /// <param name="ssl">Set to true to enable SSL encryption.</param>
        /// <param name="verifyCertificate">Set to false to disable certificate verification.</param>
        /// <param name="homegearUsername">The username to send to Homegear for authentication.</param>
        /// <param name="homegearPassword">The password to send to Homegear for authentication.</param>
        /// <param name="callbackUsername">The username Homegear needs to send to our callback server for authentication. This needs to be configured in Homegear's rpcClients.conf.</param>
        /// <param name="callbackPassword">The password Homegear needs to send to our callback server for authentication.</param>
        public RPC(String homegearHostname, int homegearPort, String callbackHostname, String callbackListenIP, int callbackListenPort, String callbackServerCertificatePath = "", bool ssl = false, bool verifyCertificate = true, string homegearUsername = "", string homegearPassword = "", string callbackUsername = "", string callbackPassword = "")
        {
            _client = new RPCClient(homegearHostname, homegearPort, ssl, verifyCertificate, homegearUsername, homegearPassword);
            _client.Connected += _client_Connected;
            _client.Disconnected += _client_Disconnected;
            _server = new RPCServer(callbackHostname, callbackListenIP, callbackListenPort, callbackServerCertificatePath, ssl);
            _server.OnRPCEvent += _server_OnRPCEvent;
            _keepAliveTimer = new System.Timers.Timer(10000);
            _keepAliveTimer.Elapsed += _workerTimer_Elapsed;
        }

        void _workerTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            try
            {
                if (!clientServerInitialized("HomegearLib"))
                {
                    _client.Disconnect();
                }
            }
            catch(Exception)
            {
                _client.Disconnect();
            }
        }

        void _server_OnRPCEvent(RPCServer sender, uint peerID, int channel, string parameterName, RPCVariable value)
        {
            if (OnRPCEvent != null) OnRPCEvent(this, peerID, channel, parameterName, value);
        }

        public void Dispose()
        {
            _client.Disconnect();
            _server.Stop();
        }

        void _client_Disconnected(RPCClient sender)
        {
            if (Disconnected != null) Disconnected(this);
        }

        void _client_Connected(RPCClient sender)
        {
            if (Connected != null) Connected(this);
            init("HomegearLib");
        }

        public void Connect()
        {
            _server.Start();
            _client.Connect();
            if (Connected != null) Connected(this);
            _keepAliveTimer.Start();
        }

        public void Disconnect()
        {
            _keepAliveTimer.Stop();
            init("");
            _client.Disconnect();
            _server.Stop();
            if (Disconnected != null) Disconnected(this);
        }

        public bool clientServerInitialized(string interfaceID)
        {
            RPCVariable result = _client.CallMethod("clientServerInitialized", new List<RPCVariable> { new RPCVariable(interfaceID) });
            return result.BooleanValue;
        }

        public void init(string interfaceID)
        {
            string prefix;
            if (_server.SSL) prefix = "binarys://";
            else prefix = "binary://";
            _client.CallMethod("init", new List<RPCVariable> { new RPCVariable(prefix + _server.Hostname + ":" + _server.ListenPort.ToString()), new RPCVariable(interfaceID), new RPCVariable(7) });
        }
    }
}
