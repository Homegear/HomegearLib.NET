using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomegearLib.Encoding
{
    internal class BinaryEncoder
    {
        public void EncodeInteger(List<byte> encodedData, int value)
        {
            encodedData.Add((byte)(value >> 24));
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
            EncodeInteger(encodedData, value.Length);
            if (value.Length == 0) return;
            encodedData.InsertRange(encodedData.Count(), System.Text.ASCIIEncoding.ASCII.GetBytes(value));
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
            if(temp != 0 && temp < 0.5)
            {
                while(temp < 0.5)
                {
                    temp *= 2;
                    exponent--;
                }
            }
            else
            {
                while(temp >= 1)
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
