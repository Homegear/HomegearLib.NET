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
        public Family Family { get { return _family; } set { _family = value; } }

        private String _id = "";
        public String ID { get { return _id; } set { _id = value; } }

        private String _type = "";
        public String Type { get { return _type; } set { _type = value; } }

        private bool _connected = false;
        public bool Connected { get { return _connected; } set { _connected = value; } }

        private bool _default = false;
        public bool Default { get { return _default; } set { _default = value; } }

        private Int32 _physicalAddress = 0;
        public Int32 PhysicalAddress { get { return _physicalAddress; } set { _physicalAddress = value; } }

        private Int32 _lastPacketReceived = 0;
        public Int32 LastPacketReceived { get { return _lastPacketReceived; } set { _lastPacketReceived = value; } }

        private Int32 _lastPacketSent = 0;
        public Int32 LastPacketSent { get { return _lastPacketSent; } set { _lastPacketSent = value; } }

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
