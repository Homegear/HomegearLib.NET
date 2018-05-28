using HomegearLib.RPC;
using System;
using System.Collections.Generic;

namespace HomegearLib
{
    public class DeviceConfig : ReadOnlyDictionary<string, ConfigParameter>, IDisposable
    {
        private RPCController _rpc = null;

        internal delegate void VariableReloadRequiredEventHandler(DeviceConfig sender, bool reloadDevice);

        internal event VariableReloadRequiredEventHandler VariableReloadRequiredEvent;

        private long _peerId = 0;
        public long PeerID { get { return _peerId; } }

        private long _channel;
        public long Channel { get { return _channel; } }

        private long _remotePeerID = 0;
        public long RemotePeerID { get { return _remotePeerID; } }

        private long _remoteChannel = -1;
        public long RemoteChannel { get { return _remoteChannel; } }

        private RPCParameterSetType _type;
        public RPCParameterSetType Type { get { return _type; } }

        public DeviceConfig(RPCController rpc, long peerId, long channel, RPCParameterSetType type, Dictionary<string, ConfigParameter> deviceConfig) : base(deviceConfig)
        {
            _rpc = rpc;
            _peerId = peerId;
            _channel = channel;
            _type = type;
        }

        public DeviceConfig(RPCController rpc, long peerId, long channel, long remotePeerID, long remoteChannel, RPCParameterSetType type, Dictionary<string, ConfigParameter> deviceConfig)
            : base(deviceConfig)
        {
            _rpc = rpc;
            _peerId = peerId;
            _channel = channel;
            _remotePeerID = remotePeerID;
            _remoteChannel = remoteChannel;
            _type = type;
        }

        public void Dispose()
        {
            _rpc = null;
        }

        public List<ConfigParameter> Reload()
        {
            if (_type == RPCParameterSetType.rpcLink)
            {
                return _rpc.GetParamset(_peerId, _channel, _remotePeerID, _remoteChannel, this);
            }
            else
            {
                return _rpc.GetParamset(_peerId, _channel, RPCParameterSetType.rpcMaster, this);
            }
        }

        public void Put()
        {
            Dictionary<string, ConfigParameter> changedParameters = new Dictionary<string, ConfigParameter>();
            foreach (KeyValuePair<string, ConfigParameter> parameter in _dictionary)
            {
                if (parameter.Value.DataPending)
                {
                    changedParameters.Add(parameter.Key, parameter.Value);
                }
            }
            if (changedParameters.Count == 0)
            {
                return;
            }

            if (_type == RPCParameterSetType.rpcLink)
            {
                _rpc.PutParamset(_peerId, _channel, _remotePeerID, _remoteChannel, changedParameters);
            }
            else
            {
                _rpc.PutParamset(_peerId, _channel, _type, changedParameters);
            }

            bool reloadRequired = false;
            foreach (KeyValuePair<string, ConfigParameter> parameter in changedParameters)
            {
                parameter.Value.DataPending = false;
                if (parameter.Value.UIFlags == VariableUIFlags.fTransform)
                {
                    reloadRequired = true;
                }

                if (parameter.Key == "AES_ACTIVE")
                {
                    VariableReloadRequiredEvent(this, true);
                }
            }
            if (reloadRequired && VariableReloadRequiredEvent != null)
            {
                VariableReloadRequiredEvent(this, false);
            }
        }
    }
}
