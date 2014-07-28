using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HomegearLib.RPC;

namespace HomegearLib
{
    public class Events : ReadOnlyDictionary<String, Event>, IDisposable
    {
        RPCController _rpc = null;

        public Events(RPCController rpc, Dictionary<String, Event> events) : base(events)
        {
            _rpc = rpc;
        }

        public void Dispose()
        {
            _rpc = null;
            foreach (KeyValuePair<String, Event> element in _dictionary)
            {
                element.Value.Dispose();
            }
        }
    }
}
