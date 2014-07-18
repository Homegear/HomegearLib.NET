using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HomegearLib.RPC;

namespace HomegearLib
{
    public class ConfigParameter : Variable
    {
        protected Boolean _dataPending = false;
        public Boolean DataPending { get { return _dataPending; } internal set { _dataPending = value; } }

        /// <summary>
        /// Sets the boolean value of the configuration parameter. After setting all parameters of the parameter set, you need to call "put" to send the data to Homegear.
        /// </summary>
        public override Boolean BooleanValue
        {
            get
            {
                return _booleanValue;
            }
            set
            {
                if (_rpc == null) throw new HomegearVariableException("No RPC controller specified.");
                if (!_writeable) throw new HomegearVariableReadOnlyException("Config parameter is readonly");
                if (_type != VariableType.tBoolean) throw new HomegearVariableTypeException("Config parameter is not of type boolean.");
                _booleanValue = value;
                _dataPending = true;
            }
        }

        /// <summary>
        /// Sets the integer value of the configuration parameter. After setting all parameters of the parameter set, you need to call "put" to send the data to Homegear.
        /// </summary>
        public override Int32 IntegerValue
        {
            get
            {
                return _integerValue;
            }
            set
            {
                if (_rpc == null) throw new HomegearVariableException("No RPC controller specified.");
                if (!_writeable) throw new HomegearVariableReadOnlyException("Config parameter is readonly");
                if (_type != VariableType.tInteger || _type != VariableType.tEnum) throw new HomegearVariableTypeException("Config parameter is not of type integer or enum.");
                if ((_integerValue > _maxInteger || _integerValue < _minInteger) && !_specialIntegerValues.ContainsKey(value)) throw new HomegearVariableValueOutOfBoundsException("Value is out of bounds.");
                _integerValue = value;
                _dataPending = true;
            }
        }

        /// <summary>
        /// Sets the double value of the configuration parameter. After setting all parameters of the parameter set, you need to call "put" to send the data to Homegear.
        /// </summary>
        public override Double DoubleValue
        {
            get
            {
                return _doubleValue;
            }
            set
            {
                if (_rpc == null) throw new HomegearVariableException("No RPC controller specified.");
                if (!_writeable) throw new HomegearVariableReadOnlyException("Config parameter is readonly");
                if (_type != VariableType.tDouble) throw new HomegearVariableTypeException("Config parameter is not of type double.");
                if ((_doubleValue > _maxDouble || _doubleValue < _minDouble) && !_specialDoubleValues.ContainsKey(value)) throw new HomegearVariableValueOutOfBoundsException("Value is out of bounds.");
                _doubleValue = value;
                _dataPending = true;
            }
        }

        /// <summary>
        /// Sets the string value of the configuration parameter. After setting all parameters of the parameter set, you need to call "put" to send the data to Homegear.
        /// </summary>
        public override String StringValue
        {
            get
            {
                return _stringValue;
            }
            set
            {
                if (_rpc == null) throw new HomegearVariableException("No RPC controller specified.");
                if (!_writeable) throw new HomegearVariableReadOnlyException("Config parameter is readonly");
                if (_type != VariableType.tString) throw new HomegearVariableTypeException("Config parameter is not of type string.");
                _stringValue = value;
                _dataPending = true;
            }
        }

        public ConfigParameter(Int32 peerID, Int32 channel, String name) : this(null, peerID, channel, name)
        {

        }

        public ConfigParameter(RPCController rpc, Int32 peerID, Int32 channel, String name) : base(rpc, peerID, channel, name)
        {
            
        }

        internal ConfigParameter(Int32 peerID, Int32 channel, String name, RPCVariable rpcVariable) : this(null, peerID, channel, name, rpcVariable)
        {
            
        }

        internal ConfigParameter(RPCController rpc, Int32 peerID, Int32 channel, String name, RPCVariable rpcVariable) : base(rpc, peerID, channel, name)
        {
            
        }
    }
}
