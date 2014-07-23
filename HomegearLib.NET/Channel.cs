using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HomegearLib.RPC;

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
        bool _descriptionRequested = false;

        private Int32 _peerID = 0;
        public Int32 PeerID { get { return _peerID; } }

        private Int32 _index = 0;
        public Int32 Index { get { return _index; } }

        private Variables _variables;
        public Variables Variables { get { return _variables; } internal set { _variables = value; } }

        private DeviceConfig _config = null;
        public DeviceConfig Config
        { 
            get
            {
                if (_config == null || _config.Count == 0)
                {
                    _config = new DeviceConfig(_rpc, _peerID, _index, RPCParameterSetType.rpcMaster, _rpc.GetParamsetDescription(_peerID, _index, RPCParameterSetType.rpcMaster));
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
                if(_links == null || _links.Count == 0)
                {
                    _links = new Links(_rpc, _peerID, _index);
                    _links.Reload();                    
                }
                return _links;
            }
            internal set
            {
                _links = value;
            }
        }

        private String _typeString = "";
        public String TypeString
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

        private String[] _linkSourceRoles = new String[0];
        public String[] LinkSourceRoles
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

        private String[] _linkTargetRoles = new String[0];
        public String[] LinkTargetRoles
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

        private Int32 _groupedWith = -1;
        public Int32 GroupedWith
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

        private String _team = "";
        public String Team
        {
            get
            {
                if (!_descriptionRequested)
                {
                    _rpc.GetDeviceDescription(this);
                    _descriptionRequested = true;
                }
                return _team;
            }
            internal set { _team = value; }
        }

        private String _teamTag = "";
        public String TeamTag
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

        private String[] _teamMembers = new String[0];
        public String[] TeamMembers
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

        public Channel(RPCController rpc, Int32 peerID, Int32 index)
        {
            _rpc = rpc;
            _peerID = peerID;
            _index = index;
        }

        public void Dispose()
        {
            if (_config != null) _config.Dispose();
            if (_links != null) _links.Dispose();
            _rpc = null;
        }

        public void Reload()
        {
            _descriptionRequested = false;
            _links = null;
            _config = null;
            _variables = null;
        }
    }
}
