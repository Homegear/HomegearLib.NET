using System;
using System.Collections.Generic;
using System.Linq;

namespace HomegearLib.RPC
{
    public enum RPCVariableType
    {
        rpcVoid = 0,
        rpcInteger32 = 1,
        rpcBoolean = 2,
        rpcString = 3,
        rpcFloat = 4,
        rpcArray = 0x100,
        rpcStruct = 0x101,
        rpcDate = 0x10,
        rpcBase64 = 0x11,
        rpcBinary = 0xD0,
        rpcInteger = 0xD1,
        rpcVariant = 0x1111
    }

    public class RPCVariable
    {
        public bool ErrorStruct
        {
            get { return _structValue.Count() > 0 && _structValue.ContainsKey("faultCode"); }
        }

        protected RPCVariableType _type = RPCVariableType.rpcVoid;
        public virtual RPCVariableType Type
        {
            get { return _type; }
            set { _type = value; }
        }

        protected string _stringValue = "";
        public virtual string StringValue
        {
            get { return _stringValue; }
            set { _stringValue = (value == null) ? "" : value; }
        }

        protected long _integerValue;
        public virtual long IntegerValue
        {
            get { return _integerValue; }
            set { _integerValue = value; }
        }

        protected bool _booleanValue;
        public virtual bool BooleanValue
        {
            get { return _booleanValue; }
            set { _booleanValue = value; }
        }

        protected double _floatValue;
        public virtual double FloatValue
        {
            get { return _floatValue; }
            set { _floatValue = value; }
        }

        protected List<RPCVariable> _arrayValue = new List<RPCVariable>();
        public virtual List<RPCVariable> ArrayValue
        {
            get { return _arrayValue; }
            set { _arrayValue = value; }
        }

        protected Dictionary<string, RPCVariable> _structValue = new Dictionary<string, RPCVariable>();
        public virtual Dictionary<string, RPCVariable> StructValue
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

        public RPCVariable(int value)
        {
            _type = RPCVariableType.rpcInteger;
            _integerValue = value;
        }

        public RPCVariable(uint value)
        {
            _type = RPCVariableType.rpcInteger;
            _integerValue = value;
        }

        public RPCVariable(long value)
        {
            _type = RPCVariableType.rpcInteger;
            _integerValue = value;
        }

        public RPCVariable(ulong value)
        {
            _type = RPCVariableType.rpcInteger;
            _integerValue = (int)value;
        }

        public RPCVariable(byte value)
        {
            _type = RPCVariableType.rpcInteger;
            _integerValue = value;
        }

        public RPCVariable(string value)
        {
            _type = RPCVariableType.rpcString;
            if (value == null)
            {
                return;
            }

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

        public RPCVariable(IReadOnlyList<RPCVariable> value)
        {
            _type = RPCVariableType.rpcArray;
            _arrayValue = new List<RPCVariable>();
            foreach (RPCVariable element in value)
            {
                _arrayValue.Add(element);
            }
        }

        public RPCVariable(List<RPCVariable> value)
        {
            _type = RPCVariableType.rpcArray;
            _arrayValue = value;
        }

        public RPCVariable(Variable variable)
        {
            switch (variable.Type)
            {
                case VariableType.tBoolean:
                    _booleanValue = variable.BooleanValue;
                    _type = RPCVariableType.rpcBoolean;
                    break;
                case VariableType.tAction:
                    _booleanValue = variable.BooleanValue;
                    _type = RPCVariableType.rpcBoolean;
                    break;
                case VariableType.tInteger:
                    _integerValue = variable.IntegerValue;
                    _type = RPCVariableType.rpcInteger;
                    break;
                case VariableType.tDouble:
                    _floatValue = variable.DoubleValue;
                    _type = RPCVariableType.rpcFloat;
                    break;
                case VariableType.tString:
                    _stringValue = (variable.StringValue == null) ? "" : variable.StringValue;
                    _type = RPCVariableType.rpcString;
                    break;
                case VariableType.tEnum:
                    _integerValue = variable.IntegerValue;
                    _type = RPCVariableType.rpcInteger;
                    break;
            }
        }

        public static RPCVariable CreateError(int faultCode, string faultString)
        {
            RPCVariable errorStruct = new RPCVariable(RPCVariableType.rpcStruct);
            errorStruct.StructValue.Add("faultCode", new RPCVariable(faultCode));
            errorStruct.StructValue.Add("faultString", new RPCVariable(faultString));
            return errorStruct;
        }

        public static RPCVariable CreateFromTypeString(string type)
        {
            switch (type)
            {
                case "BOOL":
                    return new RPCVariable(RPCVariableType.rpcBoolean);
                case "STRING":
                    return new RPCVariable(RPCVariableType.rpcString);
                case "ACTION":
                    return new RPCVariable(RPCVariableType.rpcBoolean);
                case "INTEGER":
                    return new RPCVariable(RPCVariableType.rpcInteger);
                case "INTEGER64":
                    return new RPCVariable(RPCVariableType.rpcInteger);
                case "ENUM":
                    return new RPCVariable(RPCVariableType.rpcInteger);
                case "FLOAT":
                    return new RPCVariable(RPCVariableType.rpcFloat);
            }
            return new RPCVariable(RPCVariableType.rpcVoid);
        }

        public override string ToString()
        {
            switch (_type)
            {
                case RPCVariableType.rpcVoid:
                    return "Void";
                case RPCVariableType.rpcBoolean:
                    return _booleanValue.ToString();
                case RPCVariableType.rpcInteger32:
                    return _integerValue.ToString();
                case RPCVariableType.rpcInteger:
                    return _integerValue.ToString();
                case RPCVariableType.rpcString:
                    return _stringValue;
                case RPCVariableType.rpcFloat:
                    return _floatValue.ToString();
                case RPCVariableType.rpcArray:
                    return "Array";
                case RPCVariableType.rpcStruct:
                    return "Struct";
                case RPCVariableType.rpcDate:
                    return "Date";
                case RPCVariableType.rpcBase64:
                    return _stringValue;
            }
            return "";
        }

        public bool Compare(RPCVariable variable)
        {
            if (Type != variable.Type)
            {
                return false;
            }

            switch (_type)
            {
                case RPCVariableType.rpcBoolean:
                    if (_booleanValue != variable.BooleanValue)
                    {
                        return false;
                    }

                    break;
                case RPCVariableType.rpcArray:
                    if (_arrayValue.Count != variable.ArrayValue.Count)
                    {
                        return false;
                    }
                    else
                    {
                        for (int i = 0; i < _arrayValue.Count; i++)
                        {
                            if (!_arrayValue[i].Compare(variable.ArrayValue[i]))
                            {
                                return false;
                            }
                        }
                    }
                    break;
                case RPCVariableType.rpcStruct:
                    if (_structValue.Count != variable.StructValue.Count)
                    {
                        return false;
                    }
                    else
                    {
                        for (int i = 0; i < _structValue.Count; i++)
                        {
                            if (_structValue.Keys.ElementAt(i) != variable.StructValue.Keys.ElementAt(i))
                            {
                                return false;
                            }

                            if (!_structValue.Values.ElementAt(i).Compare(variable.StructValue.Values.ElementAt(i)))
                            {
                                return false;
                            }
                        }
                    }
                    break;
                case RPCVariableType.rpcInteger:
                    if (_integerValue != variable.IntegerValue)
                    {
                        return false;
                    }

                    break;
                case RPCVariableType.rpcString:
                    if (_stringValue != variable.StringValue)
                    {
                        return false;
                    }

                    break;
                case RPCVariableType.rpcBase64:
                    if (_stringValue != variable.StringValue)
                    {
                        return false;
                    }

                    break;
                case RPCVariableType.rpcFloat:
                    if (_floatValue != variable.FloatValue)
                    {
                        return false;
                    }

                    break;
            }
            return true;
        }

        public bool SetValue(RPCVariable value)
        {
            bool valueChanged = !Compare(value);
            if (!valueChanged)
            {
                return false;
            }

            switch (_type)
            {
                case RPCVariableType.rpcBoolean:
                    _booleanValue = value.BooleanValue;
                    break;
                case RPCVariableType.rpcArray:
                    _arrayValue = value.ArrayValue;
                    break;
                case RPCVariableType.rpcStruct:
                    _structValue = value.StructValue;
                    break;
                case RPCVariableType.rpcInteger32:
                    _integerValue = value.IntegerValue;
                    break;
                case RPCVariableType.rpcInteger:
                    _integerValue = value.IntegerValue;
                    break;
                case RPCVariableType.rpcString:
                    _stringValue = (value.StringValue == null) ? "" : value.StringValue;
                    break;
                case RPCVariableType.rpcBase64:
                    _stringValue = (value.StringValue == null) ? "" : value.StringValue;
                    break;
                case RPCVariableType.rpcFloat:
                    _floatValue = value.FloatValue;
                    break;
            }
            return true;
        }
    }
}
