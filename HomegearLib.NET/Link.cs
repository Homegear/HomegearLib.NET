using HomegearLib.RPC;
using System;

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

        protected Int32 _peerId = 0;
        public Int32 PeerID { get { return _peerId; } internal set { _peerId = value; } }

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
        public String Name
        {
            get { return _name; }
            set
            {
                _name = value;
                _rpc.SetLinkInfo(this);
            }
        }

        /// <summary>
        /// Sets the name of the link without calling any RPC functions
        /// </summary>
        /// <param name="name">The name of the link</param>
        internal void SetNameNoRPC(String name)
        {
            _name = name;
        }

        protected String _description = "";
        public String Description
        {
            get { return _description; }
            set
            {
                _description = value;
                _rpc.SetLinkInfo(this);
            }
        }

        /// <summary>
        /// Sets the description of the link without calling any RPC functions
        /// </summary>
        /// <param name="name">The description of the link</param>
        internal void SetDescriptionNoRPC(String description)
        {
            _description = description;
        }

        private DeviceConfig _config = null;
        public DeviceConfig Config
        {
            get
            {
                if (_config == null || _config.Count == 0)
                {
                    _config = new DeviceConfig(_rpc, _peerId, _channel, _remotePeerID, _remoteChannel, RPCParameterSetType.rpcLink, _rpc.GetParamsetDescription(_peerId, _channel, RPCParameterSetType.rpcLink));
                    _config.Reload();
                }
                return _config;
            }
            internal set
            {
                _config = value;
            }
        }

        public Link(RPCController rpc, Int32 peerId, Int32 channel, Int32 remotePeerID, Int32 remoteChannel, bool isSender)
        {
            _rpc = rpc;
            _peerId = peerId;
            _channel = channel;
            _remotePeerID = remotePeerID;
            _remoteChannel = remoteChannel;
            _isSender = isSender;
        }

        public void Dispose()
        {
            _rpc = null;
        }

        public void Remove()
        {
            _rpc.RemoveLink(this);
        }
    }
}
