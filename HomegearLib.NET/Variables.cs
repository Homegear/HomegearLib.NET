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
        Int32 _peerId = 0;
        Int32 _channel = -1;

        internal Variables(RPCController rpc, Int32 peerId, Int32 channel) : base()
        {
            _rpc = rpc;
            _peerId = peerId;
            _channel = channel;
            Reload();
        }

        public Variables(RPCController rpc, Int32 peerId, Int32 channel, Dictionary<String, Variable> variables) : base(variables)
        {
            _rpc = rpc;
            _peerId = peerId;
            _channel = channel;
        }

        public void Reload()
        {
            _dictionary = _rpc.GetAllValues(_peerId, _channel);
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
