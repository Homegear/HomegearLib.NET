using System;
using System.Collections.Generic;

namespace HomegearLib.RPC.Encoding
{
    internal class RPCDecoder
    {
        private BinaryDecoder _decoder = new BinaryDecoder();

        public List<RPCVariable> DecodeRequest(byte[] packet, ref string methodName)
        {
            uint position = 4;
            uint headerSize = 0;
            List<RPCVariable> parameters = new List<RPCVariable>();
            if (packet == null) return parameters;
            if (packet.Length < 4) return parameters;
            if (packet[3] == 0x40 || packet[3] == 0x41) headerSize = (uint)_decoder.DecodeInteger(packet, ref position) + 4;
            position = 8 + headerSize;
            methodName = _decoder.DecodeString(packet, ref position);
            int parameterCount = _decoder.DecodeInteger(packet, ref position);
            if (parameterCount > 100) return parameters;
            for (int i = 0; i < parameterCount; i++)
            {
                parameters.Add(DecodeParameter(packet, ref position));
            }
            return parameters;
        }

        public RPCVariable DecodeResponse(byte[] packet, uint offset = 0)
        {
            uint position = offset + 8;
            RPCVariable response = DecodeParameter(packet, ref position);
            if (packet.Length < 4) return response; //response is Void when packet is empty.
            if (packet[3] == 0xFF)
            {
                if (!response.StructValue.ContainsKey("faultCode")) response.StructValue.Add("faultCode", new RPCVariable(-1));
                if (!response.StructValue.ContainsKey("faultString")) response.StructValue.Add("faultString", new RPCVariable("undefined"));
            }
            return response;
        }

        public RPCHeader DecodeHeader(byte[] packet)
        {
            RPCHeader header = new RPCHeader();
            if (packet.Length < 12 || (packet[3] != 0x40 && packet[3] != 0x41)) return header;
            uint position = 4;
            int headerSize = 0;
            headerSize = _decoder.DecodeInteger(packet, ref position);
            if (headerSize < 4) return header;
            int parameterCount = _decoder.DecodeInteger(packet, ref position);
            for (int i = 0; i < parameterCount; i++)
            {
                string field = _decoder.DecodeString(packet, ref position).ToLower();
                string value = _decoder.DecodeString(packet, ref position);
                if (field == "authorization") header.Authorization = value;
            }
            return header;
        }

        private RPCVariableType DecodeType(byte[] packet, ref uint position)
        {
            return (RPCVariableType)_decoder.DecodeInteger(packet, ref position);
        }

        private RPCVariable DecodeParameter(byte[] packet, ref uint position)
        {
            RPCVariableType type = DecodeType(packet, ref position);
            RPCVariable variable = new RPCVariable(type);
            if (type == RPCVariableType.rpcString || type == RPCVariableType.rpcBase64)
            {
                variable.StringValue = _decoder.DecodeString(packet, ref position);
            }
            else if (type == RPCVariableType.rpcBinary)
            {
                _decoder.DecodeBinary(packet, ref position);
            }
            else if (type == RPCVariableType.rpcInteger)
            {
                variable.IntegerValue = _decoder.DecodeInteger(packet, ref position);
            }
            else if (type == RPCVariableType.rpcInteger64)
            {
                variable.IntegerValue64 = _decoder.DecodeInteger64(packet, ref position);
            }
            else if (type == RPCVariableType.rpcFloat)
            {
                variable.FloatValue = _decoder.DecodeFloat(packet, ref position);
            }
            else if (type == RPCVariableType.rpcBoolean)
            {
                variable.BooleanValue = _decoder.DecodeBoolean(packet, ref position);
            }
            else if (type == RPCVariableType.rpcArray)
            {
                variable.ArrayValue = DecodeArray(packet, ref position);
            }
            else if (type == RPCVariableType.rpcStruct)
            {
                variable.StructValue = DecodeStruct(packet, ref position);
            }
            return variable;
        }

        List<RPCVariable> DecodeArray(byte[] packet, ref uint position)
        {
            int arrayLength = _decoder.DecodeInteger(packet, ref position);
            List<RPCVariable> rpcArray = new List<RPCVariable>();
            for (int i = 0; i < arrayLength; i++)
            {
                rpcArray.Add(DecodeParameter(packet, ref position));
            }
            return rpcArray;
        }

        Dictionary<String, RPCVariable> DecodeStruct(byte[] packet, ref uint position)
        {
            int structLength = _decoder.DecodeInteger(packet, ref position);
            Dictionary<String, RPCVariable> rpcStruct = new Dictionary<string, RPCVariable>();
            for (int i = 0; i < structLength; i++)
            {
                string name = _decoder.DecodeString(packet, ref position);
                rpcStruct.Add(name, DecodeParameter(packet, ref position));
            }
            return rpcStruct;
        }
    }
}
