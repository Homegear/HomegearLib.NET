﻿using HomegearLib.RPC;

namespace HomegearLib
{
    public class ConfigParameter : Variable
    {
        protected bool _dataPending = false;
        public bool DataPending { get { return _dataPending; } internal set { _dataPending = value; } }

        /// <summary>
        /// Sets the boolean value of the configuration parameter. After setting all parameters of the parameter set, you need to call "put" to send the data to Homegear.
        /// </summary>
        public override bool BooleanValue
        {
            get
            {
                return _booleanValue;
            }
            set
            {
                if (_rpc == null)
                {
                    throw new HomegearVariableException("No RPC controller specified.");
                }

                if (!_writeable)
                {
                    throw new HomegearVariableReadOnlyException("Config parameter is readonly");
                }

                if (_type != VariableType.tBoolean)
                {
                    throw new HomegearVariableTypeException("Config parameter is not of type boolean.");
                }

                _booleanValue = value;
                _dataPending = true;
            }
        }

        /// <summary>
        /// Sets the integer value of the configuration parameter. After setting all parameters of the parameter set, you need to call "put" to send the data to Homegear.
        /// </summary>
        public override long IntegerValue
        {
            get
            {
                return _integerValue;
            }
            set
            {
                if (_rpc == null)
                {
                    throw new HomegearVariableException("No RPC controller specified.");
                }

                if (!_writeable)
                {
                    throw new HomegearVariableReadOnlyException("Config parameter is readonly");
                }

                if (_type != VariableType.tInteger && _type != VariableType.tEnum)
                {
                    throw new HomegearVariableTypeException("Config parameter is not of type integer or enum.");
                }

                if ((value > _maxInteger || value < _minInteger) && !_specialIntegerValues.ContainsKey(value))
                {
                    throw new HomegearVariableValueOutOfBoundsException("Value is out of bounds.");
                }

                _integerValue = value;
                _dataPending = true;
            }
        }

        /// <summary>
        /// Sets the double value of the configuration parameter. After setting all parameters of the parameter set, you need to call "put" to send the data to Homegear.
        /// </summary>
        public override double DoubleValue
        {
            get
            {
                return _doubleValue;
            }
            set
            {
                if (_rpc == null)
                {
                    throw new HomegearVariableException("No RPC controller specified.");
                }

                if (!_writeable)
                {
                    throw new HomegearVariableReadOnlyException("Config parameter is readonly");
                }

                if (_type != VariableType.tDouble)
                {
                    throw new HomegearVariableTypeException("Config parameter is not of type double.");
                }

                if ((value > _maxDouble || value < _minDouble) && !_specialDoubleValues.ContainsKey(value))
                {
                    throw new HomegearVariableValueOutOfBoundsException("Value is out of bounds.");
                }

                _doubleValue = value;
                _dataPending = true;
            }
        }

        /// <summary>
        /// Sets the string value of the configuration parameter. After setting all parameters of the parameter set, you need to call "put" to send the data to Homegear.
        /// </summary>
        public override string StringValue
        {
            get
            {
                return _stringValue;
            }
            set
            {
                if (_rpc == null)
                {
                    throw new HomegearVariableException("No RPC controller specified.");
                }

                if (!_writeable)
                {
                    throw new HomegearVariableReadOnlyException("Config parameter is readonly");
                }

                if (_type != VariableType.tString)
                {
                    throw new HomegearVariableTypeException("Config parameter is not of type string.");
                }

                if (value == null)
                {
                    _stringValue = "";
                }
                else
                {
                    _stringValue = value;
                }

                _dataPending = true;
            }
        }

        public ConfigParameter(long peerId, long channel, string name) : this(null, peerId, channel, name)
        {

        }

        public ConfigParameter(RPCController rpc, long peerId, long channel, string name) : base(rpc, peerId, channel, name)
        {

        }
    }
}
