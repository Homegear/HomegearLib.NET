using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Linq.Expressions;
using System.Security.Authentication;

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
        internal delegate void DeviceVariableUpdatedEventHandler(RPCController sender, Variable value, string eventSource);
        internal delegate void SystemVariableUpdatedEventHandler(RPCController sender, SystemVariable value);
        internal delegate void PongEventHandler(RPCController sender, string id);
        internal delegate void SystemVariableDeletedEventHandler(RPCController sender);
        internal delegate void MetadataUpdatedEventHandler(RPCController sender, long peerId, MetadataVariable value);
        internal delegate void MetadataDeletedEventHandler(RPCController sender, long peerId);
        internal delegate void NewDevicesEventHandler(RPCController sender);
        internal delegate void DevicesDeletedEventHandler(RPCController sender);
        internal delegate void UpdateDeviceEventHandler(RPCController sender, long peerId, long channel, RPCUpdateDeviceFlags flags);
        internal delegate void NewEventEventHandler(RPCController sender, string id, EventType type, long peerId, long channel, string variableName);
        internal delegate void EventDeletedEventHandler(RPCController sender, string id, EventType type, long peerId, long channel, string variableName);
        internal delegate void UpdateEventEventHandler(RPCController sender, string id, EventType type, long peerId, long channel, string variableName);
        internal delegate void RequestUiRefreshEventHandler(RPCController sender, string id);
        internal delegate void InitCompletedEventHandler(RPCController sender);
        internal delegate void HomegearErrorEventHandler(RPCController sender, long level, string message);
        public delegate void ConnectedEventHandler(RPCClient sender, CipherAlgorithmType cipherAlgorithm = CipherAlgorithmType.Null, int cipherStrength = -1);
        public delegate void DisconnectedEventHandler(RPCClient sender);

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
        internal event RequestUiRefreshEventHandler RequestUiRefreshEvent;
        internal event InitCompletedEventHandler InitCompleted;
        internal event HomegearErrorEventHandler HomegearError;

        /// <summary>
        /// Raised, when there is a successful incoming connection from Homegear to the library's callback event server.
        /// </summary>
        public event ConnectedEventHandler Connected;

        /// <summary>
        /// Raised, when the incoming connection to our event server is closed.
        /// </summary>
        public event DisconnectedEventHandler Disconnected;
        #endregion


        private volatile bool _disposing = false;
        private volatile bool _events = false;

        /// <summary>
        /// Returns "true" when the RPC controller is connected to Homegear.
        /// </summary>
        public bool IsConnected { get { return _client != null && _client.IsConnected; } }

        /// <summary>
        /// When set to "true" events from Homegear caused by setting a value through the library are ignored.
        /// </summary>
        public bool IgnoreEventsFromMyself { get; set; } = false;

        private Dictionary<long, Family> _families = null;
        internal Dictionary<long, Family> Families
        {
            get
            {
                if (_families == null || _families.Count == 0)
                {
                    _families = ListFamilies();
                }

                return _families;
            }
        }

        private Dictionary<long, Device> _devices = null;
        internal Dictionary<long, Device> Devices
        {
            get
            {
                if (_devices == null || _devices.Count == 0)
                {
                    _devices = GetAllValues();
                }

                return _devices;
            }
        }

        private Dictionary<string, Interface> _interfaces = null;
        internal Dictionary<string, Interface> Interfaces
        {
            get
            {
                if (_interfaces == null || _interfaces.Count == 0)
                {
                    _interfaces = ListInterfaces();
                }

                return _interfaces;
            }
            set
            {
                _interfaces = value;
            }
        }

        private Dictionary<ulong, Building> _buildings = null;
        internal Dictionary<ulong, Building> Buildings
        {
            get
            {
                if (_buildings == null || _buildings.Count == 0)
                {
                    _buildings = GetBuildings();
                }

                return _buildings;
            }
            set
            {
                _buildings = value;
            }
        }

        private Dictionary<ulong, Story> _stories = null;
        internal Dictionary<ulong, Story> Stories
        {
            get
            {
                if (_stories == null || _stories.Count == 0)
                {
                    _stories = GetStories();
                }

                return _stories;
            }
            set
            {
                _stories = value;
            }
        }

        private Dictionary<ulong, Room> _rooms = null;
        internal Dictionary<ulong, Room> Rooms
        {
            get
            {
                if (_rooms == null || _rooms.Count == 0)
                {
                    _rooms = GetRooms();
                }

                return _rooms;
            }
            set
            {
                _rooms = value;
            }
        }

        private Dictionary<ulong, Role> _roles = null;
        internal Dictionary<ulong, Role> Roles
        {
            get
            {
                if (_roles == null || _roles.Count == 0)
                {
                    _roles = GetRoles();
                }

                return _roles;
            }
            set
            {
                _roles = value;
            }
        }

        private Dictionary<string, SystemVariable> _systemVariables = null;
        internal Dictionary<string, SystemVariable> SystemVariables
        {
            get
            {
                if (_systemVariables == null || _systemVariables.Count == 0)
                {
                    _systemVariables = GetAllSystemVariables();
                }

                return _systemVariables;
            }
            set
            {
                _systemVariables = value;
            }
        }

        private SslInfo _sslClientInfo;

        private readonly string _clientId = "HomegearLib." + System.Guid.NewGuid().ToString();

        private RPCClient _client = null;
        public RPCClient Client { get { return _client; } }

        private bool _asciiDeviceTypeIdString = false;
        public bool AsciiDeviceTypeIdString { get { return _asciiDeviceTypeIdString; } set { _asciiDeviceTypeIdString = value; } }

        private System.Timers.Timer _keepAliveTimer;

        /// <summary>
        /// Creates a new RPCController object with event support.
        /// </summary>
        /// <param name="homegearHostname">The hostname or IP address of the Homegear server to connect to.</param>
        /// <param name="homegearPort">The port Homegear is listening on.</param>
        /// <param name="sslClientInfo">When a SSLClientInfo object is passed, the connection to Homegear will be SSL encrypted.</param>
        public RPCController(string homegearHostname, int homegearPort, SslInfo sslClientInfo = null)
        {
            init(homegearHostname, homegearPort, sslClientInfo);
        }

        void init(string homegearHostname, int homegearPort, SslInfo sslClientInfo)
        {
            _sslClientInfo = sslClientInfo;
            _client = new RPCClient(homegearHostname, homegearPort, sslClientInfo);
            _client.Connected += _client_Connected;
            _client.Disconnected += _client_Disconnected;
            _client.HomegearError += _client_HomegearError;
            _client.RPCEvent += _client_OnRPCEvent;
            _client.NewDevices += _client_OnNewDevices;
            _client.DevicesDeleted += _client_OnDevicesDeleted;
            _client.UpdateDevice += _client_OnUpdateDevice;
            _client.NewEvent += _client_OnNewEvent;
            _client.EventDeleted += _client_OnEventDeleted;
            _client.UpdateEvent += _client_OnUpdateEvent;
            _client.RequestUiRefreshEvent += _client_RequestUiRefreshEvent;
            _keepAliveTimer = new System.Timers.Timer(30000);
            _keepAliveTimer.Elapsed += _workerTimer_Elapsed;
        }

        private void _client_HomegearError(RPCClient sender, long level, string message)
        {
            HomegearError?.Invoke(this, level, message);
        }

        private void _client_OnUpdateDevice(RPCClient sender, long peerId, long channel, long flags)
        {
            UpdateDevice?.Invoke(this, peerId, channel, (RPCUpdateDeviceFlags)flags);
        }

        private void _client_OnDevicesDeleted(RPCClient sender)
        {
            DevicesDeleted?.Invoke(this);
        }

        private void _client_OnNewDevices(RPCClient sender)
        {
            NewDevices?.Invoke(this);
        }

        private void _client_OnUpdateEvent(RPCClient sender, string id, long eventType, long peerId, long channel, string variable)
        {
            UpdateEvent?.Invoke(this, id, (EventType)eventType, peerId, channel, variable);
        }

        private void _client_OnEventDeleted(RPCClient sender, string id, long eventType, long peerId, long channel, string variable)
        {
            EventDeleted?.Invoke(this, id, (EventType)eventType, peerId, channel, variable);
        }

        private void _client_OnNewEvent(RPCClient sender, string id, long eventType, long peerId, long channel, string variable)
        {
            NewEvent?.Invoke(this, id, (EventType)eventType, peerId, channel, variable);
        }

        private void _client_RequestUiRefreshEvent(RPCClient sender, string id)
        {
            RequestUiRefreshEvent?.Invoke(this, id);
        }

        private void _workerTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            try
            {
                _keepAliveTimer.Interval = 10000;
                if (_client.CallMethod("logLevel", new List<RPCVariable>()).ErrorStruct)
                {
                    _client.Disconnect();
                }
            }
            catch (Exception)
            {
                _client.Disconnect();
            }
        }

        private void _client_OnRPCEvent(RPCClient sender, long peerId, long channel, string parameterName, RPCVariable value, string eventSource)
        {
            if (IgnoreEventsFromMyself && eventSource == _clientId) return;

            if (peerId == 0)
            {
                if (value.Type == RPCVariableType.rpcStruct && value.StructValue.Count == 2 && value.StructValue.ContainsKey("CODE") && value.StructValue["CODE"].IntegerValue == 1 && value.StructValue.ContainsKey("TYPE") && value.StructValue["TYPE"].IntegerValue == 0)
                {
                    SystemVariableDeleted?.Invoke(this);
                }
                else if (parameterName == "PONG")
                {
                    Pong?.Invoke(this, value.StringValue);
                }
                else
                {
                    SystemVariableUpdated?.Invoke(this, new SystemVariable(null, parameterName, value));
                }
            }
            else if (channel == -1)
            {
                if (value.Type == RPCVariableType.rpcStruct && value.StructValue.Count == 2 && value.StructValue.ContainsKey("CODE") && value.StructValue["CODE"].IntegerValue == 1 && value.StructValue.ContainsKey("TYPE") && value.StructValue["TYPE"].IntegerValue == 1)
                {
                    MetadataDeleted?.Invoke(this, peerId);
                }
                else
                {
                    MetadataUpdated?.Invoke(this, peerId, new MetadataVariable(null, peerId, parameterName, value));
                }
            }
            else
            {
                DeviceVariableUpdated?.Invoke(this, new Variable(peerId, channel, parameterName, value), eventSource);
            }
        }

        /// <summary>
        /// Disconnects from Homegear and stops the controller's RPC server.
        /// </summary>
        public void Dispose()
        {
            try
            {
                _disposing = true;

                if (_client != null)
                {
                    _client.Connected -= _client_Connected;
                    _client.Disconnected -= _client_Disconnected;
                    _client.HomegearError -= _client_HomegearError;
                    _client.RPCEvent -= _client_OnRPCEvent;
                    _client.NewDevices -= _client_OnNewDevices;
                    _client.DevicesDeleted -= _client_OnDevicesDeleted;
                    _client.UpdateDevice -= _client_OnUpdateDevice;
                    _client.NewEvent -= _client_OnNewEvent;
                    _client.EventDeleted -= _client_OnEventDeleted;
                    _client.UpdateEvent -= _client_OnUpdateEvent;
                    _client.RequestUiRefreshEvent -= _client_RequestUiRefreshEvent;

                    _client.Disconnect();
                }

                Disconnect();
                Clear();
            }
            catch (Exception)
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
            Disconnected?.Invoke(sender);
        }

        private void _client_Connected(RPCClient sender, CipherAlgorithmType cipherAlgorithm, int cipherStrength)
        {
            Connected?.Invoke(sender, cipherAlgorithm, cipherStrength);
            if (_events)
            {
                Init(_clientId);
            }
            InitCompleted?.Invoke(this);
        }

        /// <summary>
        /// Connects to Homegear and starts the RPC controller's callback event server. Don't call this method, when you pass this object to the Homegear object's constructor!
        /// <param name="events">When set to "true" the library starts an event server to receive events from Homegear.</param>
        /// </summary>
        public void Connect(bool events)
        {
            _events = events;
            if (_disposing)
            {
                throw new ObjectDisposedException("RPC");
            }

            _client.Connect();
            _keepAliveTimer.Start();
        }

        /// <summary>
        /// Disconnects from Homegear and stops the RPC controller's callback event server. Don't call this method, when you passed this object to the Homegear object!
        /// </summary>
        public void Disconnect()
        {
            _keepAliveTimer?.Stop();
            _client?.Disconnect();
        }

        private void ThrowError(string methodName, RPCVariable errorStruct)
        {
            if (!errorStruct.ErrorStruct)
            {
                return;
            }

            throw new HomegearRpcClientException("Error calling RPC method \"" + methodName + "\". faultCode: " + errorStruct.StructValue["faultCode"].IntegerValue.ToString() + " faultString: " + errorStruct.StructValue["faultString"].StringValue);
        }

        #region "RPC methods"
        public void AbortEventReset(string id)
        {
            if (_disposing)
            {
                throw new ObjectDisposedException("RPC");
            }

            RPCVariable response = _client.CallMethod("abortEventReset", new List<RPCVariable> { new RPCVariable(id) });
            if (response.ErrorStruct)
            {
                ThrowError("abortEventReset", response);
            }
        }

        public bool AddDevice(string serialNumber)
        {
            if (_disposing)
            {
                throw new ObjectDisposedException("RPC");
            }

            RPCVariable response = _client.CallMethod("addDevice", new List<RPCVariable> { new RPCVariable(serialNumber) });
            if (response.ErrorStruct)
            {
                if (response.StructValue["faultCode"].IntegerValue == -1)
                {
                    return false;
                }

                ThrowError("addDevice", response);
            }
            return true;
        }

        public void AddEvent(Event newEvent)
        {
            if (_disposing)
            {
                throw new ObjectDisposedException("RPC");
            }

            RPCVariable eventDescription = new RPCVariable(RPCVariableType.rpcStruct);
            if (newEvent is TimedEvent)
            {
                TimedEvent timedEvent = (TimedEvent)newEvent;
                eventDescription.StructValue.Add("TYPE", new RPCVariable(1));
                eventDescription.StructValue.Add("ID", new RPCVariable(timedEvent.ID));
                eventDescription.StructValue.Add("ENABLED", new RPCVariable(timedEvent.Enabled));
                eventDescription.StructValue.Add("EVENTTIME", new RPCVariable(HomegearHelpers.DateTimeToUnixTimeStamp(timedEvent.EventTime)));
                eventDescription.StructValue.Add("RECUREVERY", new RPCVariable(timedEvent.RecurEvery));
                if (timedEvent.EndTime != DateTime.MinValue)
                {
                    eventDescription.StructValue.Add("ENDTIME", new RPCVariable(HomegearHelpers.DateTimeToUnixTimeStamp(timedEvent.EndTime)));
                }

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
                eventDescription.StructValue.Add("TRIGGER", new RPCVariable((long)triggeredEvent.Trigger));
                eventDescription.StructValue.Add("TRIGGERVALUE", triggeredEvent.TriggerValue);
                eventDescription.StructValue.Add("EVENTMETHOD", new RPCVariable(triggeredEvent.EventMethod));
                eventDescription.StructValue.Add("EVENTMETHODPARAMS", new RPCVariable(triggeredEvent.EventMethodParams));
                if (triggeredEvent.ResetAfterDynamic != null)
                {
                    RPCVariable resetStruct = new RPCVariable(RPCVariableType.rpcStruct);
                    resetStruct.StructValue.Add("INITIALTIME", new RPCVariable(triggeredEvent.ResetAfterDynamic.InitialTime));
                    resetStruct.StructValue.Add("RESETAFTER", new RPCVariable(triggeredEvent.ResetAfterDynamic.ResetAfter));
                    resetStruct.StructValue.Add("OPERATION", new RPCVariable((long)triggeredEvent.ResetAfterDynamic.Operation));
                    resetStruct.StructValue.Add("FACTOR", new RPCVariable(triggeredEvent.ResetAfterDynamic.Factor));
                    resetStruct.StructValue.Add("LIMIT", new RPCVariable(triggeredEvent.ResetAfterDynamic.Limit));
                    eventDescription.StructValue.Add("RESETAFTER", resetStruct);
                }
                else
                {
                    eventDescription.StructValue.Add("RESETAFTER", new RPCVariable(triggeredEvent.ResetAfterStatic));
                }

                if (triggeredEvent.ResetMethod.Length > 0)
                {
                    eventDescription.StructValue.Add("RESETMETHOD", new RPCVariable(triggeredEvent.ResetMethod));
                    eventDescription.StructValue.Add("RESETMETHODPARAMS", new RPCVariable(triggeredEvent.ResetMethodParams));
                }
            }
            RPCVariable response = _client.CallMethod("addEvent", new List<RPCVariable> { eventDescription });
            if (response.ErrorStruct)
            {
                ThrowError("addEvent", response);
            }
        }

        public void AddLink(long senderID, long senderChannel, long receiverID, long receiverChannel)
        {
            if (_disposing)
            {
                throw new ObjectDisposedException("RPC");
            }

            RPCVariable response = _client.CallMethod("addLink", new List<RPCVariable> { new RPCVariable(senderID), new RPCVariable(senderChannel), new RPCVariable(receiverID), new RPCVariable(receiverChannel) });
            if (response.ErrorStruct)
            {
                ThrowError("addLink", response);
            }
        }

        public void AddLink(long senderID, long senderChannel, long receiverID, long receiverChannel, string name, string description)
        {
            if (_disposing)
            {
                throw new ObjectDisposedException("RPC");
            }

            RPCVariable response = _client.CallMethod("addLink", new List<RPCVariable> { new RPCVariable(senderID), new RPCVariable(senderChannel), new RPCVariable(receiverID), new RPCVariable(receiverChannel), new RPCVariable(name), new RPCVariable(description) });
            if (response.ErrorStruct)
            {
                ThrowError("addLink", response);
            }
        }

        public bool ClientServerInitialized(string interfaceID)
        {
            if (_disposing)
            {
                throw new ObjectDisposedException("RPC");
            }

            RPCVariable response = _client.CallMethod("clientServerInitialized", new List<RPCVariable> { new RPCVariable(interfaceID) });
            if (response.ErrorStruct)
            {
                ThrowError("clientServerInitialized", response);
            }

            return response.BooleanValue;
        }

        public long CreateDevice(Family family, long deviceType, string serialNumber, long address, long firmwareVersion)
        {
            if (_disposing)
            {
                throw new ObjectDisposedException("RPC");
            }

            RPCVariable response = _client.CallMethod("createDevice", new List<RPCVariable> { new RPCVariable(family.ID), new RPCVariable(deviceType), new RPCVariable(serialNumber), new RPCVariable(address), new RPCVariable(firmwareVersion) });
            if (response.ErrorStruct)
            {
                ThrowError("createDevice", response);
            }

            return response.IntegerValue;
        }

        public void DeleteDevice(long peerId, RPCDeleteDeviceFlags flags)
        {
            if (_disposing)
            {
                throw new ObjectDisposedException("RPC");
            }

            RPCVariable response = _client.CallMethod("deleteDevice", new List<RPCVariable> { new RPCVariable(peerId), new RPCVariable((long)flags) });
            if (response.ErrorStruct)
            {
                ThrowError("deleteDevice", response);
            }
        }

        public void DeleteMetadata(long peerId)
        {
            if (_disposing)
            {
                throw new ObjectDisposedException("RPC");
            }

            RPCVariable response = _client.CallMethod("deleteMetadata", new List<RPCVariable> { new RPCVariable(peerId) });
            if (response.ErrorStruct)
            {
                ThrowError("deleteMetadata", response);
            }
        }

        public void DeleteMetadata(MetadataVariable variable)
        {
            if (_disposing)
            {
                throw new ObjectDisposedException("RPC");
            }

            RPCVariable response = _client.CallMethod("deleteMetadata", new List<RPCVariable> { new RPCVariable(variable.PeerID), new RPCVariable(variable.Name) });
            if (response.ErrorStruct)
            {
                ThrowError("deleteMetadata", response);
            }
        }

        public void DeleteSystemVariable(SystemVariable variable)
        {
            if (_disposing)
            {
                throw new ObjectDisposedException("RPC");
            }

            RPCVariable response = _client.CallMethod("deleteSystemVariable", new List<RPCVariable> { new RPCVariable(variable.Name) });
            if (response.ErrorStruct)
            {
                ThrowError("deleteSystemVariable", response);
            }
        }

        public void EnableEvent(string id, bool enabled)
        {
            if (_disposing)
            {
                throw new ObjectDisposedException("RPC");
            }

            RPCVariable response = _client.CallMethod("enableEvent", new List<RPCVariable> { new RPCVariable(id), new RPCVariable(enabled) });
            if (response.ErrorStruct)
            {
                ThrowError("enableEvent", response);
            }
        }

        public Dictionary<string, MetadataVariable> GetAllMetadata(long peerId)
        {
            if (_disposing)
            {
                throw new ObjectDisposedException("RPC");
            }

            Dictionary<string, MetadataVariable> metadataVariables = new Dictionary<string, MetadataVariable>();
            RPCVariable response = _client.CallMethod("getAllMetadata", new List<RPCVariable> { new RPCVariable(peerId) });
            if (response.ErrorStruct)
            {
                if (response.StructValue["faultCode"].IntegerValue == -1)
                {
                    return metadataVariables;
                }
                else
                {
                    ThrowError("getAllMetadata", response);
                }
            }
            foreach (KeyValuePair<string, RPCVariable> element in response.StructValue)
            {
                MetadataVariable variable = new MetadataVariable(this, peerId, element.Key, element.Value);
                metadataVariables.Add(element.Key, variable);
            }
            return metadataVariables;
        }

        public Dictionary<string, SystemVariable> GetAllSystemVariables()
        {
            if (_disposing)
            {
                throw new ObjectDisposedException("RPC");
            }

            Dictionary<string, SystemVariable> systemVariables = new Dictionary<string, SystemVariable>();
            RPCVariable response = _client.CallMethod("getAllSystemVariables", new List<RPCVariable>());
            if (response.ErrorStruct)
            {
                ThrowError("getAllSystemVariables", response);
            }

            foreach (KeyValuePair<string, RPCVariable> element in response.StructValue)
            {
                SystemVariable variable = new SystemVariable(this, element.Key, element.Value);
                systemVariables.Add(element.Key, variable);
            }
            return systemVariables;
        }

        public Dictionary<string, Variable> GetAllValues(long peerId, long channelIndex)
        {
            if (_disposing)
            {
                throw new ObjectDisposedException("RPC");
            }

            RPCVariable response = _client.CallMethod("getAllValues", new List<RPCVariable> { new RPCVariable(peerId), new RPCVariable(true) });
            if (response.ErrorStruct)
            {
                ThrowError("getAllValues", response);
            }

            Dictionary<long, Family> families = Families;
            Dictionary<string, Variable> variables = new Dictionary<string, Variable>();
            foreach (RPCVariable deviceStruct in response.ArrayValue)
            {
                if (deviceStruct.StructValue.ContainsKey("CHANNELS"))
                {
                    Dictionary<long, Channel> channels = new Dictionary<long, Channel>();
                    foreach (RPCVariable channelStruct in deviceStruct.StructValue["CHANNELS"].ArrayValue)
                    {
                        if (!channelStruct.StructValue.ContainsKey("INDEX") || !channelStruct.StructValue.ContainsKey("PARAMSET"))
                        {
                            continue;
                        }

                        if (channelStruct.StructValue["INDEX"].IntegerValue != channelIndex)
                        {
                            continue;
                        }

                        Dictionary<string, RPCVariable> parameterSet = channelStruct.StructValue["PARAMSET"].StructValue;
                        for (int i = 0; i < parameterSet.Count; i++)
                        {
                            if (parameterSet.ElementAt(i).Key.Length == 0)
                            {
                                continue;
                            }

                            Dictionary<string, RPCVariable> variableInfo = parameterSet.ElementAt(i).Value.StructValue;
                            bool readable = true;
                            if (variableInfo.ContainsKey("READABLE"))
                            {
                                readable = variableInfo["READABLE"].BooleanValue;
                            };
                            string typeString = "";
                            if (variableInfo.ContainsKey("TYPE"))
                            {
                                typeString = variableInfo["TYPE"].StringValue;
                            }

                            RPCVariable value = null;
                            if (variableInfo.ContainsKey("VALUE"))
                            {
                                value = variableInfo["VALUE"];
                            }
                            else
                            {
                                if (readable || typeString.Length == 0)
                                {
                                    continue;
                                }

                                value = RPCVariable.CreateFromTypeString(typeString);
                                if (value.Type == RPCVariableType.rpcVoid)
                                {
                                    continue;
                                }
                            }
                            Variable variable = new Variable(this, peerId, channelIndex, parameterSet.ElementAt(i).Key, typeString, value);

                            if (variableInfo.ContainsKey("WRITEABLE"))
                            {
                                variable.Writeable = variableInfo["WRITEABLE"].BooleanValue;
                            }

                            variable.Readable = readable;
                            if (variableInfo.ContainsKey("MIN"))
                            {
                                variable.SetMin(variableInfo["MIN"]);
                            }

                            if (variableInfo.ContainsKey("MAX"))
                            {
                                variable.SetMax(variableInfo["MAX"]);
                            }

                            if (variableInfo.ContainsKey("UNIT"))
                            {
                                variable.Unit = variableInfo["UNIT"].StringValue;
                            }

                            if (variableInfo.ContainsKey("ROOM"))
                            {
                                variable.RoomID = (ulong)variableInfo["ROOM"].IntegerValue;
                            }

                            if (variableInfo.ContainsKey("ROLES"))
                            {
                                var roles = new Dictionary<ulong, Variable.RoleElement>();
                                var rolesArray = variableInfo["ROLES"].ArrayValue;
                                foreach(var roleStruct in rolesArray)
                                {
                                    if (!roleStruct.StructValue.ContainsKey("id")) continue;
                                    var roleElement = new Variable.RoleElement();
                                    roleElement.ID = (ulong)roleStruct.StructValue["id"].IntegerValue;
                                    long direction = 2;
                                    if (roleStruct.StructValue.ContainsKey("direction")) direction = roleStruct.StructValue["direction"].IntegerValue;
                                    if (direction == 0) roleElement.Direction = Variable.RoleElementDirection.input;
                                    else if (direction == 1) roleElement.Direction = Variable.RoleElementDirection.output;
                                    else roleElement.Direction = Variable.RoleElementDirection.both;
                                    if (roleStruct.StructValue.ContainsKey("invert")) roleElement.Invert = roleStruct.StructValue["invert"].BooleanValue;
                                    roles.Add(roleElement.ID, roleElement);
                                }
                                variable.Roles = roles;
                            }

                            if (variableInfo.ContainsKey("SPECIAL"))
                            {
                                variable.SetSpecialValues(variableInfo["SPECIAL"]);
                            }

                            if (variableInfo.ContainsKey("VALUE_LIST"))
                            {
                                variable.SetValueList(variableInfo["VALUE_LIST"]);
                            }

                            variables.Add(variable.Name, variable);
                        }
                    }
                }
            }
            return variables;
        }

        public Dictionary<long, Device> GetAllValues()
        {
            if (_disposing)
            {
                throw new ObjectDisposedException("RPC");
            }

            Dictionary<long, Device> devices = new Dictionary<long, Device>();
            RPCVariable response = _client.CallMethod("getAllValues", new List<RPCVariable> { new RPCVariable(true) });
            if (response.ErrorStruct)
            {
                ThrowError("getAllValues", response);
            }

            Dictionary<long, Family> families = Families;
            foreach (RPCVariable deviceStruct in response.ArrayValue)
            {
                if (!deviceStruct.StructValue.ContainsKey("ID") || !deviceStruct.StructValue.ContainsKey("FAMILY"))
                {
                    continue;
                }

                if (!families.ContainsKey(deviceStruct.StructValue["FAMILY"].IntegerValue))
                {
                    continue;
                }

                Device device = new Device(this, families[deviceStruct.StructValue["FAMILY"].IntegerValue], deviceStruct.StructValue["ID"].IntegerValue);
                if (deviceStruct.StructValue.ContainsKey("ADDRESS"))
                {
                    device.SerialNumber = deviceStruct.StructValue["ADDRESS"].StringValue;
                }

                if (deviceStruct.StructValue.ContainsKey("TYPE"))
                {
                    device.TypeString = deviceStruct.StructValue["TYPE"].StringValue;
                    if (_asciiDeviceTypeIdString)
                    {
                        device.TypeString = device.TypeString.Replace(' ', '_');
                        System.Text.RegularExpressions.Regex regex = new System.Text.RegularExpressions.Regex("[^a-zA-Z0-9_-]");
                        device.TypeString = regex.Replace(device.TypeString, "");
                    }
                }
                if (deviceStruct.StructValue.ContainsKey("TYPE_ID"))
                {
                    device.TypeID = deviceStruct.StructValue["TYPE_ID"].IntegerValue;
                }

                if (deviceStruct.StructValue.ContainsKey("CHANNELS"))
                {
                    Dictionary<long, Channel> channels = new Dictionary<long, Channel>();
                    foreach (RPCVariable channelStruct in deviceStruct.StructValue["CHANNELS"].ArrayValue)
                    {
                        if (!channelStruct.StructValue.ContainsKey("INDEX") || !channelStruct.StructValue.ContainsKey("PARAMSET"))
                        {
                            continue;
                        }

                        Dictionary<string, Variable> variables = new Dictionary<string, Variable>();
                        Dictionary<string, RPCVariable> parameterSet = channelStruct.StructValue["PARAMSET"].StructValue;
                        Channel channel = new Channel(this, device.ID, channelStruct.StructValue["INDEX"].IntegerValue);
                        channels.Add(channel.Index, channel);
                        for (int i = 0; i < parameterSet.Count; i++)
                        {
                            if (parameterSet.ElementAt(i).Key.Length == 0)
                            {
                                continue;
                            }

                            Dictionary<string, RPCVariable> variableInfo = parameterSet.ElementAt(i).Value.StructValue;
                            bool readable = true;
                            if (variableInfo.ContainsKey("READABLE"))
                            {
                                readable = variableInfo["READABLE"].BooleanValue;
                            };
                            string typeString = "";
                            if (variableInfo.ContainsKey("TYPE"))
                            {
                                typeString = variableInfo["TYPE"].StringValue;
                            }

                            RPCVariable value = null;
                            if (variableInfo.ContainsKey("VALUE"))
                            {
                                value = variableInfo["VALUE"];
                            }
                            else
                            {
                                if (readable || typeString.Length == 0)
                                {
                                    continue;
                                }

                                value = RPCVariable.CreateFromTypeString(typeString);
                                if (value.Type == RPCVariableType.rpcVoid)
                                {
                                    continue;
                                }
                            }
                            Variable variable = new Variable(this, device.ID, channel.Index, parameterSet.ElementAt(i).Key, typeString, value);

                            if (variableInfo.ContainsKey("WRITEABLE"))
                            {
                                variable.Writeable = variableInfo["WRITEABLE"].BooleanValue;
                            }

                            variable.Readable = readable;
                            if (variableInfo.ContainsKey("MIN"))
                            {
                                variable.SetMin(variableInfo["MIN"]);
                            }

                            if (variableInfo.ContainsKey("MAX"))
                            {
                                variable.SetMax(variableInfo["MAX"]);
                            }

                            if (variableInfo.ContainsKey("UNIT"))
                            {
                                variable.Unit = variableInfo["UNIT"].StringValue;
                            }

                            if (variableInfo.ContainsKey("ROOM"))
                            {
                                variable.RoomID = (ulong)variableInfo["ROOM"].IntegerValue;
                            }

                            if (variableInfo.ContainsKey("ROLES"))
                            {
                                var roles = new Dictionary<ulong, Variable.RoleElement>();
                                var rolesArray = variableInfo["ROLES"].ArrayValue;
                                foreach (var roleStruct in rolesArray)
                                {
                                    if (!roleStruct.StructValue.ContainsKey("id")) continue;
                                    var roleElement = new Variable.RoleElement();
                                    roleElement.ID = (ulong)roleStruct.StructValue["id"].IntegerValue;
                                    long direction = 2;
                                    if (roleStruct.StructValue.ContainsKey("direction")) direction = roleStruct.StructValue["direction"].IntegerValue;
                                    if (direction == 0) roleElement.Direction = Variable.RoleElementDirection.input;
                                    else if (direction == 1) roleElement.Direction = Variable.RoleElementDirection.output;
                                    else roleElement.Direction = Variable.RoleElementDirection.both;
                                    if (roleStruct.StructValue.ContainsKey("invert")) roleElement.Invert = roleStruct.StructValue["invert"].BooleanValue;
                                    roles.Add(roleElement.ID, roleElement);
                                }
                                variable.Roles = roles;
                            }

                            if (variableInfo.ContainsKey("SPECIAL"))
                            {
                                variable.SetSpecialValues(variableInfo["SPECIAL"]);
                            }

                            if (variableInfo.ContainsKey("VALUE_LIST"))
                            {
                                variable.SetValueList(variableInfo["VALUE_LIST"]);
                            }

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
            if (_disposing)
            {
                throw new ObjectDisposedException("RPC");
            }

            RPCVariable response = _client.CallMethod("getDeviceDescription", new List<RPCVariable> { new RPCVariable(device.ID), new RPCVariable(-1) });
            if (response.ErrorStruct)
            {
                ThrowError("getDeviceDescription", response);
            }

            if (response.StructValue.ContainsKey("FAMILY"))
            {
                if (Families.ContainsKey(response.StructValue["FAMILY"].IntegerValue))
                {
                    device.Family = Families[response.StructValue["FAMILY"].IntegerValue];
                }
            }
            if (response.StructValue.ContainsKey("ADDRESS"))
            {
                device.SerialNumber = response.StructValue["ADDRESS"].StringValue;
            }

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
            if (response.StructValue.ContainsKey("TYPE_ID"))
            {
                device.TypeID = response.StructValue["TYPE_ID"].IntegerValue;
            }

            if (response.StructValue.ContainsKey("NAME"))
            {
                device.SetNameNoRPC(response.StructValue["NAME"].StringValue);
            }

            if (response.StructValue.ContainsKey("ROOM"))
            {
                if (Rooms.ContainsKey((ulong)response.StructValue["ROOM"].IntegerValue))
                {
                    device.SetRoomNoRPC(Rooms[(ulong)response.StructValue["ROOM"].IntegerValue]);
                }
            }

            if (response.StructValue.ContainsKey("PHYSICAL_ADDRESS"))
            {
                device.Address = response.StructValue["PHYSICAL_ADDRESS"].IntegerValue;
            }

            if (response.StructValue.ContainsKey("RX_MODE"))
            {
                device.RXMode = (DeviceRXMode)response.StructValue["RX_MODE"].IntegerValue;
            }

            if (response.StructValue.ContainsKey("FIRMWARE"))
            {
                device.Firmware = response.StructValue["FIRMWARE"].StringValue;
            }

            if (response.StructValue.ContainsKey("AVAILABLE_FIRMWARE"))
            {
                device.AvailableFirmware = response.StructValue["AVAILABLE_FIRMWARE"].StringValue;
            }
            else if (response.StructValue.ContainsKey("FIRMWARE"))
            {
                device.AvailableFirmware = response.StructValue["FIRMWARE"].StringValue;
            }
        }

        public void GetDeviceDescription(Channel channel)
        {
            if (_disposing)
            {
                throw new ObjectDisposedException("RPC");
            }

            RPCVariable response = _client.CallMethod("getDeviceDescription", new List<RPCVariable> { new RPCVariable(channel.PeerID), new RPCVariable(channel.Index) });
            if (response.ErrorStruct)
            {
                ThrowError("getDeviceDescription", response);
            }

            if (response.StructValue.ContainsKey("TYPE"))
            {
                channel.TypeString = response.StructValue["TYPE"].StringValue;
            }

            if (response.StructValue.ContainsKey("NAME"))
            {
                channel.SetNameNoRPC(response.StructValue["NAME"].StringValue);
            }

            if (response.StructValue.ContainsKey("ROOM"))
            {
                if (Rooms.ContainsKey((ulong)response.StructValue["ROOM"].IntegerValue))
                {
                    channel.SetRoomNoRPC(Rooms[(ulong)response.StructValue["ROOM"].IntegerValue]);
                }
            }

            if (response.StructValue.ContainsKey("AES_ACTIVE"))
            {
                channel.AESActive = (response.StructValue["AES_ACTIVE"].IntegerValue != 0);
            }

            if (response.StructValue.ContainsKey("DIRECTION"))
            {
                channel.Direction = (ChannelDirection)response.StructValue["DIRECTION"].IntegerValue;
            }

            if (response.StructValue.ContainsKey("LINK_SOURCE_ROLES"))
            {
                string[] temp = response.StructValue["LINK_SOURCE_ROLES"].StringValue.Split(' ');
                if (temp.Length > 0 && temp[0] != "")
                {
                    channel.LinkSourceRoles = temp;
                }
            }
            if (response.StructValue.ContainsKey("LINK_TARGET_ROLES"))
            {
                string[] temp = response.StructValue["LINK_TARGET_ROLES"].StringValue.Split(' ');
                if (temp.Length > 0 && temp[0] != "")
                {
                    channel.LinkTargetRoles = response.StructValue["LINK_TARGET_ROLES"].StringValue.Split(' ');
                }
            }
            if (response.StructValue.ContainsKey("GROUP") && response.StructValue["GROUP"].StringValue.Length > 0)
            {
                string[] temp = response.StructValue["GROUP"].StringValue.Split(':');
                if (temp.Length == 2)
                {
                    long groupChannel = -1;
                    long.TryParse(temp[1], out groupChannel);
                    channel.GroupedWith = groupChannel;
                }
            }
            if (response.StructValue.ContainsKey("TEAM"))
            {
                channel.TeamSerialNumber = response.StructValue["TEAM"].StringValue;
            }

            if (response.StructValue.ContainsKey("TEAM_ID"))
            {
                channel.TeamID = response.StructValue["TEAM_ID"].IntegerValue;
            }

            if (response.StructValue.ContainsKey("TEAM_CHANNEL"))
            {
                channel.TeamChannel = response.StructValue["TEAM_CHANNEL"].IntegerValue;
            }

            if (response.StructValue.ContainsKey("TEAM_TAG"))
            {
                channel.TeamTag = response.StructValue["TEAM_TAG"].StringValue;
            }

            if (response.StructValue.ContainsKey("TEAM_CHANNELS"))
            {
                List<RPCVariable> teamMemberArray = response.StructValue["TEAM_CHANNELS"].ArrayValue;
                string[] teamMembers = new string[teamMemberArray.Count];
                for (int i = 0; i < teamMemberArray.Count; i++)
                {
                    teamMembers[i] = teamMemberArray[i].StringValue;
                }
                channel.TeamMembers = teamMembers;
            }
        }

        public void GetDeviceInfo(Device device)
        {
            if (_disposing)
            {
                throw new ObjectDisposedException("RPC");
            }

            RPCVariable response = _client.CallMethod("getDeviceInfo", new List<RPCVariable> { new RPCVariable(device.ID) });
            if (response.ErrorStruct)
            {
                ThrowError("getDeviceInfo", response);
            }

            if (response.StructValue.ContainsKey("INTERFACE"))
            {
                if (!Interfaces.ContainsKey(response.StructValue["INTERFACE"].StringValue)) Interfaces.Add(response.StructValue["INTERFACE"].StringValue, new Interface(device.Family, response.StructValue["INTERFACE"].StringValue, "Unknown"));
                device.SetInterfaceNoRPC(Interfaces[response.StructValue["INTERFACE"].StringValue]);
            }
        }

        public void GetEvent(Event eventToUpdate)
        {
            if (_disposing)
            {
                throw new ObjectDisposedException("RPC");
            }

            RPCVariable response = _client.CallMethod("getEvent", new List<RPCVariable> { new RPCVariable(eventToUpdate.ID) });
            if (response.ErrorStruct)
            {
                ThrowError("getEvent", response);
            }

            ParseEvent(response, eventToUpdate);
        }

        public long GetInstallMode()
        {
            if (_disposing)
            {
                throw new ObjectDisposedException("RPC");
            }

            RPCVariable response = _client.CallMethod("getInstallMode", new List<RPCVariable>());
            if (response.ErrorStruct)
            {
                ThrowError("getInstallMode", response);
            }

            return response.IntegerValue;
        }

        public long GetInstallMode(long familyID)
        {
            if (_disposing)
            {
                throw new ObjectDisposedException("RPC");
            }

            RPCVariable response = _client.CallMethod("getInstallMode", new List<RPCVariable> { new RPCVariable(familyID) });
            if (response.ErrorStruct)
            {
                ThrowError("getInstallMode", response);
            }

            return response.IntegerValue;
        }

        public void GetLinkInfo(Link link)
        {
            if (_disposing)
            {
                throw new ObjectDisposedException("RPC");
            }

            RPCVariable response = null;
            if (link.IsSender)
            {
                response = _client.CallMethod("getLinkInfo", new List<RPCVariable> { new RPCVariable(link.PeerID), new RPCVariable(link.Channel), new RPCVariable(link.RemotePeerID), new RPCVariable(link.RemoteChannel) });
            }
            else
            {
                response = _client.CallMethod("getLinkInfo", new List<RPCVariable> { new RPCVariable(link.RemotePeerID), new RPCVariable(link.RemoteChannel), new RPCVariable(link.PeerID), new RPCVariable(link.Channel) });
            }

            if (response.ErrorStruct)
            {
                ThrowError("getLinkInfo", response);
            }

            if (response.StructValue.ContainsKey("NAME"))
            {
                link.SetNameNoRPC(response.StructValue["NAME"].StringValue);
            }

            if (response.StructValue.ContainsKey("DESCRIPTION"))
            {
                link.SetNameNoRPC(response.StructValue["DESCRIPTION"].StringValue);
            }
        }

        public List<Link> GetLinks()
        {
            return GetLinks(0, -1, 0);
        }

        public List<Link> GetLinks(long peerId)
        {
            return GetLinks(peerId, -1, 0);
        }

        public List<Link> GetLinks(long peerId, long channel)
        {
            return GetLinks(peerId, channel, 0);
        }

        public List<Link> GetLinks(long peerId, long channel, long flags)
        {
            if (_disposing)
            {
                throw new ObjectDisposedException("RPC");
            }

            RPCVariable response = _client.CallMethod("getLinks", new List<RPCVariable> { new RPCVariable(peerId), new RPCVariable(channel), new RPCVariable(flags) });
            if (response.ErrorStruct)
            {
                ThrowError("getLinks", response);
            }

            List<Link> links = new List<Link>();
            foreach (RPCVariable rpcLink in response.ArrayValue)
            {
                if (!rpcLink.StructValue.ContainsKey("SENDER_ID") || !rpcLink.StructValue.ContainsKey("SENDER_CHANNEL") || !rpcLink.StructValue.ContainsKey("RECEIVER_ID") || !rpcLink.StructValue.ContainsKey("RECEIVER_CHANNEL"))
                {
                    continue;
                }

                long remotePeerID = 0;
                long remoteChannel = -1;
                bool isSender = true;
                if (rpcLink.StructValue["SENDER_ID"].IntegerValue == peerId && rpcLink.StructValue["SENDER_CHANNEL"].IntegerValue == channel)
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
                Link link = new Link(this, peerId, channel, remotePeerID, remoteChannel, isSender);
                if (rpcLink.StructValue.ContainsKey("NAME"))
                {
                    link.SetNameNoRPC(rpcLink.StructValue["NAME"].StringValue);
                }

                if (rpcLink.StructValue.ContainsKey("DESCRIPTION"))
                {
                    link.SetDescriptionNoRPC(rpcLink.StructValue["DESCRIPTION"].StringValue);
                }

                links.Add(link);
            }
            return links;
        }

        public List<ConfigParameter> GetParamset(long peerId, long channel, long remotePeerID, long remoteChannel, DeviceConfig parameters)
        {
            return GetParamset(peerId, channel, remotePeerID, remoteChannel, RPCParameterSetType.rpcLink, parameters);
        }

        public List<ConfigParameter> GetParamset(long peerId, long channel, RPCParameterSetType type, DeviceConfig parameters)
        {
            return GetParamset(peerId, channel, 0, -1, type, parameters);
        }

        private List<ConfigParameter> GetParamset(long peerId, long channel, long remotePeerID, long remoteChannel, RPCParameterSetType type, DeviceConfig parameters)
        {
            if (_disposing)
            {
                throw new ObjectDisposedException("RPC");
            }

            RPCVariable response = null;
            if (type == RPCParameterSetType.rpcLink)
            {
                response = _client.CallMethod("getParamset", new List<RPCVariable> { new RPCVariable(peerId), new RPCVariable(channel), new RPCVariable(remotePeerID), new RPCVariable(remoteChannel) });
            }
            else
            {
                response = _client.CallMethod("getParamset", new List<RPCVariable> { new RPCVariable(peerId), new RPCVariable(channel), new RPCVariable("MASTER") });
            }
            if (response.ErrorStruct)
            {
                ThrowError("getParamset", response);
            }

            List<ConfigParameter> changedParameters = new List<ConfigParameter>();
            foreach (KeyValuePair<string, RPCVariable> value in response.StructValue)
            {
                if (!parameters.ContainsKey(value.Key))
                {
                    continue;
                }

                if (parameters[value.Key].SetValue(value.Value))
                {
                    changedParameters.Add(parameters[value.Key]);
                }
            }
            return changedParameters;
        }

        public Dictionary<string, ConfigParameter> GetParamsetDescription(long peerId, long channel, RPCParameterSetType type)
        {
            if (_disposing)
            {
                throw new ObjectDisposedException("RPC");
            }

            Dictionary<string, ConfigParameter> parameters = new Dictionary<string, ConfigParameter>();
            string typeString = "MASTER";
            if (type == RPCParameterSetType.rpcLink)
            {
                typeString = "LINK";
            }

            RPCVariable response = _client.CallMethod("getParamsetDescription", new List<RPCVariable> { new RPCVariable(peerId), new RPCVariable(channel), new RPCVariable(typeString) });
            if (response.ErrorStruct)
            {
                ThrowError("getParamsetDescription", response);
            }

            foreach (KeyValuePair<string, RPCVariable> parameterDescription in response.StructValue)
            {
                if (parameterDescription.Key.Length == 0)
                {
                    continue;
                }

                if (!parameterDescription.Value.StructValue.ContainsKey("TYPE"))
                {
                    continue;
                }

                RPCVariable rpcParameter = RPCVariable.CreateFromTypeString(parameterDescription.Value.StructValue["TYPE"].StringValue);
                if (rpcParameter.Type == RPCVariableType.rpcVoid)
                {
                    continue;
                }

                ConfigParameter configParameter = new ConfigParameter(this, peerId, channel, parameterDescription.Key, rpcParameter);
                if (parameterDescription.Value.StructValue.ContainsKey("MIN"))
                {
                    configParameter.SetMin(parameterDescription.Value.StructValue["MIN"]);
                }

                if (parameterDescription.Value.StructValue.ContainsKey("MAX"))
                {
                    configParameter.SetMax(parameterDescription.Value.StructValue["MAX"]);
                }

                if (parameterDescription.Value.StructValue.ContainsKey("DEFAULT"))
                {
                    configParameter.SetDefault(parameterDescription.Value.StructValue["DEFAULT"]);
                }

                if (parameterDescription.Value.StructValue.ContainsKey("OPERATIONS"))
                {
                    long operations = parameterDescription.Value.StructValue["OPERATIONS"].IntegerValue;
                    if ((operations & 1) == 1 || (operations & 4) == 4)
                    {
                        configParameter.Readable = true;
                    }

                    if ((operations & 2) == 2)
                    {
                        configParameter.Writeable = true;
                    }
                }
                if (parameterDescription.Value.StructValue.ContainsKey("FLAGS"))
                {
                    configParameter.UIFlags = (VariableUIFlags)parameterDescription.Value.StructValue["FLAGS"].IntegerValue;
                }

                if (parameterDescription.Value.StructValue.ContainsKey("UNIT"))
                {
                    configParameter.Unit = parameterDescription.Value.StructValue["UNIT"].StringValue;
                }

                if (parameterDescription.Value.StructValue.ContainsKey("SPECIAL"))
                {
                    configParameter.SetSpecialValues(parameterDescription.Value.StructValue["SPECIAL"]);
                }

                if (parameterDescription.Value.StructValue.ContainsKey("VALUE_LIST"))
                {
                    configParameter.SetValueList(parameterDescription.Value.StructValue["VALUE_LIST"]);
                }

                configParameter.Name = parameterDescription.Key;
                parameters.Add(parameterDescription.Key, configParameter);
            }
            return parameters;
        }

        public MetadataVariable GetMetadata(long peerId, string name)
        {
            if (_disposing)
            {
                throw new ObjectDisposedException("RPC");
            }

            RPCVariable response = _client.CallMethod("getMetadata", new List<RPCVariable> { new RPCVariable(peerId), new RPCVariable(name) });
            if (response.ErrorStruct)
            {
                ThrowError("getMetadata", response);
            }

            return new MetadataVariable(this, peerId, name, response);
        }

        public List<ServiceMessage> GetServiceMessages()
        {
            if (_disposing)
            {
                throw new ObjectDisposedException("RPC");
            }

            List<ServiceMessage> messages = new List<ServiceMessage>();
            RPCVariable response = _client.CallMethod("getServiceMessages", new List<RPCVariable> { new RPCVariable(true) });
            if (response.ErrorStruct)
            {
                ThrowError("getServiceMessages", response);
            }

            foreach (RPCVariable element in response.ArrayValue)
            {
                if (element.Type != RPCVariableType.rpcStruct || !element.StructValue.ContainsKey("TYPE"))
                {
                    continue;
                }

                ServiceMessage.ServiceMessageType serviceMessageType = (ServiceMessage.ServiceMessageType)element.StructValue["TYPE"].IntegerValue;
                if (serviceMessageType == ServiceMessage.ServiceMessageType.global && (!element.StructValue.ContainsKey("TIMESTAMP") || !element.StructValue.ContainsKey("MESSAGE_ID") || !element.StructValue.ContainsKey("MESSAGE") || !element.StructValue.ContainsKey("DATA") || !element.StructValue.ContainsKey("VALUE")))
                {
                    continue;
                }
                else if (serviceMessageType == ServiceMessage.ServiceMessageType.family && (!element.StructValue.ContainsKey("FAMILY_ID") || !element.StructValue.ContainsKey("TIMESTAMP") || !element.StructValue.ContainsKey("MESSAGE_ID") || !element.StructValue.ContainsKey("MESSAGE") || !element.StructValue.ContainsKey("DATA") || !element.StructValue.ContainsKey("VALUE")))
                {
                    continue;
                }
                else if (serviceMessageType == ServiceMessage.ServiceMessageType.device && (!element.StructValue.ContainsKey("PEER_ID") || !element.StructValue.ContainsKey("TIMESTAMP") || !element.StructValue.ContainsKey("MESSAGE") || !element.StructValue.ContainsKey("VALUE")))
                {
                    continue;
                }

                ServiceMessage message;
                if(serviceMessageType == ServiceMessage.ServiceMessageType.global)
                {
                    long value;
                    if (element.StructValue["VALUE"].Type == RPCVariableType.rpcBoolean)
                    {
                        value = element.StructValue["VALUE"].BooleanValue ? 1 : 0;
                    }
                    else
                    {
                        value = element.StructValue["VALUE"].IntegerValue;
                    }

                    message = new ServiceMessage(HomegearHelpers.UnixTimeStampToDateTime(element.StructValue["TIMESTAMP"].IntegerValue), element.StructValue["MESSAGE_ID"].IntegerValue, element.StructValue["MESSAGE"].StringValue, element.StructValue["DATA"], value);
                    messages.Add(message);
                }
                else if (serviceMessageType == ServiceMessage.ServiceMessageType.family)
                {
                    long value;
                    if (element.StructValue["VALUE"].Type == RPCVariableType.rpcBoolean)
                    {
                        value = element.StructValue["VALUE"].BooleanValue ? 1 : 0;
                    }
                    else
                    {
                        value = element.StructValue["VALUE"].IntegerValue;
                    }

                    message = new ServiceMessage(HomegearHelpers.UnixTimeStampToDateTime(element.StructValue["TIMESTAMP"].IntegerValue), element.StructValue["FAMILY_ID"].IntegerValue, element.StructValue["MESSAGE_ID"].IntegerValue, element.StructValue["MESSAGE"].StringValue, element.StructValue["DATA"], value);
                    messages.Add(message);
                }
                else if (serviceMessageType == ServiceMessage.ServiceMessageType.device)
                {
                    long channel = -1;
                    if (element.StructValue.ContainsKey("CHANNEL"))
                    {
                        channel = element.StructValue["CHANNEL"].IntegerValue;
                    }

                    long value;
                    if (element.StructValue["VALUE"].Type == RPCVariableType.rpcBoolean)
                    {
                        value = element.StructValue["VALUE"].BooleanValue ? 1 : 0;
                    }
                    else
                    {
                        value = element.StructValue["VALUE"].IntegerValue;
                    }

                    message = new ServiceMessage(HomegearHelpers.UnixTimeStampToDateTime(element.StructValue["TIMESTAMP"].IntegerValue), element.StructValue["PEER_ID"].IntegerValue, channel, element.StructValue["MESSAGE"].StringValue, value);
                    messages.Add(message);
                }
            }
            return messages;
        }

        public Dictionary<long, SniffedDeviceInfo> GetSniffedDevices(Family family)
        {
            if (_disposing)
            {
                throw new ObjectDisposedException("RPC");
            }

            Dictionary<long, SniffedDeviceInfo> sniffedDevices = new Dictionary<long, SniffedDeviceInfo>();
            RPCVariable response = _client.CallMethod("getSniffedDevices", new List<RPCVariable> { new RPCVariable(family.ID) });
            if (response.ErrorStruct)
            {
                ThrowError("getSniffedDevices", response);
            }

            Dictionary<long, Family> families = Families;

            foreach (RPCVariable deviceStruct in response.ArrayValue)
            {
                if (!deviceStruct.StructValue.ContainsKey("PACKETS") || !deviceStruct.StructValue.ContainsKey("FAMILYID"))
                {
                    continue;
                }

                long familyID = deviceStruct.StructValue["FAMILYID"].IntegerValue;
                if (!families.ContainsKey(familyID))
                {
                    continue;
                }

                SniffedDeviceInfo deviceInfo = new SniffedDeviceInfo(families[familyID]);
                foreach (KeyValuePair<string, RPCVariable> structElement in deviceStruct.StructValue)
                {
                    switch (structElement.Key)
                    {
                        case "FAMILYID":
                            break;
                        case "ADDRESS":
                            deviceInfo.Address = structElement.Value.IntegerValue;
                            break;
                        case "RSSI":
                            deviceInfo.Rssi = structElement.Value.IntegerValue;
                            break;
                        case "PACKETS":
                            break;
                        default:
                            deviceInfo.AdditionalData.Add(structElement.Key, structElement.Value.ToString());
                            break;
                    }
                }

                foreach (RPCVariable packetInfo in deviceStruct.StructValue["PACKETS"].ArrayValue)
                {
                    if (!packetInfo.StructValue.ContainsKey("TIME_RECEIVED") || !packetInfo.StructValue.ContainsKey("PACKET"))
                    {
                        continue;
                    }

                    deviceInfo.Packets.Add(new SniffedDevicePacketInfo((uint)packetInfo.StructValue["TIME_RECEIVED"].IntegerValue, packetInfo.StructValue["PACKET"].StringValue));
                }

                sniffedDevices.Add(deviceInfo.Address, deviceInfo);
            }

            return sniffedDevices;
        }

        public UpdateStatus GetUpdateStatus()
        {
            if (_disposing)
            {
                throw new ObjectDisposedException("RPC");
            }

            RPCVariable response = _client.CallMethod("getUpdateStatus", null);
            if (response.ErrorStruct)
            {
                ThrowError("getUpdateStatus", response);
            }

            long currentDevice = -1;
            long currentDeviceProgress = -1;
            long deviceCount = -1;
            long currentUpdate = 0;
            Dictionary<long, UpdateResult> results = new Dictionary<long, UpdateResult>();
            if (response.StructValue.ContainsKey("CURRENT_DEVICE"))
            {
                currentDevice = response.StructValue["CURRENT_DEVICE"].IntegerValue;
            }

            if (response.StructValue.ContainsKey("CURRENT_DEVICE_PROGRESS"))
            {
                currentDeviceProgress = response.StructValue["CURRENT_DEVICE_PROGRESS"].IntegerValue;
            }

            if (response.StructValue.ContainsKey("DEVICE_COUNT"))
            {
                deviceCount = response.StructValue["DEVICE_COUNT"].IntegerValue;
            }

            if (response.StructValue.ContainsKey("CURRENT_UPDATE"))
            {
                currentUpdate = response.StructValue["CURRENT_UPDATE"].IntegerValue;
            }

            if (response.StructValue.ContainsKey("RESULTS"))
            {
                foreach (KeyValuePair<string, RPCVariable> devicePair in response.StructValue["RESULTS"].StructValue)
                {
                    long peerId = 0;
                    if (!long.TryParse(devicePair.Key, out peerId))
                    {
                        continue;
                    }

                    if (!devicePair.Value.StructValue.ContainsKey("RESULT_CODE") || !devicePair.Value.StructValue.ContainsKey("RESULT_STRING"))
                    {
                        continue;
                    }

                    UpdateResult result = new UpdateResult((UpdateResultCode)devicePair.Value.StructValue["RESULT_CODE"].IntegerValue, devicePair.Value.StructValue["RESULT_STRING"].StringValue);
                    results.Add(peerId, result);
                }
            }
            return new UpdateStatus(currentDevice, currentDeviceProgress, deviceCount, currentUpdate, results);
        }

        public void GetValue(Variable variable, bool requestFromDevice = false)
        {
            if (_disposing)
            {
                throw new ObjectDisposedException("RPC");
            }

            RPCVariable response = _client.CallMethod("getValue", new List<RPCVariable> { new RPCVariable(variable.PeerID), new RPCVariable(variable.Channel), new RPCVariable(variable.Name), new RPCVariable(requestFromDevice) });
            if (response.ErrorStruct)
            {
                ThrowError("setValue", response);
            }

            variable.SetValue(response);
        }

        public string GetVersion()
        {
            if (_disposing)
            {
                throw new ObjectDisposedException("RPC");
            }

            RPCVariable response = _client.CallMethod("getVersion", new List<RPCVariable>());
            if (response.ErrorStruct)
            {
                ThrowError("getVersion", response);
            }

            return response.StringValue;
        }

        public SystemVariable GetSystemVariable(string name)
        {
            if (_disposing)
            {
                throw new ObjectDisposedException("RPC");
            }

            RPCVariable response = _client.CallMethod("getSystemVariable", new List<RPCVariable> { new RPCVariable(name) });
            if (response.ErrorStruct)
            {
                ThrowError("getSystemVariable", response);
            }

            return new SystemVariable(this, name, response);
        }

        public void Init(string interfaceID)
        {
            if (_disposing)
            {
                throw new ObjectDisposedException("RPC");
            }

            RPCVariable response = _client.CallMethod("init", new List<RPCVariable> { new RPCVariable(interfaceID), new RPCVariable(0x27) });
            if (response.ErrorStruct)
            {
                ThrowError("init", response);
            }
        }

        public RPCVariable invokeFamilyMethod(Family family, string method, List<RPCVariable> parameters)
        {
            if (_disposing)
            {
                throw new ObjectDisposedException("RPC");
            }

            return _client.CallMethod("invokeFamilyMethod", new List<RPCVariable> { new RPCVariable(family.ID), new RPCVariable(method), new RPCVariable(parameters) });
        }

        public Event ParseEvent(RPCVariable eventStruct, Event eventToUpdate = null)
        {
            if (!eventStruct.StructValue.ContainsKey("TYPE") || !eventStruct.StructValue.ContainsKey("ID"))
            {
                return null;
            }

            EventType type = (EventType)eventStruct.StructValue["TYPE"].IntegerValue;
            if (type == EventType.Timed)
            {
                TimedEvent element = (eventToUpdate != null && eventToUpdate is TimedEvent) ? (TimedEvent)eventToUpdate : new TimedEvent(this, eventStruct.StructValue["ID"].StringValue);
                if (eventStruct.StructValue.ContainsKey("ENABLED"))
                {
                    element.SetEnabledNoRPC(eventStruct.StructValue["ENABLED"].BooleanValue);
                }

                if (eventStruct.StructValue.ContainsKey("EVENTTIME"))
                {
                    element.EventTime = HomegearHelpers.UnixTimeStampToDateTime(eventStruct.StructValue["EVENTTIME"].IntegerValue);
                }

                if (eventStruct.StructValue.ContainsKey("RECUREVERY"))
                {
                    element.RecurEvery = eventStruct.StructValue["RECUREVERY"].IntegerValue;
                }

                if (eventStruct.StructValue.ContainsKey("ENDTIME"))
                {
                    element.EndTime = HomegearHelpers.UnixTimeStampToDateTime(eventStruct.StructValue["ENDTIME"].IntegerValue);
                }

                if (eventStruct.StructValue.ContainsKey("EVENTMETHOD"))
                {
                    element.EventMethod = eventStruct.StructValue["EVENTMETHOD"].StringValue;
                }

                if (eventStruct.StructValue.ContainsKey("EVENTMETHODPARAMS"))
                {
                    element.SetEventMethodParams(eventStruct.StructValue["EVENTMETHODPARAMS"].ArrayValue);
                }

                return element;
            }
            else
            {
                TriggeredEvent element = (eventToUpdate != null && eventToUpdate is TriggeredEvent) ? (TriggeredEvent)eventToUpdate : new TriggeredEvent(this, eventStruct.StructValue["ID"].StringValue);
                if (eventStruct.StructValue.ContainsKey("ENABLED"))
                {
                    element.SetEnabledNoRPC(eventStruct.StructValue["ENABLED"].BooleanValue);
                }

                if (eventStruct.StructValue.ContainsKey("PEERID"))
                {
                    element.PeerID = eventStruct.StructValue["PEERID"].IntegerValue;
                }

                if (eventStruct.StructValue.ContainsKey("PEERCHANNEL"))
                {
                    element.PeerChannel = eventStruct.StructValue["PEERCHANNEL"].IntegerValue;
                }

                if (eventStruct.StructValue.ContainsKey("VARIABLE"))
                {
                    element.VariableName = eventStruct.StructValue["VARIABLE"].StringValue;
                }

                if (eventStruct.StructValue.ContainsKey("TRIGGER"))
                {
                    element.Trigger = (EventTrigger)eventStruct.StructValue["TRIGGER"].IntegerValue;
                }

                if (eventStruct.StructValue.ContainsKey("TRIGGERVALUE"))
                {
                    element.TriggerValue = eventStruct.StructValue["TRIGGERVALUE"];
                }

                if (eventStruct.StructValue.ContainsKey("EVENTMETHOD"))
                {
                    element.EventMethod = eventStruct.StructValue["EVENTMETHOD"].StringValue;
                }

                if (eventStruct.StructValue.ContainsKey("EVENTMETHODPARAMS"))
                {
                    element.SetEventMethodParams(eventStruct.StructValue["EVENTMETHODPARAMS"].ArrayValue);
                }

                if (eventStruct.StructValue.ContainsKey("RESETAFTER"))
                {
                    if (eventStruct.StructValue["RESETAFTER"].Type == RPCVariableType.rpcInteger)
                    {
                        element.ResetAfterStatic = eventStruct.StructValue["RESETAFTER"].IntegerValue;
                    }
                    else
                    {
                        Dictionary<string, RPCVariable> resetStruct = eventStruct.StructValue["RESETAFTER"].StructValue;
                        element.ResetAfterDynamic = new DynamicResetTime();
                        if (resetStruct.ContainsKey("INITIALTIME"))
                        {
                            element.ResetAfterDynamic.InitialTime = resetStruct["INITIALTIME"].IntegerValue;
                        }

                        if (resetStruct.ContainsKey("RESETAFTER"))
                        {
                            element.ResetAfterDynamic.ResetAfter = resetStruct["RESETAFTER"].IntegerValue;
                        }

                        if (resetStruct.ContainsKey("OPERATION"))
                        {
                            element.ResetAfterDynamic.Operation = (DynamicResetTimeOperation)resetStruct["OPERATION"].IntegerValue;
                        }

                        if (resetStruct.ContainsKey("FACTOR"))
                        {
                            element.ResetAfterDynamic.Factor = resetStruct["FACTOR"].FloatValue;
                        }

                        if (resetStruct.ContainsKey("LIMIT"))
                        {
                            element.ResetAfterDynamic.Limit = resetStruct["LIMIT"].IntegerValue;
                        }

                        if (resetStruct.ContainsKey("CURRENTTIME"))
                        {
                            element.ResetAfterDynamic.CurrentTime = resetStruct["CURRENTTIME"].IntegerValue;
                        }
                    }
                }
                if (eventStruct.StructValue.ContainsKey("RESETMETHOD"))
                {
                    element.ResetMethod = eventStruct.StructValue["RESETMETHOD"].StringValue;
                }

                if (eventStruct.StructValue.ContainsKey("RESETMETHODPARAMS"))
                {
                    element.SetResetMethodParams(eventStruct.StructValue["RESETMETHODPARAMS"].ArrayValue);
                }

                if (eventStruct.StructValue.ContainsKey("LASTVALUE"))
                {
                    element.LastValue = eventStruct.StructValue["LASTVALUE"];
                }

                if (eventStruct.StructValue.ContainsKey("LASTRAISED"))
                {
                    element.LastRaised = HomegearHelpers.UnixTimeStampToDateTime(eventStruct.StructValue["LASTRAISED"].IntegerValue);
                }

                if (eventStruct.StructValue.ContainsKey("LASTRESET"))
                {
                    element.LastReset = HomegearHelpers.UnixTimeStampToDateTime(eventStruct.StructValue["LASTRESET"].IntegerValue);
                }

                return element;
            }
        }

        public Dictionary<string, Event> ListEvents(EventType type)
        {
            if (_disposing)
            {
                throw new ObjectDisposedException("RPC");
            }

            Dictionary<string, Event> events = new Dictionary<string, Event>();
            RPCVariable response = _client.CallMethod("listEvents", new List<RPCVariable> { new RPCVariable((long)type) });
            if (response.ErrorStruct)
            {
                ThrowError("listEvents", response);
            }

            foreach (RPCVariable eventStruct in response.ArrayValue)
            {
                Event element = ParseEvent(eventStruct);
                if (element != null)
                {
                    events.Add(element.ID, element);
                }
            }
            return events;
        }

        public Dictionary<string, Event> ListEvents(long id)
        {
            return ListEvents(id, -1);
        }

        public Dictionary<string, Event> ListEvents(long id, long channel)
        {
            if (_disposing)
            {
                throw new ObjectDisposedException("RPC");
            }

            Dictionary<string, Event> events = new Dictionary<string, Event>();
            RPCVariable response = _client.CallMethod("listEvents", new List<RPCVariable> { new RPCVariable(id), new RPCVariable(channel) });
            if (response.ErrorStruct)
            {
                ThrowError("listEvents", response);
            }

            foreach (RPCVariable eventStruct in response.ArrayValue)
            {
                Event element = ParseEvent(eventStruct);
                if (element != null)
                {
                    events.Add(element.ID, element);
                }
            }
            return events;
        }

        public Dictionary<string, Event> ListEvents(long id, long channel, string variableName)
        {
            if (_disposing)
            {
                throw new ObjectDisposedException("RPC");
            }

            Dictionary<string, Event> events = new Dictionary<string, Event>();
            RPCVariable response = _client.CallMethod("listEvents", new List<RPCVariable> { new RPCVariable(id), new RPCVariable(channel), new RPCVariable(variableName) });
            if (response.ErrorStruct)
            {
                ThrowError("listEvents", response);
            }

            foreach (RPCVariable eventStruct in response.ArrayValue)
            {
                Event element = ParseEvent(eventStruct);
                if (element != null)
                {
                    events.Add(element.ID, element);
                }
            }
            return events;
        }

        public Dictionary<long, Family> ListFamilies()
        {
            if (_disposing)
            {
                throw new ObjectDisposedException("RPC");
            }

            Dictionary<long, Family> families = new Dictionary<long, Family>();
            RPCVariable response = _client.CallMethod("listFamilies", null);
            if (response.ErrorStruct)
            {
                ThrowError("listFamilies", response);
            }

            foreach (RPCVariable familyStruct in response.ArrayValue)
            {
                if (!familyStruct.StructValue.ContainsKey("ID") || !familyStruct.StructValue.ContainsKey("NAME"))
                {
                    continue;
                }

                Family family = new Family(this, familyStruct.StructValue["ID"].IntegerValue, familyStruct.StructValue["NAME"].StringValue);
                if (familyStruct.StructValue.ContainsKey("PAIRING_METHODS"))
                {
                    List<string> pairingMethods = new List<string>();
                    foreach (RPCVariable pairingMethod in familyStruct.StructValue["PAIRING_METHODS"].ArrayValue)
                    {
                        if (pairingMethod.StringValue.Length > 0)
                        {
                            pairingMethods.Add(pairingMethod.StringValue);
                        }
                    }
                    family.SetPairingMethods(pairingMethods.AsReadOnly());
                }
                families.Add(familyStruct.StructValue["ID"].IntegerValue, family);
            }
            return families;
        }

        public Dictionary<string, Interface> ListInterfaces()
        {
            if (_disposing)
            {
                throw new ObjectDisposedException("RPC");
            }

            Dictionary<string, Interface> interfaces = new Dictionary<string, Interface>();
            RPCVariable response = _client.CallMethod("listInterfaces", null);
            if (response.ErrorStruct)
            {
                ThrowError("listInterfaces", response);
            }

            Dictionary<long, Family> families = Families;
            foreach (RPCVariable interfaceStruct in response.ArrayValue)
            {
                if (!interfaceStruct.StructValue.ContainsKey("ID") || !interfaceStruct.StructValue.ContainsKey("TYPE") || !interfaceStruct.StructValue.ContainsKey("FAMILYID"))
                {
                    continue;
                }

                long familyID = interfaceStruct.StructValue["FAMILYID"].IntegerValue;
                if (!families.ContainsKey(familyID))
                {
                    continue;
                }

                Interface physicalInterface = new Interface(families[familyID], interfaceStruct.StructValue["ID"].StringValue, interfaceStruct.StructValue["TYPE"].StringValue);
                if (interfaceStruct.StructValue.ContainsKey("CONNECTED"))
                {
                    physicalInterface.Connected = interfaceStruct.StructValue["CONNECTED"].BooleanValue;
                }

                if (interfaceStruct.StructValue.ContainsKey("DEFAULT"))
                {
                    physicalInterface.Default = interfaceStruct.StructValue["DEFAULT"].BooleanValue;
                }

                if (interfaceStruct.StructValue.ContainsKey("PHYSICALADDRESS"))
                {
                    physicalInterface.PhysicalAddress = interfaceStruct.StructValue["PHYSICALADDRESS"].IntegerValue;
                }

                if (interfaceStruct.StructValue.ContainsKey("IP_ADDRESS"))
                {
                    physicalInterface.IpAddress = interfaceStruct.StructValue["IP_ADDRESS"].StringValue;
                }

                if (interfaceStruct.StructValue.ContainsKey("HOSTNAME"))
                {
                    physicalInterface.Hostname = interfaceStruct.StructValue["HOSTNAME"].StringValue;
                }

                if (interfaceStruct.StructValue.ContainsKey("LASTPACKETRECEIVED"))
                {
                    physicalInterface.LastPacketReceived = interfaceStruct.StructValue["LASTPACKETRECEIVED"].IntegerValue;
                }

                if (interfaceStruct.StructValue.ContainsKey("LASTPACKETSENT"))
                {
                    physicalInterface.LastPacketSent = interfaceStruct.StructValue["LASTPACKETSENT"].IntegerValue;
                }

                interfaces.Add(physicalInterface.ID, physicalInterface);
            }
            return interfaces;
        }

        public long LogLevel()
        {
            if (_disposing)
            {
                throw new ObjectDisposedException("RPC");
            }

            RPCVariable response = _client.CallMethod("logLevel", new List<RPCVariable>());
            if (response.ErrorStruct)
            {
                ThrowError("logLevel", response);
            }

            return response.IntegerValue;
        }

        public long LogLevel(long newLevel)
        {
            if (_disposing)
            {
                throw new ObjectDisposedException("RPC");
            }

            RPCVariable response = _client.CallMethod("logLevel", new List<RPCVariable> { new RPCVariable(newLevel) });
            if (response.ErrorStruct)
            {
                ThrowError("logLevel", response);
            }

            return response.IntegerValue;
        }

        public void PutParamset(long peerId, long channel, RPCParameterSetType type, Dictionary<string, ConfigParameter> parameters)
        {
            PutParamset(peerId, channel, 0, -1, type, parameters);
        }

        public void PutParamset(long peerId, long channel, long remotePeerID, long remoteChannel, Dictionary<string, ConfigParameter> parameters)
        {
            PutParamset(peerId, channel, remotePeerID, remoteChannel, RPCParameterSetType.rpcLink, parameters);
        }

        private void PutParamset(long peerId, long channel, long remotePeerID, long remoteChannel, RPCParameterSetType type, Dictionary<string, ConfigParameter> parameters)
        {
            if (_disposing)
            {
                throw new ObjectDisposedException("RPC");
            }

            if (parameters.Count == 0)
            {
                return;
            }

            string typeString = "MASTER";
            if (type == RPCParameterSetType.rpcValues)
            {
                typeString = "VALUES";
            }

            RPCVariable rpcParameters = new RPCVariable(RPCVariableType.rpcStruct);
            foreach (KeyValuePair<string, ConfigParameter> parameter in parameters)
            {
                rpcParameters.StructValue.Add(parameter.Key, new RPCVariable(parameter.Value));
            }
            RPCVariable response = null;
            if (type == RPCParameterSetType.rpcLink)
            {
                response = _client.CallMethod("putParamset", new List<RPCVariable> { new RPCVariable(peerId), new RPCVariable(channel), new RPCVariable(remotePeerID), new RPCVariable(remoteChannel), rpcParameters });
            }
            else
            {
                response = _client.CallMethod("putParamset", new List<RPCVariable> { new RPCVariable(peerId), new RPCVariable(channel), new RPCVariable(typeString), rpcParameters });
            }
            if (response.ErrorStruct)
            {
                ThrowError("putParamset", response);
            }
        }

        public void RemoveEvent(string id)
        {
            if (_disposing)
            {
                throw new ObjectDisposedException("RPC");
            }

            RPCVariable response = _client.CallMethod("removeEvent", new List<RPCVariable> { new RPCVariable(id) });
            if (response.ErrorStruct)
            {
                ThrowError("removeEvent", response);
            }
        }

        public void RemoveLink(Link link)
        {
            if (_disposing)
            {
                throw new ObjectDisposedException("RPC");
            }

            RPCVariable response = null;
            if (link.IsSender)
            {
                response = _client.CallMethod("removeLink", new List<RPCVariable> { new RPCVariable(link.PeerID), new RPCVariable(link.Channel), new RPCVariable(link.RemotePeerID), new RPCVariable(link.RemoteChannel) });
            }
            else
            {
                response = _client.CallMethod("removeLink", new List<RPCVariable> { new RPCVariable(link.RemotePeerID), new RPCVariable(link.RemoteChannel), new RPCVariable(link.PeerID), new RPCVariable(link.Channel) });
            }
            if (response.ErrorStruct)
            {
                ThrowError("removeLink", response);
            }
        }

        public long RunScript(string filename)
        {
            if (_disposing)
            {
                throw new ObjectDisposedException("RPC");
            }

            RPCVariable response = _client.CallMethod("runScript", new List<RPCVariable> { new RPCVariable(filename) });
            if (response.ErrorStruct)
            {
                ThrowError("runScript", response);
            }

            return response.IntegerValue;
        }

        public long RunScript(string filename, bool wait)
        {
            if (_disposing)
            {
                throw new ObjectDisposedException("RPC");
            }

            RPCVariable response = _client.CallMethod("runScript", new List<RPCVariable> { new RPCVariable(filename), new RPCVariable(wait) });
            if (response.ErrorStruct)
            {
                ThrowError("runScript", response);
            }

            return response.IntegerValue;
        }

        public long RunScript(string filename, string arguments)
        {
            if (_disposing)
            {
                throw new ObjectDisposedException("RPC");
            }

            RPCVariable response = _client.CallMethod("runScript", new List<RPCVariable> { new RPCVariable(filename), new RPCVariable(arguments) });
            if (response.ErrorStruct)
            {
                ThrowError("runScript", response);
            }

            return response.IntegerValue;
        }

        public long RunScript(string filename, string arguments, bool wait)
        {
            if (_disposing)
            {
                throw new ObjectDisposedException("RPC");
            }

            RPCVariable response = _client.CallMethod("runScript", new List<RPCVariable> { new RPCVariable(filename), new RPCVariable(arguments), new RPCVariable(wait) });
            if (response.ErrorStruct)
            {
                ThrowError("runScript", response);
            }

            return response.IntegerValue;
        }

        public long SearchDevices()
        {
            if (_disposing)
            {
                throw new ObjectDisposedException("RPC");
            }

            RPCVariable response = _client.CallMethod("searchDevices", new List<RPCVariable>());
            if (response.ErrorStruct)
            {
                ThrowError("searchDevices", response);
            }

            return response.IntegerValue;
        }

        public long SearchDevices(long familyID)
        {
            if (_disposing)
            {
                throw new ObjectDisposedException("RPC");
            }

            RPCVariable response = _client.CallMethod("searchDevices", new List<RPCVariable> { new RPCVariable(familyID) });
            if (response.ErrorStruct)
            {
                ThrowError("searchDevices", response);
            }

            return response.IntegerValue;
        }

        public void SetInstallMode(bool value)
        {
            SetInstallMode(value, 60);
        }

        public void SetInstallMode(bool value, long duration)
        {
            if (_disposing)
            {
                throw new ObjectDisposedException("RPC");
            }

            RPCVariable response = null;
            if (value)
            {
                response = _client.CallMethod("setInstallMode", new List<RPCVariable> { new RPCVariable(value), new RPCVariable(duration) });
            }
            else
            {
                response = _client.CallMethod("setInstallMode", new List<RPCVariable> { new RPCVariable(value) });
            }

            if (response.ErrorStruct)
            {
                ThrowError("setInstallMode", response);
            }
        }

        public void SetInstallMode(long familyID, bool value)
        {
            SetInstallMode(familyID, value, 60);
        }

        public void SetInstallMode(long familyID, bool value, long duration)
        {
            if (_disposing)
            {
                throw new ObjectDisposedException("RPC");
            }

            RPCVariable response = _client.CallMethod("setInstallMode", new List<RPCVariable> { new RPCVariable(familyID), new RPCVariable(value), new RPCVariable(duration) });
            if (response.ErrorStruct)
            {
                ThrowError("setInstallMode", response);
            }
        }

        public void SetId(long currentPeerID, long newPeerId)
        {
            if (_disposing)
            {
                throw new ObjectDisposedException("RPC");
            }

            RPCVariable response = _client.CallMethod("setId", new List<RPCVariable> { new RPCVariable(currentPeerID), new RPCVariable(newPeerId) });
            if (response.ErrorStruct)
            {
                ThrowError("setId", response);
            }
        }

        public void SetInterface(long peerId, Interface physicalInterface)
        {
            if (_disposing)
            {
                throw new ObjectDisposedException("RPC");
            }

            RPCVariable response;
            if (physicalInterface == null)
            {
                response = _client.CallMethod("setInterface", new List<RPCVariable> { new RPCVariable(peerId), new RPCVariable("") });
            }
            else
            {
                response = _client.CallMethod("setInterface", new List<RPCVariable> { new RPCVariable(peerId), new RPCVariable(physicalInterface.ID) });
            }

            if (response.ErrorStruct)
            {
                ThrowError("setInterface", response);
            }
        }

        public void SetName(long peerId, string name)
        {
            if (_disposing)
            {
                throw new ObjectDisposedException("RPC");
            }

            RPCVariable response = _client.CallMethod("setName", new List<RPCVariable> { new RPCVariable(peerId), new RPCVariable(name) });
            if (response.ErrorStruct)
            {
                ThrowError("setName", response);
            }
        }

        public void SetName(long peerId, long channel, string name)
        {
            if (_disposing)
            {
                throw new ObjectDisposedException("RPC");
            }

            RPCVariable response = _client.CallMethod("setName", new List<RPCVariable> { new RPCVariable(peerId), new RPCVariable(channel), new RPCVariable(name) });
            if (response.ErrorStruct)
            {
                ThrowError("setName", response);
            }
        }

        public void SetLinkInfo(Link link)
        {
            if (_disposing)
            {
                throw new ObjectDisposedException("RPC");
            }

            RPCVariable response = null;
            if (link.IsSender)
            {
                response = _client.CallMethod("setLinkInfo", new List<RPCVariable> { new RPCVariable(link.PeerID), new RPCVariable(link.Channel), new RPCVariable(link.RemotePeerID), new RPCVariable(link.RemoteChannel), new RPCVariable(link.Name), new RPCVariable(link.Description) });
            }
            else
            {
                response = _client.CallMethod("setLinkInfo", new List<RPCVariable> { new RPCVariable(link.RemotePeerID), new RPCVariable(link.RemoteChannel), new RPCVariable(link.PeerID), new RPCVariable(link.Channel), new RPCVariable(link.Name), new RPCVariable(link.Description) });
            }

            if (response.ErrorStruct)
            {
                ThrowError("setLinkInfo", response);
            }
        }

        public void SetMetadata(MetadataVariable variable)
        {
            if (_disposing)
            {
                throw new ObjectDisposedException("RPC");
            }

            RPCVariable response = _client.CallMethod("setMetadata", new List<RPCVariable> { new RPCVariable(variable.PeerID), new RPCVariable(variable.Name), variable });
            if (response.ErrorStruct)
            {
                ThrowError("setMetadata", response);
            }
        }

        public void SetSystemVariable(SystemVariable variable)
        {
            if (_disposing)
            {
                throw new ObjectDisposedException("RPC");
            }

            RPCVariable response = _client.CallMethod("setSystemVariable", new List<RPCVariable> { new RPCVariable(variable.Name), variable });
            if (response.ErrorStruct)
            {
                ThrowError("setSystemVariable", response);
            }
        }

        public void SetTeam(long peerId, long peerChannel)
        {
            SetTeam(peerId, peerChannel, 0, -1);
        }

        public void SetTeam(long peerId, long peerChannel, long teamID, long teamChannel)
        {
            if (_disposing)
            {
                throw new ObjectDisposedException("RPC");
            }

            RPCVariable response = _client.CallMethod("setTeam", new List<RPCVariable> { new RPCVariable(peerId), new RPCVariable(peerChannel), new RPCVariable(teamID), new RPCVariable(teamChannel) });
            if (response.ErrorStruct)
            {
                ThrowError("setTeam", response);
            }
        }

        public void SetValue(Variable variable)
        {
            if (_disposing)
            {
                throw new ObjectDisposedException("RPC");
            }

            RPCVariable response = _client.CallMethod("setValue", new List<RPCVariable> { new RPCVariable(variable.PeerID), new RPCVariable(variable.Channel), new RPCVariable(variable.Name), new RPCVariable(variable), new RPCVariable(variable.SetValueWait) });
            if (response.ErrorStruct)
            {
                ThrowError("setValue", response);
            }
        }

        public void StartSniffing(Family family)
        {
            if (_disposing)
            {
                throw new ObjectDisposedException("RPC");
            }

            RPCVariable response = _client.CallMethod("startSniffing", new List<RPCVariable> { new RPCVariable(family.ID) });
            if (response.ErrorStruct)
            {
                ThrowError("startSniffing", response);
            }
        }

        public void StopSniffing(Family family)
        {
            if (_disposing)
            {
                throw new ObjectDisposedException("RPC");
            }

            RPCVariable response = _client.CallMethod("stopSniffing", new List<RPCVariable> { new RPCVariable(family.ID) });
            if (response.ErrorStruct)
            {
                ThrowError("stopSniffing", response);
            }
        }

        public void TriggerEvent(string id)
        {
            if (_disposing)
            {
                throw new ObjectDisposedException("RPC");
            }

            RPCVariable response = _client.CallMethod("triggerEvent", new List<RPCVariable> { new RPCVariable(id) });
            if (response.ErrorStruct)
            {
                ThrowError("triggerEvent", response);
            }
        }

        public void UpdateFirmware(Device device, bool manually)
        {
            if (_disposing)
            {
                throw new ObjectDisposedException("RPC");
            }

            RPCVariable response = _client.CallMethod("updateFirmware", new List<RPCVariable> { new RPCVariable(device.ID), new RPCVariable(manually) });
            if (response.ErrorStruct)
            {
                ThrowError("updateFirmware", response);
            }
        }
        #endregion

        #region Buildings
        public void AddStoryToBuilding(Building building, Story story)
        {
            if (_disposing)
            {
                throw new ObjectDisposedException("RPC");
            }

            RPCVariable response = _client.CallMethod("addStoryToBuilding", new List<RPCVariable> { new RPCVariable(building.ID), new RPCVariable(story.ID) });
            if (response.ErrorStruct)
            {
                ThrowError("addStoryToBuilding", response);
            }
        }

        public void RemoveStoryFromBuilding(Building building, Story story)
        {
            if (_disposing)
            {
                throw new ObjectDisposedException("RPC");
            }

            RPCVariable response = _client.CallMethod("removeStoryFromBuilding", new List<RPCVariable> { new RPCVariable(building.ID), new RPCVariable(story.ID) });
            if (response.ErrorStruct)
            {
                ThrowError("removeStoryFromBuilding", response);
            }
        }

        public ulong CreateBuilding(Building building)
        {
            return Create("createBuilding", building.Translations);
        }

        public ulong DeleteBuilding(Building building)
        {
            return Delete("deleteBuilding", building.ID);
        }

        public ulong UpdateBuilding(Building building)
        {
            return Update("updateBuilding", building.ID, building.Translations);
        }

        public Dictionary<ulong, Building> GetBuildings()
        {
            if (_disposing)
            {
                throw new ObjectDisposedException("RPC");
            }

            var buildings = new Dictionary<ulong, Building>();
            RPCVariable response = _client.CallMethod("getBuildings", null);
            if (response.ErrorStruct)
            {
                ThrowError("getBuildings", response);
            }

            foreach (RPCVariable buildingStruct in response.ArrayValue)
            {
                if (!buildingStruct.StructValue.ContainsKey("ID") || !buildingStruct.StructValue.ContainsKey("TRANSLATIONS"))
                {
                    continue;
                }

                var translations = new Dictionary<string, string>();
                foreach (var element in buildingStruct.StructValue["TRANSLATIONS"].StructValue)
                {
                    translations.Add(element.Key, element.Value.StringValue);
                }

                Building building = new Building(this, (ulong)buildingStruct.StructValue["ID"].IntegerValue, translations);

                var stories = new Dictionary<ulong, Story>();
                if (buildingStruct.StructValue.ContainsKey("STORIES"))
                {
                    foreach (var element in buildingStruct.StructValue["STORIES"].ArrayValue)
                    {
                        if(Stories.ContainsKey((ulong)element.IntegerValue))
                        {
                            stories.Add((ulong)element.IntegerValue, Stories[(ulong)element.IntegerValue]);
                        }
                    }
                }
                building.Stories = new Stories(this, stories);

                buildings.Add(building.ID, building);
            }
            return buildings;
        }

        public Dictionary<string, RPCVariable> GetBuildingMetadata(Building building)
        {
            if (_disposing)
            {
                throw new ObjectDisposedException("RPC");
            }

            RPCVariable response = _client.CallMethod("getBuildingMetadata", new List<RPCVariable> { new RPCVariable(building.ID) });
            if (response.ErrorStruct)
            {
                ThrowError("getBuildingMetadata", response);
            }

            return response.StructValue;
        }

        public void SetBuildingMetadata(Building building, Dictionary<string, RPCVariable> metadata)
        {
            if (_disposing)
            {
                throw new ObjectDisposedException("RPC");
            }

            var metadataStruct = new RPCVariable(RPCVariableType.rpcStruct);
            foreach (var m in metadata)
            {
                metadataStruct.StructValue.Add(m.Key, m.Value);
            }

            RPCVariable response = _client.CallMethod("setBuildingMetadata", new List<RPCVariable> { new RPCVariable(building.ID), metadataStruct });
            if (response.ErrorStruct)
            {
                ThrowError("setBuildingMetadata", response);
            }
        }
        #endregion

        #region Stories
        public void AddRoomToStory(Story story, Room room)
        {
            if (_disposing)
            {
                throw new ObjectDisposedException("RPC");
            }

            RPCVariable response = _client.CallMethod("addRoomToStory", new List<RPCVariable> { new RPCVariable(story.ID), new RPCVariable(room.ID) });
            if (response.ErrorStruct)
            {
                ThrowError("addRoomToStory", response);
            }
        }

        public void RemoveRoomFromStory(Story story, Room room)
        {
            if (_disposing)
            {
                throw new ObjectDisposedException("RPC");
            }

            RPCVariable response = _client.CallMethod("removeRoomFromStory", new List<RPCVariable> { new RPCVariable(story.ID), new RPCVariable(room.ID) });
            if (response.ErrorStruct)
            {
                ThrowError("removeRoomFromStory", response);
            }
        }

        public ulong CreateStory(Story story)
        {
            return Create("createStory", story.Translations);
        }

        public ulong UpdateStory(Story story)
        {
            return Update("updateStory", story.ID, story.Translations);
        }

        public ulong DeleteStory(Story story)
        {
            return Delete("deleteStory", story.ID);
        }

        public Dictionary<ulong, Story> GetStories()
        {
            if (_disposing)
            {
                throw new ObjectDisposedException("RPC");
            }

            var stories = new Dictionary<ulong, Story>();
            RPCVariable response = _client.CallMethod("getStories", null);
            if (response.ErrorStruct)
            {
                ThrowError("getStories", response);
            }

            foreach (RPCVariable storyStruct in response.ArrayValue)
            {
                if (!storyStruct.StructValue.ContainsKey("ID") || !storyStruct.StructValue.ContainsKey("TRANSLATIONS"))
                {
                    continue;
                }

                var translations = new Dictionary<string, string>();
                foreach (var element in storyStruct.StructValue["TRANSLATIONS"].StructValue)
                {
                    translations.Add(element.Key, element.Value.StringValue);
                }

                Story story = new Story(this, (ulong)storyStruct.StructValue["ID"].IntegerValue, translations);

                var rooms = new Dictionary<ulong, Room>();
                if (storyStruct.StructValue.ContainsKey("ROOMS"))
                {
                    foreach (var element in storyStruct.StructValue["ROOMS"].ArrayValue)
                    {
                        if (Rooms.ContainsKey((ulong)element.IntegerValue))
                        {
                            rooms.Add((ulong)element.IntegerValue, Rooms[(ulong)element.IntegerValue]);
                        }
                    }
                }
                story.Rooms = new Rooms(this, rooms);

                stories.Add(story.ID, story);
            }
            return stories;
        }

        public Dictionary<string, RPCVariable> GetStoryMetadata(Story story)
        {
            if (_disposing)
            {
                throw new ObjectDisposedException("RPC");
            }

            RPCVariable response = _client.CallMethod("getStoryMetadata", new List<RPCVariable> { new RPCVariable(story.ID) });
            if (response.ErrorStruct)
            {
                ThrowError("getStoryMetadata", response);
            }

            return response.StructValue;
        }

        public void SetStoryMetadata(Story story, Dictionary<string, RPCVariable> metadata)
        {
            if (_disposing)
            {
                throw new ObjectDisposedException("RPC");
            }

            var metadataStruct = new RPCVariable(RPCVariableType.rpcStruct);
            foreach (var m in metadata)
            {
                metadataStruct.StructValue.Add(m.Key, m.Value);
            }

            RPCVariable response = _client.CallMethod("setStoryMetadata", new List<RPCVariable> { new RPCVariable(story.ID), metadataStruct });
            if (response.ErrorStruct)
            {
                ThrowError("setStoryMetadata", response);
            }
        }
        #endregion

        #region Rooms
        public void AddChannelToRoom(Channel channel, Room room)
        {
            if (_disposing)
            {
                throw new ObjectDisposedException("RPC");
            }

            RPCVariable response = _client.CallMethod("addChannelToRoom", new List<RPCVariable> { new RPCVariable(channel.PeerID), new RPCVariable(channel.Index), new RPCVariable(room.ID) });
            if (response.ErrorStruct)
            {
                ThrowError("addChannelToRoom", response);
            }
        }

        public void AddDeviceToRoom(Device device, Room room)
        {
            if (_disposing)
            {
                throw new ObjectDisposedException("RPC");
            }

            RPCVariable response = _client.CallMethod("addDeviceToRoom", new List<RPCVariable> { new RPCVariable(device.ID), new RPCVariable(room.ID) });
            if (response.ErrorStruct)
            {
                ThrowError("addDeviceToRoom", response);
            }
        }

        public void AddVariableToRoom(Variable variable, Room room)
        {
            if (_disposing)
            {
                throw new ObjectDisposedException("RPC");
            }

            RPCVariable response = _client.CallMethod("addVariableToRoom", new List<RPCVariable> { new RPCVariable(variable.PeerID), new RPCVariable(variable.Channel), new RPCVariable(variable.Name), new RPCVariable(room.ID) });
            if (response.ErrorStruct)
            {
                ThrowError("addVariableToRoom", response);
            }
        }

        public ulong CreateRoom(Room room)
        {
            return Create("createRoom", room.Translations);
        }

        public ulong UpdateRoom(Room room)
        {
            return Update("updateRoom", room.ID, room.Translations);
        }


        public ulong DeleteRoom(Room room)
        {
            return Delete("deleteRoom", room.ID);
        }

        public Dictionary<ulong, Room> GetRooms()
        {
            if (_disposing)
            {
                throw new ObjectDisposedException("RPC");
            }

            var rooms = new Dictionary<ulong, Room>();
            RPCVariable response = _client.CallMethod("getRooms", null);
            if (response.ErrorStruct)
            {
                ThrowError("getRooms", response);
            }

            foreach (RPCVariable roomStruct in response.ArrayValue)
            {
                if (!roomStruct.StructValue.ContainsKey("ID") || !roomStruct.StructValue.ContainsKey("TRANSLATIONS"))
                {
                    continue;
                }

                var translations = new Dictionary<string, string>();
                foreach (var element in roomStruct.StructValue["TRANSLATIONS"].StructValue)
                {
                    translations.Add(element.Key, element.Value.StringValue);
                }

                Room room = new Room(this, (ulong)roomStruct.StructValue["ID"].IntegerValue, translations);

                rooms.Add(room.ID, room);
            }
            return rooms;
        }

        public void RemoveChannelFromRoom(Channel channel, Room room)
        {
            if (_disposing)
            {
                throw new ObjectDisposedException("RPC");
            }

            RPCVariable response = _client.CallMethod("removeChannelFromRoom", new List<RPCVariable> { new RPCVariable(channel.PeerID), new RPCVariable(channel.Index), new RPCVariable(room.ID) });
            if (response.ErrorStruct)
            {
                ThrowError("removeChannelFromRoom", response);
            }
        }

        public void RemoveDeviceFromRoom(Device device, Room room)
        {
            if (_disposing)
            {
                throw new ObjectDisposedException("RPC");
            }

            RPCVariable response = _client.CallMethod("removeDeviceFromRoom", new List<RPCVariable> { new RPCVariable(device.ID), new RPCVariable(room.ID) });
            if (response.ErrorStruct)
            {
                ThrowError("removeDeviceFromRoom", response);
            }
        }

        public void RemoveVariableFromRoom(Variable variable, Room room)
        {
            if (_disposing)
            {
                throw new ObjectDisposedException("RPC");
            }

            RPCVariable response = _client.CallMethod("removeVariableFromRoom", new List<RPCVariable> { new RPCVariable(variable.PeerID), new RPCVariable(variable.Channel), new RPCVariable(variable.Name), new RPCVariable(room.ID) });
            if (response.ErrorStruct)
            {
                ThrowError("removeVariableFromRoom", response);
            }
        }

        public Dictionary<string, RPCVariable> GetRoomMetadata(Room room)
        {
            if (_disposing)
            {
                throw new ObjectDisposedException("RPC");
            }

            RPCVariable response = _client.CallMethod("getRoomMetadata", new List<RPCVariable> { new RPCVariable(room.ID) });
            if (response.ErrorStruct)
            {
                ThrowError("getRoomMetadata", response);
            }

            return response.StructValue;
        }

        public void SetRoomMetadata(Room room, Dictionary<string, RPCVariable> metadata)
        {
            if (_disposing)
            {
                throw new ObjectDisposedException("RPC");
            }

            var metadataStruct = new RPCVariable(RPCVariableType.rpcStruct);
            foreach (var m in metadata)
            {
                metadataStruct.StructValue.Add(m.Key, m.Value);
            }

            RPCVariable response = _client.CallMethod("setRoomMetadata", new List<RPCVariable> { new RPCVariable(room.ID), metadataStruct });
            if (response.ErrorStruct)
            {
                ThrowError("setRoomMetadata", response);
            }
        }

        #endregion

        #region Common

        public ulong Create(String call, Dictionary<String, String> translations)
        {
            if (_disposing)
            {
                throw new ObjectDisposedException("RPC");
            }

            var translationsStruct = new RPCVariable(RPCVariableType.rpcStruct);
            foreach (var translation in translations)
            {
                translationsStruct.StructValue.Add(translation.Key, new RPCVariable(translation.Value));
            }

            RPCVariable response = _client.CallMethod(call, new List<RPCVariable> { translationsStruct });
            if (response.ErrorStruct)
            {
                ThrowError(call, response);
            }

            return (ulong)response.IntegerValue;
        }

        public ulong Update(String call, ulong id, Dictionary<String, String> translations)
        {
            if (_disposing)
            {
                throw new ObjectDisposedException("RPC");
            }

            var translationsStruct = new RPCVariable(RPCVariableType.rpcStruct);
            foreach (var translation in translations)
            {
                translationsStruct.StructValue.Add(translation.Key, new RPCVariable(translation.Value));
            }

            RPCVariable response = _client.CallMethod(call, new List<RPCVariable> { new RPCVariable(id), translationsStruct });
            if (response.ErrorStruct)
            {
                ThrowError(call, response);
            }

            return (ulong)response.IntegerValue;
        }


        public ulong Delete(String call, ulong id)
        {
            if (_disposing)
            {
                throw new ObjectDisposedException("RPC");
            }

            RPCVariable response = _client.CallMethod(call, new List<RPCVariable> { new RPCVariable(id) });
            if (response.ErrorStruct)
            {
                ThrowError(call, response);
            }

            return (ulong)response.IntegerValue;
        }

        #endregion

        #region Roles
        public void AddRoleToVariable(Variable variable, Variable.RoleElement role)
        {
            if (_disposing)
            {
                throw new ObjectDisposedException("RPC");
            }

            RPCVariable response = _client.CallMethod("addRoleToVariable", new List<RPCVariable> { new RPCVariable(variable.PeerID), new RPCVariable(variable.Channel), new RPCVariable(variable.Name), new RPCVariable(role.ID), new RPCVariable((long)role.Direction), new RPCVariable(role.Invert) });
            if (response.ErrorStruct)
            {
                ThrowError("addRoleToVariable", response);
            }
        }

        public ulong CreateRole(Role role)
        {
            return Create("createRole", role.Translations);
        }

        public ulong UpdateRole(Role role)
        {
            return Update("updateRole", role.ID, role.Translations);
        }

        public ulong DeleteRole(Role role)
        {
            return Delete("deleteRole", role.ID);
        }

        public Dictionary<ulong, Role> GetRoles()
        {
            if (_disposing)
            {
                throw new ObjectDisposedException("RPC");
            }

            var roles = new Dictionary<ulong, Role>();
            RPCVariable response = _client.CallMethod("getRoles", null);
            if (response.ErrorStruct)
            {
                ThrowError("getRoles", response);
            }

            foreach (RPCVariable roleStruct in response.ArrayValue)
            {
                if (!roleStruct.StructValue.ContainsKey("ID") || !roleStruct.StructValue.ContainsKey("TRANSLATIONS"))
                {
                    continue;
                }

                var translations = new Dictionary<string, string>();
                foreach(var element in roleStruct.StructValue["TRANSLATIONS"].StructValue)
                {
                    translations.Add(element.Key, element.Value.StringValue);
                }

                Role role = new Role((ulong)roleStruct.StructValue["ID"].IntegerValue, translations);

                roles.Add(role.ID, role);
            }
            return roles;
        }

        public void RemoveRoleFromVariable(Variable variable, Variable.RoleElement role)
        {
            if (_disposing)
            {
                throw new ObjectDisposedException("RPC");
            }

            RPCVariable response = _client.CallMethod("removeRoleFromVariable", new List<RPCVariable> { new RPCVariable(variable.PeerID), new RPCVariable(variable.Channel), new RPCVariable(variable.Name), new RPCVariable(role.ID) });
            if (response.ErrorStruct)
            {
                ThrowError("removeRoleFromVariable", response);
            }
        }

        #endregion

        #region Management
        public void ManagementUploadDeviceDescriptionFile(string filename, ref byte[] data, ulong familyID)
        {
            if (_disposing)
            {
                throw new ObjectDisposedException("RPC");
            }

            RPCVariable response = _client.CallMethod("managementUploadDeviceDescriptionFile", new List<RPCVariable> { new RPCVariable(filename), new RPCVariable(ref data), new RPCVariable(familyID), new RPCVariable(false) });
            if (response.ErrorStruct)
            {
                ThrowError("managementUploadDeviceDescriptionFile", response);
            }
        }
        #endregion

        #region UI
        public ulong AddUiElement(Variable variable, string label)
        {
            if (_disposing)
            {
                throw new ObjectDisposedException("RPC");
            }

            RPCVariable response = _client.CallMethod("addUiElement", new List<RPCVariable> { new RPCVariable(variable.PeerID), new RPCVariable(variable.Channel), new RPCVariable(variable.Name), new RPCVariable(label) });
            if (response.ErrorStruct)
            {
                ThrowError("addUiElement", response);
            }

            return (ulong)response.IntegerValue;
        }

        public struct CheckUiElementSimpleCreationResult
        {
            public bool Visualizable;
            public bool Visualized;
            public List<ulong> VisualizedByUiElements;
            public string VisualizationCheckError;
        }

        public CheckUiElementSimpleCreationResult CheckUiElementSimpleCreation(Variable variable)
        {
            if (_disposing)
            {
                throw new ObjectDisposedException("RPC");
            }

            RPCVariable response = _client.CallMethod("checkUiElementSimpleCreation", new List<RPCVariable> { new RPCVariable(variable.PeerID), new RPCVariable(variable.Channel), new RPCVariable(variable.Name) });
            if (response.ErrorStruct)
            {
                ThrowError("checkUiElementSimpleCreation", response);
            }

            CheckUiElementSimpleCreationResult result = new CheckUiElementSimpleCreationResult();
            if (response.StructValue.ContainsKey("visualizable")) result.Visualizable = response.StructValue["visualizable"].BooleanValue;
            else result.Visualizable = false;
            if (response.StructValue.ContainsKey("visualized")) result.Visualized = response.StructValue["visualized"].BooleanValue;
            else result.Visualizable = false;
            result.VisualizedByUiElements = new List<ulong>();
            if (response.StructValue.ContainsKey("uiElements"))
            {
                foreach(var element in response.StructValue["uiElements"].ArrayValue)
                {
                    result.VisualizedByUiElements.Add((ulong)element.IntegerValue);
                }
            }
            if (response.StructValue.ContainsKey("reason")) result.VisualizationCheckError = response.StructValue["reason"].StringValue;

            return result;
        }

        public void RemoveUiElement(Variable variable)
        {
            if (_disposing)
            {
                throw new ObjectDisposedException("RPC");
            }
           
            foreach (var id in variable.VisualizedByUiElements)
            {
                RPCVariable response = _client.CallMethod("removeUiElement", new List<RPCVariable> { new RPCVariable(id) });
                if (response.ErrorStruct)
                {
                    ThrowError("removeUiElement", response);
                }
            }
        }

        public List<RPCVariable> GetAllUiElements(string language = "en-US")
        {
            if (_disposing)
            {
                throw new ObjectDisposedException("RPC");
            }

           
            RPCVariable response = _client.CallMethod("getAllUiElements", new List<RPCVariable> { new RPCVariable(language) } );
            if (response.ErrorStruct)
            {
                ThrowError("getAllUiElements", response);
            }

            return response.ArrayValue;
        }

        public List<RPCVariable> GetAvailableUiElements(string language = "en-US")
        {
            if (_disposing)
            {
                throw new ObjectDisposedException("RPC");
            }


            RPCVariable response = _client.CallMethod("getAvailableUiElements", new List<RPCVariable> { new RPCVariable(language) });
            if (response.ErrorStruct)
            {
                ThrowError("getAvailableUiElements", response);
            }

            return response.ArrayValue;
        }

        public List<RPCVariable> GetRoomUiElements(Room room, string language = "en-US")
        {
            if (_disposing)
            {
                throw new ObjectDisposedException("RPC");
            }


            RPCVariable response = _client.CallMethod("getRoomUiElements", new List<RPCVariable> { new RPCVariable(room.ID), new RPCVariable(language) });
            if (response.ErrorStruct)
            {
                ThrowError("getRoomUiElements", response);
            }

            return response.ArrayValue;
        }

        public Dictionary<string, RPCVariable> GetUiElementMetadata(ulong id)
        {
            if (_disposing)
            {
                throw new ObjectDisposedException("RPC");
            }

            RPCVariable response = _client.CallMethod("getUiElementMetadata", new List<RPCVariable> { new RPCVariable(id) });
            if (response.ErrorStruct)
            {
                ThrowError("getUiElementMetadata", response);
            }

            return response.StructValue;
        }

        public void SetUiElementMetadata(ulong id, Dictionary<string, RPCVariable> metadata)
        {
            if (_disposing)
            {
                throw new ObjectDisposedException("RPC");
            }

            var metadataStruct = new RPCVariable(RPCVariableType.rpcStruct);
            foreach (var m in metadata)
            {
                metadataStruct.StructValue.Add(m.Key, m.Value);
            }

            RPCVariable response = _client.CallMethod("setUiElementMetadata", new List<RPCVariable> { new RPCVariable(id), metadataStruct });
            if (response.ErrorStruct)
            {
                ThrowError("setUiElementMetadata", response);
            }
        }


        #endregion
    }
}
