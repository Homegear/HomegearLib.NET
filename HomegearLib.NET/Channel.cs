using HomegearLib.RPC;
using System;
using System.Linq;

namespace HomegearLib
{
    public enum ChannelDirection
    {
        None = 0,
        Sender = 1,
        Receiver = 2
    }

    public class Channel : IDisposable
    {
        private RPCController _rpc = null;

        internal delegate void VariableReloadRequiredEventHandler(Channel sender, bool reloadDevice);

        internal event VariableReloadRequiredEventHandler VariableReloadRequiredEvent;

        bool _descriptionRequested = false;

        readonly private long _peerId = 0;
        public long PeerID { get { return _peerId; } }

        readonly private long _index = 0;
        public long Index { get { return _index; } }

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
                _rpc.SetName(_peerId, _index, _name);
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
                if (value == null && _room != null) _rpc.RemoveChannelFromRoom(this, _room);
                else _rpc.AddChannelToRoom(this, value);
                _room = value;
            }
        }

        /// <summary>
        /// Sets the room without calling any RPC functions
        /// </summary>
        /// <param name="room">The room object</param>
        internal void SetRoomNoRPC(Room room)
        {
            _room = room != null && room.ID == 0 ? null : room;
        }

        private Variables _variables;
        public Variables Variables
        {
            get
            {
                if (_variables == null || _variables.Count == 0)
                {
                    _variables = new Variables(_rpc, _peerId, _index);
                }

                return _variables;
            }
            internal set { _variables = value; }
        }

        private DeviceConfig _config = null;
        public DeviceConfig Config
        {
            get
            {
                if (_config == null || _config.Count == 0)
                {
                    _config = new DeviceConfig(_rpc, _peerId, _index, RPCParameterSetType.rpcMaster, _rpc.GetParamsetDescription(_peerId, _index, RPCParameterSetType.rpcMaster));
                    _config.VariableReloadRequiredEvent += Config_OnVariableReloadRequired;
                    _config.Reload();
                }
                return _config;
            }
            internal set
            {
                _config = value;
            }
        }

        private Links _links = null;
        public Links Links
        {
            get
            {
                if (_links == null || _links.Count == 0)
                {
                    _links = new Links(_rpc, _peerId, _index);
                    _links.Reload();
                }
                return _links;
            }
            internal set
            {
                _links = value;
            }
        }

        private string _typeString = "";
        public string TypeString
        {
            get
            {
                if (!_descriptionRequested)
                {
                    _rpc.GetDeviceDescription(this);
                    _descriptionRequested = true;
                }
                return _typeString;
            }
            internal set { _typeString = value; }
        }

        private bool _aesActive = false;
        public bool AESActive
        {
            get
            {
                if (!_descriptionRequested)
                {
                    _rpc.GetDeviceDescription(this);
                    _descriptionRequested = true;
                }
                return _aesActive;
            }
            internal set { _aesActive = value; }
        }

        private ChannelDirection _direction = ChannelDirection.None;
        public ChannelDirection Direction
        {
            get
            {
                if (!_descriptionRequested)
                {
                    _rpc.GetDeviceDescription(this);
                    _descriptionRequested = true;
                }
                return _direction;
            }
            internal set { _direction = value; }
        }

        private string[] _linkSourceRoles = new string[0];
        public string[] LinkSourceRoles
        {
            get
            {
                if (!_descriptionRequested)
                {
                    _rpc.GetDeviceDescription(this);
                    _descriptionRequested = true;
                }
                //Return a copy
                return _linkSourceRoles.ToArray();
            }
            internal set { _linkSourceRoles = value; }
        }

        private string[] _linkTargetRoles = new string[0];
        public string[] LinkTargetRoles
        {
            get
            {
                if (!_descriptionRequested)
                {
                    _rpc.GetDeviceDescription(this);
                    _descriptionRequested = true;
                }
                //Return a copy
                return _linkTargetRoles.ToArray();
            }
            internal set { _linkTargetRoles = value; }
        }

        private long _groupedWith = -1;
        public long GroupedWith
        {
            get
            {
                if (!_descriptionRequested)
                {
                    _rpc.GetDeviceDescription(this);
                    _descriptionRequested = true;
                }
                return _groupedWith;
            }
            internal set { _groupedWith = value; }
        }

        private string _teamSerialNumber = "";
        public string TeamSerialNumber
        {
            get
            {
                if (!_descriptionRequested)
                {
                    _rpc.GetDeviceDescription(this);
                    _descriptionRequested = true;
                }
                return _teamSerialNumber;
            }
            internal set { _teamSerialNumber = value; }
        }

        private long _teamID = 0;
        public long TeamID
        {
            get
            {
                if (!_descriptionRequested)
                {
                    _rpc.GetDeviceDescription(this);
                    _descriptionRequested = true;
                }
                return _teamID;
            }
            internal set { _teamID = value; }
        }

        private long _teamChannel = 0;
        public long TeamChannel
        {
            get
            {
                if (!_descriptionRequested)
                {
                    _rpc.GetDeviceDescription(this);
                    _descriptionRequested = true;
                }
                return _teamChannel;
            }
            internal set { _teamChannel = value; }
        }

        public void ResetTeam()
        {
            _rpc.SetTeam(this.PeerID, this.Index);
        }

        public void SetTeam(long teamID, long teamChannel)
        {
            _rpc.SetTeam(this.PeerID, this.Index, teamID, teamChannel);
        }

        private string _teamTag = "";
        public string TeamTag
        {
            get
            {
                if (!_descriptionRequested)
                {
                    _rpc.GetDeviceDescription(this);
                    _descriptionRequested = true;
                }
                return _teamTag;
            }
            internal set { _teamTag = value; }
        }

        private string[] _teamMembers = new string[0];
        public string[] TeamMembers
        {
            get
            {
                if (!_descriptionRequested)
                {
                    _rpc.GetDeviceDescription(this);
                    _descriptionRequested = true;
                }
                return _teamMembers.ToArray();
            }
            internal set { _teamMembers = value; }
        }

        public Channel(RPCController rpc, long peerId, long index)
        {
            _rpc = rpc;
            _peerId = peerId;
            _index = index;
        }

        public void Dispose()
        {
            if (_config != null)
            {
                _config.Dispose();
            }

            if (_links != null)
            {
                _links.Dispose();
            }

            _rpc = null;
        }

        public void Reload()
        {
            _descriptionRequested = false;
            _links = null;
            _config = null;
            _variables = null;
        }

        void Config_OnVariableReloadRequired(DeviceConfig sender, bool reloadDevice)
        {
            VariableReloadRequiredEvent?.Invoke(this, reloadDevice);
        }
    }
}
