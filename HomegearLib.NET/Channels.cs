using HomegearLib.RPC;
using System;
using System.Collections.Generic;

namespace HomegearLib
{
    public class Channels : ReadOnlyDictionary<Int32, Channel>, IDisposable
    {
        RPCController _rpc = null;

        public Channels(RPCController rpc, Dictionary<Int32, Channel> channels) : base(channels)
        {
            _rpc = rpc;
        }

        public void Dispose()
        {
            _rpc = null;
            foreach (KeyValuePair<Int32, Channel> channel in _dictionary)
            {
                channel.Value.Dispose();
            }
        }
    }
}
