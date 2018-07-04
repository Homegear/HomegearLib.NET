using HomegearLib.RPC.Encoding;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Security;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading;

namespace HomegearLib.RPC
{
    public class RPCClient : IDisposable
    {
        public delegate void ConnectedEventHandler(RPCClient sender, CipherAlgorithmType cipherAlgorithm = CipherAlgorithmType.Null, int cipherStrength = -1);
        public delegate void DisconnectedEventHandler(RPCClient sender);
        public delegate void HomegearErrorEventHandler(RPCClient sender, long level, string message);
        public delegate void RPCEventEventHandler(RPCClient sender, long peerId, long channel, string parameterName, RPCVariable value, string eventSource);
        public delegate void NewDevicesEventHandler(RPCClient sender);
        public delegate void DevicesDeletedEventHandler(RPCClient sender);
        public delegate void UpdateDeviceEventHandler(RPCClient sender, long peerId, long channel, long flags);
        public delegate void NewEventEventHandler(RPCClient sender, string id, long eventType, long peerId, long channel, string variableName);
        public delegate void EventDeletedEventHandler(RPCClient sender, string id, long eventType, long peerId, long channel, string variableName);
        public delegate void UpdateEventEventHandler(RPCClient sender, string id, long eventType, long peerId, long channel, string variableName);

        #region "Events"
        public event ConnectedEventHandler Connected;
        public event DisconnectedEventHandler Disconnected;
        public event HomegearErrorEventHandler HomegearError;
        public event RPCEventEventHandler RPCEvent;
        public event NewDevicesEventHandler NewDevices;
        public event DevicesDeletedEventHandler DevicesDeleted;
        public event UpdateDeviceEventHandler UpdateDevice;
        public event NewEventEventHandler NewEvent;
        public event EventDeletedEventHandler EventDeleted;
        public event UpdateEventEventHandler UpdateEvent;

        #endregion

        const int _maxTries = 3;
        private readonly SslInfo _sslInfo;
        private volatile bool _connecting = false;
        private Thread _readClientThread = null;
        private volatile bool _stopThread = false;
        private volatile TcpClient _client = null;
        private volatile SslStream _sslStream = null;
        private volatile BinaryRpc _binaryRpc = new BinaryRpc();
        private object _sendLock = new object();
        private object _callMethodLock = new object();
        private volatile RPCVariable _rpcResponse = null;
        private volatile ManualResetEvent _responseReceived = new ManualResetEvent(false);

        private Queue<byte[]> _eventQueue;

        private readonly string _hostname;
        private readonly int _port;

        public bool Ssl { get; } = false;

        private SecureString _authString = null;
        private readonly Encoding.RPCEncoder _rpcEncoder = new Encoding.RPCEncoder();
        private readonly Encoding.RPCDecoder _rpcDecoder = new Encoding.RPCDecoder();

        public bool IsConnected => _client != null && _client.Connected;

        public bool IsMono => Type.GetType("Mono.Runtime") != null;

        public CipherAlgorithmType CipherAlgorithm
        {
            get
            {
                if (_sslStream != null && !IsMono) return _sslStream.CipherAlgorithm;

                return CipherAlgorithmType.Null;
            }
        }

        public int CipherStrength
        {
            get
            {
                if (_sslStream != null && !IsMono) return _sslStream.CipherStrength;

                return -1;
            }
        }

        public RPCClient(string hostname, int port, SslInfo sslInfo = null)
        {
            _hostname = hostname;
            _port = port;
            Ssl = sslInfo != null;
            _sslInfo = sslInfo;
            if (_sslInfo != null && _sslInfo.Username.Length > 0)
            {
                _authString = GetSecureString("Basic " + Convert.ToBase64String(System.Text.UTF8Encoding.UTF8.GetBytes(Marshal.PtrToStringAuto(Marshal.SecureStringToBSTR(_sslInfo.Username)) + ":" + Marshal.PtrToStringAuto(Marshal.SecureStringToBSTR(_sslInfo.Password)))));
            }
        }

        ~RPCClient()
        {
        }

        public void Dispose()
        {
            _eventQueue?.Shutdown();
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

        public bool ValidateServerCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            if (_sslInfo.CaCertificate.Any())
            {
                var certificate2 = new X509Certificate2(certificate);
                if (_sslInfo.VerifyHostname)
                {
                    if (_sslInfo.ServerCertificateCommonName.Any())
                    {
                        if (certificate2.GetNameInfo(X509NameType.DnsName, false) != _sslInfo.ServerCertificateCommonName) return false;
                    }
                    else if (certificate2.GetNameInfo(X509NameType.DnsName, false) != _hostname) return false;
                }
                var validationChain = new X509Chain();
                validationChain.ChainPolicy.ExtraStore.Add(new X509Certificate2(System.Text.Encoding.UTF8.GetBytes(_sslInfo.CaCertificate)));
                validationChain.ChainPolicy.RevocationMode = _sslInfo.CheckCertificateRevocationStatus ? X509RevocationMode.Offline : X509RevocationMode.NoCheck;
                validationChain.ChainPolicy.VerificationFlags = X509VerificationFlags.NoFlag | X509VerificationFlags.AllowUnknownCertificateAuthority;
                if (validationChain.Build(certificate2)) return true;
            }
            else if (sslPolicyErrors == SslPolicyErrors.None)
            {
                return true;
            }
            else if (!_sslInfo.VerifyHostname && ((sslPolicyErrors & SslPolicyErrors.RemoteCertificateChainErrors) == SslPolicyErrors.RemoteCertificateChainErrors || (sslPolicyErrors & SslPolicyErrors.RemoteCertificateNameMismatch) == SslPolicyErrors.RemoteCertificateNameMismatch))
            {
                return true;
            }
            return false;
        }

        public void Connect()
        {
            if (_connecting)
            {
                return;
            }

            try
            {
                _connecting = true;
                _client?.Close();

                try
                {
                    _client = new TcpClient(_hostname, _port) { ReceiveTimeout = 10000 };
                }
                catch (SocketException ex)
                {
                    _connecting = false;
                    throw new HomegearRpcClientException("Could not connect to server " + _hostname + " on port " + _port.ToString() + ": " + ex.Message);
                }

                if (Ssl)
                {
                    _sslStream = new SslStream(_client.GetStream(), false, new RemoteCertificateValidationCallback(ValidateServerCertificate), null);
                    try
                    {
                        X509CertificateCollection certificates = new X509CertificateCollection();

                        if (_sslInfo.ClientCertificateFile.Any())
                        {
                            if (!File.Exists(_sslInfo.ClientCertificateFile)) throw new HomegearRpcClientSSLException("The specified certificate file does not exist.");

                            if (IsMono)
                            {
                                // [MonoTODO ("SecureString is incomplete")]
                                // https://github.com/mono/mono/blob/master/mcs/class/System/System.Security.Cryptography.X509Certificates/X509Certificate2.cs#L227
                                var password = new System.Net.NetworkCredential(string.Empty, _sslInfo.CertificatePassword).Password;
                                var certificate = new X509Certificate2(_sslInfo.ClientCertificateFile, password);
                                certificates.Add(certificate);
                            }
                            else
                            {
                                var certificate = new X509Certificate2(_sslInfo.ClientCertificateFile, _sslInfo.CertificatePassword);
                                certificates.Add(certificate);
                            }
                        }

                        _sslStream.AuthenticateAsClient(_hostname, certificates, SslProtocols.Tls12, _sslInfo.CheckCertificateRevocationStatus);
                    }
                    catch (AuthenticationException ex)
                    {
                        _client.Close();
                        _connecting = false;
                        throw new HomegearRpcClientSSLException("Server authentication failed: " + ex.Message);
                    }
                    catch (System.IO.IOException ex)
                    {
                        _client.Close();
                        _connecting = false;
                        throw new HomegearRpcClientSSLException("Server authentication failed: " + ex.Message);
                    }
                }

                _stopThread = false;
                _readClientThread = new Thread(ReadClient);
                _readClientThread.Start();

                _eventQueue?.Shutdown();
                _eventQueue = new Queue<byte[]>(10, consumeAction: packet => { ProcessPacket(packet); });

                if (Ssl)
                {
                    var cipherStrength = IsMono ? -1 : _sslStream.CipherStrength;
                    Connected?.Invoke(this, _sslStream.CipherAlgorithm, cipherStrength);
                }
                else
                {
                    Connected?.Invoke(this);
                }

                _connecting = false;
            }
            catch (Exception ex)
            {
                _connecting = false;
                throw ex;
            }
            _connecting = false;
        }

        public void Disconnect()
        {
            _eventQueue?.Shutdown();
            _eventQueue = null;
            _stopThread = true;
            _client?.Close(); //Don't lock _client here
            if (_readClientThread != null && _readClientThread.IsAlive)
            {
                if (!_readClientThread.Join(100))
                {
                    try
                    {
                        _readClientThread.Abort();
                    }
                    catch (Exception) { }
                }
            }
            _readClientThread = null;
            _client = null;
            _sslStream = null;

            Disconnected?.Invoke(this);
        }

        private void ReadClient()
        {
            byte[] buffer = new byte[2048];
            int bytesReceived = 0;
            int processedBytes = 0;
            while (!_stopThread)
            {
                try
                {
                    if (_client == null || !_client.Connected || _stopThread)
                    {
                        break;
                    }

                    if (Ssl)
                    {
                        bytesReceived = _sslStream.Read(buffer, 0, buffer.Length);
                    }
                    else
                    {
                        bytesReceived = _client.Client.Receive(buffer, 0, buffer.Length, SocketFlags.None);
                    }

                    if (bytesReceived < 0) continue;

                    try
                    {
                        processedBytes = 0;
                        while (processedBytes < bytesReceived)
                        {
                            processedBytes += _binaryRpc.Process(buffer, processedBytes, bytesReceived - processedBytes);
                            if (_binaryRpc.IsFinished)
                            {
                                byte[] packet = _binaryRpc.Data;
                                _binaryRpc.Reset();
                                System.Diagnostics.Debug.WriteLine("Packet received " + BitConverter.ToString(packet));
                                _eventQueue.Enque(packet);
                            }
                        }
                    }
                    catch (HomegearBinaryRpcException ex)
                    {
                        System.Diagnostics.Debug.WriteLine(ex.Message);
                        _binaryRpc.Reset();
                    }
                }
                catch (TimeoutException)
                {
                    _binaryRpc.Reset();
                    continue;
                }
                catch (SocketException ex)
                {
                    if (ex.SocketErrorCode == SocketError.TimedOut)
                    {
                        _binaryRpc.Reset();
                        continue;
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine(ex.Message);
                        break;
                    }
                }
                catch (IOException ex)
                {
                    if (ex.HResult == -2146232800)
                    {
                        _binaryRpc.Reset();
                        continue;
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine(ex.Message);
                        break;
                    }
                }
                catch (AggregateException ex)
                {
                    // This exception occurs sporadically (not reproducible)
                    System.Diagnostics.Debug.WriteLine($"AggregateException {ex}");
                    _binaryRpc.Reset();
                    continue;
                }
            }
        }

        private void SendPacket(byte[] packet)
        {
            lock (_sendLock)
            {
                int bytesSent = 1024;
                int bytesToSend = 0;
                if (_client == null || !_client.Connected || _stopThread)
                {
                    return;
                }

                for (int i = 0; i < packet.Length; i += bytesSent)
                {
                    bytesToSend = (i + 1024 >= packet.Length) ? packet.Length - i : 1024;
                    if (Ssl)
                    {
                        _sslStream.Write(packet, i, bytesToSend);
                        _sslStream.Flush();
                    }
                    else
                    {
                        bytesSent = _client.Client.Send(packet, i, bytesToSend, SocketFlags.None);
                    }
                }
            }
        }

        private void ProcessPacket(byte[] packet)
        {
            if (packet == null || packet.Length < 8)
            {
                return;
            }

            if ((packet[3] & 1) == 0) //Request
            {
                string methodName = "";
                List<RPCVariable> parameters = _rpcDecoder.DecodeRequest(packet, ref methodName);
                RPCVariable response = new RPCVariable(RPCVariableType.rpcVoid);
                if (methodName == "")
                {
                    response = RPCVariable.CreateError(-1,
                        "Packet is not well formed: " + BitConverter.ToString(packet));
                }
                else if (methodName == "system.listMethods")
                {
                    response = new RPCVariable(RPCVariableType.rpcArray);
                    response.ArrayValue.Add(new RPCVariable("system.multicall"));
                    response.ArrayValue.Add(new RPCVariable("error"));
                    response.ArrayValue.Add(new RPCVariable("event"));
                    response.ArrayValue.Add(new RPCVariable("newDevices"));
                    response.ArrayValue.Add(new RPCVariable("updateDevice"));
                    response.ArrayValue.Add(new RPCVariable("deleteDevices"));
                    response.ArrayValue.Add(new RPCVariable("newEvent"));
                    response.ArrayValue.Add(new RPCVariable("deleteEvent"));
                    response.ArrayValue.Add(new RPCVariable("updateEvent"));
                }
                else if (methodName == "system.multicall" && parameters.Any())
                {
                    foreach (RPCVariable method in parameters[0].ArrayValue)
                    {
                        if (method.Type != RPCVariableType.rpcStruct || method.StructValue.Count() != 2)
                        {
                            continue;
                        }

                        if (method.StructValue["methodName"].StringValue != "event")
                        {
                            continue;
                        }

                        List<RPCVariable> eventParams = method.StructValue["params"].ArrayValue;
                        if (eventParams.Count() != 6 || eventParams[0].Type != RPCVariableType.rpcString ||
                            eventParams[1].Type != RPCVariableType.rpcInteger ||
                            eventParams[2].Type != RPCVariableType.rpcInteger ||
                            eventParams[3].Type != RPCVariableType.rpcString ||
                            eventParams[5].Type != RPCVariableType.rpcString)
                        {
                            continue;
                        }

                        RPCEvent?.Invoke(this, eventParams[1].IntegerValue, eventParams[2].IntegerValue, eventParams[3].StringValue, eventParams[4], eventParams[5].StringValue);
                    }
                }
                else if (methodName == "error" && parameters.Count() == 3 &&
                         parameters[1].Type == RPCVariableType.rpcInteger &&
                         parameters[2].Type == RPCVariableType.rpcString)
                {
                    HomegearError?.Invoke(this, parameters[1].IntegerValue, parameters[2].StringValue);
                }
                else if (methodName == "newDevices")
                {
                    NewDevices?.Invoke(this);
                }
                else if (methodName == "deleteDevices")
                {
                    DevicesDeleted?.Invoke(this);
                }
                else if (methodName == "updateDevice")
                {
                    if (parameters.Count == 4 && parameters[0].Type == RPCVariableType.rpcString &&
                        parameters[1].Type == RPCVariableType.rpcInteger &&
                        parameters[2].Type == RPCVariableType.rpcInteger &&
                        parameters[3].Type == RPCVariableType.rpcInteger)
                    {
                        UpdateDevice?.Invoke(this, parameters[1].IntegerValue, parameters[2].IntegerValue,
                            parameters[3].IntegerValue);
                    }
                }
                else if (methodName == "newEvent")
                {
                    if (parameters.Count == 2 && parameters[1].Type == RPCVariableType.rpcStruct)
                    {
                        string id = "";
                        long type = -1;
                        long peerId = 0;
                        long channel = -1;
                        string variable = "";
                        if (parameters[1].StructValue.ContainsKey("ID"))
                        {
                            id = parameters[1].StructValue["ID"].StringValue;
                        }

                        if (parameters[1].StructValue.ContainsKey("TYPE"))
                        {
                            type = parameters[1].StructValue["TYPE"].IntegerValue;
                        }

                        if (parameters[1].StructValue.ContainsKey("PEERID"))
                        {
                            peerId = parameters[1].StructValue["PEERID"].IntegerValue;
                        }

                        if (parameters[1].StructValue.ContainsKey("PEERCHANNEL"))
                        {
                            channel = parameters[1].StructValue["PEERCHANNEL"].IntegerValue;
                        }

                        if (parameters[1].StructValue.ContainsKey("VARIABLE"))
                        {
                            variable = parameters[1].StructValue["VARIABLE"].StringValue;
                        }

                        NewEvent?.Invoke(this, id, type, peerId, channel, variable);
                    }
                }
                else if (methodName == "deleteEvent")
                {
                    if (parameters.Count == 6)
                    {
                        EventDeleted?.Invoke(this, parameters[1].StringValue, parameters[2].IntegerValue,
                            parameters[3].IntegerValue, parameters[4].IntegerValue, parameters[5].StringValue);
                    }
                }
                else if (methodName == "updateEvent")
                {
                    if (parameters.Count == 6)
                    {
                        UpdateEvent?.Invoke(this, parameters[1].StringValue, parameters[2].IntegerValue,
                            parameters[3].IntegerValue, parameters[4].IntegerValue, parameters[5].StringValue);
                    }
                }
                byte[] responsePacket = _rpcEncoder.EncodeResponse(response).ToArray();
                SendPacket(responsePacket);
            }
            else //Response
            {
                _rpcResponse = _rpcDecoder.DecodeResponse(packet);
                _responseReceived.Set();
            }
        }

        public RPCVariable CallMethod(string name, List<RPCVariable> parameters)
        {
            try
            {
                lock (_callMethodLock)
                {
                    for (int j = 0; j < _maxTries; j++)
                    {
                        for (int i = 0; i < 3; i++)
                        {
                            try
                            {
                                if (_client == null || !_client.Connected)
                                {
                                    Connect();
                                }

                                if (Ssl && _sslStream == null)
                                {
                                    Connect();
                                }

                                break;
                            }
                            catch (HomegearRpcClientSSLException ex)
                            {
                                throw ex;
                            }
                            catch (HomegearRpcClientException ex)
                            {
                                if (i == 2)
                                {
                                    throw ex;
                                }
                            }

                            if (_stopThread) break;
                            Thread.Sleep(1000);
                            if (_stopThread) break;
                        }

                        _responseReceived.Reset();
                        _rpcResponse = null;

                        Encoding.RPCHeader header = null;
                        if (Ssl && _authString != null && _authString.Length > 0)
                        {
                            header = new Encoding.RPCHeader();
                            header.Authorization = Marshal.PtrToStringAuto(Marshal.SecureStringToBSTR(_authString));
                        }
                        byte[] requestPacket = _rpcEncoder.EncodeRequest(name, parameters, header).ToArray();
                        try
                        {
                            SendPacket(requestPacket);
                        }
                        catch (System.IO.IOException ex)
                        {
                            Disconnect();
                            if (j == _maxTries - 1)
                            {
                                throw new HomegearRpcClientException(
                                    "Error calling rpc method " + name + " on server " + _hostname + " and port " +
                                    _port.ToString() + ": " + ex.Message);
                            }

                            continue;
                        }
                        catch (SocketException ex)
                        {
                            Disconnect();
                            if (j == _maxTries - 1)
                            {
                                throw new HomegearRpcClientException(
                                    "Error calling rpc method " + name + " on server " + _hostname + " and port " +
                                    _port.ToString() + ": " + ex.Message);
                            }

                            continue;
                        }

                        if (_responseReceived.WaitOne(15000) && _rpcResponse != null)
                        {
                            break;
                        }
                    }

                    if (_rpcResponse == null)
                    {
                        return RPCVariable.CreateError(-32501, "No response received.");
                    }

                    if (_rpcResponse.ErrorStruct && _rpcResponse.StructValue["faultCode"].IntegerValue == -32603)
                    {
                        _client.Close(); //Unauthorized
                    }

                    return _rpcResponse;
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
