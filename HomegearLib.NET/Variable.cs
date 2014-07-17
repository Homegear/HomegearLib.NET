using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HomegearLib.RPC;

namespace HomegearLib
{
    public enum VariableType
    { 
        tBoolean,
        tInteger,
        tFloat,
        tString,
        tEnum
    }

    public class Variable : IDisposable
    {
        private VariableType _type = VariableType.tInteger;
        public VariableType Type { get { return _type; } }

        private Int32 _peerID = 0;
        public Int32 PeerID { get { return _peerID; } internal set { _peerID = value; } }

        private Int32 _channel = -1;
        public Int32 Channel { get { return _channel; } internal set { _channel = value; } }

        private String _name = "";
        public String Name { get { return _name; } internal set { _name = value; } }

        private Int64 _minInteger = 0;
        public Int64 MinInteger { get { return _minInteger; } internal set { _minInteger = value; } }

        private Int64 _maxInteger = 0;
        public Int64 MaxInteger { get { return _maxInteger; } internal set { _maxInteger = value; } }

        private Double _minDouble = 0;
        public Double MinDouble { get { return _minDouble; } internal set { _minDouble = value; } }

        private Double _maxDouble = 0;
        public Double MaxDouble { get { return _maxDouble; } internal set { _maxDouble = value; } }

        private Boolean _writeable = false;
        public Boolean Writeable { get { return _writeable; } internal set { _writeable = value; } }

        private Boolean _boolValue = false;
        public Boolean BoolValue { get { return _boolValue; } set { _boolValue = value; } }

        private Int32 _integerValue = 0;
        public Int32 IntegerValue { get { return _integerValue; } set { _integerValue = value; } }

        private Double _doubleValue = 0;
        public Double DoubleValue { get { return _doubleValue; } set { _doubleValue = value; } }

        private String _stringValue = "";
        public String StringValue { get { return _stringValue; } set { _stringValue = value; } }

        private String[] _valueList = new String[0];
        public String[] ValueList { get { return _valueList; } internal set {_valueList = value; } }

        public Variable(Int32 peerID, Int32 channel, String name)
        {
            _peerID = peerID;
            _channel = channel;
            _name = name;
        }

        internal Variable(Int32 peerID, Int32 channel, String name, RPCVariable rpcVariable) : this(peerID, channel, name)
        {
            switch(rpcVariable.Type)
            {
                case RPCVariableType.rpcBoolean:
                    _boolValue = rpcVariable.BooleanValue;
                    _type = VariableType.tBoolean;
                    break;
                case RPCVariableType.rpcInteger:
                    _integerValue = rpcVariable.IntegerValue;
                    if(_type != VariableType.tEnum) _type = VariableType.tInteger;
                    break;
                case RPCVariableType.rpcFloat:
                    _doubleValue = rpcVariable.FloatValue;
                    _type = VariableType.tFloat;
                    break;
                case RPCVariableType.rpcString:
                    _stringValue = rpcVariable.StringValue;
                    _type = VariableType.tString;
                    break;
            }
        }

        public void Dispose()
        {

        }

        internal void SetMin(RPCVariable min)
        {
            if (min.Type == RPCVariableType.rpcInteger) _minInteger = min.IntegerValue;
            else if (min.Type == RPCVariableType.rpcFloat) _minDouble = min.FloatValue;
        }

        internal void SetMax(RPCVariable max)
        {
            if (max.Type == RPCVariableType.rpcInteger) _maxInteger = max.IntegerValue;
            else if (max.Type == RPCVariableType.rpcFloat) _maxDouble = max.FloatValue;
        }

        internal void SetValue(Variable variable)
        {
            switch (variable.Type)
            {
                case VariableType.tBoolean:
                    _boolValue = variable.BoolValue;
                    _type = VariableType.tBoolean;
                    break;
                case VariableType.tInteger:
                    _integerValue = variable.IntegerValue;
                    if (_type != VariableType.tEnum) _type = VariableType.tInteger;
                    break;
                case VariableType.tFloat:
                    _doubleValue = variable.DoubleValue;
                    _type = VariableType.tFloat;
                    break;
                case VariableType.tString:
                    _stringValue = variable.StringValue;
                    _type = VariableType.tString;
                    break;
                case VariableType.tEnum:
                    _integerValue = variable.IntegerValue;
                    break;
            }
        }

        internal void SetValueList(RPCVariable valueList)
        {
            if (_type == VariableType.tInteger) _type = VariableType.tEnum;
            _valueList = new String[valueList.ArrayValue.Count];
            for(int i = 0; i < valueList.ArrayValue.Count; i++)
            {
                if (valueList.ArrayValue[i].Type != RPCVariableType.rpcString) continue;
                _valueList[i] = valueList.ArrayValue[i].StringValue;
            }
        }

        public override String ToString()
        {
            switch (_type)
            {
                case VariableType.tBoolean:
                    return _boolValue.ToString();
                case VariableType.tInteger:
                    return _integerValue.ToString();
                case VariableType.tFloat:
                    return _doubleValue.ToString();
                case VariableType.tString:
                    return _stringValue;
                case VariableType.tEnum:
                    return _integerValue.ToString();
            }
            return "";
        }
    }
}
