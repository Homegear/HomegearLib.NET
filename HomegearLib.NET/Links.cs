using HomegearLib.RPC;
using System;
using System.Collections.Generic;

namespace HomegearLib
{
    public class Links : ReadOnlyDictionary<long, ReadOnlyDictionary<long, Link>>, IDisposable
    {
        RPCController _rpc = null;

        readonly long _peerId = 0;
        public long PeerID { get { return _peerId; } }

        readonly long _channel = -1;
        public long Channel { get { return _channel; } }

        public Links(RPCController rpc, long peerId, long channel) : base()
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
            Dictionary<long, Dictionary<long, Link>> links = new Dictionary<long, Dictionary<long, Link>>();
            foreach (Link link in allLinks)
            {
                if (!links.ContainsKey(link.RemotePeerID))
                {
                    links.Add(link.RemotePeerID, new Dictionary<long, Link>());
                }

                if (links[link.RemotePeerID].ContainsKey(link.RemoteChannel))
                {
                    continue;
                }

                links[link.RemotePeerID].Add(link.RemoteChannel, link);
            }
            Dictionary<long, ReadOnlyDictionary<long, Link>> links2 = new Dictionary<long, ReadOnlyDictionary<long, Link>>();
            foreach (KeyValuePair<long, Dictionary<long, Link>> pair in links)
            {
                links2.Add(pair.Key, new ReadOnlyDictionary<long, Link>(pair.Value));
            }
            _dictionary = links2;
        }

        public void Add(long remoteID, long remoteChannel, bool isSender)
        {
            if (isSender)
            {
                _rpc.AddLink(_peerId, _channel, remoteID, remoteChannel);
            }
            else
            {
                _rpc.AddLink(remoteID, remoteChannel, _peerId, _channel);
            }
        }
    }
}
