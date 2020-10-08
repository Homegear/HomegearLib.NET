using System;
using System.Collections.Generic;

namespace HomegearLib.RPC.Encoding
{
    public class RPCDecoder
    {
        public static List<RPCVariable> DecodeRequest(byte[] packet, ref string methodName)
        {
            uint position = 4;
            uint headerSize = 0;
            List<RPCVariable> parameters = new List<RPCVariable>();
            if (packet == null)
            {
                return parameters;
            }

            if (packet.Length < 4)
            {
                return parameters;
            }

            if (packet[3] == 0x40 || packet[3] == 0x41)
            {
                headerSize = (uint)BinaryDecoder.DecodeInteger32(packet, ref position) + 4;
            }

            position = 8 + headerSize;
            methodName = BinaryDecoder.DecodeString(packet, ref position);
            int parameterCount = BinaryDecoder.DecodeInteger32(packet, ref position);
            if (parameterCount > 100)
            {
                return parameters;
            }

            for (int i = 0; i < parameterCount; i++)
            {
                parameters.Add(DecodeParameter(packet, ref position));
            }
            return parameters;
        }

        public static RPCVariable DecodeResponse(byte[] packet, uint offset = 0)
        {
            uint position = offset + 8;
            RPCVariable response = DecodeParameter(packet, ref position);
            if (packet.Length < 4)
            {
                return response; //response is Void when packet is empty.
            }

            if (packet[3] == 0xFF)
            {
                if (!response.StructValue.ContainsKey("faultCode"))
                {
                    response.StructValue.Add("faultCode", new RPCVariable(-1));
                }

                if (!response.StructValue.ContainsKey("faultString"))
                {
                    response.StructValue.Add("faultString", new RPCVariable("undefined"));
                }
            }
            return response;
        }

        public static RPCHeader DecodeHeader(byte[] packet)
        {
            RPCHeader header = new RPCHeader();
            if (packet.Length < 12 || (packet[3] != 0x40 && packet[3] != 0x41))
            {
                return header;
            }

            uint position = 4;
            int headerSize = BinaryDecoder.DecodeInteger32(packet, ref position);
            if (headerSize < 4)
            {
                return header;
            }

            int parameterCount = BinaryDecoder.DecodeInteger32(packet, ref position);
            for (int i = 0; i < parameterCount; i++)
            {
                string field = BinaryDecoder.DecodeString(packet, ref position).ToLower();
                string value = BinaryDecoder.DecodeString(packet, ref position);
                if (field == "authorization")
                {
                    header.Authorization = value;
                }
            }
            return header;
        }

        private static RPCVariableType DecodeType(byte[] packet, ref uint position)
        {
            return (RPCVariableType)BinaryDecoder.DecodeInteger32(packet, ref position);
        }

        private static RPCVariable DecodeParameter(byte[] packet, ref uint position)
        {
            RPCVariableType type = DecodeType(packet, ref position);
            RPCVariable variable = new RPCVariable(type);
            if (type == RPCVariableType.rpcString || type == RPCVariableType.rpcBase64)
            {
                variable.StringValue = BinaryDecoder.DecodeString(packet, ref position);
            }
            else if (type == RPCVariableType.rpcBinary)
            {
                variable.BinaryValue = BinaryDecoder.DecodeBinary(packet, ref position);
            }
            else if (type == RPCVariableType.rpcBinary)
            {
                BinaryDecoder.DecodeBinary(packet, ref position);
            }
            else if (type == RPCVariableType.rpcInteger32)
            {
                variable.IntegerValue = BinaryDecoder.DecodeInteger32(packet, ref position);
                variable.Type = RPCVariableType.rpcInteger;
            }
            else if (type == RPCVariableType.rpcInteger)
            {
                variable.IntegerValue = BinaryDecoder.DecodeInteger64(packet, ref position);
            }
            else if (type == RPCVariableType.rpcFloat)
            {
                variable.FloatValue = BinaryDecoder.DecodeFloat(packet, ref position);
            }
            else if (type == RPCVariableType.rpcBoolean)
            {
                variable.BooleanValue = BinaryDecoder.DecodeBoolean(packet, ref position);
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

        private static List<RPCVariable> DecodeArray(byte[] packet, ref uint position)
        {
            int arrayLength = BinaryDecoder.DecodeInteger32(packet, ref position);
            List<RPCVariable> rpcArray = new List<RPCVariable>();
            for (int i = 0; i < arrayLength; i++)
            {
                rpcArray.Add(DecodeParameter(packet, ref position));
            }
            return rpcArray;
        }

        private static Dictionary<string, RPCVariable> DecodeStruct(byte[] packet, ref uint position)
        {
            int structLength = BinaryDecoder.DecodeInteger32(packet, ref position);
            Dictionary<string, RPCVariable> rpcStruct = new Dictionary<string, RPCVariable>();
            for (int i = 0; i < structLength; i++)
            {
                string name = BinaryDecoder.DecodeString(packet, ref position);
                rpcStruct.Add(name, DecodeParameter(packet, ref position));
            }
            return rpcStruct;
        }
    }
}
