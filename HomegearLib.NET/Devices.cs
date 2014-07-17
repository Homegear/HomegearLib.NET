using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HomegearLib.RPC;

namespace HomegearLib
{
    public class Devices : IDictionary<Int32, Device>, IDisposable
    {
        RPCController _rpc = null;
        Dictionary<Int32, Device> _devices = null;

        public Devices(RPCController rpc, Dictionary<Int32, Device> Devices)
        {
            _rpc = rpc;
            _devices = Devices;
        }

        public void Dispose()
        {
            _rpc = null;
            foreach(KeyValuePair<Int32, Device> device in _devices)
            {
                device.Value.Dispose();
            }
        }

        public void Add(Int32 key, Device value)
        {
            throw new HomegearReadOnlyException("Devices is readonly.");
        }

        public bool ContainsKey(Int32 key)
        {
            return _devices.ContainsKey(key);
        }

        public ICollection<Int32> Keys
        {
            get { return _devices.Keys; }
        }

        public bool Remove(Int32 key)
        {
            throw new HomegearReadOnlyException("Devices is readonly.");
        }

        public bool TryGetValue(Int32 key, out Device value)
        {
            return _devices.TryGetValue(key, out value);
        }

        public ICollection<Device> Values
        {
            get { return _devices.Values; }
        }

        public Device this[Int32 key]
        {
            get
            {
                return _devices[key];
            }
            set
            {
                throw new HomegearReadOnlyException("Devices is readonly.");
            }
        }

        public void Add(KeyValuePair<Int32, Device> item)
        {
            throw new HomegearReadOnlyException("Devices is readonly.");
        }

        public void Clear()
        {
            throw new HomegearReadOnlyException("Devices is readonly.");
        }

        public bool Contains(KeyValuePair<Int32, Device> item)
        {
            return _devices.Contains(item);
        }

        public void CopyTo(KeyValuePair<Int32, Device>[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        public int Count
        {
            get { return _devices.Count; }
        }

        public bool IsReadOnly
        {
            get { return true; }
        }

        public bool Remove(KeyValuePair<Int32, Device> item)
        {
            throw new HomegearReadOnlyException("Devices is readonly.");
        }

        public IEnumerator<KeyValuePair<Int32, Device>> GetEnumerator()
        {
            return _devices.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return _devices.GetEnumerator();
        }
    }
}
