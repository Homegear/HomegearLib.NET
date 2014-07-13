using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomegearLib
{
    internal enum RPCVariableType
    { 
        rpcVoid = 0,
        rpcInteger = 1,
        rpcBoolean = 2,
        rpcString = 3,
        rpcFloat = 4,
        rpcArray = 0x100,
        rpcStruct = 0x101,
        rpcDate = 0x10,
        rpcBase64 = 0x11,
        rpcVariant = 0x1111
    }

    internal class RPCVariable
    {
        public bool ErrorStruct
        {
            get { return _structValue.Count() > 0 && _structValue.ContainsKey("faultCode"); }
        }

        private RPCVariableType _type = RPCVariableType.rpcVoid;
        public RPCVariableType Type
        {
            get { return _type; }
            set { _type = value; }
        }

        private string _stringValue;
        public string StringValue
        {
            get { return _stringValue; }
            set { _stringValue = value; }
        }

        private int _integerValue;
        public int IntegerValue
        {
            get { return _integerValue; }
            set { _integerValue = value; }
        }

        private bool _booleanValue;
        public bool BooleanValue
        {
            get { return _booleanValue; }
            set { _booleanValue = value; }
        }

        private double _floatValue;
        public double FloatValue
        {
            get { return _floatValue; }
            set { _floatValue = value; }
        }

        private List<RPCVariable> _arrayValue = new List<RPCVariable>();
        public List<RPCVariable> ArrayValue
        {
            get { return _arrayValue; }
            set { _arrayValue = value; }
        }

        private Dictionary<String, RPCVariable> _structValue = new Dictionary<string,RPCVariable>();
        public Dictionary<String, RPCVariable> StructValue
        {
            get { return _structValue; }
            set { _structValue = value; }
        }

        public RPCVariable()
        {
        }

        public RPCVariable(RPCVariableType type)
        {
            _type = type;
        }

        public RPCVariable(Int32 value)
        {
            _type = RPCVariableType.rpcInteger;
            _integerValue = value;
        }

        public RPCVariable(UInt32 value)
        {
            _type = RPCVariableType.rpcInteger;
            _integerValue = (Int32)value;
        }

        public RPCVariable(Byte value)
        {
            _type = RPCVariableType.rpcInteger;
            _integerValue = (Int32)value;
        }

        public RPCVariable(String value)
        {
            _type = RPCVariableType.rpcString;
            _stringValue = value;
        }

        public RPCVariable(bool value)
        {
            _type = RPCVariableType.rpcBoolean;
            _booleanValue = value;
        }

        public RPCVariable(double value)
        {
            _type = RPCVariableType.rpcFloat;
            _floatValue = value;
        }

        public static RPCVariable CreateError(int faultCode, string faultString)
        {
            RPCVariable errorStruct = new RPCVariable(RPCVariableType.rpcStruct);
            errorStruct.StructValue.Add("faultCode", new RPCVariable(faultCode));
            errorStruct.StructValue.Add("faultString", new RPCVariable(faultString));
            return errorStruct;
        }
    }
}
