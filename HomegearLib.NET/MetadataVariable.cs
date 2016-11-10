using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HomegearLib.RPC;

namespace HomegearLib
{
    public class MetadataVariable : RPCVariable, IDisposable
    {
        RPCController _rpc = null;

        private Int32 _peerID;
        public Int32 PeerID { get { return _peerID; } }

        private String _name;
        public String Name { get { return _name; } }

        public override RPCVariableType Type
        {
            get { return _type; }
        }

        public override string StringValue
        {
            get { return _stringValue; }
            set
            {
                _stringValue = value;
                if (_rpc != null) _rpc.SetMetadata(this);
            }
        }

        public override Int32 IntegerValue
        {
            get { return _integerValue; }
            set
            {
                _integerValue = value;
                if (_rpc != null) _rpc.SetMetadata(this);
            }
        }

        public override Int64 IntegerValue64
        {
            get { return _integerValue64; }
            set
            {
                _integerValue64 = value;
                if (_rpc != null) _rpc.SetMetadata(this);
            }
        }

        public override bool BooleanValue
        {
            get { return _booleanValue; }
            set
            {
                _booleanValue = value;
                if (_rpc != null) _rpc.SetMetadata(this);
            }
        }

        public override double FloatValue
        {
            get { return _floatValue; }
            set
            {
                _floatValue = value;
                if (_rpc != null) _rpc.SetMetadata(this);
            }
        }

        public override List<RPCVariable> ArrayValue
        {
            get { return _arrayValue; }
            set
            {
                _arrayValue = value;
                if (_rpc != null) _rpc.SetMetadata(this);
            }
        }

        public override Dictionary<String, RPCVariable> StructValue
        {
            get { return _structValue; }
            set
            {
                _structValue = value;
                if(_rpc != null) _rpc.SetMetadata(this);
            }
        }

        public MetadataVariable(Int32 peerID, String name, RPCVariable variable)
        {
            _peerID = peerID;
            _name = name;
            Type = variable.Type;
            SetValue(variable);
        }

        internal MetadataVariable(RPCController rpc, Int32 peerID, String name, RPCVariable variable)
        {
            _rpc = rpc;
            _peerID = peerID;
            _name = name;
            Type = variable.Type;
            SetValue(variable);
        }

        public MetadataVariable(Int32 peerID, String name, RPCVariableType type)
        {
            _peerID = peerID;
            _name = name;
            _type = type;
        }

        public MetadataVariable(Int32 peerID, String name, Int32 value)
        {
            _peerID = peerID;
            _name = name;
            _type = RPCVariableType.rpcInteger;
            _integerValue = value;
        }

        public MetadataVariable(Int32 peerID, String name, UInt32 value)
        {
            _peerID = peerID;
            _name = name;
            _type = RPCVariableType.rpcInteger;
            _integerValue = (Int32)value;
        }

        public MetadataVariable(Int32 peerID, String name, Int64 value)
        {
            _peerID = peerID;
            _name = name;
            _type = RPCVariableType.rpcInteger64;
            _integerValue64 = value;
        }

        public MetadataVariable(Int32 peerID, String name, UInt64 value)
        {
            _peerID = peerID;
            _name = name;
            _type = RPCVariableType.rpcInteger64;
            _integerValue64 = (Int32)value;
        }

        public MetadataVariable(Int32 peerID, String name, Byte value)
        {
            _peerID = peerID;
            _name = name;
            _type = RPCVariableType.rpcInteger;
            _integerValue = (Int32)value;
        }

        public MetadataVariable(Int32 peerID, String name, String value)
        {
            _peerID = peerID;
            _name = name;
            _type = RPCVariableType.rpcString;
            _stringValue = value;
        }

        public MetadataVariable(Int32 peerID, String name, bool value)
        {
            _peerID = peerID;
            _name = name;
            _type = RPCVariableType.rpcBoolean;
            _booleanValue = value;
        }

        public MetadataVariable(Int32 peerID, String name, double value)
        {
            _peerID = peerID;
            _name = name;
            _type = RPCVariableType.rpcFloat;
            _floatValue = value;
        }

        public void Remove()
        {
            _rpc.DeleteMetadata(this);
        }

        public void Dispose()
        {
            _rpc = null;
        }
    }
}
