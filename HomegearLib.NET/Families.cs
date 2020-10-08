using HomegearLib.RPC;
using System;
using System.Collections.Generic;

namespace HomegearLib
{
    public class Families : ReadOnlyDictionary<long, Family>, IDisposable
    {
        public Families(Dictionary<long, Family> families) : base(families)
        {
        }

        public void Dispose()
        {
            foreach (KeyValuePair<long, Family> family in _dictionary)
            {
                family.Value.Dispose();
            }
        }
    }
}
