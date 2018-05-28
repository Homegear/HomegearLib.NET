using HomegearLib.RPC;
using System;
using System.Collections.Generic;

namespace HomegearLib
{
    public class MetadataVariable : RPCVariable, IDisposable
    {
        RPCController _rpc = null;

        private long _peerId;
        public long PeerID { get { return _peerId; } }

        private string _name;
        public string Name { get { return _name; } }

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
                if (_rpc != null)
                {
                    _rpc.SetMetadata(this);
                }
            }
        }

        public override long IntegerValue
        {
            get { return _integerValue; }
            set
            {
                _integerValue = value;
                if (_rpc != null)
                {
                    _rpc.SetMetadata(this);
                }
            }
        }

        public override bool BooleanValue
        {
            get { return _booleanValue; }
            set
            {
                _booleanValue = value;
                if (_rpc != null)
                {
                    _rpc.SetMetadata(this);
                }
            }
        }

        public override double FloatValue
        {
            get { return _floatValue; }
            set
            {
                _floatValue = value;
                if (_rpc != null)
                {
                    _rpc.SetMetadata(this);
                }
            }
        }

        public override List<RPCVariable> ArrayValue
        {
            get { return _arrayValue; }
            set
            {
                _arrayValue = value;
                if (_rpc != null)
                {
                    _rpc.SetMetadata(this);
                }
            }
        }

        public override Dictionary<string, RPCVariable> StructValue
        {
            get { return _structValue; }
            set
            {
                _structValue = value;
                if (_rpc != null)
                {
                    _rpc.SetMetadata(this);
                }
            }
        }

        public MetadataVariable(long peerId, string name, RPCVariable variable)
        {
            _peerId = peerId;
            _name = name;
            Type = variable.Type;
            SetValue(variable);
        }

        internal MetadataVariable(RPCController rpc, long peerId, string name, RPCVariable variable)
        {
            _rpc = rpc;
            _peerId = peerId;
            _name = name;
            Type = variable.Type;
            SetValue(variable);
        }

        public MetadataVariable(long peerId, string name, RPCVariableType type)
        {
            _peerId = peerId;
            _name = name;
            _type = type;
        }

        public MetadataVariable(long peerId, string name, int value)
        {
            _peerId = peerId;
            _name = name;
            _type = RPCVariableType.rpcInteger;
            _integerValue = value;
        }

        public MetadataVariable(long peerId, string name, uint value)
        {
            _peerId = peerId;
            _name = name;
            _type = RPCVariableType.rpcInteger;
            _integerValue = value;
        }

        public MetadataVariable(long peerId, string name, long value)
        {
            _peerId = peerId;
            _name = name;
            _type = RPCVariableType.rpcInteger;
            _integerValue = value;
        }

        public MetadataVariable(long peerId, string name, ulong value)
        {
            _peerId = peerId;
            _name = name;
            _type = RPCVariableType.rpcInteger;
            _integerValue = (int)value;
        }

        public MetadataVariable(long peerId, string name, byte value)
        {
            _peerId = peerId;
            _name = name;
            _type = RPCVariableType.rpcInteger;
            _integerValue = value;
        }

        public MetadataVariable(long peerId, string name, string value)
        {
            _peerId = peerId;
            _name = name;
            _type = RPCVariableType.rpcString;
            _stringValue = value;
        }

        public MetadataVariable(long peerId, string name, bool value)
        {
            _peerId = peerId;
            _name = name;
            _type = RPCVariableType.rpcBoolean;
            _booleanValue = value;
        }

        public MetadataVariable(long peerId, string name, double value)
        {
            _peerId = peerId;
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
