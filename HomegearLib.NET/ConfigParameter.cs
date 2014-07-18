using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HomegearLib.RPC;

namespace HomegearLib
{
    public class ConfigParameter : Variable
    {
        public ConfigParameter(Int32 peerID, Int32 channel, String name) : this(null, peerID, channel, name)
        {

        }

        public ConfigParameter(RPCController rpc, Int32 peerID, Int32 channel, String name) : base(rpc, peerID, channel, name)
        {
            
        }

        internal ConfigParameter(Int32 peerID, Int32 channel, String name, RPCVariable rpcVariable) : this(null, peerID, channel, name, rpcVariable)
        {
            
        }

        internal ConfigParameter(RPCController rpc, Int32 peerID, Int32 channel, String name, RPCVariable rpcVariable) : base(rpc, peerID, channel, name)
        {
            
        }
    }
}
