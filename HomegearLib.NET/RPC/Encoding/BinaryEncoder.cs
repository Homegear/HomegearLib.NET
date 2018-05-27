using System;
using System.Collections.Generic;
using System.Linq;

namespace HomegearLib.RPC.Encoding
{
    internal class BinaryEncoder
    {
        public void EncodeInteger(List<byte> encodedData, int value)
        {
            encodedData.Add((byte)((value >> 24) & 0xFF));
            encodedData.Add((byte)((value >> 16) & 0xFF));
            encodedData.Add((byte)((value >> 8) & 0xFF));
            encodedData.Add((byte)(value & 0xFF));
        }

        public void EncodeInteger64(List<byte> encodedData, long value)
        {
            encodedData.Add((byte)((value >> 56) & 0xFF));
            encodedData.Add((byte)((value >> 48) & 0xFF));
            encodedData.Add((byte)((value >> 40) & 0xFF));
            encodedData.Add((byte)((value >> 32) & 0xFF));
            encodedData.Add((byte)((value >> 24) & 0xFF));
            encodedData.Add((byte)((value >> 16) & 0xFF));
            encodedData.Add((byte)((value >> 8) & 0xFF));
            encodedData.Add((byte)(value & 0xFF));
        }

        public void EncodeByte(List<byte> encodedData, byte value)
        {
            encodedData.Add(value);
        }

        public void EncodeString(List<byte> encodedData, string value)
        {
            if (value == null)
            {
                EncodeInteger(encodedData, 0);
                return;
            }
            byte[] stringBytes = System.Text.UTF8Encoding.UTF8.GetBytes(value);
            EncodeInteger(encodedData, stringBytes.Length);
            if (value.Length == 0) return;
            encodedData.InsertRange(encodedData.Count(), stringBytes);
        }

        public void EncodeBoolean(List<byte> encodedData, bool value)
        {
            if (value) encodedData.Add(1);
            else encodedData.Add(0);
        }

        public void EncodeFloat(List<byte> encodedData, double value)
        {
            double temp = Math.Abs(value);
            int exponent = 0;
            if (temp != 0 && temp < 0.5)
            {
                while (temp < 0.5)
                {
                    temp *= 2;
                    exponent--;
                }
            }
            else
            {
                while (temp >= 1)
                {
                    temp /= 2;
                    exponent++;
                }
            }
            if (value < 0) temp *= -1;
            int mantissa = (int)Math.Round(temp * (double)0x40000000);
            EncodeInteger(encodedData, mantissa);
            EncodeInteger(encodedData, exponent);
        }
    }
}
