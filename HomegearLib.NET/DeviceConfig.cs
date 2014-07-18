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

        public DeviceConfig(RPCController rpc, Dictionary<String, ConfigParameter> deviceConfig) : base(deviceConfig)
        {
            _rpc = rpc;
        }

        public void Dispose()
        {
            _rpc = null;
        }
    }
}
