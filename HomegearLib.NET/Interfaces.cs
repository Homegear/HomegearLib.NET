using HomegearLib.RPC;
using System;
using System.Collections.Generic;

namespace HomegearLib
{
    public class Interfaces : ReadOnlyDictionary<string, Interface>, IDisposable
    {
        RPCController _rpc = null;

        public Interfaces(RPCController rpc, Dictionary<string, Interface> interfaces) : base(interfaces)
        {
            _rpc = rpc;
        }

        public void Dispose()
        {
            _rpc = null;
            foreach (KeyValuePair<string, Interface> physicalInterface in _dictionary)
            {
                physicalInterface.Value.Dispose();
            }
        }

        public void Reload()
        {
            _rpc.Interfaces = null;
            _dictionary = _rpc.Interfaces;
        }
    }
}