using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HomegearLib.RPC;

namespace HomegearLib
{
    public class Links : ReadOnlyDictionary<Int32, ReadOnlyDictionary<Int32, Link>>, IDisposable
    {
        RPCController _rpc = null;

        Int32 _peerID = 0;
        public Int32 PeerID { get { return _peerID; } }

        Int32 _channel = -1;
        public Int32 Channel { get { return _channel; } }

        public Links(RPCController rpc, Int32 peerID, Int32 channel) : base()
        {
            _rpc = rpc;
            _peerID = peerID;
            _channel = channel;
        }

        public void Dispose()
        {
            _rpc = null;
        }

        public void Reload()
        {
            List<Link> allLinks = _rpc.GetLinks(_peerID, _channel);
            Dictionary<Int32, Dictionary<Int32, Link>> links = new Dictionary<Int32, Dictionary<Int32, Link>>();
            foreach (Link link in allLinks)
            {
                if (!links.ContainsKey(link.RemotePeerID)) links.Add(link.RemotePeerID, new Dictionary<Int32, Link>());
                if (links[link.RemotePeerID].ContainsKey(link.RemoteChannel)) continue;
                links[link.RemotePeerID].Add(link.RemoteChannel, link);
            }
            Dictionary<Int32, ReadOnlyDictionary<Int32, Link>> links2 = new Dictionary<Int32, ReadOnlyDictionary<Int32, Link>>();
            foreach (KeyValuePair<Int32, Dictionary<Int32, Link>> pair in links)
            {
                links2.Add(pair.Key, new ReadOnlyDictionary<Int32, Link>(pair.Value));
            }
            _dictionary = links2;
        }

        public void Add(Int32 remoteID, Int32 remoteChannel, bool isSender)
        {
            if (isSender) _rpc.AddLink(_peerID, _channel, remoteID, remoteChannel);
            else _rpc.AddLink(remoteID, remoteChannel, _peerID, _channel);
        }
    }
}
