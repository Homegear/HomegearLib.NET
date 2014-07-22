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

    public enum RPCUpdateDeviceFlags
    {
        rpcConfig = 0,
        rpcLinks = 1
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
        public delegate void DeviceVariableUpdatedEventHandler(RPCController sender, Variable value);
        public delegate void SystemVariableUpdatedEventHandler(RPCController sender, SystemVariable value);
        public delegate void NewDevicesEventHandler(RPCController sender);
        public delegate void DevicesDeletedEventHandler(RPCController sender);
        public delegate void UpdateDeviceEventHandler(RPCController sender, Int32 peerID, Int32 channel, RPCUpdateDeviceFlags flags);
        public delegate void ConnectedEventHandler(RPCController sender);
        public delegate void DisconnectedEventHandler(RPCController sender);
        public delegate void InitCompletedEventHandler(RPCController sender);

        #region "Events"
        public event DeviceVariableUpdatedEventHandler DeviceVariableUpdated;
        public event SystemVariableUpdatedEventHandler SystemVariableUpdated;
        public event NewDevicesEventHandler NewDevices;
        public event DevicesDeletedEventHandler DevicesDeleted;
        public event UpdateDeviceEventHandler UpdateDevice;
        public event ConnectedEventHandler Connected;
        public event DisconnectedEventHandler Disconnected;
        public event InitCompletedEventHandler InitCompleted;
        #endregion

        String _callbackHostname = "";
        
        volatile bool _disposing = false;
        public bool IsConnected { get { return _client != null && _client.IsConnected; } }

        Dictionary<Int32, Family> _families = null;
        public Dictionary<Int32, Family> Families
        {
            get
            {
                if (_families == null || _families.Count == 0) _families = ListFamilies();
                return _families;
            }
        }

        Dictionary<Int32, Device> _devices = null;
        public Dictionary<Int32, Device> Devices
        {
            get
            {
                if (_devices == null || _devices.Count == 0) _devices = GetAllValues();
                return _devices;
            }
        }

        Dictionary<String, Interface> _interfaces = null;
        public Dictionary<String, Interface> Interfaces
        {
            get
            {
                if (_interfaces == null || _interfaces.Count == 0) _interfaces = ListInterfaces();
                return _interfaces;
            }
        }

        Dictionary<String, SystemVariable> _systemVariables = null;
        public Dictionary<String, SystemVariable> SystemVariables
        {
            get
            {
                if (_systemVariables == null || _systemVariables.Count == 0) _systemVariables = GetAllSystemVariables();
                return _systemVariables;
            }
            internal set
            {
                _systemVariables = value;
            }
        }

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
            _server.RPCEvent += _server_OnRPCEvent;
            _server.NewDevices += _server_OnNewDevices;
            _server.DevicesDeleted += _server_OnDevicesDeleted;
            _server.UpdateDevice += _server_OnUpdateDevice;
            _keepAliveTimer = new System.Timers.Timer(10000);
            _keepAliveTimer.Elapsed += _workerTimer_Elapsed;
        }

        void _server_OnUpdateDevice(RPCServer sender, int peerID, int channel, int flags)
        {
            if (UpdateDevice != null) UpdateDevice(this, peerID, channel, (RPCUpdateDeviceFlags)flags);
        }

        private void _server_OnDevicesDeleted(RPCServer sender)
        {
            if (DevicesDeleted != null) DevicesDeleted(this);
        }

        void _server_OnNewDevices(RPCServer sender)
        {
            if (NewDevices != null) NewDevices(this);
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
            if (peerID == 0)
            {
                if (SystemVariableUpdated != null) SystemVariableUpdated(this, new SystemVariable(null, parameterName, value));
            }
            else
            {
                if (DeviceVariableUpdated != null) DeviceVariableUpdated(this, new Variable(peerID, channel, parameterName, value));
            }
        }

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

        public void Clear()
        {
            _families = null;
            _devices = null;
            _interfaces = null;
            _systemVariables = null;
        }

        void _client_Disconnected(RPCClient sender)
        {
            if (Disconnected != null) Disconnected(this);
        }

        void _client_Connected(RPCClient sender)
        {
            if (Connected != null) Connected(this);
            _server.KnownDevices = Devices;
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

        public void DeleteSystemVariable(SystemVariable variable)
        {
            if (_disposing) throw new ObjectDisposedException("RPC");
            RPCVariable response = _client.CallMethod("deleteSystemVariable", new List<RPCVariable> { new RPCVariable(variable.Name) });
            if (response.ErrorStruct) ThrowError("deleteSystemVariable", response);
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
                            if (variableInfo.ContainsKey("SPECIAL")) variable.SetSpecialValues(variableInfo["SPECIAL"]);
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

        public void GetDeviceDescription(Device device)
        {
            if (_disposing) throw new ObjectDisposedException("RPC");
            RPCVariable response = _client.CallMethod("getDeviceDescription", new List<RPCVariable> { new RPCVariable(device.ID), new RPCVariable(-1) });
            if (response.ErrorStruct) ThrowError("getDeviceDescription", response);
            if (response.StructValue.ContainsKey("AES_ACTIVE")) device.AESActive = response.StructValue["AES_ACTIVE"].BooleanValue;
            if (response.StructValue.ContainsKey("PHYSICAL_ADDRESS")) device.Address = response.StructValue["PHYSICAL_ADDRESS"].IntegerValue;
            if (response.StructValue.ContainsKey("RX_MODE")) device.RXMode = (DeviceRXMode)response.StructValue["RX_MODE"].IntegerValue;
            if (response.StructValue.ContainsKey("FIRMWARE")) device.Firmware = response.StructValue["FIRMWARE"].StringValue;
            if (response.StructValue.ContainsKey("AVAILABLE_FIRMWARE")) device.AvailableFirmware = response.StructValue["AVAILABLE_FIRMWARE"].StringValue;
        }

        public void GetDeviceDescription(Channel channel)
        {
            if (_disposing) throw new ObjectDisposedException("RPC");
            RPCVariable response = _client.CallMethod("getDeviceDescription", new List<RPCVariable> { new RPCVariable(channel.PeerID), new RPCVariable(channel.Index) });
            if (response.ErrorStruct) ThrowError("getDeviceDescription", response);
            if (response.StructValue.ContainsKey("TYPE")) channel.TypeString = response.StructValue["TYPE"].StringValue;
            if (response.StructValue.ContainsKey("AES_ACTIVE")) channel.AESActive = response.StructValue["AES_ACTIVE"].BooleanValue;
            if (response.StructValue.ContainsKey("DIRECTION")) channel.Direction = (ChannelDirection)response.StructValue["DIRECTION"].IntegerValue;
            if (response.StructValue.ContainsKey("LINK_SOURCE_ROLES")) channel.LinkSourceRoles = response.StructValue["LINK_SOURCE_ROLES"].StringValue.Split(' ');
            if (response.StructValue.ContainsKey("LINK_TARGET_ROLES")) channel.LinkTargetRoles = response.StructValue["LINK_TARGET_ROLES"].StringValue.Split(' ');
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
            if (response.StructValue.ContainsKey("TEAM")) channel.Team = response.StructValue["TEAM"].StringValue;
            if (response.StructValue.ContainsKey("TEAM_TAG")) channel.Team = response.StructValue["TEAM_TAG"].StringValue;
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
                if(rpcLink.StructValue["SENDER_ID"].IntegerValue == peerID)
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
                if (rpcLink.StructValue.ContainsKey("NAME")) link.Name = rpcLink.StructValue["NAME"].StringValue;
                if (rpcLink.StructValue.ContainsKey("DESCRIPTION")) link.Description = rpcLink.StructValue["DESCRIPTION"].StringValue;
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
            RPCVariable response = _client.CallMethod("setInstallMode", new List<RPCVariable> { new RPCVariable(value), new RPCVariable(duration) });
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

        public void SetName(Int32 peerID, String name)
        {
            if (_disposing) throw new ObjectDisposedException("RPC");
            RPCVariable response = _client.CallMethod("setName", new List<RPCVariable> { new RPCVariable(peerID), new RPCVariable(name) });
            if (response.ErrorStruct) ThrowError("setName", response);
        }

        public void SetInterface(Int32 peerID, Interface physicalInterface)
        {
            if (_disposing) throw new ObjectDisposedException("RPC");
            RPCVariable response = _client.CallMethod("setInterface", new List<RPCVariable> { new RPCVariable(peerID), new RPCVariable(physicalInterface.ID) });
            if (response.ErrorStruct) ThrowError("setInterface", response);
        }

        public void SetSystemVariable(SystemVariable variable)
        {
            if (_disposing) throw new ObjectDisposedException("RPC");
            RPCVariable response = _client.CallMethod("setSystemVariable", new List<RPCVariable> { new RPCVariable(variable.Name), variable });
            if (response.ErrorStruct) ThrowError("setSystemVariable", response);
        }

        public void SetValue(Variable variable)
        {
            if (_disposing) throw new ObjectDisposedException("RPC");
            RPCVariable response = _client.CallMethod("setValue", new List<RPCVariable> { new RPCVariable(variable.PeerID), new RPCVariable(variable.Channel), new RPCVariable(variable.Name), new RPCVariable(variable) });
            if (response.ErrorStruct) ThrowError("setValue", response);
        }
        #endregion
    }
}
