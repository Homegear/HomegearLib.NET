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
        tDouble,
        tString,
        tEnum
    }

    public class Variable : IDisposable
    {
        private RPCController _rpc = null;

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

        private Boolean _readable = true;
        public Boolean Readable { get { return _readable; } internal set { _readable = value; } }

        private Boolean _writeable = true;
        public Boolean Writeable { get { return _writeable; } internal set { _writeable = value; } }

        private Boolean _booleanValue = false;
        public Boolean BooleanValue 
        {
            get
            {
                return _booleanValue;
            } 
            set 
            {
                if (_rpc == null) throw new HomegearVariableException("No RPC controller specified.");
                if (!_writeable) throw new HomegearVariableReadOnlyException("Variable is readonly");
                if (_type != VariableType.tBoolean) throw new HomegearVariableTypeException("Variable is not of type boolean.");
                _booleanValue = value;
                _rpc.SetValue(this);
            } 
        }

        private Int32 _integerValue = 0;
        public Int32 IntegerValue 
        { 
            get
            {
                return _integerValue;
            } 
            set 
            {
                if (_rpc == null) throw new HomegearVariableException("No RPC controller specified.");
                if (!_writeable) throw new HomegearVariableReadOnlyException("Variable is readonly");
                if (_type != VariableType.tInteger || _type != VariableType.tEnum) throw new HomegearVariableTypeException("Variable is not of type integer or enum.");
                _integerValue = value;
                if (_integerValue > _maxInteger || _integerValue < _minInteger) throw new HomegearVariableValueOutOfBoundsException("Value is out of bounds.");
                _rpc.SetValue(this);
            } 
        }

        private Double _doubleValue = 0;
        public Double DoubleValue 
        { 
            get
            {
                return _doubleValue;
            } 
            set 
            {
                if (_rpc == null) throw new HomegearVariableException("No RPC controller specified.");
                if (!_writeable) throw new HomegearVariableReadOnlyException("Variable is readonly");
                if (_type != VariableType.tDouble) throw new HomegearVariableTypeException("Variable is not of type double.");
                _doubleValue = value;
                if (_doubleValue > _maxDouble || _doubleValue < _minDouble) throw new HomegearVariableValueOutOfBoundsException("Value is out of bounds.");
                _rpc.SetValue(this);
            } 
        }

        private String _stringValue = "";
        public String StringValue 
        { 
            get
            {
                return _stringValue;
            } 
            set 
            {
                if (_rpc == null) throw new HomegearVariableException("No RPC controller specified.");
                if (!_writeable) throw new HomegearVariableReadOnlyException("Variable is readonly");
                if (_type != VariableType.tString) throw new HomegearVariableTypeException("Variable is not of type string.");
                _stringValue = value;
                _rpc.SetValue(this);
            } 
        }

        private String[] _valueList = new String[0];
        public String[] ValueList { get { return _valueList; } internal set {_valueList = value; } }

        public Variable(Int32 peerID, Int32 channel, String name) : this(null, peerID, channel, name)
        {
        }

        public Variable(RPCController rpc, Int32 peerID, Int32 channel, String name)
        {
            _rpc = rpc;
            _peerID = peerID;
            _channel = channel;
            _name = name;
        }

        internal Variable(Int32 peerID, Int32 channel, String name, RPCVariable rpcVariable) : this(null, peerID, channel, name, rpcVariable)
        {
            
        }

        internal Variable(RPCController rpc, Int32 peerID, Int32 channel, String name, RPCVariable rpcVariable) : this(rpc, peerID, channel, name)
        {
            switch(rpcVariable.Type)
            {
                case RPCVariableType.rpcBoolean:
                    _booleanValue = rpcVariable.BooleanValue;
                    _type = VariableType.tBoolean;
                    break;
                case RPCVariableType.rpcInteger:
                    _integerValue = rpcVariable.IntegerValue;
                    if(_type != VariableType.tEnum) _type = VariableType.tInteger;
                    break;
                case RPCVariableType.rpcFloat:
                    _doubleValue = rpcVariable.FloatValue;
                    _type = VariableType.tDouble;
                    break;
                case RPCVariableType.rpcString:
                    _stringValue = rpcVariable.StringValue;
                    _type = VariableType.tString;
                    break;
            }
        }

        public void Dispose()
        {
            _rpc = null;
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
                    _booleanValue = variable.BooleanValue;
                    _type = VariableType.tBoolean;
                    break;
                case VariableType.tInteger:
                    _integerValue = variable.IntegerValue;
                    if (_type != VariableType.tEnum) _type = VariableType.tInteger;
                    break;
                case VariableType.tDouble:
                    _doubleValue = variable.DoubleValue;
                    _type = VariableType.tDouble;
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
                    return _booleanValue.ToString();
                case VariableType.tInteger:
                    return _integerValue.ToString();
                case VariableType.tDouble:
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
