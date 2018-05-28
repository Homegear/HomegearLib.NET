using System;
using System.Collections.Generic;

namespace HomegearLib
{
    public class SniffedDevicePacketInfo : IDisposable
    {
        private uint _timeReceived;
        public uint TimeReceived { get { return _timeReceived; } internal set { _timeReceived = value; } }

        private string _packet;
        public string Packet { get { return _packet; } internal set { _packet = value; } }

        public SniffedDevicePacketInfo(uint timeReceived, string packet)
        {
            _timeReceived = timeReceived;
            _packet = packet;
        }

        public void Dispose()
        {
        }
    }

    public class SniffedDeviceInfo : IDisposable
    {
        private Family _family = null;
        public Family Family { get { return _family; } internal set { _family = value; } }

        private long _address;
        public long Address { get { return _address; } internal set { _address = value; } }

        private long _rssi;
        public long Rssi { get { return _rssi; } internal set { _rssi = value; } }

        private Dictionary<string, string> _additionalData = new Dictionary<string, string>();
        public Dictionary<string, string> AdditionalData { get { return _additionalData; } }

        private List<SniffedDevicePacketInfo> _packets = new List<SniffedDevicePacketInfo>();
        public List<SniffedDevicePacketInfo> Packets { get { return _packets; } }

        public SniffedDeviceInfo(Family family)
        {
            _family = family;
        }

        public void Dispose()
        {

        }
    }
}