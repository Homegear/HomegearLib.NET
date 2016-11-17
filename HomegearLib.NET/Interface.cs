using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomegearLib
{
    public class Interface : IDisposable
    {
        private Family _family = null;
        public Family Family { get { return _family; } internal set { _family = value; } }

        private String _id = "";
        public String ID { get { return _id; } internal set { _id = value; } }

        private String _type = "";
        public String Type { get { return _type; } internal set { _type = value; } }

        private bool _connected = false;
        public bool Connected { get { return _connected; } internal set { _connected = value; } }

        private bool _default = false;
        public bool Default { get { return _default; } internal set { _default = value; } }

        private Int32 _physicalAddress = 0;
        public Int32 PhysicalAddress { get { return _physicalAddress; } internal set { _physicalAddress = value; } }

        private String _ipAddress = "";
        public String IpAddress { get { return _ipAddress; } internal set { _ipAddress = value; } }

        private String _hostname = "";
        public String Hostname { get { return _hostname; } internal set { _hostname = value; } }

        private Int32 _lastPacketReceived = 0;
        public Int32 LastPacketReceived { get { return _lastPacketReceived; } internal set { _lastPacketReceived = value; } }

        private Int32 _lastPacketSent = 0;
        public Int32 LastPacketSent { get { return _lastPacketSent; } internal set { _lastPacketSent = value; } }

        public Interface(Family family, String id, String type)
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
