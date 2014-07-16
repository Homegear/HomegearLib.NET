using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HomegearLib.RPC;

namespace HomegearLib
{
    public class Channels : IDictionary<Int32, Variables>
    {
        RPCController _rpc = null;
        Dictionary<Int32, Variables> _channels = null;

        public Channels(RPCController rpc, Dictionary<Int32, Variables> channels)
        {
            _rpc = rpc;
            _channels = channels;
        }

        public void Add(Int32 key, Variables value)
        {
            throw new HomegearReadOnlyException("Channels is readonly.");
        }

        public bool ContainsKey(Int32 key)
        {
            return _channels.ContainsKey(key);
        }

        public ICollection<Int32> Keys
        {
            get { return _channels.Keys; }
        }

        public bool Remove(Int32 key)
        {
            throw new HomegearReadOnlyException("Channels is readonly.");
        }

        public bool TryGetValue(Int32 key, out Variables value)
        {
            return _channels.TryGetValue(key, out value);
        }

        public ICollection<Variables> Values
        {
            get { return _channels.Values; }
        }

        public Variables this[Int32 key]
        {
            get
            {
                return _channels[key];
            }
            set
            {
                throw new HomegearReadOnlyException("Channels is readonly.");
            }
        }

        public void Add(KeyValuePair<Int32, Variables> item)
        {
            throw new HomegearReadOnlyException("Channels is readonly.");
        }

        public void Clear()
        {
            throw new HomegearReadOnlyException("Channels is readonly.");
        }

        public bool Contains(KeyValuePair<Int32, Variables> item)
        {
            return _channels.Contains(item);
        }

        public void CopyTo(KeyValuePair<Int32, Variables>[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        public int Count
        {
            get { return _channels.Count; }
        }

        public bool IsReadOnly
        {
            get { return true; }
        }

        public bool Remove(KeyValuePair<Int32, Variables> item)
        {
            throw new HomegearReadOnlyException("Channels is readonly.");
        }

        public IEnumerator<KeyValuePair<Int32, Variables>> GetEnumerator()
        {
            return _channels.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return _channels.GetEnumerator();
        }
    }
}
