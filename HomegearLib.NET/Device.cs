using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomegearLib
{
    public class Device
    {
        private Family _family = null;
        public Family Family { get { return _family; } set { _family = value; } }

        private Int32 _id = -1;
        public Int32 ID { get { return _id; } set { _id = value; } }

        private Int32 _address = -1;
        public Int32 Address { get { return _address; } set { _address = value; } }

        private String _typeString = "";
        public String TypeString { get { return _typeString; } set { _typeString = value; } }

        private Channels _channels;
        public Channels Channels { get { return _channels; } internal set { _channels = value; } }
    }
}
