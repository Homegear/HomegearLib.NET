using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using HomegearLib.RPC;
using System.Security.Authentication;

namespace HomegearLib
{
    /// <summary>
    /// Used as a parameter for the event "ReloadRequired".
    /// </summary>
    public enum ReloadType
    {
        Full = 1,
        SystemVariables = 2,
        Events
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

        String _description = "";
        public String Description { get { return _description; } }

        public UpdateResult(UpdateResultCode code, String description)
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
        Int32 _currentDevice = -1;
        public Int32 CurrentDevice { get { return _currentDevice; } }

        Int32 _currentDeviceProgress = -1;
        public Int32 CurrentDeviceProgress { get { return _currentDeviceProgress; } }

        Int32 _deviceCount = -1;
        public Int32 DeviceCount { get { return _deviceCount; } }

        Int32 _currentUpdate = 0;
        public Int32 CurrentUpdate { get { return _currentUpdate; } }

        ReadOnlyDictionary<Int32, UpdateResult> _results = new ReadOnlyDictionary<Int32,UpdateResult>();
        public ReadOnlyDictionary<Int32, UpdateResult> Results { get { return _results; } }

        public UpdateStatus(Int32 currentDevice, Int32 currentDeviceProgress, Int32 deviceCount, Int32 currentUpdate, Dictionary<Int32, UpdateResult> results)
        {
            _currentDevice = currentDevice;
            _currentDeviceProgress = currentDeviceProgress;
            _deviceCount = deviceCount;
            _currentUpdate = currentUpdate;
            _results = new ReadOnlyDictionary<Int32, UpdateResult>(results);
        }
    }

    /// <summary>
    /// Main class to access Homegear features.
    /// </summary>
    public class Homegear : IDisposable
    {
        public delegate void ConnectErrorEventHandler(Homegear sender, String message, String stackTrace);
        public delegate void ConnectedEventHandler(Homegear sender);
        public delegate void DisconnectedEventHandler(Homegear sender);
        public delegate void HomegearErrorEventHandler(Homegear sender, Int32 level, String message);
        public delegate void DataReloadEventHandler(Homegear sender);
        public delegate void SystemVariableUpdatedEventHandler(Homegear sender, SystemVariable variable);
        public delegate void MetadataUpdatedEventHandler(Homegear sender, Device device, MetadataVariable variable);
        public delegate void DeviceVariableUpdatedEventHandler(Homegear sender, Device device, Channel channel, Variable variable);
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
        /// Raised when an error occures within Homegear.
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
        private RPCController _rpc = null;
        private volatile bool _disposing = false;
        private volatile bool _stopConnectThread = false;
        private Thread _connectThread = null;

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
                if (_interfaces == null || _interfaces.Count == 0) _interfaces = new Interfaces(_rpc, _rpc.Interfaces);
                bool interfacesAdded = false;
                bool interfacesRemoved = false;
                _interfaces.Update(out interfacesRemoved, out interfacesAdded);
                if ((interfacesAdded || interfacesRemoved) && ReloadRequired != null) ReloadRequired(this, ReloadType.Full);
                return _interfaces;
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
                if (_timedEvents == null || _timedEvents.Count == 0) _timedEvents = new Events(_rpc, _rpc.ListEvents(EventType.Timed), EventType.Timed);
                return _timedEvents;
            }
        }

        private String _version = "";
        /// <summary>
        /// Returns the Homegear version. E. g.: "Homegear 0.5.5"
        /// </summary>
        public String Version
        {
            get
            {
                if (_version.Length == 0) _version = _rpc.GetVersion();
                return _version;
            }
        }

        /// <summary>
        /// Gets or sets the log level of Homegear. Valid values are between 0 (no logging) and 5 (debug). Don't use a log level of 5 for production on slow systems!
        /// </summary>
        public Int32 LogLevel { get { return _rpc.LogLevel(); } set { _rpc.LogLevel(value); } }

        /// <summary>
        /// Returns a list of all active service messages.
        /// </summary>
        public List<ServiceMessage> ServiceMessages { get { return _rpc.GetServiceMessages(); } }

        /// <summary>
        /// Creates and initializes the Homegear object. Upon instantiation, the object tries to connect to Homegear. And it tries to keep the connection up, no matter what. To orderly destroy the object again and to orderly disconnect from Homegear, call "Dispose".
        /// </summary>
        /// <param name="rpc">An initialized RPC controller object</param>
        public Homegear(RPCController rpc)
        {
            if (rpc == null) throw new NullReferenceException("RPC object is null.");
            _rpc = rpc;
            _families = new Families(_rpc, new Dictionary<Int32, Family>());
            _devices = new Devices(_rpc, new Dictionary<Int32, Device>());
            _systemVariables = new SystemVariables(_rpc, new Dictionary<String, SystemVariable>());
            _rpc.ClientDisconnected += _rpc_ClientDisconnected;
            _rpc.InitCompleted += _rpc_InitCompleted;
            _rpc.HomegearError += _rpc_HomegearError;
            _rpc.DeviceVariableUpdated += _rpc_OnDeviceVariableUpdated;
            _rpc.SystemVariableUpdated += _rpc_OnSystemVariableUpdated;
            _rpc.SystemVariableDeleted += _rpc_OnSystemVariableDeleted;
            _rpc.MetadataUpdated += _rpc_OnMetadataUpdated;
            _rpc.MetadataDeleted += _rpc_OnMetadataDeleted;
            _rpc.NewDevices += _rpc_OnNewDevices;
            _rpc.DevicesDeleted += _rpc_OnDevicesDeleted;
            _rpc.UpdateDevice += _rpc_OnUpdateDevice;
            _rpc.NewEvent += _rpc_OnNewEvent;
            _rpc.EventDeleted += _rpc_OnEventDeleted;
            _rpc.UpdateEvent += _rpc_OnUpdateEvent;
            _stopConnectThread = false;
            _connectThread = new Thread(Connect);
            _connectThread.Start();
            while (!_connectThread.IsAlive) ;
        }

        void _rpc_HomegearError(RPCController sender, int level, string message)
        {
            if (HomegearError != null) HomegearError(this, level, message);
        }

        private void _rpc_OnNewEvent(RPCController sender, String id, EventType type, Int32 peerID, Int32 channelIndex, String variable)
        {
            if (type == EventType.Timed)
            {
                if (ReloadRequired != null) ReloadRequired(this, ReloadType.Events);
            }
            else
            {
                if (!Devices.ContainsKey(peerID)) return;
                Device device = Devices[peerID];
                if (!device.Channels.ContainsKey(channelIndex)) return;
                Channel channel = device.Channels[channelIndex];
                if (DeviceReloadRequired != null) DeviceReloadRequired(this, device, channel, DeviceReloadType.Events);
            }
        }

        private void _rpc_OnUpdateEvent(RPCController sender, String id, EventType type, Int32 peerID, Int32 channelIndex, String variable)
        {
            if (type == EventType.Timed)
            {
                if (!TimedEvents.ContainsKey(id)) return;
                Event currentEvent = TimedEvents[id];
                _rpc.GetEvent(currentEvent);
                if (EventUpdated != null) EventUpdated(this, currentEvent);
            }
            else
            {
                if (!Devices.ContainsKey(peerID)) return;
                Device device = Devices[peerID];
                if (!device.Events.ContainsKey(id)) return;
                Event currentEvent = device.Events[id];
                _rpc.GetEvent(currentEvent);
                if (EventUpdated != null) EventUpdated(this, currentEvent);              
            }
        }

        private void _rpc_OnEventDeleted(RPCController sender, String id, EventType type, Int32 peerID, Int32 channelIndex, String variable)
        {
            if (type == EventType.Timed)
            {
                if (ReloadRequired != null) ReloadRequired(this, ReloadType.Events);
            }
            else
            {
                if (!Devices.ContainsKey(peerID)) return;
                Device device = Devices[peerID];
                if (!device.Channels.ContainsKey(channelIndex)) return;
                Channel channel = device.Channels[channelIndex];
                if (DeviceReloadRequired != null) DeviceReloadRequired(this, device, channel, DeviceReloadType.Events);
            }
        }

        private void _rpc_OnNewDevices(RPCController sender)
        {
            if (ReloadRequired != null) ReloadRequired(this, ReloadType.Full);
        }

        private void OnDevice_VariableReloadRequired(Device device, Channel channel)
        {
            if(DeviceReloadRequired != null)
            {
                if (channel == null) DeviceReloadRequired(this, device, channel, DeviceReloadType.Full);
                else DeviceReloadRequired(this, device, channel, DeviceReloadType.Variables);
            }
        }

        private void _rpc_OnUpdateDevice(RPCController sender, int peerID, int channelIndex, RPCUpdateDeviceFlags flags)
        {
            if (!Devices.ContainsKey(peerID)) return;
            Device device = Devices[peerID];
            if (!device.Channels.ContainsKey(channelIndex)) return;
            Channel channel = device.Channels[channelIndex];
            if(flags == RPCUpdateDeviceFlags.rpcConfig)
            {
                List<ConfigParameter> changedParameters = channel.Config.Reload();
                foreach (ConfigParameter parameter in changedParameters)
                {
                    if (DeviceConfigParameterUpdated != null) DeviceConfigParameterUpdated(this, device, channel, parameter);
                }
                foreach (KeyValuePair<Int32, ReadOnlyDictionary<Int32, Link>> remotePeer in channel.Links)
                {
                    foreach (KeyValuePair<Int32, Link> linkPair in remotePeer.Value)
                    {
                        changedParameters = linkPair.Value.Config.Reload();
                        foreach (ConfigParameter parameter in changedParameters)
                        {
                            if (DeviceLinkConfigParameterUpdated != null) DeviceLinkConfigParameterUpdated(this, device, channel, linkPair.Value, parameter);
                        }
                    }
                }
            }
            else if(flags == RPCUpdateDeviceFlags.rpcTeam)
            {
                if (DeviceReloadRequired != null) DeviceReloadRequired(this, device, channel, DeviceReloadType.Team);
            }
            else
            {
                if (DeviceReloadRequired != null) DeviceReloadRequired(this, device, channel, DeviceReloadType.Links);
            }
        }

        private void _rpc_OnDevicesDeleted(RPCController sender)
        {
            if (ReloadRequired != null) ReloadRequired(this, ReloadType.Full);
        }

        private void _rpc_OnDeviceVariableUpdated(RPCController sender, Variable value)
        {
            if (_disposing) return;
            if(value.PeerID == 0) return; //System variable
            if (!Devices.ContainsKey(value.PeerID)) return;
            Device device = Devices[value.PeerID];
            if (!device.Channels.ContainsKey(value.Channel)) return;
            Channel deviceChannel = device.Channels[value.Channel];
            if (!deviceChannel.Variables.ContainsKey(value.Name)) return;
            Variable variable = deviceChannel.Variables[value.Name];
            variable.SetValue(value);
            if (DeviceVariableUpdated != null) DeviceVariableUpdated(this, device, deviceChannel, variable);
        }

        private void _rpc_OnSystemVariableUpdated(RPCController sender, SystemVariable value)
        {
            if (_disposing) return;
            if (!SystemVariables.ContainsKey(value.Name))
            {
                System.Diagnostics.Debug.Write("Position 1");
                if (ReloadRequired != null) ReloadRequired(this, ReloadType.SystemVariables);
                return;
            }
            SystemVariable variable = SystemVariables[value.Name];
            variable.SetValue(value);
            if (SystemVariableUpdated != null) SystemVariableUpdated(this, variable);
        }

        private void _rpc_OnSystemVariableDeleted(RPCController sender)
        {
            if (_disposing) return;
            System.Diagnostics.Debug.Write("Position 2");
            if (ReloadRequired != null) ReloadRequired(this, ReloadType.SystemVariables);
        }

        private void _rpc_OnMetadataUpdated(RPCController sender, Int32 peerID, MetadataVariable value)
        {
            if (_disposing) return;
            if(!Devices.ContainsKey(peerID))
            {
                if (ReloadRequired != null) ReloadRequired(this, ReloadType.Full);
                return;
            }
            Device device = Devices[peerID];
            if (!device.Metadata.ContainsKey(value.Name))
            {
                if (DeviceReloadRequired != null) DeviceReloadRequired(this, device, null, DeviceReloadType.Metadata);
                return;
            }
            MetadataVariable variable = device.Metadata[value.Name];
            variable.SetValue(value);
            if (MetadataUpdated != null) MetadataUpdated(this, device, variable);
        }

        private void _rpc_OnMetadataDeleted(RPCController sender, Int32 peerID)
        {
            if (_disposing) return;
            if (!Devices.ContainsKey(peerID)) return;
            Device device = Devices[peerID];
            if (DeviceReloadRequired != null) DeviceReloadRequired(this, device, null, DeviceReloadType.Metadata);
        }

        private void _rpc_InitCompleted(RPCController sender)
        {
            if (_disposing) return;
            if (Devices.Count == 0) Reload();
            else
            {
                bool devicesDeleted = false;
                bool newDevices = false;
                List<Variable> updatedVariables = Devices.UpdateVariables(_rpc.GetAllValues(), out devicesDeleted, out newDevices);
                foreach(Variable variable in updatedVariables)
                {
                    if(!Devices.ContainsKey(variable.PeerID)) continue;
                    Device device = Devices[variable.PeerID];
                    if(!device.Channels.ContainsKey(variable.Channel)) continue;
                    if (DeviceVariableUpdated != null) DeviceVariableUpdated(this, device, device.Channels[variable.Channel], variable);
                }
                bool systemVariablesAdded = false;
                bool systemVariablesDeleted = false;
                List<SystemVariable> updatedSystemVariables = SystemVariables.Update(out systemVariablesDeleted, out systemVariablesAdded);
                foreach(SystemVariable variable in updatedSystemVariables)
                {
                    if (SystemVariableUpdated != null) SystemVariableUpdated(this, variable);
                }
                if ((devicesDeleted || newDevices) && ReloadRequired != null) ReloadRequired(this, ReloadType.Full);
                else
                {
                    if ((systemVariablesAdded || systemVariablesDeleted) && ReloadRequired != null)
                    {
                        System.Diagnostics.Debug.Write("Position 3");
                        ReloadRequired(this, ReloadType.SystemVariables);
                    }
                    foreach (KeyValuePair<Int32, Device> devicePair in Devices)
                    {
                        if (devicePair.Value.MetadataRequested)
                        {
                            bool variablesAdded = false;
                            bool variablesDeleted = false;
                            List<MetadataVariable> updatedMetadata = devicePair.Value.Metadata.Update(out variablesDeleted, out variablesAdded);
                            foreach (MetadataVariable variable in updatedMetadata)
                            {
                                if (MetadataUpdated != null) MetadataUpdated(this, devicePair.Value, variable);
                            }
                            if ((variablesAdded || variablesDeleted) && DeviceReloadRequired != null) DeviceReloadRequired(this, devicePair.Value, null, DeviceReloadType.Metadata);
                        }
                    }
                }
            }
        }

        ~Homegear()
        {
            _stopConnectThread = true;
            if (_connectThread.IsAlive)
            {
                if(!_connectThread.Join(20000))
                {
                    try
                    {
                        _connectThread.Abort();
                    }
                    catch (Exception) { }
                }
            }
        }

        /// <summary>
        /// Releases all ressources and orderly disconnects from Homegear. It might take a few seconds, before the method returns.
        /// </summary>
        public void Dispose()
        {
            if (_disposing) return;
            _disposing = true;
            _rpc.ClientDisconnected -= _rpc_ClientDisconnected;
            _rpc.InitCompleted -= _rpc_InitCompleted;
            _rpc.DeviceVariableUpdated -= _rpc_OnDeviceVariableUpdated;
            _rpc.SystemVariableUpdated -= _rpc_OnSystemVariableUpdated;
            _rpc.SystemVariableDeleted -= _rpc_OnSystemVariableDeleted;
            _rpc.MetadataUpdated -= _rpc_OnMetadataUpdated;
            _rpc.MetadataDeleted -= _rpc_OnMetadataDeleted;
            _rpc.NewDevices -= _rpc_OnNewDevices;
            _rpc.DevicesDeleted -= _rpc_OnDevicesDeleted;
            _rpc.UpdateDevice -= _rpc_OnUpdateDevice;
            _rpc.NewEvent -= _rpc_OnNewEvent;
            _rpc.EventDeleted -= _rpc_OnEventDeleted;
            _rpc.UpdateEvent -= _rpc_OnUpdateEvent;
            _stopConnectThread = true;
            if (_connectThread.IsAlive)
            {
                if(!_connectThread.Join(20000))
                {
                    try
                    {
                        _connectThread.Abort();
                    }
                    catch (Exception) { }
                }
            }
            _rpc.Disconnect();
        }

        /// <summary>
        /// Disposes all subobjects and initiates a full reload.
        /// </summary>
        public void Reload()
        {
            if (_disposing) return;
            _rpc.Clear();
            if(_families != null) _families.Dispose();
            _families = new Families(_rpc, _rpc.Families);
            if(_devices != null) _devices.Dispose();
            _devices = new Devices(_rpc, _rpc.Devices);
            foreach (KeyValuePair<Int32, Device> device in _devices)
            {
                device.Value.VariableReloadRequiredEvent += OnDevice_VariableReloadRequired;
            }
            if(_interfaces != null) _interfaces.Dispose();
            _interfaces = null;
            if (_systemVariables != null) _systemVariables.Dispose();
            _systemVariables = new SystemVariables(_rpc, _rpc.SystemVariables);
            if (Reloaded != null) Reloaded(this);
        }

        private void Connect()
        {
            if (_disposing || _connecting) return;
            _connecting = true;
            try
            {
                while (!_stopConnectThread && !_disposing)
                {
                    try
                    {
                        if (!_rpc.IsConnected) _rpc.Connect();
                        break;
                    }
                    catch (Exception ex)
                    {
                        if (ConnectError != null) ConnectError(this, ex.Message, ex.StackTrace);
                        Thread.Sleep(10000);
                    }
                }
            }
            catch (Exception ex)
            {
                _connecting = false;
                throw ex;
            }
            _connecting = false;
        }

        private void _rpc_ClientDisconnected(RPCClient sender)
        {
            if (_disposing) return;
            _stopConnectThread = true;
            if (_connectThread.IsAlive)
            {
                if (!_connectThread.Join(20000))
                {
                    try
                    {
                        _connectThread.Abort();
                    }
                    catch (Exception) { }
                }
            }
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
        public void EnablePairingMode(bool value, Int32 duration)
        {
            _rpc.SetInstallMode(value, duration);
        }

        /// <summary>
        /// Returns the time left in pairing mode in seconds.
        /// </summary>
        /// <returns>The time left in pairing mode in seconds.</returns>
        public Int32 TimeLeftInPairingMode()
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
        /// Excecutes a script in Homegear's script directory.
        /// </summary>
        /// <param name="filename">The filename of the script.</param>
        public void RunScript(String filename)
        {
            _rpc.RunScript(filename);
        }

        /// <summary>
        /// Excecutes a script in Homegear's script directory.
        /// </summary>
        /// <param name="filename">The filename of the script.</param>
        /// <param name="wait">When "true" this method waits for the script to finish and returns the exit code.</param>
        /// <returns>The exit code when "wait" is "true".</returns>
        public Int32 RunScript(String filename, Boolean wait)
        {
            return _rpc.RunScript(filename, wait);
        }

        /// <summary>
        /// Excecutes a script in Homegear's script directory.
        /// </summary>
        /// <param name="filename">The filename of the script.</param>
        /// <param name="arguments">Arguments to pass to the script.</param>
        public void RunScript(String filename, String arguments)
        {
            _rpc.RunScript(filename, arguments);
        }

        /// <summary>
        /// Excecutes a script in Homegear's script directory.
        /// </summary>
        /// <param name="filename">The filename of the script.</param>
        /// <param name="arguments">Arguments to pass to the script.</param>
        /// <param name="wait">When "true" this method waits for the script to finish and returns the exit code.</param>
        /// <returns>The exit code when "wait" is "true".</returns>
        public Int32 RunScript(String filename, String arguments, Boolean wait)
        {
            return _rpc.RunScript(filename, arguments, wait);
        }

        /// <summary>
        /// Searches for new devices on all supported device families and returns the number of newly found devices.
        /// </summary>
        /// <returns>The number of newly found devices.</returns>
        public Int32 SearchDevices()
        {
            return _rpc.SearchDevices();
        }
    }
}
