using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomegearLib
{
    public class SniffedDevicePacketInfo : IDisposable
    {
        private UInt32 _timeReceived;
        public UInt32 TimeReceived { get { return _timeReceived; } internal set { _timeReceived = value; } }

        private String _packet;
        public String Packet { get { return _packet; } internal set { _packet = value; } }

        public SniffedDevicePacketInfo(UInt32 timeReceived, String packet)
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

        private Int32 _address;
        public Int32 Address { get { return _address; } internal set { _address = value; } }

        private Int32 _rssi;
        public Int32 Rssi { get { return _rssi; } internal set { _rssi = value; } }

        private Dictionary<String, String> _additionalData = new Dictionary<string,string>();
        public Dictionary<String, String> AdditionalData { get { return _additionalData; } }

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
