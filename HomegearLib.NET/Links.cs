using HomegearLib.RPC;
using System;
using System.Collections.Generic;

namespace HomegearLib
{
    public class Links : ReadOnlyDictionary<int, ReadOnlyDictionary<int, Link>>, IDisposable
    {
        RPCController _rpc = null;

        int _peerId = 0;
        public int PeerID { get { return _peerId; } }

        int _channel = -1;
        public int Channel { get { return _channel; } }

        public Links(RPCController rpc, int peerId, int channel) : base()
        {
            _rpc = rpc;
            _peerId = peerId;
            _channel = channel;
        }

        public void Dispose()
        {
            _rpc = null;
        }

        public void Reload()
        {
            List<Link> allLinks = _rpc.GetLinks(_peerId, _channel);
            Dictionary<int, Dictionary<int, Link>> links = new Dictionary<int, Dictionary<int, Link>>();
            foreach (Link link in allLinks)
            {
                if (!links.ContainsKey(link.RemotePeerID)) links.Add(link.RemotePeerID, new Dictionary<int, Link>());
                if (links[link.RemotePeerID].ContainsKey(link.RemoteChannel)) continue;
                links[link.RemotePeerID].Add(link.RemoteChannel, link);
            }
            Dictionary<int, ReadOnlyDictionary<int, Link>> links2 = new Dictionary<int, ReadOnlyDictionary<int, Link>>();
            foreach (KeyValuePair<int, Dictionary<int, Link>> pair in links)
            {
                links2.Add(pair.Key, new ReadOnlyDictionary<int, Link>(pair.Value));
            }
            _dictionary = links2;
        }

        public void Add(int remoteID, int remoteChannel, bool isSender)
        {
            if (isSender) _rpc.AddLink(_peerId, _channel, remoteID, remoteChannel);
            else _rpc.AddLink(remoteID, remoteChannel, _peerId, _channel);
        }
    }
}
