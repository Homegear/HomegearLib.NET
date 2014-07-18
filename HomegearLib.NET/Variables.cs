using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HomegearLib.RPC;

namespace HomegearLib
{
    public class Variables : ReadOnlyDictionary<String, Variable>, IDisposable
    {
        RPCController _rpc = null;

        public Variables(RPCController rpc, Dictionary<String, Variable> variables) : base(variables)
        {
            _rpc = rpc;
        }

        public void Dispose()
        {
            _rpc = null;
            foreach(KeyValuePair<String, Variable> variable in _dictionary)
            {
                variable.Value.Dispose();
            }
        }
    }
}
