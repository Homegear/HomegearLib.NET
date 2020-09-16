using HomegearLib.RPC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Threading;

namespace HomegearLib
{
    /// <summary>
    /// Used as a parameter for the event "ReloadRequired".
    /// </summary>
    public enum ReloadType
    {
        Full = 1,
        SystemVariables = 2,
        Events = 3,
        UI = 4
    }

    /// <summary>
    /// Used as a parameter for the event "DeviceReloadRequired".
    /// </summary>
    public enum DeviceReloadType
    {
        Full = 1,
        Metadata = 2,
        Channel = 4,
        Links = 8,
        Team = 16,
        Events = 32,
        Variables = 64
    }

    /// <summary>
    /// Used within the class UpdateResult to make the result code human-readable
    /// </summary>
    public enum UpdateResultCode
    {
        OK = 0,
        UnknownError = 1,
        NoVersionFileFound = 2,
        NoFirmwareFound = 3,
        CouldNotOpenFirmwareFile = 4,
        WrongFileFormat = 5,
        NoResponse = 6,
        NoUpdateRequest = 7,
        TooManyCommunicationErrors = 8,
        PhysicalInterfaceError = 9
    }

    /// <summary>
    /// Used within the class UpdateStatus. This class bundles update result information.
    /// </summary>
    public class UpdateResult
    {
        UpdateResultCode _code = UpdateResultCode.OK;
        public UpdateResultCode Code { get { return _code; } }

        string _description = "";
        public string Description { get { return _description; } }

        public UpdateResult(UpdateResultCode code, string description)
        {
            _code = code;
            _description = description;
        }
    }

    /// <summary>
    /// Returned by the method "GetUpdateStatus" of the Homegear object.
    /// </summary>
    public class UpdateStatus
    {
        long _currentDevice = -1;
        public long CurrentDevice { get { return _currentDevice; } }

        long _currentDeviceProgress = -1;
        public long CurrentDeviceProgress { get { return _currentDeviceProgress; } }

        long _deviceCount = -1;
        public long DeviceCount { get { return _deviceCount; } }

        long _currentUpdate = 0;
        public long CurrentUpdate { get { return _currentUpdate; } }

        ReadOnlyDictionary<long, UpdateResult> _results = new ReadOnlyDictionary<long, UpdateResult>();
        public ReadOnlyDictionary<long, UpdateResult> Results { get { return _results; } }

        public UpdateStatus(long currentDevice, long currentDeviceProgress, long deviceCount, long currentUpdate, Dictionary<long, UpdateResult> results)
        {
            _currentDevice = currentDevice;
            _currentDeviceProgress = currentDeviceProgress;
            _deviceCount = deviceCount;
            _currentUpdate = currentUpdate;
            _results = new ReadOnlyDictionary<long, UpdateResult>(results);
        }
    }

    /// <summary>
    /// Main class to access Homegear features.
    /// </summary>
    public class Homegear : IDisposable
    {
        public delegate void ConnectErrorEventHandler(Homegear sender, string message, string stackTrace);
        public delegate void ConnectedEventHandler(Homegear sender);
        public delegate void DisconnectedEventHandler(Homegear sender);
        public delegate void HomegearErrorEventHandler(Homegear sender, long level, string message);
        public delegate void DataReloadEventHandler(Homegear sender);
        public delegate void SystemVariableUpdatedEventHandler(Homegear sender, SystemVariable variable);
        public delegate void PongEventHandler(Homegear sender, string id);
        public delegate void MetadataUpdatedEventHandler(Homegear sender, Device device, MetadataVariable variable);
        public delegate void DeviceVariableUpdatedEventHandler(Homegear sender, Device device, Channel channel, Variable variable, string eventSource);
        public delegate void DeviceConfigParameterUpdatedEventHandler(Homegear sender, Device device, Channel channel, ConfigParameter parameter);
        public delegate void DeviceLinkConfigParameterUpdatedEventHandler(Homegear sender, Device device, Channel channel, Link link, ConfigParameter parameter);
        public delegate void EventUpdatedEventHandler(Homegear sender, Event homegearEvent);
        public delegate void ReloadRequiredEventHandler(Homegear sender, ReloadType reloadType);
        public delegate void DeviceReloadRequiredEventHandler(Homegear sender, Device device, Channel channel, DeviceReloadType reloadType);

        #region "Events"
        /// <summary>
        /// Raised when there is an error during the connection or reconnection procedure.
        /// </summary>
        public event ConnectErrorEventHandler ConnectError;

        /// <summary>
        /// Raised when an error occurs within Homegear.
        /// </summary>
        public event HomegearErrorEventHandler HomegearError;

        /// <summary>
        /// Raised after a full reload of the Homegear object. After this event is first raised, the Homegear object is ready for use.
        /// </summary>
        public event DataReloadEventHandler Reloaded;

        /// <summary>
        /// Raised when the value of a system variable has changed.
        /// </summary>
        public event SystemVariableUpdatedEventHandler SystemVariableUpdated;

        /// <summary>
        /// Raised when a pong packet is received.
        /// </summary>
        public event PongEventHandler Pong;

        /// <summary>
        /// Raised when the value of a metadata variable has changed.
        /// </summary>
        public event MetadataUpdatedEventHandler MetadataUpdated;

        /// <summary>
        /// Raised when the value of a device variable has changed.
        /// </summary>
        public event DeviceVariableUpdatedEventHandler DeviceVariableUpdated;

        /// <summary>
        /// Raised when the value of a device's configuration parameter has changed.
        /// </summary>
        public event DeviceConfigParameterUpdatedEventHandler DeviceConfigParameterUpdated;

        /// <summary>
        /// Raised when the value of a device's direct link configuration parameter has changed.
        /// </summary>
        public event DeviceLinkConfigParameterUpdatedEventHandler DeviceLinkConfigParameterUpdated;

        /// <summary>
        /// Raised when an event was raised or one of the event's parameters has changed.
        /// </summary>
        public event EventUpdatedEventHandler EventUpdated;

        /// <summary>
        /// Raised when a reload of the Homegear object or a subobject of the Homegear object is required.
        /// </summary>
        public event ReloadRequiredEventHandler ReloadRequired;

        /// <summary>
        /// Raised when a reload of a device object or a subobject of the device object is required.
        /// </summary>
        public event DeviceReloadRequiredEventHandler DeviceReloadRequired;
        #endregion

        private volatile bool _connecting = false;
        private object _reloadingLock = new object();
        private volatile bool _reloading = false;
        private RPCController _rpc = null;
        private volatile bool _events = false;
        private volatile bool _disposing = false;
        private volatile bool _disposed = false;
        private volatile bool _stopConnectThread = false;
        private Thread _connectThread = null;

        public RPCController Rpc { get { return _rpc; } }


        public long RestrictToFamilyId { get { return _rpc == null ? -1 : _rpc.RestrictToFamilyId; } }


        private Families _families = null;
        /// <summary>
        /// Dictionary to access all device families. The key is the family id, the value the family object. This property is currently only used to get the name of a device family.
        /// </summary>
        public Families Families { get { return _families; } }

        private Devices _devices = null;
        /// <summary>
        /// Dictionary to access all devices known to Homegear. The key is the device's id, the value the device object.
        /// </summary>
        public Devices Devices { get { return _devices; } }

        private SystemVariables _systemVariables = null;
        /// <summary>
        /// Dictionary to access all system variables known to Homegear. The key is the name of the system variable, the value the system variable object.
        /// </summary>
        public SystemVariables SystemVariables { get { return _systemVariables; } }

        private Interfaces _interfaces = null;
        /// <summary>
        /// Dictionary to access all of Homegear's physical interfaces. The key is the id string as specified in Homegear's physicalinterfaces.conf, the value the interface object.
        /// </summary>
        public Interfaces Interfaces
        {
            get
            {
                if (_interfaces == null || _interfaces.Count == 0)
                {
                    _interfaces = new Interfaces(_rpc, _rpc.Interfaces);
                }

                return _interfaces;
            }
        }

        private Buildings _buildings = null;
        /// <summary>
        /// Dictionary to access all of Homegear's buildings. The key is the building id, the value the building object.
        /// </summary>
        public Buildings Buildings
        {
            get
            {
                if (_buildings == null || _buildings.Count == 0)
                {
                    _buildings = new Buildings(_rpc, _rpc.Buildings);
                }

                return _buildings;
            }
        }

        private Stories _stories = null;
        /// <summary>
        /// Dictionary to access all of Homegear's stories. The key is the story id, the value the story object.
        /// </summary>
        public Stories Stories
        {
            get
            {
                if (_stories == null || _stories.Count == 0)
                {
                    _stories = new Stories(_rpc, _rpc.Stories);
                }

                return _stories;
            }
        }

        private Rooms _rooms = null;
        /// <summary>
        /// Dictionary to access all of Homegear's rooms. The key is the room id, the value the room object.
        /// </summary>
        public Rooms Rooms
        {
            get
            {
                if (_rooms == null || _rooms.Count == 0)
                {
                    _rooms = new Rooms(_rpc, _rpc.Rooms);
                }

                return _rooms;
            }
        }

        private Roles _roles = null;
        /// <summary>
        /// Dictionary to access all of Homegear's roles. The key is the role id, the value the role object.
        /// </summary>
        public Roles Roles
        {
            get
            {
                if (_roles == null || _roles.Count == 0)
                {
                    _roles = new Roles(_rpc, _rpc.Roles);
                }

                return _roles;
            }
        }

        private Management _management = null;
        public Management Management
        {
            get
            {
                if(_management == null)
                {
                    _management = new Management(_rpc);
                }

                return _management;
            }
        }

        private Events _timedEvents = null;
        /// <summary>
        /// Dictionary to access all timed events. The key is the id of the event, the value the event object.
        /// </summary>
        public Events TimedEvents
        {
            get
            {
                if (_timedEvents == null || _timedEvents.Count == 0)
                {
                    _timedEvents = new Events(_rpc, _rpc.ListEvents(EventType.Timed), EventType.Timed);
                }

                return _timedEvents;
            }
        }

        private string _version = "";
        /// <summary>
        /// Returns the Homegear version. E. g.: "Homegear 0.5.5"
        /// </summary>
        public string Version
        {
            get
            {
                if (_version.Length == 0)
                {
                    _version = _rpc.GetVersion();
                }

                return _version;
            }
        }

        /// <summary>
        /// Gets or sets the log level of Homegear. Valid values are between 0 (no logging) and 5 (debug). Don't use a log level of 5 for production on slow systems!
        /// </summary>
        public long LogLevel { get { return _rpc.LogLevel(); } set { _rpc.LogLevel(value); } }

        /// <summary>
        /// Returns a list of all active service messages.
        /// </summary>
        public List<ServiceMessage> ServiceMessages { get { return _rpc.GetServiceMessages(); } }

        /// <summary>
        /// Creates and initializes the Homegear object. Upon instantiation, the object tries to connect to Homegear. And it tries to keep the connection up, no matter what. To orderly destroy the object again and to orderly disconnect from Homegear, call "Dispose".
        /// </summary>
        /// <param name="rpc">An initialized RPC controller object</param>
        /// <param name="events">When set to "true" the library starts an event server to receive events from Homegear.</param>
        /// <param name="restrictFamilyId">When set to a value that is not -1 the library will restrict to parsing and returning results only for that family ID (if available). 
        /// For example, Families will contain only the specified family, getAllValues will return only devices with that family ID and so on.</param>
        public Homegear(RPCController rpc, bool events, long restrictFamilyId = -1)
        {
            if (rpc == null)
            {
                throw new NullReferenceException("RPC object is null.");
            }

            _rpc = rpc;
            if (_rpc != null)
                _rpc.RestrictToFamilyId = restrictFamilyId;

            _events = events;
            _families = new Families(_rpc, new Dictionary<long, Family>());
            _devices = new Devices(_rpc, new Dictionary<long, Device>());
            _systemVariables = new SystemVariables(_rpc, new Dictionary<string, SystemVariable>());
            _rpc.Disconnected += _rpc_Disconnected;
            _rpc.InitCompleted += _rpc_InitCompleted;
            _rpc.HomegearError += _rpc_HomegearError;
            _rpc.DeviceVariableUpdated += _rpc_OnDeviceVariableUpdated;
            _rpc.SystemVariableUpdated += _rpc_OnSystemVariableUpdated;
            _rpc.Pong += _rpc_Pong;
            _rpc.SystemVariableDeleted += _rpc_OnSystemVariableDeleted;
            _rpc.MetadataUpdated += _rpc_OnMetadataUpdated;
            _rpc.MetadataDeleted += _rpc_OnMetadataDeleted;
            _rpc.NewDevices += _rpc_OnNewDevices;
            _rpc.DevicesDeleted += _rpc_OnDevicesDeleted;
            _rpc.UpdateDevice += _rpc_OnUpdateDevice;
            _rpc.NewEvent += _rpc_OnNewEvent;
            _rpc.EventDeleted += _rpc_OnEventDeleted;
            _rpc.UpdateEvent += _rpc_OnUpdateEvent;
            _rpc.RequestUiRefreshEvent += _rpc_RequestUiRefreshEvent;
            _stopConnectThread = false;
            _connectThread = new Thread(Connect);
            _connectThread.Start();
            
            while (!_connectThread.IsAlive)
            {
                Thread.Sleep(10);
            }            
        }

        private void _rpc_HomegearError(RPCController sender, long level, string message)
        {
            HomegearError?.Invoke(this, level, message);
        }

        private void _rpc_OnNewEvent(RPCController sender, string id, EventType type, long peerId, long channelIndex, string variable)
        {
            if (type == EventType.Timed)
            {
                ReloadRequired?.Invoke(this, ReloadType.Events);
            }
            else
            {
                if (!Devices.ContainsKey(peerId))
                {
                    return;
                }

                Device device = Devices[peerId];
                if (!device.Channels.ContainsKey(channelIndex))
                {
                    return;
                }

                Channel channel = device.Channels[channelIndex];
                DeviceReloadRequired?.Invoke(this, device, channel, DeviceReloadType.Events);
            }
        }

        private void _rpc_OnUpdateEvent(RPCController sender, string id, EventType type, long peerId, long channelIndex, string variable)
        {
            if (type == EventType.Timed)
            {
                if (!TimedEvents.ContainsKey(id))
                {
                    return;
                }

                Event currentEvent = TimedEvents[id];
                _rpc.GetEvent(currentEvent);
                EventUpdated?.Invoke(this, currentEvent);
            }
            else
            {
                if (!Devices.ContainsKey(peerId))
                {
                    return;
                }

                Device device = Devices[peerId];
                if (!device.Events.ContainsKey(id))
                {
                    return;
                }

                Event currentEvent = device.Events[id];
                _rpc.GetEvent(currentEvent);
                EventUpdated?.Invoke(this, currentEvent);
            }
        }

        private void _rpc_OnEventDeleted(RPCController sender, string id, EventType type, long peerId, long channelIndex, string variable)
        {
            if (type == EventType.Timed)
            {
                if (ReloadRequired != null)
                {
                    ReloadRequired(this, ReloadType.Events);
                }
            }
            else
            {
                if (!Devices.ContainsKey(peerId))
                {
                    return;
                }

                Device device = Devices[peerId];
                if (!device.Channels.ContainsKey(channelIndex))
                {
                    return;
                }

                Channel channel = device.Channels[channelIndex];
                DeviceReloadRequired?.Invoke(this, device, channel, DeviceReloadType.Events);
            }
        }

        private void _rpc_RequestUiRefreshEvent(RPCController sender, string id)
        {
            ReloadRequired?.Invoke(this, ReloadType.UI);
        }

        private void _rpc_OnNewDevices(RPCController sender)
        {
            ReloadRequired?.Invoke(this, ReloadType.Full);
        }

        private void OnDevice_VariableReloadRequired(Device device, Channel channel, bool reloadDevice)
        {
            if (DeviceReloadRequired != null)
            {
                if (channel == null || reloadDevice)
                {
                    DeviceReloadRequired(this, device, channel, DeviceReloadType.Full);
                }
                else
                {
                    DeviceReloadRequired(this, device, channel, DeviceReloadType.Variables);
                }
            }
        }

        private void _rpc_OnUpdateDevice(RPCController sender, long peerId, long channelIndex, RPCUpdateDeviceFlags flags)
        {
            if (!Devices.ContainsKey(peerId))
            {
                return;
            }

            Device device = Devices[peerId];
            if (!device.Channels.ContainsKey(channelIndex))
            {
                return;
            }

            Channel channel = device.Channels[channelIndex];
            if (flags == RPCUpdateDeviceFlags.rpcConfig)
            {
                List<ConfigParameter> changedParameters = channel.Config.Reload();
                foreach (ConfigParameter parameter in changedParameters)
                {
                    if (DeviceConfigParameterUpdated != null)
                    {
                        DeviceConfigParameterUpdated(this, device, channel, parameter);
                    }
                }
                foreach (KeyValuePair<long, ReadOnlyDictionary<long, Link>> remotePeer in channel.Links)
                {
                    foreach (KeyValuePair<long, Link> linkPair in remotePeer.Value)
                    {
                        changedParameters = linkPair.Value.Config.Reload();
                        foreach (ConfigParameter parameter in changedParameters)
                        {
                            if (DeviceLinkConfigParameterUpdated != null)
                            {
                                DeviceLinkConfigParameterUpdated(this, device, channel, linkPair.Value, parameter);
                            }
                        }
                    }
                }
            }
            else if (flags == RPCUpdateDeviceFlags.rpcTeam)
            {
                DeviceReloadRequired?.Invoke(this, device, channel, DeviceReloadType.Team);
            }
            else
            {
                DeviceReloadRequired?.Invoke(this, device, channel, DeviceReloadType.Links);
            }
        }

        private void _rpc_OnDevicesDeleted(RPCController sender)
        {
            if (ReloadRequired != null)
            {
                ReloadRequired(this, ReloadType.Full);
            }
        }

        private void _rpc_OnDeviceVariableUpdated(RPCController sender, Variable value, string eventSource)
        {
            if (_disposing)
            {
                return;
            }

            if (value.PeerID == 0)
            {
                return; //System variable
            }

            if (!Devices.ContainsKey(value.PeerID))
            {
                return;
            }

            Device device = Devices[value.PeerID];
            if (!device.Channels.ContainsKey(value.Channel))
            {
                return;
            }

            Channel deviceChannel = device.Channels[value.Channel];
            if (!deviceChannel.Variables.ContainsKey(value.Name))
            {
                return;
            }

            Variable variable = deviceChannel.Variables[value.Name];
            variable.SetValue(value);
            DeviceVariableUpdated?.Invoke(this, device, deviceChannel, variable, eventSource);
        }

        private void _rpc_OnSystemVariableUpdated(RPCController sender, SystemVariable value)
        {
            if (_disposing)
            {
                return;
            }

            if (!SystemVariables.ContainsKey(value.Name))
            {
                ReloadRequired?.Invoke(this, ReloadType.SystemVariables);
                return;
            }
            SystemVariable variable = SystemVariables[value.Name];
            variable.SetValue(value);
            SystemVariableUpdated?.Invoke(this, variable);
        }

        private void _rpc_Pong(RPCController sender, string id)
        {
            if (_disposing)
            {
                return;
            }

            Pong?.Invoke(this, id);
        }

        private void _rpc_OnSystemVariableDeleted(RPCController sender)
        {
            if (_disposing)
            {
                return;
            }

            ReloadRequired?.Invoke(this, ReloadType.SystemVariables);
        }

        private void _rpc_OnMetadataUpdated(RPCController sender, long peerId, MetadataVariable value)
        {
            if (_disposing)
            {
                return;
            }

            if (!Devices.ContainsKey(peerId))
            {
                ReloadRequired?.Invoke(this, ReloadType.Full);
                return;
            }
            Device device = Devices[peerId];
            if (!device.Metadata.ContainsKey(value.Name))
            {
                DeviceReloadRequired?.Invoke(this, device, null, DeviceReloadType.Metadata);
                return;
            }
            MetadataVariable variable = device.Metadata[value.Name];
            variable.SetValue(value);
            MetadataUpdated?.Invoke(this, device, variable);
        }

        private void _rpc_OnMetadataDeleted(RPCController sender, long peerId)
        {
            if (_disposing)
            {
                return;
            }

            if (!Devices.ContainsKey(peerId))
            {
                return;
            }

            Device device = Devices[peerId];
            DeviceReloadRequired?.Invoke(this, device, null, DeviceReloadType.Metadata);
        }

        private void _rpc_InitCompleted(RPCController sender)
        {
            if (_disposing)
            {
                return;
            }

            if (Devices.Count == 0)
            {
                Reload();
            }
            else
            {
                bool devicesDeleted;
                bool newDevices;
                List<Variable> updatedVariables = Devices.UpdateVariables(_rpc.GetAllValues(), out devicesDeleted, out newDevices);
                foreach (Variable variable in updatedVariables)
                {
                    if (!Devices.ContainsKey(variable.PeerID))
                    {
                        continue;
                    }

                    Device device = Devices[variable.PeerID];
                    if (!device.Channels.ContainsKey(variable.Channel))
                    {
                        continue;
                    }

                    DeviceVariableUpdated?.Invoke(this, device, device.Channels[variable.Channel], variable, "HomegearLib.NET");
                }

                bool systemVariablesAdded;
                bool systemVariablesDeleted;
                List<SystemVariable> updatedSystemVariables = SystemVariables.Update(out systemVariablesDeleted, out systemVariablesAdded);
                foreach (SystemVariable variable in updatedSystemVariables)
                {
                    SystemVariableUpdated?.Invoke(this, variable);
                }
                if ((devicesDeleted || newDevices) && ReloadRequired != null)
                {
                    ReloadRequired(this, ReloadType.Full);
                }
                else
                {
                    if ((systemVariablesAdded || systemVariablesDeleted) && ReloadRequired != null)
                    {
                        ReloadRequired(this, ReloadType.SystemVariables);
                    }
                    foreach (KeyValuePair<long, Device> devicePair in Devices)
                    {
                        if (devicePair.Value.MetadataRequested)
                        {
                            bool variablesAdded;
                            bool variablesDeleted;
                            List<MetadataVariable> updatedMetadata = devicePair.Value.Metadata.Update(out variablesDeleted, out variablesAdded);
                            foreach (MetadataVariable variable in updatedMetadata)
                            {
                                MetadataUpdated?.Invoke(this, devicePair.Value, variable);
                            }
                            if (variablesAdded || variablesDeleted)
                            {
                                DeviceReloadRequired?.Invoke(this, devicePair.Value, null, DeviceReloadType.Metadata);
                            }
                        }
                    }
                }

                Reloaded?.Invoke(this);
            }
        }

        ~Homegear()
        {
            Dispose(false);
        }

        /// <summary>
        /// Releases all ressources and orderly disconnects from Homegear. It might take a few seconds, before the method returns.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            _disposing = true;

            if (disposing)
            {
                _stopConnectThread = true;

                _rpc.Disconnected -= _rpc_Disconnected;
                _rpc.InitCompleted -= _rpc_InitCompleted;
                _rpc.HomegearError -= _rpc_HomegearError;
                _rpc.DeviceVariableUpdated -= _rpc_OnDeviceVariableUpdated;
                _rpc.SystemVariableUpdated -= _rpc_OnSystemVariableUpdated;
                _rpc.SystemVariableDeleted -= _rpc_OnSystemVariableDeleted;
                _rpc.Pong -= _rpc_Pong;
                _rpc.MetadataUpdated -= _rpc_OnMetadataUpdated;
                _rpc.MetadataDeleted -= _rpc_OnMetadataDeleted;
                _rpc.NewDevices -= _rpc_OnNewDevices;
                _rpc.DevicesDeleted -= _rpc_OnDevicesDeleted;
                _rpc.UpdateDevice -= _rpc_OnUpdateDevice;
                _rpc.NewEvent -= _rpc_OnNewEvent;
                _rpc.EventDeleted -= _rpc_OnEventDeleted;
                _rpc.UpdateEvent -= _rpc_OnUpdateEvent;
                _rpc.RequestUiRefreshEvent -= _rpc_RequestUiRefreshEvent;

                if (_connectThread != null && _connectThread.IsAlive)
                {
                    if (!_connectThread.Join(1000))
                    {
                        try
                        {
                            _connectThread.Abort();
                        }
                        catch (Exception) { }
                    }
                }
                _connectThread = null;

                _rpc.Dispose();
                _rpc = null;
            }

            _disposed = true;
        }

        /// <summary>
        /// Disposes all subobjects and initiates a full reload.
        /// </summary>
        public void Reload()
        {
            if (_disposing)
            {
                return;
            }

            lock (_reloadingLock)
            {
                try
                {
                    if (_reloading) return;
                    _reloading = true;
                    _version = "";
                    _rpc.Clear();

                    _families?.Dispose();
                    _families = new Families(_rpc, _rpc.Families);


                    _buildings?.Dispose();
                    _buildings = new Buildings(_rpc, _rpc.Buildings);
                    _stories?.Dispose();
                    _stories = new Stories(_rpc, _rpc.Stories);
                    _rooms?.Dispose();
                    _rooms = new Rooms(_rpc, _rpc.Rooms);

                    _devices?.Dispose();
                    _devices = new Devices(_rpc, _rpc.Devices);
                    foreach (KeyValuePair<long, Device> device in _devices)
                    {
                        device.Value.VariableReloadRequiredEvent += OnDevice_VariableReloadRequired;
                    }

                    _interfaces?.Dispose();
                    _interfaces = new Interfaces(_rpc, _rpc.Interfaces);

                    _systemVariables?.Dispose();
                    _systemVariables = new SystemVariables(_rpc, _rpc.SystemVariables);


                    _roles?.Dispose();
                    _roles = new Roles(_rpc, _rpc.Roles);

                    Reloaded?.Invoke(this);
                    _reloading = false;
                }
                catch(Exception ex)
                {
                    _reloading = false;
                    throw ex;
                }
            }
        }

        private void Connect()
        {
            if (_disposing || _connecting)
            {
                return;
            }

            _connecting = true;
            try
            {
                int counter = 0;
                while (!_stopConnectThread && !_disposing)
                {
                    if ((counter & 0x7F) == 0)
                    {
                        try
                        {
                            if (!_rpc.IsConnected)
                            {
                                _rpc.Connect(_events);
                            }

                            break;
                        }
                        catch (Exception ex)
                        {
                            if (!_disposing)
                            {
                                ConnectError?.Invoke(this, ex.Message, ex.StackTrace);
                                if (!_stopConnectThread) Thread.Sleep(10);
                            }
                        }
                    }
                    counter++;
                }
            }
            catch (Exception ex)
            {
                _connecting = false;
                throw ex;
            }
            _connecting = false;
        }

        public static List<Tuple<string, int>> FindInstances()
        {
            HashSet<Tuple<string, int>> result = SSDP.Search(1000);
            result.UnionWith(SSDP.Search(1000));
            result.UnionWith(SSDP.Search(1000));
            result.UnionWith(SSDP.Search(1000));
            result.UnionWith(SSDP.Search(1000));
            return result.ToList();
        }

        private void _rpc_Disconnected(RPCClient sender)
        {
            if (_disposing)
                return;

            _stopConnectThread = true;
            if (_connectThread != null && _connectThread.IsAlive) _connectThread.Join();

            if (_disposing)
                return;

            _stopConnectThread = false;
            _connectThread = new Thread(Connect);
            _connectThread.Start();
        }

        /// <summary>
        /// Enables or disables the pairing mode on all supported device families.
        /// </summary>
        /// <param name="value">"true" to enable or "false" to disable the pairing mode.</param>
        public void EnablePairingMode(bool value)
        {
            _rpc.SetInstallMode(value);
        }

        /// <summary>
        /// Enables or disables the pairing mode on all supported device families.
        /// </summary>
        /// <param name="value">"true" to enable or "false" to disable the pairing mode.</param>
        /// <param name="duration">The duration the pairing mode should be enabled in seconds.</param>
        public void EnablePairingMode(bool value, int duration)
        {
            _rpc.SetInstallMode(value, duration);
        }

        /// <summary>
        /// Returns the time left in pairing mode in seconds.
        /// </summary>
        /// <returns>The time left in pairing mode in seconds.</returns>
        public long TimeLeftInPairingMode()
        {
            return _rpc.GetInstallMode();
        }

        /// <summary>
        /// Returns the status of currently running firmware updates and the result of all firmware updates since the last call to "UpdateFirmware".
        /// </summary>
        /// <returns>The status of the currently running firmware update and the result of all firmware updates since the last call to "UpdateFirmware".</returns>
        public UpdateStatus GetUpdateStatus()
        {
            return _rpc.GetUpdateStatus();
        }

        /// <summary>
        /// Executes a script in Homegear's script directory.
        /// </summary>
        /// <param name="filename">The filename of the script.</param>
        public void RunScript(string filename)
        {
            _rpc.RunScript(filename);
        }

        /// <summary>
        /// Executes a script in Homegear's script directory.
        /// </summary>
        /// <param name="filename">The filename of the script.</param>
        /// <param name="wait">When "true" this method waits for the script to finish and returns the exit code.</param>
        /// <returns>The exit code when "wait" is "true".</returns>
        public long RunScript(string filename, bool wait)
        {
            return _rpc.RunScript(filename, wait);
        }

        /// <summary>
        /// Executes a script in Homegear's script directory.
        /// </summary>
        /// <param name="filename">The filename of the script.</param>
        /// <param name="arguments">Arguments to pass to the script.</param>
        public void RunScript(string filename, string arguments)
        {
            _rpc.RunScript(filename, arguments);
        }

        /// <summary>
        /// Executes a script in Homegear's script directory.
        /// </summary>
        /// <param name="filename">The filename of the script.</param>
        /// <param name="arguments">Arguments to pass to the script.</param>
        /// <param name="wait">When "true" this method waits for the script to finish and returns the exit code.</param>
        /// <returns>The exit code when "wait" is "true".</returns>
        public long RunScript(string filename, string arguments, bool wait)
        {
            return _rpc.RunScript(filename, arguments, wait);
        }


        /// <summary>
        /// Returns a string with an uuid for the homegear instance
        /// </summary>
        /// <returns>The uuid string.</returns>
        public string GetInstanceId()
        {
            return _rpc.GetInstanceId();
        }

        /// <summary>
        /// Generic method call.
        /// </summary>
        /// <returns>The result of the call.</returns>
        public RPCVariable CallMethod(string name, List<RPCVariable> parameters)
        {
            return _rpc.CallMethod(name, parameters);
        }
    
    }
}
