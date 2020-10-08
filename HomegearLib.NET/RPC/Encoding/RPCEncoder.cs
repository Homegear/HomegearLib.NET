using System;
using System.Collections.Generic;
using System.Linq;

namespace HomegearLib.RPC.Encoding
{
    public class RPCEncoder
    {
        private static List<byte> _packetStartRequest = new List<byte> { 0x42, 0x69, 0x6E, 0 };
        private static List<byte> _packetStartResponse = new List<byte> { 0x42, 0x69, 0x6E, 1 };
        private static List<byte> _packetStartError = new List<byte> { 0x42, 0x69, 0x6E, 0xFF };

        public static List<byte> EncodeRequest(string methodName, List<RPCVariable> parameters, RPCHeader header = null)
        {
            //The "Bin", the type byte after that and the length itself are not part of the length
            List<byte> packet = new List<byte>();
            packet.AddRange(_packetStartRequest);
            uint headerSize = 0;
            if (header != null)
            {
                headerSize = EncodeHeader(packet, header) + 4;
                if (headerSize > 0)
                {
                    packet[3] |= 0x40;
                }
            }
            BinaryEncoder.EncodeString(packet, methodName);
            if (parameters == null)
            {
                BinaryEncoder.EncodeInteger32(packet, 0);
            }
            else
            {
                BinaryEncoder.EncodeInteger32(packet, parameters.Count());
            }

            if (parameters != null)
            {
                foreach (RPCVariable parameter in parameters)
                {
                    EncodeVariable(packet, parameter);
                }
            }

            int dataSize = (int)(packet.Count() - 4 - headerSize);
            List<byte> sizeBytes = new List<byte>(4)
            {
                (byte)((dataSize >> 24) & 0xFF),
                (byte)((dataSize >> 16) & 0xFF),
                (byte)((dataSize >> 8) & 0xFF),
                (byte)(dataSize & 0xFF)
            };
            packet.InsertRange((int)headerSize + 4, sizeBytes);

            return packet;
        }

        public static List<byte> EncodeResponse(RPCVariable variable)
        {
            //The "Bin", the type byte after that and the length itself are not part of the length
            List<byte> packet = new List<byte>();
            if (variable == null)
            {
                return packet;
            }

            if (variable.ErrorStruct)
            {
                packet.AddRange(_packetStartError);
            }
            else
            {
                packet.AddRange(_packetStartResponse);
            }

            EncodeVariable(packet, variable);

            int dataSize = packet.Count() - 4;
            List<byte> sizeBytes = new List<byte>(4)
            {
                (byte)((dataSize >> 24) & 0xFF),
                (byte)((dataSize >> 16) & 0xFF),
                (byte)((dataSize >> 8) & 0xFF),
                (byte)(dataSize & 0xFF)
            };
            packet.InsertRange(4, sizeBytes);

            return packet;
        }

        public static void InsertHeader(List<byte> packet, RPCHeader header)
        {
            if (packet.Count() < 4)
            {
                return;
            }

            List<byte> headerData = new List<byte>();
            uint headerSize = EncodeHeader(headerData, header);
            if (headerSize > 0)
            {
                packet[3] |= 0x40;
                packet.InsertRange(4, headerData);
            }
        }

        private static uint EncodeHeader(List<byte> packet, RPCHeader header)
        {
            uint oldPacketSize = (uint)packet.Count();
            if (header.Authorization.Length > 0)
            {
                packet.Add((byte)0x0);
                packet.Add((byte)0x0);
                packet.Add((byte)0x0);
                packet.Add((byte)0x1);
                BinaryEncoder.EncodeString(packet, "Authorization");
                BinaryEncoder.EncodeString(packet, header.Authorization);
            }
            else
            {
                return 0;
            }

            uint headerSize = (uint)packet.Count() - oldPacketSize;
            List<byte> sizeBytes = new List<byte>(4)
            {
                (byte)((headerSize >> 24) & 0xFF),
                (byte)((headerSize >> 16) & 0xFF),
                (byte)((headerSize >> 8) & 0xFF),
                (byte)(headerSize & 0xFF)
            };
            packet.InsertRange((int)oldPacketSize, sizeBytes);

            return headerSize;
        }

        private static void EncodeVariable(List<byte> packet, RPCVariable variable)
        {
            if (variable.Type == RPCVariableType.rpcVoid)
            {
                EncodeVoid(packet);
            }
            else if (variable.Type == RPCVariableType.rpcInteger32)
            {
                EncodeInteger(packet, variable);
            }
            else if (variable.Type == RPCVariableType.rpcInteger)
            {
                EncodeInteger(packet, variable);
            }
            else if (variable.Type == RPCVariableType.rpcFloat)
            {
                EncodeFloat(packet, variable);
            }
            else if (variable.Type == RPCVariableType.rpcBoolean)
            {
                EncodeBoolean(packet, variable);
            }
            else if (variable.Type == RPCVariableType.rpcString)
            {
                EncodeString(packet, variable);
            }
            else if (variable.Type == RPCVariableType.rpcBinary)
            {
                EncodeBinary(packet, variable);
            }
            else if (variable.Type == RPCVariableType.rpcBase64)
            {
                EncodeBase64(packet, variable);
            }
            else if (variable.Type == RPCVariableType.rpcStruct)
            {
                EncodeStruct(packet, variable);
            }
            else if (variable.Type == RPCVariableType.rpcArray)
            {
                EncodeArray(packet, variable);
            }
        }

        private static void EncodeStruct(List<byte> packet, RPCVariable variable)
        {
            EncodeType(packet, RPCVariableType.rpcStruct);
            BinaryEncoder.EncodeInteger32(packet, variable.StructValue.Count());
            for (int i = 0; i < variable.StructValue.Count(); i++)
            {
                if (variable.StructValue.ElementAt(i).Value == null)
                {
                    continue;
                }

                BinaryEncoder.EncodeString(packet, variable.StructValue.ElementAt(i).Key);
                EncodeVariable(packet, variable.StructValue.ElementAt(i).Value);
            }
        }

        private static void EncodeArray(List<byte> packet, RPCVariable variable)
        {
            EncodeType(packet, RPCVariableType.rpcArray);
            BinaryEncoder.EncodeInteger32(packet, variable.ArrayValue.Count());
            foreach (RPCVariable element in variable.ArrayValue)
            {
                EncodeVariable(packet, element);
            }
        }

        private static void EncodeType(List<byte> packet, RPCVariableType type)
        {
            BinaryEncoder.EncodeInteger32(packet, (int)type);
        }

        private static void EncodeInteger(List<byte> packet, RPCVariable variable)
        {
            EncodeType(packet, RPCVariableType.rpcInteger);
            BinaryEncoder.EncodeInteger64(packet, variable.IntegerValue);
        }

        private static void EncodeFloat(List<byte> packet, RPCVariable variable)
        {
            EncodeType(packet, RPCVariableType.rpcFloat);
            BinaryEncoder.EncodeFloat(packet, variable.FloatValue);
        }

        private static void EncodeBoolean(List<byte> packet, RPCVariable variable)
        {
            EncodeType(packet, RPCVariableType.rpcBoolean);
            BinaryEncoder.EncodeBoolean(packet, variable.BooleanValue);
        }

        private static void EncodeString(List<byte> packet, RPCVariable variable)
        {
            EncodeType(packet, RPCVariableType.rpcString);
            BinaryEncoder.EncodeString(packet, variable.StringValue);
        }

        private static void EncodeBase64(List<byte> packet, RPCVariable variable)
        {
            EncodeType(packet, RPCVariableType.rpcBase64);
            BinaryEncoder.EncodeString(packet, variable.StringValue);
        }

        private static void EncodeBinary(List<byte> packet, RPCVariable variable)
        {
            EncodeType(packet, RPCVariableType.rpcBinary);
            BinaryEncoder.EncodeBinary(packet, variable.BinaryValue);
        }

        private static void EncodeVoid(List<byte> packet)
        {
            EncodeString(packet, new RPCVariable(RPCVariableType.rpcString));
        }
    }
}
