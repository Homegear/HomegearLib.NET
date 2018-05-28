using HomegearLib.RPC;
using System;
using System.Collections.Generic;

namespace HomegearLib
{
    public class MetadataVariables : ReadOnlyDictionary<string, MetadataVariable>, IDisposable
    {
        RPCController _rpc = null;

        private long _peerId;
        public long PeerID { get { return _peerId; } }

        public MetadataVariables(RPCController rpc, long peerId, Dictionary<string, MetadataVariable> metadataVariables) : base(metadataVariables)
        {
            _rpc = rpc;
            _peerId = peerId;
        }

        public void Add(MetadataVariable variable)
        {
            _rpc.SetMetadata(variable);
        }

        public void Reload()
        {
            _dictionary = _rpc.GetAllMetadata(_peerId);
        }

        public void Dispose()
        {
            _rpc = null;
            foreach (KeyValuePair<string, MetadataVariable> metadataVariable in _dictionary)
            {
                metadataVariable.Value.Dispose();
            }
        }

        public List<MetadataVariable> Update(out bool variablesDeleted, out bool variablesAdded)
        {
            Dictionary<string, MetadataVariable> variables = _rpc.GetAllMetadata(_peerId);
            variablesDeleted = false;
            variablesAdded = false;
            List<MetadataVariable> changedVariables = new List<MetadataVariable>();
            foreach (KeyValuePair<string, MetadataVariable> variablePair in variables)
            {
                if (!_dictionary.ContainsKey(variablePair.Key))
                {
                    variablesAdded = true;
                    continue;
                }
                MetadataVariable variable = _dictionary[variablePair.Key];
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
            foreach (KeyValuePair<string, MetadataVariable> variablePair in _dictionary)
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
