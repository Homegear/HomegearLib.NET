using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HomegearLib.RPC;

namespace HomegearLib
{
    public class SystemVariable : RPCVariable, IDisposable
    {
        RPCController _rpc = null;

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
                if (_rpc != null) _rpc.SetSystemVariable(this);
            }
        }

        public override int IntegerValue
        {
            get { return _integerValue; }
            set
            {
                _integerValue = value;
                if (_rpc != null) _rpc.SetSystemVariable(this);
            }
        }

        public override bool BooleanValue
        {
            get { return _booleanValue; }
            set
            {
                _booleanValue = value;
                if (_rpc != null) _rpc.SetSystemVariable(this);
            }
        }

        public override double FloatValue
        {
            get { return _floatValue; }
            set
            {
                _floatValue = value;
                if (_rpc != null) _rpc.SetSystemVariable(this);
            }
        }

        public override List<RPCVariable> ArrayValue
        {
            get { return _arrayValue; }
            set
            {
                _arrayValue = value;
                if (_rpc != null) _rpc.SetSystemVariable(this);
            }
        }

        public override Dictionary<String, RPCVariable> StructValue
        {
            get { return _structValue; }
            set
            {
                _structValue = value;
                if(_rpc != null) _rpc.SetSystemVariable(this);
            }
        }

        public SystemVariable(String name, RPCVariable variable)
        {
            _name = name;
            Type = variable.Type;
            SetValue(variable);
        }

        internal SystemVariable(RPCController rpc, String name, RPCVariable variable)
        {
            _rpc = rpc;
            _name = name;
            Type = variable.Type;
            SetValue(variable);
        }

        public SystemVariable(String name, RPCVariableType type)
        {
            _name = name;
            _type = type;
        }

        public SystemVariable(String name, Int32 value)
        {
            _name = name;
            _type = RPCVariableType.rpcInteger;
            _integerValue = value;
        }

        public SystemVariable(String name, UInt32 value)
        {
            _name = name;
            _type = RPCVariableType.rpcInteger;
            _integerValue = (Int32)value;
        }

        public SystemVariable(String name, Byte value)
        {
            _name = name;
            _type = RPCVariableType.rpcInteger;
            _integerValue = (Int32)value;
        }

        public SystemVariable(String name, String value)
        {
            _name = name;
            _type = RPCVariableType.rpcString;
            _stringValue = value;
        }

        public SystemVariable(String name, bool value)
        {
            _name = name;
            _type = RPCVariableType.rpcBoolean;
            _booleanValue = value;
        }

        public SystemVariable(String name, double value)
        {
            _name = name;
            _type = RPCVariableType.rpcFloat;
            _floatValue = value;
        }

        public void SetValue(RPCVariable value)
        {
            _booleanValue = value.BooleanValue;
            _arrayValue = value.ArrayValue;
            _structValue = value.StructValue;
            _integerValue = value.IntegerValue;
            _stringValue = value.StringValue;
            _floatValue = value.FloatValue;
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
