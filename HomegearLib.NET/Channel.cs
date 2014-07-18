using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HomegearLib.RPC;

namespace HomegearLib
{
    public class Channel : IDisposable
    {
        private RPCController _rpc = null;

        private Int32 _peerID = 0;
        public Int32 PeerID { get { return _peerID; } }

        private Int32 _index = 0;
        public Int32 Index { get { return _index; } }

        private Variables _variables;
        public Variables Variables { get { return _variables; } internal set { _variables = value; } }

        private DeviceConfig _config;
        public DeviceConfig Config
        { 
            get
            {
                if (_config == null || _config.Count == 0)
                {
                    _config = new DeviceConfig(_rpc, _peerID, _index, RPCParameterSetType.rpcMaster, _rpc.GetParamsetDescription(_peerID, Index, RPCParameterSetType.rpcMaster));
                    _rpc.GetParamset(_peerID, Index, RPCParameterSetType.rpcMaster, _config);
                }
                return _config;
            } 
            internal set
            {
                _config = value;
            }
        }

        public Channel(RPCController rpc, Int32 peerID, Int32 index)
        {
            _rpc = rpc;
            _peerID = peerID;
            _index = index;
        }

        public void Dispose()
        {
            if(_variables != null) _variables.Dispose();
            if(_config != null) _config.Dispose();
            _rpc = null;
        }
    }
}
