using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HomegearLib.RPC;

namespace HomegearLib
{
    public class Devices : ReadOnlyDictionary<Int32, Device>, IDisposable
    {
        RPCController _rpc = null;

        public Devices(RPCController rpc, Dictionary<Int32, Device> devices) : base(devices)
        {
            _rpc = rpc;
        }

        public void Dispose()
        {
            _rpc = null;
            foreach(KeyValuePair<Int32, Device> device in _dictionary)
            {
                device.Value.Dispose();
            }
        }
    }
}
