using HomegearLib.RPC;
using System;
using System.Collections.Generic;

namespace HomegearLib
{
    public class Family : IDisposable
    {
        RPCController _rpc = null;

        private long _id = -1;
        public long ID { get { return _id; } set { _id = value; } }

        private string _name = "";
        public string Name { get { return _name; } set { _name = value; } }

        private IReadOnlyList<string> _pairingMethods = (new List<string>()).AsReadOnly();
        public IReadOnlyList<string> PairingMethods { get { return _pairingMethods; } }
        internal void SetPairingMethods(IReadOnlyList<string> pairingMethods)
        {
            _pairingMethods = pairingMethods;
        }

        public Family(RPCController rpc, long id, string name)
        {
            _rpc = rpc;
            _id = id;
            _name = name;
        }

        public void Dispose()
        {

        }

        public override string ToString()
        {
            return _name;
        }

        public RPCVariable invokeMethod(string method, List<RPCVariable> parameters)
        {
            return _rpc.invokeFamilyMethod(this, method, parameters);
        }
    }
}