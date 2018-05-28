using HomegearLib.RPC;
using System;
using System.Collections.Generic;

namespace HomegearLib
{
    public class SystemVariables : ReadOnlyDictionary<string, SystemVariable>, IDisposable
    {
        RPCController _rpc = null;

        public SystemVariables(RPCController rpc, Dictionary<string, SystemVariable> systemVariables) : base(systemVariables)
        {
            _rpc = rpc;
        }

        public void Dispose()
        {
            _rpc = null;
            foreach (KeyValuePair<string, SystemVariable> systemVariable in _dictionary)
            {
                systemVariable.Value.Dispose();
            }
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

        public List<SystemVariable> Update(out bool variablesDeleted, out bool variablesAdded)
        {
            Dictionary<string, SystemVariable> variables = _rpc.GetAllSystemVariables();
            variablesDeleted = false;
            variablesAdded = false;
            List<SystemVariable> changedVariables = new List<SystemVariable>();
            foreach (KeyValuePair<string, SystemVariable> variablePair in variables)
            {
                if (!_dictionary.ContainsKey(variablePair.Key))
                {
                    variablesAdded = true;
                    continue;
                }
                SystemVariable variable = _dictionary[variablePair.Key];
                if (variable.Type != variablePair.Value.Type)
                {
                    variablesAdded = true;
                    variablesDeleted = true;
                    continue;
                }
                if (variable.SetValue(variablePair.Value))
                {
                    changedVariables.Add(variable);
                }
            }
            foreach (KeyValuePair<string, SystemVariable> variablePair in _dictionary)
            {
                if (!variables.ContainsKey(variablePair.Key))
                {
                    variablesDeleted = true;
                    break;
                }
            }
            return changedVariables;
        }
    }
}
