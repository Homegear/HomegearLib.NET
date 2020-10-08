using HomegearLib.RPC;
using System;
using System.Collections.Generic;

namespace HomegearLib
{
    public class Channels : ReadOnlyDictionary<long, Channel>, IDisposable
    {
        public Channels(Dictionary<long, Channel> channels) : base(channels)
        {
        }

        public void Dispose()
        {
            foreach (KeyValuePair<long, Channel> channel in _dictionary)
            {
                channel.Value.Dispose();
            }
        }
    }
}
