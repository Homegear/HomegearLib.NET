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

        public void Update(out bool interfacesRemoved, out bool interfacesAdded)
        {
            interfacesRemoved = false;
            interfacesAdded = false;
            Dictionary<string, Interface> interfaces = _rpc.ListInterfaces();
            foreach (KeyValuePair<string, Interface> interfacePair in interfaces)
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
                physicalInterface.IpAddress = interfacePair.Value.IpAddress;
                physicalInterface.Hostname = interfacePair.Value.Hostname;
                physicalInterface.LastPacketReceived = interfacePair.Value.LastPacketReceived;
                physicalInterface.LastPacketSent = interfacePair.Value.LastPacketSent;
                physicalInterface.Connected = interfacePair.Value.Connected;
            }
            foreach (KeyValuePair<string, Interface> interfacePair in _dictionary)
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