using HomegearLib.RPC;
using System;
using System.Collections.Generic;

namespace HomegearLib
{
    public class SystemVariable : RPCVariable, IDisposable
    {
        RPCController _rpc = null;

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
                    _rpc.SetSystemVariable(this);
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
                    _rpc.SetSystemVariable(this);
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
                    _rpc.SetSystemVariable(this);
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
                    _rpc.SetSystemVariable(this);
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
                    _rpc.SetSystemVariable(this);
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
                    _rpc.SetSystemVariable(this);
                }
            }
        }

        public SystemVariable(string name, RPCVariable variable)
        {
            _name = name;
            Type = variable.Type;
            SetValue(variable);
        }

        internal SystemVariable(RPCController rpc, string name, RPCVariable variable)
        {
            _rpc = rpc;
            _name = name;
            Type = variable.Type;
            SetValue(variable);
        }

        public SystemVariable(string name, RPCVariableType type)
        {
            _name = name;
            _type = type;
        }

        public SystemVariable(string name, int value)
        {
            _name = name;
            _type = RPCVariableType.rpcInteger;
            _integerValue = value;
        }

        public SystemVariable(string name, uint value)
        {
            _name = name;
            _type = RPCVariableType.rpcInteger;
            _integerValue = (int)value;
        }

        public SystemVariable(string name, long value)
        {
            _name = name;
            _type = RPCVariableType.rpcInteger;
            _integerValue = value;
        }

        public SystemVariable(string name, ulong value)
        {
            _name = name;
            _type = RPCVariableType.rpcInteger;
            _integerValue = (int)value;
        }

        public SystemVariable(string name, byte value)
        {
            _name = name;
            _type = RPCVariableType.rpcInteger;
            _integerValue = value;
        }

        public SystemVariable(string name, string value)
        {
            _name = name;
            _type = RPCVariableType.rpcString;
            _stringValue = value;
        }

        public SystemVariable(string name, bool value)
        {
            _name = name;
            _type = RPCVariableType.rpcBoolean;
            _booleanValue = value;
        }

        public SystemVariable(string name, double value)
        {
            _name = name;
            _type = RPCVariableType.rpcFloat;
            _floatValue = value;
        }

        public void Remove()
        {
            _rpc.DeleteSystemVariable(this);
        }

        public void Dispose()
        {
            _rpc = null;
        }
    }
}
