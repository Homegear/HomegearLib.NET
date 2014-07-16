using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HomegearLib.RPC;

namespace HomegearLib
{
    public class Families : IDictionary<Int32, Family>
    {
        RPCController _rpc = null;
        Dictionary<Int32, Family> _families = null;

        public Families(RPCController rpc, Dictionary<Int32, Family> families)
        {
            _rpc = rpc;
            _families = families;
        }

        public void Add(Int32 key, Family value)
        {
            throw new HomegearReadOnlyException("Families is readonly.");
        }

        public bool ContainsKey(Int32 key)
        {
            return _families.ContainsKey(key);
        }

        public ICollection<Int32> Keys
        {
            get { return _families.Keys; }
        }

        public bool Remove(Int32 key)
        {
            throw new HomegearReadOnlyException("Families is readonly.");
        }

        public bool TryGetValue(Int32 key, out Family value)
        {
            return _families.TryGetValue(key, out value);
        }

        public ICollection<Family> Values
        {
            get { return _families.Values; }
        }

        public Family this[Int32 key]
        {
            get
            {
                return _families[key];
            }
            set
            {
                throw new HomegearReadOnlyException("Families is readonly.");
            }
        }

        public void Add(KeyValuePair<Int32, Family> item)
        {
            throw new HomegearReadOnlyException("Families is readonly.");
        }

        public void Clear()
        {
            throw new HomegearReadOnlyException("Families is readonly.");
        }

        public bool Contains(KeyValuePair<Int32, Family> item)
        {
            return _families.Contains(item);
        }

        public void CopyTo(KeyValuePair<Int32, Family>[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        public int Count
        {
            get { return _families.Count; }
        }

        public bool IsReadOnly
        {
            get { return true; }
        }

        public bool Remove(KeyValuePair<Int32, Family> item)
        {
            throw new HomegearReadOnlyException("Families is readonly.");
        }

        public IEnumerator<KeyValuePair<Int32, Family>> GetEnumerator()
        {
            return _families.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return _families.GetEnumerator();
        }
    }
}
