using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HomegearLib.RPC;

namespace HomegearLib
{
    public class Variables : IDictionary<String, Variable>
    {
        RPCController _rpc = null;
        Dictionary<String, Variable> _variables = null;

        public Variables(RPCController rpc, Dictionary<String, Variable> variables)
        {
            _rpc = rpc;
            _variables = variables;
        }

        public void Add(String key, Variable value)
        {
            throw new HomegearReadOnlyException("Variables is readonly.");
        }

        public bool ContainsKey(String key)
        {
            return _variables.ContainsKey(key);
        }

        public ICollection<String> Keys
        {
            get { return _variables.Keys; }
        }

        public bool Remove(String key)
        {
            throw new HomegearReadOnlyException("Variables is readonly.");
        }

        public bool TryGetValue(String key, out Variable value)
        {
            return _variables.TryGetValue(key, out value);
        }

        public ICollection<Variable> Values
        {
            get { return _variables.Values; }
        }

        public Variable this[String key]
        {
            get
            {
                return _variables[key];
            }
            set
            {
                throw new HomegearReadOnlyException("Variables is readonly.");
            }
        }

        public void Add(KeyValuePair<String, Variable> item)
        {
            throw new HomegearReadOnlyException("Variables is readonly.");
        }

        public void Clear()
        {
            throw new HomegearReadOnlyException("Variables is readonly.");
        }

        public bool Contains(KeyValuePair<String, Variable> item)
        {
            return _variables.Contains(item);
        }

        public void CopyTo(KeyValuePair<String, Variable>[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        public int Count
        {
            get { return _variables.Count; }
        }

        public bool IsReadOnly
        {
            get { return true; }
        }

        public bool Remove(KeyValuePair<String, Variable> item)
        {
            throw new HomegearReadOnlyException("Variables is readonly.");
        }

        public IEnumerator<KeyValuePair<String, Variable>> GetEnumerator()
        {
            return _variables.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return _variables.GetEnumerator();
        }
    }
}
