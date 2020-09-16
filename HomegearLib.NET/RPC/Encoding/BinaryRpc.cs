using System;
using System.Collections.Generic;

using System.Linq;

namespace HomegearLib.RPC.Encoding
{
    internal enum BinaryRpcType
    {
        unknown,
        request,
        response
    }

    internal class BinaryRpc
    {
        private int _headerSize = 0;
        private int _dataSize = 0;

        public BinaryRpcType RpcType { get; private set; }
        public bool HasHeader { get; private set; }
        public bool ProcessingStarted { get; private set; }
        public bool IsFinished { get; private set; }

        private List<byte> _data = new List<byte>(1024);
        public byte[] Data { get { return _data.ToArray();  } }

        public void Reset()
        {
            _data.Clear();
            RpcType = BinaryRpcType.unknown;
            ProcessingStarted = false;
            IsFinished = false;
            HasHeader = false;
            _headerSize = 0;
            _dataSize = 0;
        }


        private static IEnumerable<byte> SubArray(byte[] buffer, int bufferPos, int bufferLength)
        {
            return buffer.Skip(bufferPos).Take(bufferLength);
        }

        public int Process(byte[] buffer, int bufferPos, int bufferLength)
        {
            int initialBufferLength = bufferLength;
            int sizeToInsert = 0;
            if (bufferLength == 0 || IsFinished) return 0;
            ProcessingStarted = true;
            if(_data.Count + bufferLength < 8)
            {
                _data.AddRange(SubArray(buffer, bufferPos, bufferLength));
                return initialBufferLength;
            }
            else if(_data.Count < 8)
            {
                sizeToInsert = 8 - _data.Count;
                _data.AddRange(SubArray(buffer, bufferPos, sizeToInsert));
                bufferPos += sizeToInsert;
                bufferLength -= sizeToInsert;
            }
            if(_data[0] != 'B' || _data[1] != 'i' || _data[2] != 'n')
            {
                IsFinished = true;
                throw new HomegearBinaryRpcException("Packet does not start with \"Bin\".");
            }
            RpcType = ((_data[3] & 1) == 1) ? BinaryRpcType.response : BinaryRpcType.request;
            if(_data[3] == 0x40 || _data[3] == 0x41)
            {
                HasHeader = true;
                _headerSize = (_data[4] << 24) | (_data[5] << 16) | (_data[6] << 8) | _data[7];
                if (_headerSize > 10485760) throw new HomegearBinaryRpcException("Header is larger than 10 MiB.");
            }
            else
            {
                _dataSize = (_data[4] << 24) | (_data[5] << 16) | (_data[6] << 8) | _data[7];
                if (_dataSize > 104857600) throw new HomegearBinaryRpcException("Header is larger than 100 MiB.");
            }
            if(_dataSize == 0 && _headerSize == 0)
            {
                IsFinished = true;
                throw new HomegearBinaryRpcException("Invalid packet format.");
            }
            if (_dataSize == 0) //Has header
            {
                if (_data.Count + bufferLength < 8 + _headerSize + 4)
                {
                    if (_headerSize + 8 + 100 > _data.Capacity) _data.Capacity = _headerSize + 8 + 1024;
                    _data.AddRange(SubArray(buffer, bufferPos, bufferLength));
                    return initialBufferLength;
                }
                else
                {
                    sizeToInsert = (8 + _headerSize + 4) - _data.Count;
                    _data.AddRange(SubArray(buffer, bufferPos, sizeToInsert));
                    bufferPos += sizeToInsert;
                    bufferLength -= sizeToInsert;
                    _dataSize = (_data[8 + _headerSize] << 24) | (_data[8 + _headerSize] << 16) | (_data[8 + _headerSize] << 8) | _data[8 + _headerSize];
                    _dataSize += _headerSize + 4;
                    if (_dataSize > 104857600) throw new HomegearBinaryRpcException("Data is data larger than 100 MiB.");
                }
            }
            _data.Capacity = 8 + _dataSize;
            if (_data.Count + bufferLength < _dataSize + 8)
            {
                _data.AddRange(SubArray(buffer, bufferPos, bufferLength));
                return initialBufferLength;
            }
            else
            {
                sizeToInsert = (8 + _dataSize) - _data.Count;
                _data.AddRange(SubArray(buffer, bufferPos, sizeToInsert));
                bufferLength -= sizeToInsert;
                IsFinished = true;
                return initialBufferLength - bufferLength;
            }
        }
    }
}
