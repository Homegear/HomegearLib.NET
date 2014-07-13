using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomegearLib.Encoding
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
