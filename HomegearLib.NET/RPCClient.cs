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

namespace HomegearLib
{
    internal class HomegearRPCClientException : HomegearException
    {
        public HomegearRPCClientException() : base()
        {
            
        }

        public HomegearRPCClientException(String message) : base(message)
        {

        }
    }

    internal class HomegearRPCClientSSLException : HomegearRPCClientException
    {
        public HomegearRPCClientSSLException() : base()
        {

        }

        public HomegearRPCClientSSLException(String message) : base(message)
        {

        }
    }

    internal class RPCClient
    {
        public delegate void ConnectedEventHandler(RPCClient sender);
        public delegate void DisconnectedEventHandler(RPCClient sender);

        #region "Events"
        public event ConnectedEventHandler Connected;
        public event DisconnectedEventHandler Disconnected;
        #endregion

        const int _maxTries = 3;
        private String _hostname = "homegear";
        private int _port = 2001;
        private bool _ssl = false;
        private bool _verifyCertificate = true;
        private string _username;
        private string _password;
        private TcpClient _client = null;
        private SslStream _sslStream = null;
        private Encoding.RPCEncoder _rpcEncoder = new Encoding.RPCEncoder();
        private Encoding.RPCDecoder _rpcDecoder = new Encoding.RPCDecoder();

        public bool IsConnected { get { return _client != null && _client.Connected; } }

        public RPCClient(String hostname, int port, bool ssl = false, bool verifyCertificate = true, string username = "", string password = "")
        {
            _hostname = hostname;
            _port = port;
            _ssl = ssl;
            _verifyCertificate = verifyCertificate;
            _username = username;
            _password = password;
        }

        public bool ValidateServerCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            if (sslPolicyErrors == SslPolicyErrors.None) return true;
            else if (!_verifyCertificate && (sslPolicyErrors == SslPolicyErrors.RemoteCertificateChainErrors || sslPolicyErrors == SslPolicyErrors.RemoteCertificateNameMismatch)) return true;
            return false;
        }

        public void Connect()
        {
            Disconnect();
            try
            {
                _client = new TcpClient(_hostname, _port);
                _client.ReceiveTimeout = 20000;
            }
            catch (SocketException ex)
            {
                throw new HomegearRPCClientException("Could not connect to server " + _hostname + " on port " + _port.ToString() + ": " + ex.Message);
            }
            
            if(_ssl)
            {
                _sslStream = new SslStream(_client.GetStream(), false, new RemoteCertificateValidationCallback(ValidateServerCertificate), null);
                try
                {
                    _sslStream.AuthenticateAsClient(_hostname);
                }
                catch (AuthenticationException ex)
                {
                    _client.Close();
                    throw new HomegearRPCClientSSLException("Server authentication failed: " + ex.Message);
                }
            }
            if(Connected != null) Connected(this);
        }

        public void Disconnect()
        {
            if (_client != null)
            {
                _client.Close();
                _client = null;
                _sslStream = null;
                if (Disconnected != null) Disconnected(this);
            }
        }

        public RPCVariable CallMethod(String name, List<RPCVariable> parameters)
        {
            RPCVariable result = null;
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
                }
                Encoding.RPCHeader header = null;
                if (_ssl && _username.Length > 0)
                {
                    header = new Encoding.RPCHeader();
                    header.Authorization = "Basic " + Convert.ToBase64String(System.Text.ASCIIEncoding.UTF8.GetBytes(_username + ":" + _password));
                }
                List<byte> requestPacket = _rpcEncoder.EncodeRequest(name, parameters, header);
                try
                {
                    if (_ssl)
                    {
                        _sslStream.Write(requestPacket.ToArray());
                        _sslStream.Flush();
                    }
                    else
                    {
                        _client.Client.Send(requestPacket.ToArray());
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
                    if (j == _maxTries - 1) throw new HomegearRPCClientException("Error calling rpc method " + name + " on server " + _hostname + " and port " + _port.ToString() + ": " + ex.Message);
                }
                catch (SocketException ex)
                {
                    Disconnect();
                    if(j == _maxTries - 1) throw new HomegearRPCClientException("Error calling rpc method " + name + " on server " + _hostname + " and port " + _port.ToString() + ": " + ex.Message);
                }
            }
            return result;
        }
    }
}
