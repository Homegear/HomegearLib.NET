using System;

namespace HomegearLib
{
    public class Interface : IDisposable
    {
        private Family _family = null;
        public Family Family { get { return _family; } internal set { _family = value; } }

        private string _id = "";
        public string ID { get { return _id; } internal set { _id = value; } }

        private string _type = "";
        public string Type { get { return _type; } internal set { _type = value; } }

        private bool _connected = false;
        public bool Connected { get { return _connected; } internal set { _connected = value; } }

        private bool _default = false;
        public bool Default { get { return _default; } internal set { _default = value; } }

        private long _physicalAddress = 0;
        public long PhysicalAddress { get { return _physicalAddress; } internal set { _physicalAddress = value; } }

        private string _ipAddress = "";
        public string IpAddress { get { return _ipAddress; } internal set { _ipAddress = value; } }

        private string _hostname = "";
        public string Hostname { get { return _hostname; } internal set { _hostname = value; } }

        private long _lastPacketReceived = 0;
        public long LastPacketReceived { get { return _lastPacketReceived; } internal set { _lastPacketReceived = value; } }

        private long _lastPacketSent = 0;
        public long LastPacketSent { get { return _lastPacketSent; } internal set { _lastPacketSent = value; } }

        public Interface(Family family, string id, string type)
        {
            _family = family;
            _id = id;
            _type = type;
        }

        public void Dispose()
        {

        }
    }
}
