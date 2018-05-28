using HomegearLib.RPC;
using System;
using System.Collections.Generic;

namespace HomegearLib
{
    public class Families : ReadOnlyDictionary<long, Family>, IDisposable
    {
        RPCController _rpc = null;

        public Families(RPCController rpc, Dictionary<long, Family> families) : base(families)
        {
            _rpc = rpc;
        }

        public void Dispose()
        {
            _rpc = null;
            foreach (KeyValuePair<long, Family> family in _dictionary)
            {
                family.Value.Dispose();
            }
        }
    }
}
