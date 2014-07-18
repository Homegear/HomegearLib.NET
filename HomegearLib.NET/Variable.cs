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

    public enum VariableUIFlags
    {
        fNone = 0,
        fVisible = 1,
        fInternal = 2,
        fTransform = 4,
        fService = 8,
        fSticky = 16
    }

    public class Variable : IDisposable
    {
        protected RPCController _rpc = null;

        protected VariableType _type = VariableType.tInteger;
        public VariableType Type { get { return _type; } }

        protected Int32 _peerID = 0;
        public Int32 PeerID { get { return _peerID; } internal set { _peerID = value; } }

        protected Int32 _channel = -1;
        public Int32 Channel { get { return _channel; } internal set { _channel = value; } }

        protected String _name = "";
        public String Name { get { return _name; } internal set { _name = value; } }

        protected String _unit = "";
        public String Unit { get { return _unit; } internal set { _unit = value; } }

        protected Boolean _defaultBoolean = false;
        public Boolean DefaultBoolean { get { return _defaultBoolean; } internal set { _defaultBoolean = value; } }

        protected Int64 _defaultInteger = 0;
        public Int64 DefaultInteger { get { return _defaultInteger; } internal set { _defaultInteger = value; } }

        protected Double _defaultDouble = 0;
        public Double DefaultDouble { get { return _defaultDouble; } internal set { _defaultDouble = value; } }

        protected String _defaultString = "";
        public String DefaultString { get { return _defaultString; } internal set { _defaultString = value; } }

        protected Int64 _minInteger = 0;
        public Int64 MinInteger { get { return _minInteger; } internal set { _minInteger = value; } }

        protected Int64 _maxInteger = 0;
        public Int64 MaxInteger { get { return _maxInteger; } internal set { _maxInteger = value; } }

        protected Double _minDouble = 0;
        public Double MinDouble { get { return _minDouble; } internal set { _minDouble = value; } }

        protected Double _maxDouble = 0;
        public Double MaxDouble { get { return _maxDouble; } internal set { _maxDouble = value; } }

        protected VariableUIFlags _uiFlags = VariableUIFlags.fNone;
        public VariableUIFlags UIFlags { get { return _uiFlags; } internal set { _uiFlags = value; } }

        protected Boolean _readable = true;
        public Boolean Readable { get { return _readable; } internal set { _readable = value; } }

        protected Boolean _writeable = true;
        public Boolean Writeable { get { return _writeable; } internal set { _writeable = value; } }

        protected ReadOnlyDictionary<Int32, String> _specialIntegerValues = new ReadOnlyDictionary<int,string>();
        public ReadOnlyDictionary<Int32, String> SpecialIntegerValues { get { return _specialIntegerValues; } }

        protected ReadOnlyDictionary<Double, String> _specialDoubleValues = new ReadOnlyDictionary<double,string>();
        public ReadOnlyDictionary<Double, String> SpecialDoubleValues { get { return _specialDoubleValues; } }

        protected Boolean _booleanValue = false;
        public virtual Boolean BooleanValue 
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

        protected Int32 _integerValue = 0;
        public virtual Int32 IntegerValue 
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
                if ((_integerValue > _maxInteger || _integerValue < _minInteger) && !_specialIntegerValues.ContainsKey(value)) throw new HomegearVariableValueOutOfBoundsException("Value is out of bounds.");
                _integerValue = value;
                _rpc.SetValue(this);
            } 
        }

        protected Double _doubleValue = 0;
        public virtual Double DoubleValue 
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
                if ((_doubleValue > _maxDouble || _doubleValue < _minDouble) && !_specialDoubleValues.ContainsKey(value)) throw new HomegearVariableValueOutOfBoundsException("Value is out of bounds.");
                _doubleValue = value;
                _rpc.SetValue(this);
            } 
        }

        protected String _stringValue = "";
        public virtual String StringValue 
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

        protected String[] _valueList = new String[0];
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
            SetValue(rpcVariable);
        }

        public void Dispose()
        {
            _rpc = null;
        }

        internal void SetDefault(RPCVariable value)
        {
            if (value.Type == RPCVariableType.rpcBoolean) _defaultBoolean = value.BooleanValue;
            else if (value.Type == RPCVariableType.rpcInteger) _defaultInteger = value.IntegerValue;
            else if (value.Type == RPCVariableType.rpcFloat) _defaultDouble = value.FloatValue;
            else if (value.Type == RPCVariableType.rpcString) _defaultString = value.StringValue;
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

        internal void SetValue(RPCVariable rpcVariable)
        {
            switch (rpcVariable.Type)
            {
                case RPCVariableType.rpcBoolean:
                    _booleanValue = rpcVariable.BooleanValue;
                    _type = VariableType.tBoolean;
                    break;
                case RPCVariableType.rpcInteger:
                    _integerValue = rpcVariable.IntegerValue;
                    if (_type != VariableType.tEnum) _type = VariableType.tInteger;
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

        internal void SetSpecialValues(RPCVariable specialValues)
        {
            Dictionary<Int32, String> specialIntegerValues = new Dictionary<int,string>();
            Dictionary<Double, String> specialDoubleValues = new Dictionary<double,string>();
            foreach (RPCVariable specialValue in specialValues.ArrayValue)
            {
                if (!specialValue.StructValue.ContainsKey("ID") || !specialValue.StructValue.ContainsKey("VALUE")) continue;
                RPCVariable value = specialValue.StructValue["VALUE"];
                if (value.Type == RPCVariableType.rpcInteger) specialIntegerValues.Add(value.IntegerValue, specialValue.StructValue["ID"].StringValue);
                else if (value.Type == RPCVariableType.rpcFloat) specialDoubleValues.Add(value.FloatValue, specialValue.StructValue["ID"].StringValue);
            }
            if (specialIntegerValues.Count > 0) _specialIntegerValues = new ReadOnlyDictionary<int, string>(specialIntegerValues);
            if (specialDoubleValues.Count > 0) _specialDoubleValues = new ReadOnlyDictionary<double, string>(specialDoubleValues);
        }

        public String DefaultToString()
        {
            switch (_type)
            {
                case VariableType.tBoolean:
                    return _defaultBoolean.ToString();
                case VariableType.tInteger:
                    return _defaultInteger.ToString();
                case VariableType.tDouble:
                    return _defaultDouble.ToString();
                case VariableType.tString:
                    return _defaultString;
                case VariableType.tEnum:
                    return _defaultInteger.ToString();
            }
            return "";
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
