using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Authentication;
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

    public enum RPCUpdateDeviceFlags
    {
        rpcConfig = 0,
        rpcLinks = 1,
        rpcTeam = 2
    }

    public enum RPCDeleteDeviceFlags
    {
        None = 0,
        Reset = 1,
        Force = 2,
        Defer = 4
    }

    public class RPCController : IDisposable
    {
        internal delegate void DeviceVariableUpdatedEventHandler(RPCController sender, Variable value);
        internal delegate void SystemVariableUpdatedEventHandler(RPCController sender, SystemVariable value);
        internal delegate void PongEventHandler(RPCController sender, String id);
        internal delegate void SystemVariableDeletedEventHandler(RPCController sender);
        internal delegate void MetadataUpdatedEventHandler(RPCController sender, Int32 peerID, MetadataVariable value);
        internal delegate void MetadataDeletedEventHandler(RPCController sender, Int32 peerID);
        internal delegate void NewDevicesEventHandler(RPCController sender);
        internal delegate void DevicesDeletedEventHandler(RPCController sender);
        internal delegate void UpdateDeviceEventHandler(RPCController sender, Int32 peerID, Int32 channel, RPCUpdateDeviceFlags flags);
        internal delegate void NewEventEventHandler(RPCController sender, String id, EventType type, Int32 peerID, Int32 channel, String variableName);
        internal delegate void EventDeletedEventHandler(RPCController sender, String id, EventType type, Int32 peerID, Int32 channel, String variableName);
        internal delegate void UpdateEventEventHandler(RPCController sender, String id, EventType type, Int32 peerID, Int32 channel, String variableName);
        internal delegate void InitCompletedEventHandler(RPCController sender);
        internal delegate void HomegearErrorEventHandler(RPCController sender, Int32 level, String message);
        public delegate void ClientConnectedEventHandler(RPCClient sender, CipherAlgorithmType cipherAlgorithm = CipherAlgorithmType.Null, Int32 cipherStrength = -1);
        public delegate void ClientDisconnectedEventHandler(RPCClient sender);
        public delegate void ServerConnectedEventHandler(RPCServer sender, CipherAlgorithmType cipherAlgorithm = CipherAlgorithmType.Null, Int32 cipherStrength = -1);
        public delegate void ServerDisconnectedEventHandler(RPCServer sender);

        #region "Events"
        internal event DeviceVariableUpdatedEventHandler DeviceVariableUpdated;
        internal event SystemVariableUpdatedEventHandler SystemVariableUpdated;
        internal event PongEventHandler Pong;
        internal event SystemVariableDeletedEventHandler SystemVariableDeleted;
        internal event MetadataUpdatedEventHandler MetadataUpdated;
        internal event MetadataDeletedEventHandler MetadataDeleted;
        internal event NewDevicesEventHandler NewDevices;
        internal event DevicesDeletedEventHandler DevicesDeleted;
        internal event UpdateDeviceEventHandler UpdateDevice;
        internal event NewEventEventHandler NewEvent;
        internal event EventDeletedEventHandler EventDeleted;
        internal event UpdateEventEventHandler UpdateEvent;
        internal event InitCompletedEventHandler InitCompleted;
        internal event HomegearErrorEventHandler HomegearError;

        /// <summary>
        /// Raised, when the Homegear object managed to successfully connect to Homegear. Important: The event is also raised, when user authentication is not successful!  
        /// </summary>
        public event ClientConnectedEventHandler ClientConnected;

        /// <summary>
        /// Raised, when the connection to Homegear is closed. 
        /// </summary>
        public event ClientDisconnectedEventHandler ClientDisconnected;

        /// <summary>
        /// Raised, when there is a successful incoming connection from Homegear to the library's callback event server.
        /// </summary>
        public event ServerConnectedEventHandler ServerConnected;

        /// <summary>
        /// Raised, when the incoming connection to our event server is closed.
        /// </summary>
        public event ServerDisconnectedEventHandler ServerDisconnected;
        #endregion

        private String _callbackHostname = "";

        private volatile bool _disposing = false;
        private volatile bool _events = false;

        /// <summary>
        /// Returns "true" when the RPC controller is connected to Homegear.
        /// </summary>
        public bool IsConnected { get { return _client != null && _client.IsConnected; } }

        private Dictionary<Int32, Family> _families = null;
        internal Dictionary<Int32, Family> Families
        {
            get
            {
                if (_families == null || _families.Count == 0) _families = ListFamilies();
                return _families;
            }
        }

        private Dictionary<Int32, Device> _devices = null;
        internal Dictionary<Int32, Device> Devices
        {
            get
            {
                if (_devices == null || _devices.Count == 0) _devices = GetAllValues();
                return _devices;
            }
        }

        private Dictionary<String, Interface> _interfaces = null;
        internal Dictionary<String, Interface> Interfaces
        {
            get
            {
                if (_interfaces == null || _interfaces.Count == 0) _interfaces = ListInterfaces();
                return _interfaces;
            }
        }

        private Dictionary<String, SystemVariable> _systemVariables = null;
        internal Dictionary<String, SystemVariable> SystemVariables
        {
            get
            {
                if (_systemVariables == null || _systemVariables.Count == 0) _systemVariables = GetAllSystemVariables();
                return _systemVariables;
            }
            set
            {
                _systemVariables = value;
            }
        }

        private SSLClientInfo _sslClientInfo;

        private RPCClient _client = null;
        public RPCClient Client { get { return _client; } }

        private RPCServer _server = null;
        public RPCServer Server { get { return _server; } }

        private bool _asciiDeviceTypeIdString = false;
        public bool AsciiDeviceTypeIdString { get { return _asciiDeviceTypeIdString; } set { _asciiDeviceTypeIdString = value; } }

        private System.Timers.Timer _keepAliveTimer;

        /// <summary>
        /// Creates a new RPCController object,
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
            _server.Connected += _server_Connected;
            _server.Disconnected += _server_Disconnected;
            _server.HomegearError += _server_HomegearError;
            _server.RPCEvent += _server_OnRPCEvent;
            _server.NewDevices += _server_OnNewDevices;
            _server.DevicesDeleted += _server_OnDevicesDeleted;
            _server.UpdateDevice += _server_OnUpdateDevice;
            _server.NewEvent += _server_OnNewEvent;
            _server.EventDeleted += _server_OnEventDeleted;
            _server.UpdateEvent += _server_OnUpdateEvent;
            _keepAliveTimer = new System.Timers.Timer(30000);
            _keepAliveTimer.Elapsed += _workerTimer_Elapsed;
        }

        void _server_HomegearError(RPCServer sender, int level, string message)
        {
            if (HomegearError != null) HomegearError(this, level, message);
        }

        private void _server_OnUpdateDevice(RPCServer sender, int peerID, int channel, int flags)
        {
            if (UpdateDevice != null) UpdateDevice(this, peerID, channel, (RPCUpdateDeviceFlags)flags);
        }

        private void _server_OnDevicesDeleted(RPCServer sender)
        {
            if (DevicesDeleted != null) DevicesDeleted(this);
        }

        private void _server_OnNewDevices(RPCServer sender)
        {
            if (NewDevices != null) NewDevices(this);
        }

        private void _server_OnUpdateEvent(RPCServer sender, String id, Int32 eventType, Int32 peerID, Int32 channel, String variable)
        {
            if (UpdateEvent != null) UpdateEvent(this, id, (EventType)eventType, peerID, channel, variable);
        }

        private void _server_OnEventDeleted(RPCServer sender, String id, Int32 eventType, Int32 peerID, Int32 channel, String variable)
        {
            if (EventDeleted != null) EventDeleted(this, id, (EventType)eventType, peerID, channel, variable);
        }

        private void _server_OnNewEvent(RPCServer sender, String id, Int32 eventType, Int32 peerID, Int32 channel, String variable)
        {
            if (NewEvent != null) NewEvent(this, id, (EventType)eventType, peerID, channel, variable);
        }

        private void _workerTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            try
            {
                _keepAliveTimer.Interval = 10000;
                if (_events)
                {
                    if (!ClientServerInitialized("HomegearLib." + _callbackHostname + ":" + _server.ListenPort))
                    {
                        _client.Disconnect();
                    }
                }
                else
                {
                    if(_client.CallMethod("logLevel", new List<RPCVariable>()).ErrorStruct)
                    {
                        _client.Disconnect();
                    }
                }
            }
            catch(Exception)
            {
                _client.Disconnect();
            }
        }

        private void _server_OnRPCEvent(RPCServer sender, Int32 peerID, Int32 channel, String parameterName, RPCVariable value)
        {
            if (peerID == 0)
            {
                if (value.Type == RPCVariableType.rpcStruct && value.StructValue.Count == 2 && value.StructValue.ContainsKey("CODE") && value.StructValue["CODE"].IntegerValue == 1 && value.StructValue.ContainsKey("TYPE") && value.StructValue["TYPE"].IntegerValue == 0)
                {
                    if (SystemVariableDeleted != null) SystemVariableDeleted(this);
                }
                else if(parameterName == "PONG")
                {
                    if (Pong != null) Pong(this, value.StringValue);
                }
                else
                {
                    if (SystemVariableUpdated != null) SystemVariableUpdated(this, new SystemVariable(null, parameterName, value));
                }
            }
            else if(channel == -1)
            {
                if (value.Type == RPCVariableType.rpcStruct && value.StructValue.Count == 2 && value.StructValue.ContainsKey("CODE") && value.StructValue["CODE"].IntegerValue == 1 && value.StructValue.ContainsKey("TYPE") && value.StructValue["TYPE"].IntegerValue == 1)
                {
                    if (MetadataDeleted != null) MetadataDeleted(this, peerID);
                }
                else
                {
                    if (MetadataUpdated != null) MetadataUpdated(this, peerID, new MetadataVariable(null, peerID, parameterName, value));
                }
            }
            else if (DeviceVariableUpdated != null) DeviceVariableUpdated(this, new Variable(peerID, channel, parameterName, value));
        }

        /// <summary>
        /// Disconnects from Homegear and stops the controller's RPC server.
        /// </summary>
        public void Dispose()
        {
            try
            {
                _disposing = true;
                _client.Disconnect();
                _server.Stop();
            }
            catch(Exception)
            {
            }
        }

        internal void Clear()
        {
            _families = null;
            _devices = null;
            _interfaces = null;
            _systemVariables = null;
        }

        private void _client_Disconnected(RPCClient sender)
        {
            if (ClientDisconnected != null) ClientDisconnected(sender);
        }

        private void _client_Connected(RPCClient sender, CipherAlgorithmType cipherAlgorithm, Int32 cipherStrength)
        {
            if (ClientConnected != null) ClientConnected(sender, cipherAlgorithm, cipherStrength);
            if (_events)
            {
                _server.KnownDevices = Devices;
                Init("HomegearLib." + _callbackHostname + ":" + _server.ListenPort);
            }
            if (InitCompleted != null) InitCompleted(this);
        }

        private void _server_Disconnected(RPCServer sender)
        {
            if (ServerDisconnected != null) ServerDisconnected(sender);
        }

        private void _server_Connected(RPCServer sender, CipherAlgorithmType cipherAlgorithm, Int32 cipherStrength)
        {
            if (ServerConnected != null) ServerConnected(sender, cipherAlgorithm, cipherStrength);
        }

        /// <summary>
        /// Connects to Homegear and starts the RPC controller's callback event server. Don't call this method, when you pass this object to the Homegear object's constructor!
        /// <param name="events">When set to "true" the library starts an event server to receive events from Homegear.</param>
        /// </summary>
        public void Connect(bool events)
        {
            _events = events;
            if (_disposing) throw new ObjectDisposedException("RPC");
            if(events) _server.Start();
            _client.Connect();
            _keepAliveTimer.Start();
        }

        /// <summary>
        /// Disconnects from Homegear and stops the RPC controller's callback event server. Don't call this method, when you passed this object to the Homegear object!
        /// </summary>
        public void Disconnect()
        {
            _keepAliveTimer.Stop();
            try
            {
                if(IsConnected && _events) Init("");
            }
            catch (Exception) { }
            _client.Disconnect();
            _server.Stop();
        }

        private void ThrowError(string methodName, RPCVariable errorStruct)
        {
            if (!errorStruct.ErrorStruct) return;
            throw new HomegearRPCClientException("Error calling RPC method \"" + methodName + "\". faultCode: " + errorStruct.StructValue["faultCode"].IntegerValue.ToString() + " faultString: " + errorStruct.StructValue["faultString"].StringValue);
        }

        #region "RPC methods"
        public void AbortEventReset(String id)
        {
            if (_disposing) throw new ObjectDisposedException("RPC");
            RPCVariable response = _client.CallMethod("abortEventReset", new List<RPCVariable> { new RPCVariable(id) });
            if (response.ErrorStruct) ThrowError("abortEventReset", response);
        }

        public bool AddDevice(String serialNumber)
        {
            if (_disposing) throw new ObjectDisposedException("RPC");
            RPCVariable response = _client.CallMethod("addDevice", new List<RPCVariable> { new RPCVariable(serialNumber) });
            if (response.ErrorStruct)
            {
                if (response.StructValue["faultCode"].IntegerValue == -1) return false;
                ThrowError("addDevice", response);
            }
            return true;
        }

        public void AddEvent(Event newEvent)
        {
            if (_disposing) throw new ObjectDisposedException("RPC");
            RPCVariable eventDescription = new RPCVariable(RPCVariableType.rpcStruct);
            if(newEvent is TimedEvent)
            {
                TimedEvent timedEvent = (TimedEvent)newEvent;
                eventDescription.StructValue.Add("TYPE", new RPCVariable(1));
                eventDescription.StructValue.Add("ID", new RPCVariable(timedEvent.ID));
                eventDescription.StructValue.Add("ENABLED", new RPCVariable(timedEvent.Enabled));
                eventDescription.StructValue.Add("EVENTTIME", new RPCVariable(HomegearHelpers.DateTimeToUnixTimeStamp(timedEvent.EventTime)));
                eventDescription.StructValue.Add("RECUREVERY", new RPCVariable(timedEvent.RecurEvery));
                if (timedEvent.EndTime != DateTime.MinValue) eventDescription.StructValue.Add("ENDTIME", new RPCVariable(HomegearHelpers.DateTimeToUnixTimeStamp(timedEvent.EndTime)));
                eventDescription.StructValue.Add("EVENTMETHOD", new RPCVariable(timedEvent.EventMethod));
                eventDescription.StructValue.Add("EVENTMETHODPARAMS", new RPCVariable(timedEvent.EventMethodParams));
            }
            else
            {
                TriggeredEvent triggeredEvent = (TriggeredEvent)newEvent;
                eventDescription.StructValue.Add("TYPE", new RPCVariable(0));
                eventDescription.StructValue.Add("ID", new RPCVariable(triggeredEvent.ID));
                eventDescription.StructValue.Add("ENABLED", new RPCVariable(triggeredEvent.Enabled));
                eventDescription.StructValue.Add("PEERID", new RPCVariable(triggeredEvent.PeerID));
                eventDescription.StructValue.Add("PEERCHANNEL", new RPCVariable(triggeredEvent.PeerChannel));
                eventDescription.StructValue.Add("VARIABLE", new RPCVariable(triggeredEvent.VariableName));
                eventDescription.StructValue.Add("TRIGGER", new RPCVariable((Int32)triggeredEvent.Trigger));
                eventDescription.StructValue.Add("TRIGGERVALUE", triggeredEvent.TriggerValue);
                eventDescription.StructValue.Add("EVENTMETHOD", new RPCVariable(triggeredEvent.EventMethod));
                eventDescription.StructValue.Add("EVENTMETHODPARAMS", new RPCVariable(triggeredEvent.EventMethodParams));
                if (triggeredEvent.ResetAfterDynamic != null)
                {
                    RPCVariable resetStruct = new RPCVariable(RPCVariableType.rpcStruct);
                    resetStruct.StructValue.Add("INITIALTIME", new RPCVariable(triggeredEvent.ResetAfterDynamic.InitialTime));
                    resetStruct.StructValue.Add("RESETAFTER", new RPCVariable(triggeredEvent.ResetAfterDynamic.ResetAfter));
                    resetStruct.StructValue.Add("OPERATION", new RPCVariable((Int32)triggeredEvent.ResetAfterDynamic.Operation));
                    resetStruct.StructValue.Add("FACTOR", new RPCVariable(triggeredEvent.ResetAfterDynamic.Factor));
                    resetStruct.StructValue.Add("LIMIT", new RPCVariable(triggeredEvent.ResetAfterDynamic.Limit));
                    eventDescription.StructValue.Add("RESETAFTER", resetStruct);
                }
                else eventDescription.StructValue.Add("RESETAFTER", new RPCVariable(triggeredEvent.ResetAfterStatic));
                if (triggeredEvent.ResetMethod.Length > 0)
                {
                    eventDescription.StructValue.Add("RESETMETHOD", new RPCVariable(triggeredEvent.ResetMethod));
                    eventDescription.StructValue.Add("RESETMETHODPARAMS", new RPCVariable(triggeredEvent.ResetMethodParams));
                }
            }
            RPCVariable response = _client.CallMethod("addEvent", new List<RPCVariable> { eventDescription });
            if (response.ErrorStruct) ThrowError("addEvent", response);
        }

        public void AddLink(Int32 senderID, Int32 senderChannel, Int32 receiverID, Int32 receiverChannel)
        {
            if (_disposing) throw new ObjectDisposedException("RPC");
            RPCVariable response = _client.CallMethod("addLink", new List<RPCVariable> { new RPCVariable(senderID), new RPCVariable(senderChannel), new RPCVariable(receiverID), new RPCVariable(receiverChannel) });
            if (response.ErrorStruct) ThrowError("addLink", response);
        }

        public void AddLink(Int32 senderID, Int32 senderChannel, Int32 receiverID, Int32 receiverChannel, String name, String description)
        {
            if (_disposing) throw new ObjectDisposedException("RPC");
            RPCVariable response = _client.CallMethod("addLink", new List<RPCVariable> { new RPCVariable(senderID), new RPCVariable(senderChannel), new RPCVariable(receiverID), new RPCVariable(receiverChannel), new RPCVariable(name), new RPCVariable(description) });
            if (response.ErrorStruct) ThrowError("addLink", response);
        }

        public bool ClientServerInitialized(string interfaceID)
        {
            if (_disposing) throw new ObjectDisposedException("RPC");
            RPCVariable response = _client.CallMethod("clientServerInitialized", new List<RPCVariable> { new RPCVariable(interfaceID) });
            if (response.ErrorStruct) ThrowError("clientServerInitialized", response);
            return response.BooleanValue;
        }

        public void DeleteDevice(Int32 peerID, RPCDeleteDeviceFlags flags)
        {
            if (_disposing) throw new ObjectDisposedException("RPC");
            RPCVariable response = _client.CallMethod("deleteDevice", new List<RPCVariable> { new RPCVariable(peerID), new RPCVariable((Int32)flags) });
            if (response.ErrorStruct) ThrowError("deleteDevice", response);
        }

        public void DeleteMetadata(Int32 peerID)
        {
            if (_disposing) throw new ObjectDisposedException("RPC");
            RPCVariable response = _client.CallMethod("deleteMetadata", new List<RPCVariable> { new RPCVariable(peerID) });
            if (response.ErrorStruct) ThrowError("deleteMetadata", response);
        }

        public void DeleteMetadata(MetadataVariable variable)
        {
            if (_disposing) throw new ObjectDisposedException("RPC");
            RPCVariable response = _client.CallMethod("deleteMetadata", new List<RPCVariable> { new RPCVariable(variable.PeerID), new RPCVariable(variable.Name) });
            if (response.ErrorStruct) ThrowError("deleteMetadata", response);
        }

        public void DeleteSystemVariable(SystemVariable variable)
        {
            if (_disposing) throw new ObjectDisposedException("RPC");
            RPCVariable response = _client.CallMethod("deleteSystemVariable", new List<RPCVariable> { new RPCVariable(variable.Name) });
            if (response.ErrorStruct) ThrowError("deleteSystemVariable", response);
        }

        public void EnableEvent(String id, Boolean enabled)
        {
            if (_disposing) throw new ObjectDisposedException("RPC");
            RPCVariable response = _client.CallMethod("enableEvent", new List<RPCVariable> { new RPCVariable(id), new RPCVariable(enabled) });
            if (response.ErrorStruct) ThrowError("enableEvent", response);
        }

        public Dictionary<String, MetadataVariable> GetAllMetadata(Int32 peerID)
        {
            if (_disposing) throw new ObjectDisposedException("RPC");
            Dictionary<String, MetadataVariable> metadataVariables = new Dictionary<String, MetadataVariable>();
            RPCVariable response = _client.CallMethod("getAllMetadata", new List<RPCVariable> { new RPCVariable(peerID) });
            if (response.ErrorStruct)
            {
                if (response.StructValue["faultCode"].IntegerValue == -1) return metadataVariables;
                else ThrowError("getAllMetadata", response);
            }
            foreach (KeyValuePair<String, RPCVariable> element in response.StructValue)
            {
                MetadataVariable variable = new MetadataVariable(this, peerID, element.Key, element.Value);
                metadataVariables.Add(element.Key, variable);
            }
            return metadataVariables;
        }

        public Dictionary<String, SystemVariable> GetAllSystemVariables()
        {
            if (_disposing) throw new ObjectDisposedException("RPC");
            Dictionary<String, SystemVariable> systemVariables = new Dictionary<String, SystemVariable>();
            RPCVariable response = _client.CallMethod("getAllSystemVariables", new List<RPCVariable>());
            if (response.ErrorStruct) ThrowError("getAllSystemVariables", response);
            foreach(KeyValuePair<String, RPCVariable> element in response.StructValue)
            {
                SystemVariable variable = new SystemVariable(this, element.Key, element.Value);
                systemVariables.Add(element.Key, variable);
            }
            return systemVariables;
        }

        public Dictionary<String, Variable> GetAllValues(Int32 peerId, Int32 channelIndex)
        {
            if (_disposing) throw new ObjectDisposedException("RPC");
            RPCVariable response = _client.CallMethod("getAllValues", new List<RPCVariable> { new RPCVariable(peerId), new RPCVariable(true) });
            if (response.ErrorStruct) ThrowError("getAllValues", response);
            Dictionary<Int32, Family> families = Families;
            Dictionary<String, Variable> variables = new Dictionary<String, Variable>();
            foreach (RPCVariable deviceStruct in response.ArrayValue)
            {
                if (deviceStruct.StructValue.ContainsKey("CHANNELS"))
                {
                    Dictionary<Int32, Channel> channels = new Dictionary<Int32, Channel>();
                    foreach (RPCVariable channelStruct in deviceStruct.StructValue["CHANNELS"].ArrayValue)
                    {
                        if (!channelStruct.StructValue.ContainsKey("INDEX") || !channelStruct.StructValue.ContainsKey("PARAMSET")) continue;
                        if(channelStruct.StructValue["INDEX"].IntegerValue != channelIndex) continue;
                        Dictionary<String, RPCVariable> parameterSet = channelStruct.StructValue["PARAMSET"].StructValue;
                        for (Int32 i = 0; i < parameterSet.Count; i++)
                        {
                            if (parameterSet.ElementAt(i).Key.Length == 0) continue;
                            Dictionary<String, RPCVariable> variableInfo = parameterSet.ElementAt(i).Value.StructValue;
                            bool readable = true;
                            if (variableInfo.ContainsKey("READABLE")) readable = variableInfo["READABLE"].BooleanValue; ;
                            String typeString = "";
                            if (variableInfo.ContainsKey("TYPE")) typeString = variableInfo["TYPE"].StringValue;
                            RPCVariable value = null;
                            if (variableInfo.ContainsKey("VALUE")) value = variableInfo["VALUE"];
                            else
                            {
                                if (readable || typeString.Length == 0) continue;
                                value = RPCVariable.CreateFromTypeString(typeString);
                                if (value.Type == RPCVariableType.rpcVoid) continue;
                            }
                            Variable variable = new Variable(this, peerId, channelIndex, parameterSet.ElementAt(i).Key, typeString, value);

                            if (variableInfo.ContainsKey("WRITEABLE")) variable.Writeable = variableInfo["WRITEABLE"].BooleanValue;
                            variable.Readable = readable;
                            if (variableInfo.ContainsKey("MIN")) variable.SetMin(variableInfo["MIN"]);
                            if (variableInfo.ContainsKey("MAX")) variable.SetMax(variableInfo["MAX"]);
                            if (variableInfo.ContainsKey("SPECIAL")) variable.SetSpecialValues(variableInfo["SPECIAL"]);
                            if (variableInfo.ContainsKey("VALUE_LIST")) variable.SetValueList(variableInfo["VALUE_LIST"]);
                            variables.Add(variable.Name, variable);
                        }
                    }
                }
            }
            return variables;
        }

        public Dictionary<Int32, Device> GetAllValues()
        {
            if (_disposing) throw new ObjectDisposedException("RPC");
            Dictionary<Int32, Device> devices = new Dictionary<Int32, Device>();
            RPCVariable response = _client.CallMethod("getAllValues", new List<RPCVariable> { new RPCVariable(true) });
            if (response.ErrorStruct) ThrowError("getAllValues", response);
            Dictionary<Int32, Family> families = Families;
            foreach (RPCVariable deviceStruct in response.ArrayValue)
            {
                if (!deviceStruct.StructValue.ContainsKey("ID") || !deviceStruct.StructValue.ContainsKey("FAMILY")) continue;
                if (!families.ContainsKey(deviceStruct.StructValue["FAMILY"].IntegerValue)) continue;
                Device device = new Device(this, families[deviceStruct.StructValue["FAMILY"].IntegerValue], deviceStruct.StructValue["ID"].IntegerValue);
                if (deviceStruct.StructValue.ContainsKey("ADDRESS")) device.SerialNumber = deviceStruct.StructValue["ADDRESS"].StringValue;
                if (deviceStruct.StructValue.ContainsKey("TYPE"))
                {
                    device.TypeString = deviceStruct.StructValue["TYPE"].StringValue;
                    if(_asciiDeviceTypeIdString)
                    {
                        device.TypeString = device.TypeString.Replace(' ', '_');
                        System.Text.RegularExpressions.Regex regex = new System.Text.RegularExpressions.Regex("[^a-zA-Z0-9_-]");
                        device.TypeString = regex.Replace(device.TypeString, "");
                    }
                }
                if (deviceStruct.StructValue.ContainsKey("TYPE_ID")) device.TypeID = deviceStruct.StructValue["TYPE_ID"].IntegerValue;
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
                            String typeString = "";
                            if (variableInfo.ContainsKey("TYPE")) typeString = variableInfo["TYPE"].StringValue;
                            RPCVariable value = null;
                            if (variableInfo.ContainsKey("VALUE")) value = variableInfo["VALUE"];
                            else
                            {
                                if(readable || typeString.Length == 0) continue;
                                value = RPCVariable.CreateFromTypeString(typeString);
                                if (value.Type == RPCVariableType.rpcVoid) continue;
                            }
                            Variable variable = new Variable(this, device.ID, channel.Index, parameterSet.ElementAt(i).Key, typeString, value);

                            if (variableInfo.ContainsKey("WRITEABLE")) variable.Writeable = variableInfo["WRITEABLE"].BooleanValue;
                            variable.Readable = readable;
                            if (variableInfo.ContainsKey("MIN")) variable.SetMin(variableInfo["MIN"]);
                            if (variableInfo.ContainsKey("MAX")) variable.SetMax(variableInfo["MAX"]);
                            if (variableInfo.ContainsKey("SPECIAL")) variable.SetSpecialValues(variableInfo["SPECIAL"]);
                            if (variableInfo.ContainsKey("VALUE_LIST")) variable.SetValueList(variableInfo["VALUE_LIST"]);
                            variables.Add(variable.Name, variable);
                        }
                        channel.Variables = new Variables(this, device.ID, channel.Index, variables);
                    }
                    device.Channels = new Channels(this, channels);
                }
                devices.Add(device.ID, device);
            }
            return devices;
        }

        public void GetDeviceDescription(Device device)
        {
            if (_disposing) throw new ObjectDisposedException("RPC");
            RPCVariable response = _client.CallMethod("getDeviceDescription", new List<RPCVariable> { new RPCVariable(device.ID), new RPCVariable(-1) });
            if (response.ErrorStruct) ThrowError("getDeviceDescription", response);
            if (response.StructValue.ContainsKey("FAMILY"))
            {
                if (Families.ContainsKey(response.StructValue["FAMILY"].IntegerValue)) device.Family = Families[response.StructValue["FAMILY"].IntegerValue];
            }
            if (response.StructValue.ContainsKey("ADDRESS")) device.SerialNumber = response.StructValue["ADDRESS"].StringValue;
            if (response.StructValue.ContainsKey("TYPE"))
            {
                device.TypeString = response.StructValue["TYPE"].StringValue;
                if (_asciiDeviceTypeIdString)
                {
                    device.TypeString = device.TypeString.Replace(' ', '_');
                    System.Text.RegularExpressions.Regex regex = new System.Text.RegularExpressions.Regex("[^a-zA-Z0-9_-]");
                    device.TypeString = regex.Replace(device.TypeString, "");
                }
            }
            if (response.StructValue.ContainsKey("TYPE_ID")) device.TypeID = response.StructValue["TYPE_ID"].IntegerValue;
            if (response.StructValue.ContainsKey("PHYSICAL_ADDRESS")) device.Address = response.StructValue["PHYSICAL_ADDRESS"].IntegerValue;
            if (response.StructValue.ContainsKey("RX_MODE")) device.RXMode = (DeviceRXMode)response.StructValue["RX_MODE"].IntegerValue;
            if (response.StructValue.ContainsKey("FIRMWARE")) device.Firmware = response.StructValue["FIRMWARE"].StringValue;
            if (response.StructValue.ContainsKey("AVAILABLE_FIRMWARE")) device.AvailableFirmware = response.StructValue["AVAILABLE_FIRMWARE"].StringValue;
            else if (response.StructValue.ContainsKey("FIRMWARE")) device.AvailableFirmware = response.StructValue["FIRMWARE"].StringValue;
        }

        public void GetDeviceDescription(Channel channel)
        {
            if (_disposing) throw new ObjectDisposedException("RPC");
            RPCVariable response = _client.CallMethod("getDeviceDescription", new List<RPCVariable> { new RPCVariable(channel.PeerID), new RPCVariable(channel.Index) });
            if (response.ErrorStruct) ThrowError("getDeviceDescription", response);
            if (response.StructValue.ContainsKey("TYPE")) channel.TypeString = response.StructValue["TYPE"].StringValue;
            if (response.StructValue.ContainsKey("AES_ACTIVE")) channel.AESActive = (response.StructValue["AES_ACTIVE"].IntegerValue != 0);
            if (response.StructValue.ContainsKey("DIRECTION")) channel.Direction = (ChannelDirection)response.StructValue["DIRECTION"].IntegerValue;
            if (response.StructValue.ContainsKey("LINK_SOURCE_ROLES"))
            {
                String[] temp = response.StructValue["LINK_SOURCE_ROLES"].StringValue.Split(' ');
                if(temp.Length > 0 && temp[0] != "") channel.LinkSourceRoles = temp;
            }
            if (response.StructValue.ContainsKey("LINK_TARGET_ROLES"))
            {
                String[] temp = response.StructValue["LINK_TARGET_ROLES"].StringValue.Split(' ');
                if (temp.Length > 0 && temp[0] != "") channel.LinkTargetRoles = response.StructValue["LINK_TARGET_ROLES"].StringValue.Split(' ');
            }
            if (response.StructValue.ContainsKey("GROUP") && response.StructValue["GROUP"].StringValue.Length > 0)
            {
                String[] temp = response.StructValue["GROUP"].StringValue.Split(':');
                if(temp.Length == 2)
                {
                    Int32 groupChannel = -1;
                    Int32.TryParse(temp[1], out groupChannel);
                    channel.GroupedWith = groupChannel;
                }
            }
            if (response.StructValue.ContainsKey("TEAM")) channel.TeamSerialNumber = response.StructValue["TEAM"].StringValue;
            if (response.StructValue.ContainsKey("TEAM_ID")) channel.TeamID = response.StructValue["TEAM_ID"].IntegerValue;
            if (response.StructValue.ContainsKey("TEAM_CHANNEL")) channel.TeamChannel = response.StructValue["TEAM_CHANNEL"].IntegerValue;
            if (response.StructValue.ContainsKey("TEAM_TAG")) channel.TeamTag = response.StructValue["TEAM_TAG"].StringValue;
            if (response.StructValue.ContainsKey("TEAM_CHANNELS"))
            {
                List<RPCVariable> teamMemberArray = response.StructValue["TEAM_CHANNELS"].ArrayValue;
                String[] teamMembers = new String[teamMemberArray.Count];
                for (Int32 i = 0; i < teamMemberArray.Count; i++)
                {
                    teamMembers[i] = teamMemberArray[i].StringValue;
                }
                channel.TeamMembers = teamMembers;
            }
        }

        public void GetDeviceInfo(Device device)
        {
            if (_disposing) throw new ObjectDisposedException("RPC");
            RPCVariable response = _client.CallMethod("getDeviceInfo", new List<RPCVariable> { new RPCVariable(device.ID) });
            if (response.ErrorStruct) ThrowError("getDeviceInfo", response);
            if (response.StructValue.ContainsKey("NAME")) device.SetNameNoRPC(response.StructValue["NAME"].StringValue);
            if (response.StructValue.ContainsKey("INTERFACE"))
            {
                if(Interfaces.ContainsKey(response.StructValue["INTERFACE"].StringValue))
                {
                    device.SetInterfaceNoRPC(Interfaces[response.StructValue["INTERFACE"].StringValue]);
                }
            }
        }

        public void GetEvent(Event eventToUpdate)
        {
            if (_disposing) throw new ObjectDisposedException("RPC");
            RPCVariable response = _client.CallMethod("getEvent", new List<RPCVariable> { new RPCVariable(eventToUpdate.ID) });
            if (response.ErrorStruct) ThrowError("getEvent", response);
            ParseEvent(response, eventToUpdate);
        }

        public Int32 GetInstallMode()
        {
            if (_disposing) throw new ObjectDisposedException("RPC");
            RPCVariable response = _client.CallMethod("getInstallMode", new List<RPCVariable>());
            if (response.ErrorStruct) ThrowError("getInstallMode", response);
            return response.IntegerValue;
        }

        public Int32 GetInstallMode(Int32 familyID)
        {
            if (_disposing) throw new ObjectDisposedException("RPC");
            RPCVariable response = _client.CallMethod("getInstallMode", new List<RPCVariable> { new RPCVariable(familyID) });
            if (response.ErrorStruct) ThrowError("getInstallMode", response);
            return response.IntegerValue;
        }

        public void GetLinkInfo(Link link)
        {
            if (_disposing) throw new ObjectDisposedException("RPC");
            RPCVariable response = null;
            if (link.IsSender) response = _client.CallMethod("getLinkInfo", new List<RPCVariable> { new RPCVariable(link.PeerID), new RPCVariable(link.Channel), new RPCVariable(link.RemotePeerID), new RPCVariable(link.RemoteChannel) });
            else response = _client.CallMethod("getLinkInfo", new List<RPCVariable> { new RPCVariable(link.RemotePeerID), new RPCVariable(link.RemoteChannel), new RPCVariable(link.PeerID), new RPCVariable(link.Channel) });
            if (response.ErrorStruct) ThrowError("getLinkInfo", response);
            if (response.StructValue.ContainsKey("NAME")) link.SetNameNoRPC(response.StructValue["NAME"].StringValue);
            if (response.StructValue.ContainsKey("DESCRIPTION")) link.SetNameNoRPC(response.StructValue["DESCRIPTION"].StringValue);
        }

        public List<Link> GetLinks()
        {
            return GetLinks(0, -1, 0);
        }

        public List<Link> GetLinks(Int32 peerID)
        {
            return GetLinks(peerID, -1, 0);
        }

        public List<Link> GetLinks(Int32 peerID, Int32 channel)
        {
            return GetLinks(peerID, channel, 0);
        }

        public List<Link> GetLinks(Int32 peerID, Int32 channel, Int32 flags)
        {
            if (_disposing) throw new ObjectDisposedException("RPC");
            RPCVariable response = _client.CallMethod("getLinks", new List<RPCVariable> { new RPCVariable(peerID), new RPCVariable(channel), new RPCVariable(flags) });
            if (response.ErrorStruct) ThrowError("getLinks", response);
            List<Link> links = new List<Link>();
            foreach (RPCVariable rpcLink in response.ArrayValue)
            {
                if (!rpcLink.StructValue.ContainsKey("SENDER_ID") || !rpcLink.StructValue.ContainsKey("SENDER_CHANNEL") || !rpcLink.StructValue.ContainsKey("RECEIVER_ID") || !rpcLink.StructValue.ContainsKey("RECEIVER_CHANNEL")) continue;
                Int32 remotePeerID = 0;
                Int32 remoteChannel = -1;
                bool isSender = true;
                if (rpcLink.StructValue["SENDER_ID"].IntegerValue == peerID && rpcLink.StructValue["SENDER_CHANNEL"].IntegerValue == channel)
                {
                    remotePeerID = rpcLink.StructValue["RECEIVER_ID"].IntegerValue;
                    remoteChannel = rpcLink.StructValue["RECEIVER_CHANNEL"].IntegerValue;
                }
                else
                {
                    isSender = false;
                    remotePeerID = rpcLink.StructValue["SENDER_ID"].IntegerValue;
                    remoteChannel = rpcLink.StructValue["SENDER_CHANNEL"].IntegerValue;
                }
                Link link = new Link(this, peerID, channel, remotePeerID, remoteChannel, isSender);
                if (rpcLink.StructValue.ContainsKey("NAME")) link.SetNameNoRPC(rpcLink.StructValue["NAME"].StringValue);
                if (rpcLink.StructValue.ContainsKey("DESCRIPTION")) link.SetDescriptionNoRPC(rpcLink.StructValue["DESCRIPTION"].StringValue);
                links.Add(link);
            }
            return links;
        }

        public List<ConfigParameter> GetParamset(Int32 peerID, Int32 channel, Int32 remotePeerID, Int32 remoteChannel, DeviceConfig parameters)
        {
            return GetParamset(peerID, channel, remotePeerID, remoteChannel, RPCParameterSetType.rpcLink, parameters);
        }

        public List<ConfigParameter> GetParamset(Int32 peerID, Int32 channel, RPCParameterSetType type, DeviceConfig parameters)
        {
            return GetParamset(peerID, channel, 0, -1, type, parameters);
        }

        private List<ConfigParameter> GetParamset(Int32 peerID, Int32 channel, Int32 remotePeerID, Int32 remoteChannel, RPCParameterSetType type, DeviceConfig parameters)
        {
            if (_disposing) throw new ObjectDisposedException("RPC");
            RPCVariable response = null;
            if (type == RPCParameterSetType.rpcLink)
            {
                response = _client.CallMethod("getParamset", new List<RPCVariable> { new RPCVariable(peerID), new RPCVariable(channel), new RPCVariable(remotePeerID), new RPCVariable(remoteChannel) });
            }
            else
            {
                response = _client.CallMethod("getParamset", new List<RPCVariable> { new RPCVariable(peerID), new RPCVariable(channel), new RPCVariable("MASTER") });
            }
            if (response.ErrorStruct) ThrowError("getParamset", response);
            List<ConfigParameter> changedParameters = new List<ConfigParameter>();
            foreach (KeyValuePair<String, RPCVariable> value in response.StructValue)
            {
                if (!parameters.ContainsKey(value.Key)) continue;
                if (parameters[value.Key].SetValue(value.Value)) changedParameters.Add(parameters[value.Key]);
            }
            return changedParameters;
        }

        public Dictionary<String, ConfigParameter> GetParamsetDescription(Int32 peerID, Int32 channel, RPCParameterSetType type)
        {
            if (_disposing) throw new ObjectDisposedException("RPC");
            Dictionary<String, ConfigParameter> parameters = new Dictionary<String, ConfigParameter>();
            String typeString = "MASTER";
            if (type == RPCParameterSetType.rpcLink) typeString = "LINK";
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

        public MetadataVariable GetMetadata(Int32 peerID, String name)
        {
            if (_disposing) throw new ObjectDisposedException("RPC");
            RPCVariable response = _client.CallMethod("getMetadata", new List<RPCVariable> { new RPCVariable(peerID), new RPCVariable(name) });
            if (response.ErrorStruct) ThrowError("getMetadata", response);
            return new MetadataVariable(this, peerID, name, response);
        }

        public List<ServiceMessage> GetServiceMessages()
        {
            if (_disposing) throw new ObjectDisposedException("RPC");
            List<ServiceMessage> messages = new List<ServiceMessage>();
            RPCVariable response = _client.CallMethod("getServiceMessages", new List<RPCVariable> { new RPCVariable(true) });
            if (response.ErrorStruct) ThrowError("getServiceMessages", response);
            foreach (RPCVariable element in response.ArrayValue)
            {
                if(element.Type != RPCVariableType.rpcArray || element.ArrayValue.Count != 4 || element.ArrayValue[0].Type != RPCVariableType.rpcInteger || element.ArrayValue[1].Type != RPCVariableType.rpcInteger || element.ArrayValue[2].Type != RPCVariableType.rpcString) continue;
                ServiceMessage message = new ServiceMessage(element.ArrayValue[0].IntegerValue, element.ArrayValue[1].IntegerValue, element.ArrayValue[2].StringValue);
                if(element.ArrayValue[3].Type == RPCVariableType.rpcBoolean) message.Value = element.ArrayValue[3].BooleanValue ? 1 : 0;
                else message.Value = element.ArrayValue[3].IntegerValue;
                messages.Add(message);
            }
            return messages;
        }

        public UpdateStatus GetUpdateStatus()
        {
            if (_disposing) throw new ObjectDisposedException("RPC");
            RPCVariable response = _client.CallMethod("getUpdateStatus", null);
            if (response.ErrorStruct) ThrowError("getUpdateStatus", response);
            Int32 currentDevice = -1;
            Int32 currentDeviceProgress = -1;
            Int32 deviceCount = -1;
            Int32 currentUpdate = 0;
            Dictionary<Int32, UpdateResult> results = new Dictionary<Int32, UpdateResult>();
            if (response.StructValue.ContainsKey("CURRENT_DEVICE")) currentDevice = response.StructValue["CURRENT_DEVICE"].IntegerValue;
            if (response.StructValue.ContainsKey("CURRENT_DEVICE_PROGRESS")) currentDeviceProgress = response.StructValue["CURRENT_DEVICE_PROGRESS"].IntegerValue;
            if (response.StructValue.ContainsKey("DEVICE_COUNT")) deviceCount = response.StructValue["DEVICE_COUNT"].IntegerValue;
            if (response.StructValue.ContainsKey("CURRENT_UPDATE")) currentUpdate = response.StructValue["CURRENT_UPDATE"].IntegerValue;
            if (response.StructValue.ContainsKey("RESULTS"))
            {
                foreach(KeyValuePair<String, RPCVariable> devicePair in response.StructValue["RESULTS"].StructValue)
                {
                    Int32 peerID = 0;
                    if (!Int32.TryParse(devicePair.Key, out peerID)) continue;
                    if (!devicePair.Value.StructValue.ContainsKey("RESULT_CODE") || !devicePair.Value.StructValue.ContainsKey("RESULT_STRING")) continue;
                    UpdateResult result = new UpdateResult((UpdateResultCode)devicePair.Value.StructValue["RESULT_CODE"].IntegerValue, devicePair.Value.StructValue["RESULT_STRING"].StringValue);
                    results.Add(peerID, result);
                }
            }
            return new UpdateStatus(currentDevice, currentDeviceProgress, deviceCount, currentUpdate, results);
        }

        public String GetVersion()
        {
            if (_disposing) throw new ObjectDisposedException("RPC");
            RPCVariable response = _client.CallMethod("getVersion", new List<RPCVariable> ());
            if (response.ErrorStruct) ThrowError("getVersion", response);
            return response.StringValue;
        }

        public SystemVariable GetSystemVariable(String name)
        {
            if (_disposing) throw new ObjectDisposedException("RPC");
            RPCVariable response = _client.CallMethod("getSystemVariable", new List<RPCVariable> { new RPCVariable(name) });
            if (response.ErrorStruct) ThrowError("getSystemVariable", response);
            return new SystemVariable(this, name, response);
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

        public Event ParseEvent(RPCVariable eventStruct, Event eventToUpdate = null)
        {
            if (!eventStruct.StructValue.ContainsKey("TYPE") || !eventStruct.StructValue.ContainsKey("ID")) return null;
            EventType type = (EventType)eventStruct.StructValue["TYPE"].IntegerValue;
            if (type == EventType.Timed)
            {
                TimedEvent element = (eventToUpdate != null && eventToUpdate is TimedEvent) ? (TimedEvent)eventToUpdate : new TimedEvent(this, eventStruct.StructValue["ID"].StringValue);
                if (eventStruct.StructValue.ContainsKey("ENABLED")) element.SetEnabledNoRPC(eventStruct.StructValue["ENABLED"].BooleanValue);
                if (eventStruct.StructValue.ContainsKey("EVENTTIME")) element.EventTime = HomegearHelpers.UnixTimeStampToDateTime(eventStruct.StructValue["EVENTTIME"].IntegerValue);
                if (eventStruct.StructValue.ContainsKey("RECUREVERY")) element.RecurEvery = eventStruct.StructValue["RECUREVERY"].IntegerValue;
                if (eventStruct.StructValue.ContainsKey("ENDTIME")) element.EndTime = HomegearHelpers.UnixTimeStampToDateTime(eventStruct.StructValue["ENDTIME"].IntegerValue);
                if (eventStruct.StructValue.ContainsKey("EVENTMETHOD")) element.EventMethod = eventStruct.StructValue["EVENTMETHOD"].StringValue;
                if (eventStruct.StructValue.ContainsKey("EVENTMETHODPARAMS")) element.SetEventMethodParams(eventStruct.StructValue["EVENTMETHODPARAMS"].ArrayValue);
                return element;
            }
            else
            {
                TriggeredEvent element = (eventToUpdate != null && eventToUpdate is TriggeredEvent) ? (TriggeredEvent)eventToUpdate : new TriggeredEvent(this, eventStruct.StructValue["ID"].StringValue);
                if (eventStruct.StructValue.ContainsKey("ENABLED")) element.SetEnabledNoRPC(eventStruct.StructValue["ENABLED"].BooleanValue);
                if (eventStruct.StructValue.ContainsKey("PEERID")) element.PeerID = eventStruct.StructValue["PEERID"].IntegerValue;
                if (eventStruct.StructValue.ContainsKey("PEERCHANNEL")) element.PeerChannel = eventStruct.StructValue["PEERCHANNEL"].IntegerValue;
                if (eventStruct.StructValue.ContainsKey("VARIABLE")) element.VariableName = eventStruct.StructValue["VARIABLE"].StringValue;
                if (eventStruct.StructValue.ContainsKey("TRIGGER")) element.Trigger = (EventTrigger)eventStruct.StructValue["TRIGGER"].IntegerValue;
                if (eventStruct.StructValue.ContainsKey("TRIGGERVALUE")) element.TriggerValue = eventStruct.StructValue["TRIGGERVALUE"];
                if (eventStruct.StructValue.ContainsKey("EVENTMETHOD")) element.EventMethod = eventStruct.StructValue["EVENTMETHOD"].StringValue;
                if (eventStruct.StructValue.ContainsKey("EVENTMETHODPARAMS")) element.SetEventMethodParams(eventStruct.StructValue["EVENTMETHODPARAMS"].ArrayValue);
                if (eventStruct.StructValue.ContainsKey("RESETAFTER"))
                {
                    if (eventStruct.StructValue["RESETAFTER"].Type == RPCVariableType.rpcInteger) element.ResetAfterStatic = eventStruct.StructValue["RESETAFTER"].IntegerValue;
                    else
                    {
                        Dictionary<String, RPCVariable> resetStruct = eventStruct.StructValue["RESETAFTER"].StructValue;
                        element.ResetAfterDynamic = new DynamicResetTime();
                        if (resetStruct.ContainsKey("INITIALTIME")) element.ResetAfterDynamic.InitialTime = resetStruct["INITIALTIME"].IntegerValue;
                        if (resetStruct.ContainsKey("RESETAFTER")) element.ResetAfterDynamic.ResetAfter = resetStruct["RESETAFTER"].IntegerValue;
                        if (resetStruct.ContainsKey("OPERATION")) element.ResetAfterDynamic.Operation = (DynamicResetTimeOperation)resetStruct["OPERATION"].IntegerValue;
                        if (resetStruct.ContainsKey("FACTOR")) element.ResetAfterDynamic.Factor = resetStruct["FACTOR"].FloatValue;
                        if (resetStruct.ContainsKey("LIMIT")) element.ResetAfterDynamic.Limit = resetStruct["LIMIT"].IntegerValue;
                        if (resetStruct.ContainsKey("CURRENTTIME")) element.ResetAfterDynamic.CurrentTime = resetStruct["CURRENTTIME"].IntegerValue;
                    }
                }
                if (eventStruct.StructValue.ContainsKey("RESETMETHOD")) element.ResetMethod = eventStruct.StructValue["RESETMETHOD"].StringValue;
                if (eventStruct.StructValue.ContainsKey("RESETMETHODPARAMS")) element.SetResetMethodParams(eventStruct.StructValue["RESETMETHODPARAMS"].ArrayValue);
                if (eventStruct.StructValue.ContainsKey("LASTVALUE")) element.LastValue = eventStruct.StructValue["LASTVALUE"];
                if (eventStruct.StructValue.ContainsKey("LASTRAISED")) element.LastRaised = HomegearHelpers.UnixTimeStampToDateTime(eventStruct.StructValue["LASTRAISED"].IntegerValue);
                if (eventStruct.StructValue.ContainsKey("LASTRESET")) element.LastReset = HomegearHelpers.UnixTimeStampToDateTime(eventStruct.StructValue["LASTRESET"].IntegerValue);
                return element;
            }
        }

        public Dictionary<String, Event> ListEvents(EventType type)
        {
            if (_disposing) throw new ObjectDisposedException("RPC");
            Dictionary<String, Event> events = new Dictionary<String, Event>();
            RPCVariable response = _client.CallMethod("listEvents", new List<RPCVariable> { new RPCVariable((Int32)type) });
            if (response.ErrorStruct) ThrowError("listEvents", response);
            foreach (RPCVariable eventStruct in response.ArrayValue)
            {
                Event element = ParseEvent(eventStruct);
                if (element != null) events.Add(element.ID, element);   
            }
            return events;
        }

        public Dictionary<String, Event> ListEvents(Int32 id)
        {
            return ListEvents(id, -1);
        }

        public Dictionary<String, Event> ListEvents(Int32 id, Int32 channel)
        {
            if (_disposing) throw new ObjectDisposedException("RPC");
            Dictionary<String, Event> events = new Dictionary<String, Event>();
            RPCVariable response = _client.CallMethod("listEvents", new List<RPCVariable> { new RPCVariable(id), new RPCVariable(channel) });
            if (response.ErrorStruct) ThrowError("listEvents", response);
            foreach (RPCVariable eventStruct in response.ArrayValue)
            {
                Event element = ParseEvent(eventStruct);
                if (element != null) events.Add(element.ID, element);
            }
            return events;
        }

        public Dictionary<String, Event> ListEvents(Int32 id, Int32 channel, String variableName)
        {
            if (_disposing) throw new ObjectDisposedException("RPC");
            Dictionary<String, Event> events = new Dictionary<String, Event>();
            RPCVariable response = _client.CallMethod("listEvents", new List<RPCVariable> { new RPCVariable(id), new RPCVariable(channel), new RPCVariable(variableName) });
            if (response.ErrorStruct) ThrowError("listEvents", response);
            foreach (RPCVariable eventStruct in response.ArrayValue)
            {
                Event element = ParseEvent(eventStruct);
                if (element != null) events.Add(element.ID, element);
            }
            return events;
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
                Family family = new Family(familyStruct.StructValue["ID"].IntegerValue, familyStruct.StructValue["NAME"].StringValue);
                if (familyStruct.StructValue.ContainsKey("PAIRING_METHODS"))
                {
                    List<String> pairingMethods = new List<String>();
                    foreach(RPCVariable pairingMethod in familyStruct.StructValue["PAIRING_METHODS"].ArrayValue)
                    {
                        if (pairingMethod.StringValue.Length > 0) pairingMethods.Add(pairingMethod.StringValue);
                    }
                    family.SetPairingMethods(pairingMethods.AsReadOnly());
                }
                families.Add(familyStruct.StructValue["ID"].IntegerValue, family);
            }
            return families;
        }

        public Dictionary<String, Interface> ListInterfaces()
        {
            if (_disposing) throw new ObjectDisposedException("RPC");
            Dictionary<String, Interface> interfaces = new Dictionary<String, Interface>();
            RPCVariable response = _client.CallMethod("listInterfaces", null);
            if (response.ErrorStruct) ThrowError("listInterfaces", response);
            Dictionary<Int32, Family> families = Families;
            foreach (RPCVariable interfaceStruct in response.ArrayValue)
            {
                if (!interfaceStruct.StructValue.ContainsKey("ID") || !interfaceStruct.StructValue.ContainsKey("TYPE") || !interfaceStruct.StructValue.ContainsKey("FAMILYID")) continue;
                Int32 familyID = interfaceStruct.StructValue["FAMILYID"].IntegerValue;
                if (!families.ContainsKey(familyID)) continue;
                Interface physicalInterface = new Interface(families[familyID], interfaceStruct.StructValue["ID"].StringValue, interfaceStruct.StructValue["TYPE"].StringValue);
                if (interfaceStruct.StructValue.ContainsKey("CONNECTED")) physicalInterface.Connected = interfaceStruct.StructValue["CONNECTED"].BooleanValue;
                if (interfaceStruct.StructValue.ContainsKey("DEFAULT")) physicalInterface.Default = interfaceStruct.StructValue["DEFAULT"].BooleanValue;
                if (interfaceStruct.StructValue.ContainsKey("PHYSICALADDRESS")) physicalInterface.PhysicalAddress = interfaceStruct.StructValue["PHYSICALADDRESS"].IntegerValue;
                if (interfaceStruct.StructValue.ContainsKey("LASTPACKETRECEIVED")) physicalInterface.LastPacketReceived = interfaceStruct.StructValue["LASTPACKETRECEIVED"].IntegerValue;
                if (interfaceStruct.StructValue.ContainsKey("LASTPACKETSENT")) physicalInterface.LastPacketSent = interfaceStruct.StructValue["LASTPACKETSENT"].IntegerValue;
                interfaces.Add(physicalInterface.ID, physicalInterface);
            }
            return interfaces;
        }

        public Int32 LogLevel()
        {
            if (_disposing) throw new ObjectDisposedException("RPC");
            RPCVariable response = _client.CallMethod("logLevel", new List<RPCVariable>());
            if (response.ErrorStruct) ThrowError("logLevel", response);
            return response.IntegerValue;
        }

        public Int32 LogLevel(Int32 newLevel)
        {
            if (_disposing) throw new ObjectDisposedException("RPC");
            RPCVariable response = _client.CallMethod("logLevel", new List<RPCVariable> { new RPCVariable(newLevel) });
            if (response.ErrorStruct) ThrowError("logLevel", response);
            return response.IntegerValue;
        }

        public void PutParamset(Int32 peerID, Int32 channel, RPCParameterSetType type, Dictionary<String, ConfigParameter> parameters)
        {
            PutParamset(peerID, channel, 0, -1, type, parameters);
        }

        public void PutParamset(Int32 peerID, Int32 channel, Int32 remotePeerID, Int32 remoteChannel, Dictionary<String, ConfigParameter> parameters)
        {
            PutParamset(peerID, channel, remotePeerID, remoteChannel, RPCParameterSetType.rpcLink, parameters);
        }

        private void PutParamset(Int32 peerID, Int32 channel, Int32 remotePeerID, Int32 remoteChannel, RPCParameterSetType type, Dictionary<String, ConfigParameter> parameters)
        {
            if (_disposing) throw new ObjectDisposedException("RPC");
            if (parameters.Count == 0) return;
            String typeString = "MASTER";
            if (type == RPCParameterSetType.rpcValues) typeString = "VALUES";
            RPCVariable rpcParameters = new RPCVariable(RPCVariableType.rpcStruct);
            foreach (KeyValuePair<String, ConfigParameter> parameter in parameters)
            {
                rpcParameters.StructValue.Add(parameter.Key, new RPCVariable(parameter.Value));
            }
            RPCVariable response = null;
            if (type == RPCParameterSetType.rpcLink)
            {
                response = _client.CallMethod("putParamset", new List<RPCVariable> { new RPCVariable(peerID), new RPCVariable(channel), new RPCVariable(remotePeerID), new RPCVariable(remoteChannel), rpcParameters });
            }
            else
            {
                response = _client.CallMethod("putParamset", new List<RPCVariable> { new RPCVariable(peerID), new RPCVariable(channel), new RPCVariable(typeString), rpcParameters });
            }
            if (response.ErrorStruct) ThrowError("putParamset", response);
        }

        public void RemoveEvent(String id)
        {
            if (_disposing) throw new ObjectDisposedException("RPC");
            RPCVariable response = _client.CallMethod("removeEvent", new List<RPCVariable> { new RPCVariable(id) });
            if (response.ErrorStruct) ThrowError("removeEvent", response);
        }

        public void RemoveLink(Link link)
        {
            if (_disposing) throw new ObjectDisposedException("RPC");
            RPCVariable response = null;
            if (link.IsSender)
            {
                response = _client.CallMethod("removeLink", new List<RPCVariable> { new RPCVariable(link.PeerID), new RPCVariable(link.Channel), new RPCVariable(link.RemotePeerID), new RPCVariable(link.RemoteChannel) });
            }
            else
            {
                response = _client.CallMethod("removeLink", new List<RPCVariable> { new RPCVariable(link.RemotePeerID), new RPCVariable(link.RemoteChannel), new RPCVariable(link.PeerID), new RPCVariable(link.Channel) });
            }
            if (response.ErrorStruct) ThrowError("removeLink", response);
        }

        public Int32 RunScript(String filename)
        {
            if (_disposing) throw new ObjectDisposedException("RPC");
            RPCVariable response = _client.CallMethod("runScript", new List<RPCVariable> { new RPCVariable(filename) });
            if (response.ErrorStruct) ThrowError("runScript", response);
            return response.IntegerValue;
        }

        public Int32 RunScript(String filename, Boolean wait)
        {
            if (_disposing) throw new ObjectDisposedException("RPC");
            RPCVariable response = _client.CallMethod("runScript", new List<RPCVariable> { new RPCVariable(filename), new RPCVariable(wait) });
            if (response.ErrorStruct) ThrowError("runScript", response);
            return response.IntegerValue;
        }

        public Int32 RunScript(String filename, String arguments)
        {
            if (_disposing) throw new ObjectDisposedException("RPC");
            RPCVariable response = _client.CallMethod("runScript", new List<RPCVariable> { new RPCVariable(filename), new RPCVariable(arguments) });
            if (response.ErrorStruct) ThrowError("runScript", response);
            return response.IntegerValue;
        }

        public Int32 RunScript(String filename, String arguments, Boolean wait)
        {
            if (_disposing) throw new ObjectDisposedException("RPC");
            RPCVariable response = _client.CallMethod("runScript", new List<RPCVariable> { new RPCVariable(filename), new RPCVariable(arguments), new RPCVariable(wait) });
            if (response.ErrorStruct) ThrowError("runScript", response);
            return response.IntegerValue;
        }

        public Int32 SearchDevices()
        {
            if (_disposing) throw new ObjectDisposedException("RPC");
            RPCVariable response = _client.CallMethod("searchDevices", new List<RPCVariable>());
            if (response.ErrorStruct) ThrowError("searchDevices", response);
            return response.IntegerValue;
        }

        public Int32 SearchDevices(Int32 familyID)
        {
            if (_disposing) throw new ObjectDisposedException("RPC");
            RPCVariable response = _client.CallMethod("searchDevices", new List<RPCVariable> { new RPCVariable(familyID) });
            if (response.ErrorStruct) ThrowError("searchDevices", response);
            return response.IntegerValue;
        }

        public void SetInstallMode(bool value)
        {
            SetInstallMode(value, 60);
        }

        public void SetInstallMode(bool value, Int32 duration)
        {
            if (_disposing) throw new ObjectDisposedException("RPC");
            RPCVariable response = null;
            if(value) response = _client.CallMethod("setInstallMode", new List<RPCVariable> { new RPCVariable(value), new RPCVariable(duration) });
            else response = _client.CallMethod("setInstallMode", new List<RPCVariable> { new RPCVariable(value) });
            if (response.ErrorStruct) ThrowError("setInstallMode", response);
        }

        public void SetInstallMode(Int32 familyID, bool value)
        {
            SetInstallMode(familyID, value, 60);
        }

        public void SetInstallMode(Int32 familyID, bool value, Int32 duration)
        {
            if (_disposing) throw new ObjectDisposedException("RPC");
            RPCVariable response = _client.CallMethod("setInstallMode", new List<RPCVariable> { new RPCVariable(familyID), new RPCVariable(value), new RPCVariable(duration) });
            if (response.ErrorStruct) ThrowError("setInstallMode", response);
        }

        public void SetId(Int32 currentPeerID, Int32 newPeerId)
        {
            if (_disposing) throw new ObjectDisposedException("RPC");
            RPCVariable response = _client.CallMethod("setId", new List<RPCVariable> { new RPCVariable(currentPeerID), new RPCVariable(newPeerId) });
            if (response.ErrorStruct) ThrowError("setId", response);
        }

        public void SetInterface(Int32 peerID, Interface physicalInterface)
        {
            if (_disposing) throw new ObjectDisposedException("RPC");
            RPCVariable response;
            if (physicalInterface == null) response = _client.CallMethod("setInterface", new List<RPCVariable> { new RPCVariable(peerID), new RPCVariable("") });
            else response = _client.CallMethod("setInterface", new List<RPCVariable> { new RPCVariable(peerID), new RPCVariable(physicalInterface.ID) });
            if (response.ErrorStruct) ThrowError("setInterface", response);
        }

        public void SetName(Int32 peerID, String name)
        {
            if (_disposing) throw new ObjectDisposedException("RPC");
            RPCVariable response = _client.CallMethod("setName", new List<RPCVariable> { new RPCVariable(peerID), new RPCVariable(name) });
            if (response.ErrorStruct) ThrowError("setName", response);
        }

        public void SetLinkInfo(Link link)
        {
            if (_disposing) throw new ObjectDisposedException("RPC");
            RPCVariable response = null;
            if (link.IsSender) response = _client.CallMethod("setLinkInfo", new List<RPCVariable> { new RPCVariable(link.PeerID), new RPCVariable(link.Channel), new RPCVariable(link.RemotePeerID), new RPCVariable(link.RemoteChannel), new RPCVariable(link.Name), new RPCVariable(link.Description) });
            else response = _client.CallMethod("setLinkInfo", new List<RPCVariable> { new RPCVariable(link.RemotePeerID), new RPCVariable(link.RemoteChannel), new RPCVariable(link.PeerID), new RPCVariable(link.Channel), new RPCVariable(link.Name), new RPCVariable(link.Description) });
            if (response.ErrorStruct) ThrowError("setLinkInfo", response);
        }

        public void SetMetadata(MetadataVariable variable)
        {
            if (_disposing) throw new ObjectDisposedException("RPC");
            RPCVariable response = _client.CallMethod("setMetadata", new List<RPCVariable> { new RPCVariable(variable.PeerID), new RPCVariable(variable.Name), variable });
            if (response.ErrorStruct) ThrowError("setMetadata", response);
        }

        public void SetSystemVariable(SystemVariable variable)
        {
            if (_disposing) throw new ObjectDisposedException("RPC");
            RPCVariable response = _client.CallMethod("setSystemVariable", new List<RPCVariable> { new RPCVariable(variable.Name), variable });
            if (response.ErrorStruct) ThrowError("setSystemVariable", response);
        }

        public void SetTeam(Int32 peerID, Int32 peerChannel)
        {
            SetTeam(peerID, peerChannel, 0, -1);
        }

        public void SetTeam(Int32 peerID, Int32 peerChannel, Int32 teamID, Int32 teamChannel)
        {
            if (_disposing) throw new ObjectDisposedException("RPC");
            RPCVariable response = _client.CallMethod("setTeam", new List<RPCVariable> { new RPCVariable(peerID), new RPCVariable(peerChannel), new RPCVariable(teamID), new RPCVariable(teamChannel) });
            if (response.ErrorStruct) ThrowError("setTeam", response);
        }

        public void SetValue(Variable variable)
        {
            if (_disposing) throw new ObjectDisposedException("RPC");
            RPCVariable response = _client.CallMethod("setValue", new List<RPCVariable> { new RPCVariable(variable.PeerID), new RPCVariable(variable.Channel), new RPCVariable(variable.Name), new RPCVariable(variable), new RPCVariable(variable.SetValueWait) });
            if (response.ErrorStruct) ThrowError("setValue", response);
        }

        public void TriggerEvent(String id)
        {
            if (_disposing) throw new ObjectDisposedException("RPC");
            RPCVariable response = _client.CallMethod("triggerEvent", new List<RPCVariable> { new RPCVariable(id) });
            if (response.ErrorStruct) ThrowError("triggerEvent", response);
        }

        public void UpdateFirmware(Device device, bool manually)
        {
            if (_disposing) throw new ObjectDisposedException("RPC");
            RPCVariable response = _client.CallMethod("updateFirmware", new List<RPCVariable> { new RPCVariable(device.ID), new RPCVariable(manually) });
            if (response.ErrorStruct) ThrowError("updateFirmware", response);
        }
        #endregion
    }
}
