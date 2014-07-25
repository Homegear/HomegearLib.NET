using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.IO;
using System.Threading;
using System.Security;
using System.Runtime.InteropServices;

namespace HomegearLib.RPC
{
    internal class HomegearRPCServerException : HomegearException
    {
        public HomegearRPCServerException() : base()
        {

        }

        public HomegearRPCServerException(String message) : base(message)
        {

        }
    }

    internal class HomegearRPCServerSSLException : HomegearRPCServerException
    {
        public HomegearRPCServerSSLException() : base()
        {

        }

        public HomegearRPCServerSSLException(String message) : base(message)
        {

        }
    }

    internal class RPCServer
    {
        public delegate void RPCEventEventHandler(RPCServer sender, Int32 peerID, Int32 channel, String parameterName, RPCVariable value);
        public delegate void NewDevicesEventHandler(RPCServer sender);
        public delegate void DevicesDeletedEventHandler(RPCServer sender);
        public delegate void UpdateDeviceEventHandler(RPCServer sender, Int32 peerID, Int32 channel, Int32 flags);

        #region "Events"
        public event RPCEventEventHandler RPCEvent;
        public event NewDevicesEventHandler NewDevices;
        public event DevicesDeletedEventHandler DevicesDeleted;
        public event UpdateDeviceEventHandler UpdateDevice;
        #endregion

        volatile bool _starting = false;
        private Thread _listenThread = null;
        private volatile bool _stopServer = false;
        private volatile ManualResetEvent _clientConnected = new ManualResetEvent(false);
        private volatile TcpListener _listener = null;
        private volatile TcpClient _client = null;
        private volatile SslStream _sslStream = null;
        private X509Certificate _serverCertificate = null;

        private IPAddress _ipAddress = null;
        public IPAddress ListenIP { get { return _ipAddress; } }

        private int _port = 9876;
        public int ListenPort { get { return _port; } }

        private bool _ssl = false;
        public bool SSL { get { return _ssl; } }

        Dictionary<Int32, Device> _knownDevices = null;
        public Dictionary<Int32, Device> KnownDevices { get { return _knownDevices; } set { _knownDevices = value; } }

        private SecureString _authString = null;
        private Encoding.RPCEncoder _rpcEncoder = new Encoding.RPCEncoder();
        private Encoding.RPCDecoder _rpcDecoder = new Encoding.RPCDecoder();

        public RPCServer(String listenIP, Int32 port, SSLServerInfo sslInfo)
        {
            _ssl = sslInfo != null;
            if (_ssl)
            {
                if (File.Exists(sslInfo.CertificatePath)) _serverCertificate = new X509Certificate2(sslInfo.CertificatePath, sslInfo.CertificatePassword);
                else throw new HomegearRPCServerSSLException("Certificate file does not exist.");
            }
            _ipAddress = IPAddress.Parse(listenIP);
            _port = port;
            if (_ssl && sslInfo.Username.Length > 0) _authString = GetSecureString("Basic " + Convert.ToBase64String(System.Text.ASCIIEncoding.UTF8.GetBytes(Marshal.PtrToStringAuto(Marshal.SecureStringToBSTR(sslInfo.Username)) + ":" + Marshal.PtrToStringAuto(Marshal.SecureStringToBSTR(sslInfo.Password)))));
        }

        ~RPCServer()
        {
        }

        unsafe SecureString GetSecureString(string value)
        {
            char[] chars = value.ToCharArray();

            SecureString secureString;
            fixed (char* pChars = chars)
            {
                secureString = new SecureString(pChars, chars.Length);
            }
            return secureString;
        }

        public void Start()
        {
            if (_starting) return;
            try
            {
                _starting = true;
                Stop();
                _stopServer = false;
                _listener = new TcpListener(_ipAddress, _port);
                _listener.Start(100);
                _listenThread = new Thread(Listen);
                _listenThread.Start();
                //Wait until the thread activates
                while (!_listenThread.IsAlive) ;
            }
            catch(Exception ex)
            {
                _starting = false;
                throw ex;
            }
            _starting = false;
        }

        public void Stop()
        {
            _stopServer = true;
            if (_listenThread != null && _listenThread.IsAlive)
            {
                if (!_listenThread.Join(10000))
                {
                    try
                    {
                        _listenThread.Abort();
                    }
                    catch (Exception) { }
                }
            }
            _listenThread = null;
            if (_client != null) _client.Close();
            _client = null;
            if(_listener != null) _listener.Stop();
            _listener = null;
        }

        void Listen()
        {
            while(!_stopServer)
            {
                _clientConnected.Reset();
                _listener.BeginAcceptTcpClient(new AsyncCallback(AcceptTCPClientCallback), _listener);
                while(!_stopServer)
                {
                    if (_clientConnected.WaitOne(1000)) break;
                }
                if(_stopServer) return;
                if(_client == null) continue;
                _sslStream = null;
                if(_ssl)
                {
                    _sslStream = new SslStream(_client.GetStream(), true);
                    try
                    {
                        _sslStream.AuthenticateAsServer(_serverCertificate, false, SslProtocols.Tls, true);
                    }
                    catch (AuthenticationException ex) { System.Diagnostics.Debug.WriteLine(ex.Message); continue; }
                    catch (IOException ex) { System.Diagnostics.Debug.WriteLine(ex.Message); continue; }
                    _sslStream.ReadTimeout = 5000;
                    _sslStream.WriteTimeout = 5000;
                }
                else
                {
                    _client.Client.ReceiveTimeout = 5000;
                    _client.Client.SendTimeout = 5000;
                }
                ReadClient();

                if (_ssl) _sslStream.Close();
                _client.Close();
                _client = null;
            }
        }

        private void ReadClient()
        {
            byte[] buffer = new byte[2048];
            int bytesReceived = 0;
            uint dataSize = 0;
            uint headerSize = 0;
            uint packetLength = 0;
            byte[] packet = null;
            while (!_stopServer)
            {
                try
                {
                    if (_client == null || !_client.Connected) break;
                    if (_ssl) bytesReceived = _sslStream.Read(buffer, 0, buffer.Length);
                    else bytesReceived =_client.Client.Receive(buffer);
                    if (bytesReceived == 0) continue;
                    UInt32 bufferPos = 0;
                    while (bufferPos == 0 || bufferPos + dataSize + 8 < bytesReceived)
                    {
                        if (buffer[bufferPos] == 'B' && buffer[bufferPos + 1] == 'i' && buffer[bufferPos + 2] == 'n')
                        {
                            if ((buffer[bufferPos + 3] & 1) == 1) continue; //Response received
                            if (bytesReceived < 8) continue;
                            if ((buffer[bufferPos + 3] & 0x40) == 0x40)
                            {
                                headerSize = (uint)((buffer[bufferPos + 4] << 24) + (buffer[bufferPos + 5] << 16) + (buffer[bufferPos + 6] << 8) + buffer[bufferPos + 7]);
                                if (bytesReceived < bufferPos + headerSize + 12) continue;
                                dataSize = (uint)((buffer[bufferPos + 8 + headerSize] << 24) + (buffer[bufferPos + 9 + headerSize] << 16) + (buffer[bufferPos + 10 + headerSize] << 8) + buffer[bufferPos + 11 + headerSize]) + headerSize + 4;
                            }
                            else
                            {
                                headerSize = 0;
                                dataSize = (uint)((buffer[bufferPos + 4] << 24) + (buffer[bufferPos + 5] << 16) + (buffer[bufferPos + 6] << 8) + buffer[bufferPos + 7]);
                            }
                            if (dataSize == 0) continue;
                            if (dataSize > 104857600) continue;
                            if (headerSize > 1024) continue;
                            packetLength = (uint)bytesReceived - 8;
                            packet = new byte[dataSize + 8];
                            Array.Copy(buffer, bufferPos, packet, 0, ((dataSize + 8) > bytesReceived) ? bytesReceived : (Int32)dataSize + 8);
                            if (_authString != null && _authString.Length > 0)
                            {
                                Encoding.RPCHeader header = _rpcDecoder.DecodeHeader(packet);
                                if (header.Authorization != Marshal.PtrToStringAuto(Marshal.SecureStringToBSTR(_authString)))
                                {
                                    packet = null;
                                    List<byte> responsePacket = _rpcEncoder.EncodeResponse(RPCVariable.CreateError(-32603, "Unauthorized"));
                                    if (_ssl)
                                    {
                                        _sslStream.Write(responsePacket.ToArray());
                                        _sslStream.Flush();
                                    }
                                    else _client.Client.Send(responsePacket.ToArray());
                                    continue;
                                }
                            }
                        }
                        else if (packet != null)
                        {
                            if (packetLength + bytesReceived > dataSize) throw new HomegearRPCClientException("RPC client received response larger than the expected size from Homegear.");
                            Array.Copy(buffer, 0, packet, packetLength + 8, bytesReceived);
                            packetLength += (uint)bytesReceived;
                        }
                        if (packetLength == dataSize)
                        {
                            ProcessPacket(packet);
                            packet = null;
                        }
                        bufferPos = dataSize + 8;
                    }
                }
                catch (SocketException ex)
                {
                    if (ex.SocketErrorCode == SocketError.TimedOut) continue;
                    else
                    {
                        System.Diagnostics.Debug.WriteLine(ex.Message);
                        break;
                    }
                }
                catch (IOException ex)
                { 
                    System.Diagnostics.Debug.WriteLine(ex.Message);
                    break;
                }
            }
        }

        private void ProcessPacket(byte[] packet)
        {
            string methodName = "";
            List<RPCVariable> parameters = _rpcDecoder.DecodeRequest(packet, ref methodName);
            RPCVariable response = new RPCVariable(RPCVariableType.rpcVoid);
            if (methodName == "") response = RPCVariable.CreateError(-1, "Packet is not well formed.");
            else if(methodName == "system.listMethods")
            {
                response = new RPCVariable(RPCVariableType.rpcArray);
                response.ArrayValue.Add(new RPCVariable("system.multicall"));
                response.ArrayValue.Add(new RPCVariable("event"));
                response.ArrayValue.Add(new RPCVariable("listDevices"));
                response.ArrayValue.Add(new RPCVariable("newDevices"));
                response.ArrayValue.Add(new RPCVariable("updateDevice"));
                response.ArrayValue.Add(new RPCVariable("deleteDevices"));
            }
            else if(methodName == "system.multicall" && parameters.Count() > 0)
            {
                foreach(RPCVariable method in parameters[0].ArrayValue)
                {
                    if (method.Type != RPCVariableType.rpcStruct || method.StructValue.Count() != 2) continue;
                    if (method.StructValue["methodName"].StringValue != "event") continue;
                    List<RPCVariable> eventParams = method.StructValue["params"].ArrayValue;
                    if (eventParams.Count() != 5 || eventParams[0].Type != RPCVariableType.rpcString || eventParams[1].Type != RPCVariableType.rpcInteger || eventParams[2].Type != RPCVariableType.rpcInteger || eventParams[3].Type != RPCVariableType.rpcString) continue;
                    if(RPCEvent != null) RPCEvent(this, eventParams[1].IntegerValue, eventParams[2].IntegerValue, eventParams[3].StringValue, eventParams[4]);
                }
            }
            else if(methodName == "listDevices")
            {
                response = new RPCVariable(RPCVariableType.rpcArray);
                if (_knownDevices != null)
                {
                    foreach (Int32 peerID in _knownDevices.Keys)
                    {
                        RPCVariable device = new RPCVariable(RPCVariableType.rpcStruct);
                        device.StructValue.Add("ID", new RPCVariable(peerID));
                        response.ArrayValue.Add(device);
                    }
                }
            }
            else if(methodName == "newDevices")
            {
                if (NewDevices != null) NewDevices(this);
            }
            else if(methodName == "deleteDevices")
            {
                if (DevicesDeleted != null) DevicesDeleted(this);
            }
            else if(methodName == "updateDevice")
            {
                if (parameters.Count == 4 && parameters[0].Type == RPCVariableType.rpcString && parameters[1].Type == RPCVariableType.rpcInteger && parameters[2].Type == RPCVariableType.rpcInteger && parameters[3].Type == RPCVariableType.rpcInteger)
                {
                    if (UpdateDevice != null) UpdateDevice(this, parameters[1].IntegerValue, parameters[2].IntegerValue, parameters[3].IntegerValue);
                }
            }
            byte[] responsePacket = _rpcEncoder.EncodeResponse(response).ToArray();
            Int32 bytesSent = 1024;
            Int32 bytesToSend = 0;
            for (int i = 0; i < responsePacket.Length; i += bytesSent)
            {
                bytesToSend = (i + 1024 >= responsePacket.Length) ? responsePacket.Length - i : 1024;
                if (_ssl)
                {
                    _sslStream.Write(responsePacket, i, bytesToSend);
                    _sslStream.Flush();
                }
                else bytesSent = _client.Client.Send(responsePacket, i, bytesToSend, SocketFlags.None);
            }
        }

        private void AcceptTCPClientCallback(IAsyncResult ar)
        {
            try
            {
                TcpListener listener = (TcpListener)ar.AsyncState;
                _client = listener.EndAcceptTcpClient(ar);
                _clientConnected.Set();
            }
            catch(ObjectDisposedException)
            {

            }
        }
    }
}
