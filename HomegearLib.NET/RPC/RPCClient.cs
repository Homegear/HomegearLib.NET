using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Collections;
using System.Security;
using System.Runtime.InteropServices;
using System.Threading;

namespace HomegearLib.RPC
{
    public class HomegearRPCClientException : HomegearException
    {
        public HomegearRPCClientException() : base()
        {
            
        }

        public HomegearRPCClientException(String message) : base(message)
        {

        }
    }

    public class HomegearRPCClientSSLException : HomegearRPCClientException
    {
        public HomegearRPCClientSSLException() : base()
        {

        }

        public HomegearRPCClientSSLException(String message) : base(message)
        {

        }
    }

    public class RPCClient : IDisposable
    {
        public delegate void ConnectedEventHandler(RPCClient sender, CipherAlgorithmType cipherAlgorithm = CipherAlgorithmType.Null, Int32 cipherStrength = -1);
        public delegate void DisconnectedEventHandler(RPCClient sender);

        #region "Events"
        public event ConnectedEventHandler Connected;
        public event DisconnectedEventHandler Disconnected;
        #endregion

        bool _disposing = false;
        Mutex _sendMutex = new Mutex();
        volatile bool _connecting = false;
        const int _maxTries = 3;
        private String _hostname = "homegear";
        private int _port = 2001;

        private bool _ssl = false;
        public bool SSL { get { return _ssl; } }
        
        private SSLClientInfo _sslInfo;
        private TcpClient _client = null;
        private SslStream _sslStream = null;
        private Encoding.RPCEncoder _rpcEncoder = new Encoding.RPCEncoder();
        private Encoding.RPCDecoder _rpcDecoder = new Encoding.RPCDecoder();
        private SecureString _authString = null;

        public bool IsConnected { get { return _client != null && _client.Connected; } }

        public CipherAlgorithmType CipherAlgorithm { get { if(_sslStream != null) return _sslStream.CipherAlgorithm; else return CipherAlgorithmType.Null; } }
        public Int32 CipherStrength { get { if (_sslStream != null) return _sslStream.CipherStrength; else return -1; } }

        public RPCClient(String hostname, int port, SSLClientInfo sslInfo = null)
        {
            _hostname = hostname;
            _port = port;
            _ssl = sslInfo != null;
            _sslInfo = sslInfo;
            if(_sslInfo != null)
            {
                _authString = GetSecureString("Basic " + Convert.ToBase64String(System.Text.ASCIIEncoding.UTF8.GetBytes(Marshal.PtrToStringAuto(Marshal.SecureStringToBSTR(_sslInfo.Username)) + ":" + Marshal.PtrToStringAuto(Marshal.SecureStringToBSTR(_sslInfo.Password)))));
            }
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
            if (sslPolicyErrors == SslPolicyErrors.None) return true;
            else if (!_sslInfo.VerifyCertificate && (sslPolicyErrors == SslPolicyErrors.RemoteCertificateChainErrors || sslPolicyErrors == SslPolicyErrors.RemoteCertificateNameMismatch)) return true;
            return false;
        }

        public void Connect()
        {
            if (_connecting) return;
            try
            {
                _connecting = true;
                if (_client != null) _client.Close();
                try
                {
                    _client = new TcpClient(_hostname, _port);
                    _client.ReceiveTimeout = 20000;
                }
                catch (SocketException ex)
                {
                    _connecting = false;
                    throw new HomegearRPCClientException("Could not connect to server " + _hostname + " on port " + _port.ToString() + ": " + ex.Message);
                }

                if (_ssl)
                {
                    _sslStream = new SslStream(_client.GetStream(), false, new RemoteCertificateValidationCallback(ValidateServerCertificate), null);
                    try
                    {
                        _sslStream.AuthenticateAsClient(_hostname);
                    }
                    catch (AuthenticationException ex)
                    {
                        _client.Close();
                        _connecting = false;
                        throw new HomegearRPCClientSSLException("Server authentication failed: " + ex.Message);
                    }
                    catch (System.IO.IOException ex)
                    {
                        _client.Close();
                        _connecting = false;
                        throw new HomegearRPCClientSSLException("Server authentication failed: " + ex.Message);
                    }
                    if (Connected != null) Connected(this, _sslStream.CipherAlgorithm, _sslStream.CipherStrength);
                }
                else if (Connected != null) Connected(this);
                _connecting = false;
            }
            catch(Exception ex)
            {
                _connecting = false;
                throw ex;
            }
        }

        public void Disconnect()
        {
            if (_client != null)
            {
                _sslStream.Close();
                _client.Close();
                _client = null;
                _sslStream = null;
                if (Disconnected != null) Disconnected(this);
            }
        }

        public RPCVariable CallMethod(String name, List<RPCVariable> parameters)
        {
            RPCVariable result = null;
            _sendMutex.WaitOne();
            try
            {
                for (int j = 0; j < _maxTries; j++)
                {
                    for (int i = 0; i < 3; i++)
                    {
                        try
                        {
                            if (_client == null || !_client.Connected) Connect();
                            if (_ssl && _sslStream == null) Connect();
                            break;
                        }
                        catch (HomegearRPCClientSSLException ex)
                        {
                            throw ex;
                        }
                        catch (HomegearRPCClientException ex)
                        {
                            if (i == 2) throw ex;
                        }
                        Thread.Sleep(1000);
                    }
                    Encoding.RPCHeader header = null;
                    if (_ssl && _authString != null && _authString.Length > 0)
                    {
                        header = new Encoding.RPCHeader();
                        header.Authorization = Marshal.PtrToStringAuto(Marshal.SecureStringToBSTR(_authString));
                    }
                    byte[] requestPacket = _rpcEncoder.EncodeRequest(name, parameters, header).ToArray();
                    try
                    {
                        Int32 bytesSent = 1024;
                        Int32 bytesToSend = 0;
                        for (int i = 0; i < requestPacket.Length; i += bytesSent)
                        {
                            bytesToSend = (i + 1024 >= requestPacket.Length) ? requestPacket.Length - i : 1024;
                            if (_ssl)
                            {
                                _sslStream.Write(requestPacket, i, bytesToSend);
                                _sslStream.Flush();
                            }
                            else bytesSent = _client.Client.Send(requestPacket, i, bytesToSend, SocketFlags.None);
                        }
                    }
                    catch (System.IO.IOException ex)
                    {
                        Disconnect();
                        if (j == _maxTries - 1) throw new HomegearRPCClientException("Error calling rpc method " + name + " on server " + _hostname + " and port " + _port.ToString() + ": " + ex.Message);
                        continue;
                    }
                    catch (SocketException ex)
                    {
                        Disconnect();
                        if (j == _maxTries - 1) throw new HomegearRPCClientException("Error calling rpc method " + name + " on server " + _hostname + " and port " + _port.ToString() + ": " + ex.Message);
                        continue;
                    }
                    try
                    {
                        byte[] packet = null;
                        int bytesReceived = 0;
                        byte[] buffer = new byte[1024];
                        uint dataSize = 0;
                        uint packetLength = 0;
                        do
                        {
                            if (_ssl) bytesReceived = _sslStream.Read(buffer, 0, buffer.Length);
                            else bytesReceived = _client.Client.Receive(buffer);
                            if (dataSize == 0)
                            {
                                if ((buffer[3] & 1) == 0) throw new HomegearRPCClientException("RPC client received binary request as response from server " + _hostname + " on port " + _port.ToString() + ".");
                                if (bytesReceived < 8) throw new HomegearRPCClientException("RPC client received binary response smaller than 8 bytes from server " + _hostname + " on port " + _port.ToString() + ".");
                                dataSize = (uint)((buffer[4] << 24) + (buffer[5] << 16) + (buffer[6] << 8) + buffer[7]);
                                if (dataSize == 0) throw new HomegearRPCClientException("RPC client received empty response from server " + _hostname + " on port " + _port.ToString() + ".");
                                if (dataSize > 104857600) throw new HomegearRPCClientException("RPC client received response larger than 100 MiB from server " + _hostname + " on port " + _port.ToString() + ".");
                                packetLength = (uint)bytesReceived - 8;
                                packet = new byte[dataSize + 8];
                                Array.Copy(buffer, packet, bytesReceived);
                            }
                            else
                            {
                                if (packetLength + bytesReceived > dataSize) throw new HomegearRPCClientException("RPC client received response larger than the expected size from server " + _hostname + " on port " + _port.ToString() + ".");
                                Array.Copy(buffer, 0, packet, packetLength + 8, bytesReceived);
                                packetLength += (uint)bytesReceived;
                            }
                            if (packetLength == dataSize) break;
                        } while (bytesReceived != 0);
                        result = _rpcDecoder.DecodeResponse(packet);
                        break;
                    }
                    catch (System.IO.IOException ex)
                    {
                        Disconnect();
                        if (j == _maxTries - 1) throw new HomegearRPCClientException("IOException thrown calling rpc method " + name + " on server " + _hostname + " and port " + _port.ToString() + ": " + ex.Message);
                    }
                    catch (SocketException ex)
                    {
                        Disconnect();
                        if (j == _maxTries - 1) throw new HomegearRPCClientException("SocketException thrown on calling rpc method " + name + " on server " + _hostname + " and port " + _port.ToString() + ": " + ex.Message);
                    }
                    catch (Exception ex)
                    {
                        Disconnect();
                        if (j == _maxTries - 1) throw new HomegearRPCClientException("Exception thrown on calling rpc method " + name + " on server " + _hostname + " and port " + _port.ToString() + ": " + ex.Message);
                    }
                }
                if (result.ErrorStruct && result.StructValue["faultCode"].IntegerValue == -32603) _client.Close();
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                _sendMutex.ReleaseMutex();
            }
            if (result == null) result = RPCVariable.CreateError(-32500, "Response was empty.");
            return result;
        }

        public void Dispose()
        {
            _disposing = true;
        }
    }
}
