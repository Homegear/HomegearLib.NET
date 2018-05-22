using HomegearLib.RPC;
using System;
using System.Collections.Generic;

namespace HomegearLib
{
    public class Families : ReadOnlyDictionary<Int32, Family>, IDisposable
    {
        RPCController _rpc = null;

        public Families(RPCController rpc, Dictionary<Int32, Family> families) : base(families)
        {
            _rpc = rpc;
        }

        public void Dispose()
        {
            _rpc = null;
            foreach (KeyValuePair<Int32, Family> family in _dictionary)
            {
                family.Value.Dispose();
            }
        }
    }
}
