using HomegearLib.RPC;
using System;
using System.Collections.Generic;

namespace HomegearLib
{
    public class Channels : ReadOnlyDictionary<int, Channel>, IDisposable
    {
        RPCController _rpc = null;

        public Channels(RPCController rpc, Dictionary<int, Channel> channels) : base(channels)
        {
            _rpc = rpc;
        }

        public void Dispose()
        {
            _rpc = null;
            foreach (KeyValuePair<int, Channel> channel in _dictionary)
            {
                channel.Value.Dispose();
            }
        }
    }
}
