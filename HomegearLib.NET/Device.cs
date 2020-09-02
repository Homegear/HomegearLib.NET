using HomegearLib.RPC;
using System;
using System.Collections.Generic;

namespace HomegearLib
{
    public enum DeviceRXMode
    {
        None = 0,
        Always = 1,
        Burst = 2,
        Config = 4,
        WakeUp = 8,
        LazyConfig = 16
    }

    public class Device : IDisposable
    {
        RPCController _rpc = null;

        internal delegate void VariableReloadRequiredEventHandler(Device device, Channel channel, bool reloadDevice);

        internal event VariableReloadRequiredEventHandler VariableReloadRequiredEvent;

        bool _descriptionRequested = false;

        private Family _family = null;
        public Family Family { get { return _family; } internal set { _family = value; } }

        private long _id = -1;
        public long ID
        {
            get { return _id; }
            set
            {
                _rpc.SetId(_id, value);
                _id = value;
            }
        }

        /// <summary>
        /// Sets the device id without calling any RPC functions
        /// </summary>
        /// <param name="value">The device id</param>
        internal void SetIDNoRPC(int value)
        {
            _id = value;
        }

        private long _address = -1;
        public long Address
        {
            get
            {
                if (!_descriptionRequested)
                {
                    _rpc.GetDeviceDescription(this);
                    _descriptionRequested = true;
                }
                return _address;
            }
            internal set { _address = value; }
        }

        private string _serialNumber = "";
        public string SerialNumber { get { return _serialNumber; } internal set { _serialNumber = value; } }

        private long _typeID = 0;
        public long TypeID { get { return _typeID; } internal set { _typeID = value; } }

        private string _typeString = "";
        public string TypeString { get { return _typeString; } internal set { _typeString = value; } }

        private Channels _channels;
        public Channels Channels
        {
            get { return _channels; }
            internal set
            {
                _channels = value;
                foreach (KeyValuePair<long, Channel> channel in _channels)
                {
                    channel.Value.VariableReloadRequiredEvent += Channel_OnVariableReloadRequired;
                }
            }
        }

        public bool MetadataRequested { get { return _metadata != null; } }

        private MetadataVariables _metadata = null;
        public MetadataVariables Metadata
        {
            get
            {
                if (_metadata == null || _metadata.Count == 0)
                {
                    _metadata = new MetadataVariables(_rpc, _id, _rpc.GetAllMetadata(_id));
                }

                return _metadata;
            }
            internal set { _metadata = value; }
        }

        private Events _events = null;
        public Events Events
        {
            get
            {
                if (_events == null || _events.Count == 0)
                {
                    _events = new Events(_rpc, _rpc.ListEvents(_id), _id);
                }

                return _events;
            }
            internal set { _events = value; }
        }

        private string _name;
        public string Name
        {
            get
            {
                if (!_descriptionRequested)
                {
                    _rpc.GetDeviceDescription(this);
                    _descriptionRequested = true;
                }
                return _name;
            }
            set
            {
                _name = value;
                _rpc.SetName(_id, _name);
            }
        }

        /// <summary>
        /// Sets the name without calling any RPC functions
        /// </summary>
        /// <param name="name">The name of the device</param>
        internal void SetNameNoRPC(string name)
        {
            _name = name;
        }

        private Room _room;
        public Room Room
        {
            get
            {
                if (!_descriptionRequested)
                {
                    _rpc.GetDeviceDescription(this);
                    _descriptionRequested = true;
                }
                return _room;
            }
            set
            {
                if (value == null && _room != null) _rpc.RemoveDeviceFromRoom(this, _room);
                else _rpc.AddDeviceToRoom(this, value);
                _room = value;
            }
        }

        /// <summary>
        /// Sets the room without calling any RPC functions
        /// </summary>
        /// <param name="room">The room object</param>
        internal void SetRoomNoRPC(Room room)
        {
            _room = (room != null && room.ID == 0) ? null : room;
        }

        private Interface _interface;
        public Interface Interface
        {
            get
            {
                _rpc.GetDeviceInfo(this);
                return _interface;
            }
            set
            {
                _interface = value;
                _rpc.SetInterface(_id, _interface);
            }
        }

        /// <summary>
        /// Sets the physical interface without calling any RPC functions
        /// </summary>
        /// <param name="physicalInterface">The interface object</param>
        internal void SetInterfaceNoRPC(Interface physicalInterface)
        {
            _interface = physicalInterface;
        }

        private DeviceRXMode _rxMode = DeviceRXMode.None;
        public DeviceRXMode RXMode
        {
            get
            {
                if (!_descriptionRequested)
                {
                    _rpc.GetDeviceDescription(this);
                    _descriptionRequested = true;
                }
                return _rxMode;
            }
            internal set { _rxMode = value; }
        }

        private bool _aesActive = false;
        public bool AESActive
        {
            get
            {
                foreach (KeyValuePair<long, Channel> channel in _channels)
                {
                    if (channel.Value.Config.ContainsKey("AES_ACTIVE") && channel.Value.Config["AES_ACTIVE"].BooleanValue)
                    {
                        return true;
                    }
                }
                return false;
            }
            internal set { _aesActive = value; }
        }

        private string _firmware;
        public string Firmware
        {
            get
            {
                if (!_descriptionRequested)
                {
                    _rpc.GetDeviceDescription(this);
                    _descriptionRequested = true;
                }
                return _firmware;
            }
            internal set { _firmware = value; }
        }

        private string _availableFirmware;
        public string AvailableFirmware
        {
            get
            {
                if (!_descriptionRequested)
                {
                    _rpc.GetDeviceDescription(this);
                    _descriptionRequested = true;
                }
                return _availableFirmware;
            }
            internal set { _availableFirmware = value; }
        }

        public Device(RPCController rpc, Family family, long id)
        {
            _rpc = rpc;
            _family = family;
            _id = id;
        }

        public void Dispose()
        {
            _family = null;
            if (_channels != null)
            {
                _channels.Dispose();
            }
        }

        public void Reload()
        {
            _descriptionRequested = false;
            _metadata = null;
            foreach (KeyValuePair<long, Channel> channel in _channels)
            {
                channel.Value.Reload();
            }
        }

        /// <summary>
        /// Resets the physical interface to the default physical interface.
        /// </summary>
        public void ResetInterface()
        {
            _rpc.SetInterface(_id, null);
        }

        public void UpdateFirmware(bool manually)
        {
            _rpc.UpdateFirmware(this, manually);
        }

        public void Unpair()
        {
            _rpc.DeleteDevice(_id, RPCDeleteDeviceFlags.Defer);
        }

        public void Reset()
        {
            _rpc.DeleteDevice(_id, RPCDeleteDeviceFlags.Reset | RPCDeleteDeviceFlags.Defer);
        }

        public void Remove()
        {
            _rpc.DeleteDevice(_id, RPCDeleteDeviceFlags.Force);
        }

        private void Channel_OnVariableReloadRequired(Channel sender, bool reloadDevice)
        {
            if (VariableReloadRequiredEvent != null)
            {
                VariableReloadRequiredEvent(this, sender, reloadDevice);
            }
        }
    }
}
