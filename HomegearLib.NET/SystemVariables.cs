using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HomegearLib.RPC;

namespace HomegearLib
{
    public class SystemVariables : ReadOnlyDictionary<String, SystemVariable>, IDisposable
    {
        RPCController _rpc = null;

        public SystemVariables(RPCController rpc, Dictionary<String, SystemVariable> systemVariables) : base(systemVariables)
        {
            _rpc = rpc;
        }

        public void Add(SystemVariable variable)
        {
            _rpc.SetSystemVariable(variable);
        }

        public void Reload()
        {
            _rpc.SystemVariables = _rpc.GetAllSystemVariables();
            _dictionary = _rpc.SystemVariables;
        }

        public void Dispose()
        {
            _rpc = null;
            foreach (KeyValuePair<String, SystemVariable> systemVariable in _dictionary)
            {
                systemVariable.Value.Dispose();
            }
        }
    }
}
