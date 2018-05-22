using System;

namespace HomegearLib.RPC.Encoding
{
    internal class RPCHeader
    {
        private String _authorization;
        public String Authorization
        {
            get { return _authorization; }
            set { _authorization = value; }
        }
    }
}
