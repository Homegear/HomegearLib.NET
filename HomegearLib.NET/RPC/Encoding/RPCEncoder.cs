using System;
using System.Collections.Generic;
using System.Linq;

namespace HomegearLib.RPC.Encoding
{
    public class RPCEncoder
    {
        private BinaryEncoder _encoder = new BinaryEncoder();
        private static List<byte> _packetStartRequest = new List<byte> { 0x42, 0x69, 0x6E, 0 };
        private static List<byte> _packetStartResponse = new List<byte> { 0x42, 0x69, 0x6E, 1 };
        private static List<byte> _packetStartError = new List<byte> { 0x42, 0x69, 0x6E, 0xFF };

        public List<byte> EncodeRequest(string methodName, List<RPCVariable> parameters, RPCHeader header = null)
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
            _encoder.EncodeString(packet, methodName);
            if (parameters == null)
            {
                _encoder.EncodeInteger32(packet, 0);
            }
            else
            {
                _encoder.EncodeInteger32(packet, parameters.Count());
            }

            if (parameters != null)
            {
                foreach (RPCVariable parameter in parameters)
                {
                    EncodeVariable(packet, parameter);
                }
            }

            int dataSize = (int)(packet.Count() - 4 - headerSize);
            List<byte> sizeBytes = new List<byte>(4);
            sizeBytes.Add((byte)((dataSize >> 24) & 0xFF));
            sizeBytes.Add((byte)((dataSize >> 16) & 0xFF));
            sizeBytes.Add((byte)((dataSize >> 8) & 0xFF));
            sizeBytes.Add((byte)(dataSize & 0xFF));
            packet.InsertRange((int)headerSize + 4, sizeBytes);

            return packet;
        }

        public List<byte> EncodeResponse(RPCVariable variable)
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
            List<byte> sizeBytes = new List<byte>(4);
            sizeBytes.Add((byte)((dataSize >> 24) & 0xFF));
            sizeBytes.Add((byte)((dataSize >> 16) & 0xFF));
            sizeBytes.Add((byte)((dataSize >> 8) & 0xFF));
            sizeBytes.Add((byte)(dataSize & 0xFF));
            packet.InsertRange(4, sizeBytes);

            return packet;
        }

        public void InsertHeader(List<byte> packet, RPCHeader header)
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

        private uint EncodeHeader(List<byte> packet, RPCHeader header)
        {
            uint oldPacketSize = (uint)packet.Count();
            if (header.Authorization.Length > 0)
            {
                packet.Add((byte)0x0);
                packet.Add((byte)0x0);
                packet.Add((byte)0x0);
                packet.Add((byte)0x1);
                _encoder.EncodeString(packet, "Authorization");
                _encoder.EncodeString(packet, header.Authorization);
            }
            else
            {
                return 0;
            }

            uint headerSize = (uint)packet.Count() - oldPacketSize;
            List<byte> sizeBytes = new List<byte>(4);
            sizeBytes.Add((byte)((headerSize >> 24) & 0xFF));
            sizeBytes.Add((byte)((headerSize >> 16) & 0xFF));
            sizeBytes.Add((byte)((headerSize >> 8) & 0xFF));
            sizeBytes.Add((byte)(headerSize & 0xFF));
            packet.InsertRange((int)oldPacketSize, sizeBytes);

            return headerSize;
        }

        private void EncodeVariable(List<byte> packet, RPCVariable variable)
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

        private void EncodeStruct(List<byte> packet, RPCVariable variable)
        {
            EncodeType(packet, RPCVariableType.rpcStruct);
            _encoder.EncodeInteger32(packet, variable.StructValue.Count());
            for (int i = 0; i < variable.StructValue.Count(); i++)
            {
                if (variable.StructValue.ElementAt(i).Value == null)
                {
                    continue;
                }

                _encoder.EncodeString(packet, variable.StructValue.ElementAt(i).Key);
                EncodeVariable(packet, variable.StructValue.ElementAt(i).Value);
            }
        }

        private void EncodeArray(List<byte> packet, RPCVariable variable)
        {
            EncodeType(packet, RPCVariableType.rpcArray);
            _encoder.EncodeInteger32(packet, variable.ArrayValue.Count());
            foreach (RPCVariable element in variable.ArrayValue)
            {
                EncodeVariable(packet, element);
            }
        }

        private void EncodeType(List<byte> packet, RPCVariableType type)
        {
            _encoder.EncodeInteger32(packet, (int)type);
        }

        private void EncodeInteger(List<byte> packet, RPCVariable variable)
        {
            EncodeType(packet, RPCVariableType.rpcInteger);
            _encoder.EncodeInteger64(packet, variable.IntegerValue);
        }

        private void EncodeFloat(List<byte> packet, RPCVariable variable)
        {
            EncodeType(packet, RPCVariableType.rpcFloat);
            _encoder.EncodeFloat(packet, variable.FloatValue);
        }

        private void EncodeBoolean(List<byte> packet, RPCVariable variable)
        {
            EncodeType(packet, RPCVariableType.rpcBoolean);
            _encoder.EncodeBoolean(packet, variable.BooleanValue);
        }

        private void EncodeString(List<byte> packet, RPCVariable variable)
        {
            EncodeType(packet, RPCVariableType.rpcString);
            _encoder.EncodeString(packet, variable.StringValue);
        }

        private void EncodeBase64(List<byte> packet, RPCVariable variable)
        {
            EncodeType(packet, RPCVariableType.rpcBase64);
            _encoder.EncodeString(packet, variable.StringValue);
        }

        private void EncodeBinary(List<byte> packet, RPCVariable variable)
        {
            EncodeType(packet, RPCVariableType.rpcBinary);
            _encoder.EncodeBinary(packet, variable.BinaryValue);
        }

        private void EncodeVoid(List<byte> packet)
        {
            EncodeString(packet, new RPCVariable(RPCVariableType.rpcString));
        }
    }
}
