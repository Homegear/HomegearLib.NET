using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HomegearLib.RPC;

namespace HomegearLib
{
    public class DeviceConfig : ReadOnlyDictionary<String, ConfigParameter>, IDisposable
    {
        private RPCController _rpc = null;

        private Int32 _peerID = 0;
        public Int32 PeerID { get { return _peerID; } }

        private Int32 _channel;
        public Int32 Channel { get { return _channel; } }

        private RPCParameterSetType _type;
        public RPCParameterSetType Type { get { return _type; } }

        public DeviceConfig(RPCController rpc, Int32 peerID, Int32 channel, RPCParameterSetType type, Dictionary<String, ConfigParameter> deviceConfig) : base(deviceConfig)
        {
            _rpc = rpc;
            _peerID = peerID;
            _channel = channel;
            _type = type;
        }

        public void Dispose()
        {
            _rpc = null;
        }

        public void Put()
        {
            Dictionary<String, ConfigParameter> changedParameters = new Dictionary<string,ConfigParameter>();
            foreach(KeyValuePair<String, ConfigParameter> parameter in _dictionary)
            {
                if(parameter.Value.DataPending) changedParameters.Add(parameter.Key, parameter.Value);
            }
            if (changedParameters.Count == 0) return;
            _rpc.PutParamset(_peerID, _channel, _type, changedParameters);
            foreach (KeyValuePair<String, ConfigParameter> parameter in changedParameters)
            {
                parameter.Value.DataPending = false;
            }
        }
    }
}
