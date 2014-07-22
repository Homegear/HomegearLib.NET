using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HomegearLib.RPC;

namespace HomegearLib
{
    public enum DeviceRXMode
    {
        None = 0,
        Always = 1,
        Burst = 2,
        Config = 4,
        WakeUp = 8,
        LazyConfig = 16
    }

    public class Device : IDisposable
    {
        RPCController _rpc = null;
        bool _descriptionRequested = false;
        bool _infoRequested = false;

        private Family _family = null;
        public Family Family { get { return _family; } internal set { _family = value; } }

        private Int32 _id = -1;
        public Int32 ID { get { return _id; } internal set { _id = value; } }

        private Int32 _address = -1;
        public Int32 Address
        { 
            get
            {
                if (!_descriptionRequested)
                {
                    _rpc.GetDeviceDescription(this);
                    _descriptionRequested = true;
                }
                return _address;
            } 
            internal set { _address = value; }
        }

        private String _serialNumber = "";
        public String SerialNumber { get { return _serialNumber; } internal set { _serialNumber = value; } }

        private String _typeString = "";
        public String TypeString { get { return _typeString; } internal set { _typeString = value; } }

        private Channels _channels;
        public Channels Channels { get { return _channels; } internal set { _channels = value; } }

        private String _name;
        public String Name
        {
            get
            {
                if (!_infoRequested)
                {
                    _rpc.GetDeviceInfo(this);
                    _infoRequested = true;
                }
                return _name;
            }
            set
            {
                _name = value;
                _rpc.SetName(_id, _name);
            }
        }

        /// <summary>
        /// Sets the name without calling any RPC functions
        /// </summary>
        /// <param name="name">The name of the device</param>
        internal void SetNameNoRPC(String name)
        {
            _name = name;
        }

        private Interface _interface;
        public Interface Interface
        {
            get
            {
                if (!_infoRequested)
                {
                    _rpc.GetDeviceInfo(this);
                    _infoRequested = true;
                }
                return _interface;
            }
            set
            {
                _interface = value;
                _rpc.SetInterface(_id, _interface);
            }
        }

        /// <summary>
        /// Sets the physical interface without calling any RPC functions
        /// </summary>
        /// <param name="physicalInterface">The interface object</param>
        internal void SetInterfaceNoRPC(Interface physicalInterface)
        {
            _interface = physicalInterface;
        }

        private DeviceRXMode _rxMode = DeviceRXMode.None;
        public DeviceRXMode RXMode
        {
            get
            {
                if (!_descriptionRequested)
                {
                    _rpc.GetDeviceDescription(this);
                    _descriptionRequested = true;
                }
                return _rxMode;
            }
            internal set { _rxMode = value; }
        }

        private bool _aesActive = false;
        public bool AESActive
        {
            get
            {
                if (!_descriptionRequested)
                {
                    _rpc.GetDeviceDescription(this);
                    _descriptionRequested = true;
                }
                return _aesActive;
            }
            internal set { _aesActive = value; }
        }

        private String _firmware;
        public String Firmware
        {
            get
            {
                if (!_descriptionRequested)
                {
                    _rpc.GetDeviceDescription(this);
                    _descriptionRequested = true;
                }
                return _firmware;
            }
            internal set { _firmware = value; }
        }

        private String _availableFirmware;
        public String AvailableFirmware
        {
            get
            {
                if (!_descriptionRequested)
                {
                    _rpc.GetDeviceDescription(this);
                    _descriptionRequested = true;
                }
                return _availableFirmware;
            }
            internal set { _availableFirmware = value; }
        }

        public Device(RPCController rpc, Family family, Int32 id)
        {
            _rpc = rpc;
            _family = family;
            _id = id;
        }

        public void Dispose()
        {
            _family = null;
            if(_channels != null) _channels.Dispose();
        }
    }
}
