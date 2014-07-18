using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomegearLib.RPC
{
    public enum RPCParameterSetType
    {
        rpcMaster,
        rpcValues,
        rpcLink
    }

    public class RPCController : IDisposable
    {
        public delegate void RPCEventEventHandler(RPCController sender, Variable value);
        public delegate void ConnectedEventHandler(RPCController sender);
        public delegate void DisconnectedEventHandler(RPCController sender);
        public delegate void InitCompletedEventHandler(RPCController sender);

        #region "Events"
        public event RPCEventEventHandler OnRPCEvent;
        public event ConnectedEventHandler Connected;
        public event DisconnectedEventHandler Disconnected;
        public event InitCompletedEventHandler InitCompleted;
        #endregion

        String _callbackHostname = "";
        
        volatile bool _disposing = false;
        public bool IsConnected { get { return _client != null && _client.IsConnected; } }

        Dictionary<Int32, Family> _families = null;
        public Dictionary<Int32, Family> Families { get { return _families; } }

        Dictionary<Int32, Device> _devices = null;
        public Dictionary<Int32, Device> Devices { get { return _devices; } }

        SSLClientInfo _sslClientInfo;
        RPCClient _client;
        RPCServer _server;
        System.Timers.Timer _keepAliveTimer;

        /// <summary>
        /// Creates a new RPCController object
        /// </summary>
        /// <param name="homegearHostname">The hostname or IP address of the Homegear server to connect to.</param>
        /// <param name="homegearPort">The port Homegear is listening on.</param>
        /// <param name="callbackHostname">The hostname of the the computer running this library. Needed by Homegear for certificate verification.</param>
        /// <param name="callbackListenIP">The IP address to bind the callback server to. Not "0.0.0.0", "::", "127.0.0.1" or "::1" Homegear sends events to the callback server.</param>
        /// <param name="callbackListenPort">The port of the callback server.</param>
        /// <param name="SSLClientInfo">When a SSLClientInfo object is passed, the connection to Homegear will be SSL encrypted.</param>
        /// <param name="SSLServerInfo">When a SSLServerInfo object is passed, the connection to the callback server will be SSL encrypted.</param>
        public RPCController(String homegearHostname, int homegearPort, String callbackHostname, String callbackListenIP, int callbackListenPort, SSLClientInfo sslClientInfo = null, SSLServerInfo sslServerInfo = null)
        {
            _callbackHostname = callbackHostname;
            _sslClientInfo = sslClientInfo;
            _client = new RPCClient(homegearHostname, homegearPort, sslClientInfo);
            _client.Connected += _client_Connected;
            _client.Disconnected += _client_Disconnected;
            _server = new RPCServer(callbackListenIP, callbackListenPort, sslServerInfo);
            _server.OnRPCEvent += _server_OnRPCEvent;
            _keepAliveTimer = new System.Timers.Timer(10000);
            _keepAliveTimer.Elapsed += _workerTimer_Elapsed;
        }

        void _workerTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            try
            {
                if (!ClientServerInitialized("HomegearLib." + _callbackHostname + ":" + _server.ListenPort))
                {
                    _client.Disconnect();
                }
            }
            catch(Exception)
            {
                _client.Disconnect();
            }
        }

        void _server_OnRPCEvent(RPCServer sender, Int32 peerID, Int32 channel, String parameterName, RPCVariable value)
        {
            if (OnRPCEvent != null) OnRPCEvent(this, new Variable(peerID, channel, parameterName, value));
        }

        public void Dispose()
        {
            _disposing = true;
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
            _families = ListFamilies();
            _devices = GetAllValues(_families);
            Init("HomegearLib." + _callbackHostname + ":" + _server.ListenPort);
            if (InitCompleted != null) InitCompleted(this);
        }

        public void Connect()
        {
            if (_disposing) throw new ObjectDisposedException("RPC");
            _server.Start();
            _client.Connect();
            _keepAliveTimer.Start();
        }

        public void Disconnect()
        {
            _keepAliveTimer.Stop();
            Init("");
            _client.Disconnect();
            _server.Stop();
        }

        void ThrowError(string methodName, RPCVariable errorStruct)
        {
            if (!errorStruct.ErrorStruct) return;
            throw new HomegearRPCClientException("Error calling RPC method \"" + methodName + "\". faultCode: " + errorStruct.StructValue["faultCode"].IntegerValue.ToString() + " faultString: " + errorStruct.StructValue["faultString"].StringValue);
        }

        #region "RPC methods"
        public bool ClientServerInitialized(string interfaceID)
        {
            if (_disposing) throw new ObjectDisposedException("RPC");
            RPCVariable response = _client.CallMethod("clientServerInitialized", new List<RPCVariable> { new RPCVariable(interfaceID) });
            if (response.ErrorStruct) ThrowError("clientServerInitialized", response);
            return response.BooleanValue;
        }

        public Dictionary<Int32, Device> GetAllValues(Dictionary<Int32, Family> families)
        {
            if (_disposing) throw new ObjectDisposedException("RPC");
            Dictionary<Int32, Device> devices = new Dictionary<Int32, Device>();
            RPCVariable response = _client.CallMethod("getAllValues", new List<RPCVariable> { new RPCVariable(true) });
            if (response.ErrorStruct) ThrowError("getAllValues", response);
            foreach (RPCVariable deviceStruct in response.ArrayValue)
            {
                if (!deviceStruct.StructValue.ContainsKey("ID") || !deviceStruct.StructValue.ContainsKey("FAMILY")) continue;
                if (!families.ContainsKey(deviceStruct.StructValue["FAMILY"].IntegerValue)) continue;
                Device device = new Device();
                device.Family = families[deviceStruct.StructValue["FAMILY"].IntegerValue];
                device.ID = deviceStruct.StructValue["ID"].IntegerValue;
                if (deviceStruct.StructValue.ContainsKey("ADDRESS")) device.SerialNumber = deviceStruct.StructValue["ADDRESS"].StringValue;
                if (deviceStruct.StructValue.ContainsKey("TYPE")) device.TypeString = deviceStruct.StructValue["TYPE"].StringValue;
                if(deviceStruct.StructValue.ContainsKey("CHANNELS"))
                {
                    Dictionary<Int32, Channel> channels = new Dictionary<Int32, Channel>();
                    foreach(RPCVariable channelStruct in deviceStruct.StructValue["CHANNELS"].ArrayValue)
                    {
                        if (!channelStruct.StructValue.ContainsKey("INDEX") || !channelStruct.StructValue.ContainsKey("PARAMSET")) continue;
                        Dictionary<String, Variable> variables = new Dictionary<String,Variable>();
                        Dictionary<String, RPCVariable> parameterSet = channelStruct.StructValue["PARAMSET"].StructValue;
                        Channel channel = new Channel(this, device.ID, channelStruct.StructValue["INDEX"].IntegerValue);
                        channels.Add(channel.Index, channel);
                        for (Int32 i = 0; i < parameterSet.Count; i++)
                        {
                            if (parameterSet.ElementAt(i).Key.Length == 0) continue;
                            Dictionary<String, RPCVariable> variableInfo = parameterSet.ElementAt(i).Value.StructValue;
                            bool readable = true;
                            if (variableInfo.ContainsKey("READABLE")) readable = variableInfo["READABLE"].BooleanValue;;
                            RPCVariable value = null;
                            if (variableInfo.ContainsKey("VALUE")) value = variableInfo["VALUE"];
                            else
                            {
                                if(readable || !variableInfo.ContainsKey("TYPE")) continue;
                                value = RPCVariable.CreateFromTypeString(variableInfo["TYPE"].StringValue);
                                if (value.Type == RPCVariableType.rpcVoid) continue;
                            }

                            Variable variable = new Variable(this, device.ID, channel.Index, parameterSet.ElementAt(i).Key, value);
                            if (variableInfo.ContainsKey("WRITEABLE")) variable.Writeable = variableInfo["WRITEABLE"].BooleanValue;
                            variable.Readable = readable;
                            if (variableInfo.ContainsKey("MIN")) variable.SetMax(variableInfo["MIN"]);
                            if (variableInfo.ContainsKey("MAX")) variable.SetMax(variableInfo["MAX"]);
                            if (variableInfo.ContainsKey("VALUE_LIST")) variable.SetValueList(variableInfo["VALUE_LIST"]);
                            variables.Add(variable.Name, variable);
                        }
                        channel.Variables = new Variables(this, variables);
                    }
                    device.Channels = new Channels(this, channels);
                }
                devices.Add(device.ID, device);
            }
            return devices;
        }

        public void GetParamset(Int32 peerID, Int32 channel, RPCParameterSetType type, DeviceConfig parameters)
        {
            if (_disposing) throw new ObjectDisposedException("RPC");
            String typeString = "MASTER";
            if (type == RPCParameterSetType.rpcValues) typeString = "VALUES";
            RPCVariable response = _client.CallMethod("getParamset", new List<RPCVariable> { new RPCVariable(peerID), new RPCVariable(channel), new RPCVariable(typeString) });
            if (response.ErrorStruct) ThrowError("getParamset", response);
            foreach (KeyValuePair<String, RPCVariable> value in response.StructValue)
            {
                if (!parameters.ContainsKey(value.Key)) continue;
                parameters[value.Key].SetValue(value.Value);
            }
        }

        public Dictionary<String, ConfigParameter> GetParamsetDescription(Int32 peerID, Int32 channel, RPCParameterSetType type)
        {
            if (_disposing) throw new ObjectDisposedException("RPC");
            Dictionary<String, ConfigParameter> parameters = new Dictionary<String, ConfigParameter>();
            String typeString = "MASTER";
            if (type == RPCParameterSetType.rpcValues) typeString = "VALUES";
            RPCVariable response = _client.CallMethod("getParamsetDescription", new List<RPCVariable> { new RPCVariable(peerID), new RPCVariable(channel), new RPCVariable(typeString) });
            if (response.ErrorStruct) ThrowError("getParamsetDescription", response);
            foreach(KeyValuePair<String, RPCVariable> parameterDescription in response.StructValue)
            {
                if(parameterDescription.Key.Length == 0) continue;
                if (!parameterDescription.Value.StructValue.ContainsKey("TYPE")) continue;
                RPCVariable rpcParameter = RPCVariable.CreateFromTypeString(parameterDescription.Value.StructValue["TYPE"].StringValue);
                if (rpcParameter.Type == RPCVariableType.rpcVoid) continue;
                ConfigParameter configParameter = new ConfigParameter(this, peerID, channel, parameterDescription.Key, rpcParameter);
                if (parameterDescription.Value.StructValue.ContainsKey("MIN")) configParameter.SetMin(parameterDescription.Value.StructValue["MIN"]);
                if (parameterDescription.Value.StructValue.ContainsKey("MAX")) configParameter.SetMax(parameterDescription.Value.StructValue["MAX"]);
                if (parameterDescription.Value.StructValue.ContainsKey("DEFAULT")) configParameter.SetDefault(parameterDescription.Value.StructValue["DEFAULT"]);
                if (parameterDescription.Value.StructValue.ContainsKey("OPERATIONS"))
                {
                    Int32 operations = parameterDescription.Value.StructValue["OPERATIONS"].IntegerValue;
                    if ((operations & 1) == 1 || (operations & 4) == 4) configParameter.Readable = true;
                    if ((operations & 2) == 2) configParameter.Writeable = true;
                }
                if (parameterDescription.Value.StructValue.ContainsKey("FLAGS")) configParameter.UIFlags = (VariableUIFlags)parameterDescription.Value.StructValue["FLAGS"].IntegerValue;
                if (parameterDescription.Value.StructValue.ContainsKey("UNIT")) configParameter.Unit = parameterDescription.Value.StructValue["UNIT"].StringValue;
                if (parameterDescription.Value.StructValue.ContainsKey("SPECIAL")) configParameter.SetSpecialValues(parameterDescription.Value.StructValue["SPECIAL"]);
                if (parameterDescription.Value.StructValue.ContainsKey("VALUE_LIST")) configParameter.SetValueList(parameterDescription.Value.StructValue["VALUE_LIST"]);
                configParameter.Name = parameterDescription.Key;
                parameters.Add(parameterDescription.Key, configParameter);
            }
            return parameters;
        }

        public void Init(string interfaceID)
        {
            if (_disposing) throw new ObjectDisposedException("RPC");
            string prefix;
            if (_server.SSL) prefix = "binarys://";
            else prefix = "binary://";
            RPCVariable response = _client.CallMethod("init", new List<RPCVariable> { new RPCVariable(prefix + _callbackHostname + ":" + _server.ListenPort.ToString()), new RPCVariable(interfaceID), new RPCVariable(7) });
            if (response.ErrorStruct) ThrowError("init", response);
        }

        public Dictionary<Int32, Family> ListFamilies()
        {
            if (_disposing) throw new ObjectDisposedException("RPC");
            Dictionary<Int32, Family> families = new Dictionary<Int32, Family>();
            RPCVariable response = _client.CallMethod("listFamilies", null);
            if (response.ErrorStruct) ThrowError("listFamilies", response);
            foreach(RPCVariable familyStruct in response.ArrayValue)
            {
                if (!familyStruct.StructValue.ContainsKey("ID") || !familyStruct.StructValue.ContainsKey("NAME")) continue;
                families.Add(familyStruct.StructValue["ID"].IntegerValue, new Family(familyStruct.StructValue["ID"].IntegerValue, familyStruct.StructValue["NAME"].StringValue));
            }
            return families;
        }

        public void SetValue(Variable variable)
        {
            if (_disposing) throw new ObjectDisposedException("RPC");
            RPCVariable response = _client.CallMethod("setValue", new List<RPCVariable> { new RPCVariable(variable.PeerID), new RPCVariable(variable.Channel), new RPCVariable(variable.Name), new RPCVariable(variable) });
        }
        #endregion
    }
}
