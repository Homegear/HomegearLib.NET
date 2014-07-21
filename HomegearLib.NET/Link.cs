using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HomegearLib.RPC;

namespace HomegearLib
{
    public enum LinkFlags
    {
        fNone = 0,
        fReceiverUnknown = 1,
        fSenderIsUnknown = 2,
        fVirtual = 4
    }

    public class Link : IDisposable
    {
        private RPCController _rpc = null;

        protected Int32 _peerID = 0;
        public Int32 PeerID { get { return _peerID; } internal set { _peerID = value; } }

        protected Int32 _channel = -1;
        public Int32 Channel { get { return _channel; } internal set { _channel = value; } }

        protected Int32 _remotePeerID = 0;
        public Int32 RemotePeerID { get { return _remotePeerID; } internal set { _remotePeerID = value; } }

        protected Int32 _remoteChannel = -1;
        public Int32 RemoteChannel { get { return _remoteChannel; } internal set { _remoteChannel = value; } }

        LinkFlags _flags = LinkFlags.fNone;
        public LinkFlags Flags { get { return _flags; } internal set { _flags = value; } }

        bool _isSender = false;
        public bool IsSender { get { return _isSender; } internal set { _isSender = value; } }

        protected String _name = "";
        public String Name { get { return _name; } internal set { _name = value; } }

        protected String _description = "";
        public String Description { get { return _description; } internal set { _description = value; } }

        private DeviceConfig _config = null;
        public DeviceConfig Config
        {
            get
            {
                if (_config == null || _config.Count == 0)
                {
                    _config = new DeviceConfig(_rpc, _peerID, _channel, _remotePeerID, _remoteChannel, RPCParameterSetType.rpcLink, _rpc.GetParamsetDescription(_peerID, _channel, RPCParameterSetType.rpcLink));
                    _config.Reload();
                }
                return _config;
            }
            internal set
            {
                _config = value;
            }
        }

        public Link(RPCController rpc, Int32 peerID, Int32 channel, Int32 remotePeerID, Int32 remoteChannel, bool isSender)
        {
            _rpc = rpc;
            _peerID = peerID;
            _channel = channel;
            _remotePeerID = remotePeerID;
            _remoteChannel = remoteChannel;
            _isSender = isSender;
        }

        public void Dispose()
        {
            _rpc = null;
        }
    }
}
