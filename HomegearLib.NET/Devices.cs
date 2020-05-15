using HomegearLib.RPC;
using System;
using System.Collections.Generic;

namespace HomegearLib
{
    public class Devices : ReadOnlyDictionary<long, Device>, IDisposable
    {
        RPCController _rpc = null;

        public Devices(RPCController rpc, Dictionary<long, Device> devices) : base(devices)
        {
            _rpc = rpc;
        }

        public void Dispose()
        {
            _rpc = null;
            foreach (KeyValuePair<long, Device> device in _dictionary)
            {
                device.Value.Dispose();
            }
        }

        public List<Variable> UpdateVariables(Dictionary<long, Device> variables, out bool devicesDeleted, out bool newDevices)
        {
            devicesDeleted = false;
            newDevices = false;
            List<Variable> changedVariables = new List<Variable>();
            foreach (KeyValuePair<long, Device> devicePair in variables)
            {
                if (!_dictionary.ContainsKey(devicePair.Key))
                {
                    newDevices = true;
                    continue;
                }
                Device device = _dictionary[devicePair.Value.ID];
                if (device.SerialNumber != devicePair.Value.SerialNumber || device.Family.ID != devicePair.Value.Family.ID)
                {
                    devicesDeleted = true;
                    newDevices = true;
                    continue;
                }
                foreach (KeyValuePair<long, Channel> channelPair in devicePair.Value.Channels)
                {
                    if (!device.Channels.ContainsKey(channelPair.Key))
                    {
                        continue;
                    }

                    Channel channel = device.Channels[channelPair.Key];
                    foreach (KeyValuePair<string, Variable> variablePair in channelPair.Value.Variables)
                    {
                        if (!channel.Variables.ContainsKey(variablePair.Key))
                        {
                            continue;
                        }

                        Variable variable = channel.Variables[variablePair.Key];
                        if (!variable.Compare(variablePair.Value))
                        {
                            variable.SetValue(variablePair.Value);
                            changedVariables.Add(variable);
                        }
                    }
                }
            }
            foreach (KeyValuePair<long, Device> devicePair in _dictionary)
            {
                if (!variables.ContainsKey(devicePair.Key))
                {
                    devicesDeleted = true;
                    break;
                }
            }
            return changedVariables;
        }

        public bool Add(string serialNumber)
        {
            return _rpc.AddDevice(serialNumber);
        }

        public long Create(Family family, long deviceType, string serialNumber, long address, long firmwareVersion)
        {
            return _rpc.CreateDevice(family, deviceType, serialNumber, address, firmwareVersion);
        }

        /// <summary>
        /// Searches for new devices on all supported device families and returns the number of newly found devices.
        /// </summary>
        /// <returns>The number of newly found devices.</returns>
        public long Search(long familyID = -1)
        {
            return familyID == -1 ? _rpc.SearchDevices() : _rpc.SearchDevices(familyID);
        }

        public void StartSniffing(Family family)
        {
            _rpc.StartSniffing(family);
        }

        public void StopSniffing(Family family)
        {
            _rpc.StopSniffing(family);
        }

        public Dictionary<long, SniffedDeviceInfo> GetSniffedDevices(Family family)
        {
            return _rpc.GetSniffedDevices(family);
        }
    }
}
