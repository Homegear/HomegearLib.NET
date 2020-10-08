using HomegearLib.RPC;
using System;
using System.Collections.Generic;

namespace HomegearLib
{
    public class Variables : ReadOnlyDictionary<string, Variable>, IDisposable
    {
        RPCController _rpc = null;
        readonly long _peerId = 0;
        readonly long _channel = -1;

        internal Variables(RPCController rpc, long peerId, long channel) : base()
        {
            _rpc = rpc;
            _peerId = peerId;
            _channel = channel;
            Reload();
        }

        public Variables(RPCController rpc, long peerId, long channel, Dictionary<string, Variable> variables) : base(variables)
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
            foreach (KeyValuePair<string, Variable> variable in _dictionary)
            {
                variable.Value.Dispose();
            }
        }
    }
}
