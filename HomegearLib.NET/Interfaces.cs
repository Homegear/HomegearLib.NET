using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HomegearLib.RPC;

namespace HomegearLib
{
    public class Interfaces : ReadOnlyDictionary<String, Interface>, IDisposable
    {
        RPCController _rpc = null;

        public Interfaces(RPCController rpc, Dictionary<String, Interface> interfaces) : base(interfaces)
        {
            _rpc = rpc;
        }

        public void Dispose()
        {
            _rpc = null;
            foreach(KeyValuePair<String, Interface> physicalInterface in _dictionary)
            {
                physicalInterface.Value.Dispose();
            }
        }

        public void Update(out bool interfacesRemoved, out bool interfacesAdded)
        {
            interfacesRemoved = false;
            interfacesAdded = false;
            Dictionary<String, Interface> interfaces = _rpc.ListInterfaces();
            foreach (KeyValuePair<String, Interface> interfacePair in interfaces)
            {
                if (!_dictionary.ContainsKey(interfacePair.Key))
                {
                    interfacesAdded = true;
                    continue;
                }
                Interface physicalInterface = _dictionary[interfacePair.Value.ID];
                if (physicalInterface.Type != interfacePair.Value.Type || physicalInterface.Family.ID != interfacePair.Value.Family.ID)
                {
                    interfacesRemoved = true;
                    interfacesAdded = true;
                    continue;
                }
                physicalInterface.LastPacketReceived = interfacePair.Value.LastPacketReceived;
                physicalInterface.LastPacketSent = interfacePair.Value.LastPacketSent;
                physicalInterface.Connected = interfacePair.Value.Connected;
            }
            foreach (KeyValuePair<String, Interface> interfacePair in _dictionary)
            {
                if (!interfaces.ContainsKey(interfacePair.Key))
                {
                    interfacesRemoved = true;
                    break;
                }
            }
        }
    }
}
