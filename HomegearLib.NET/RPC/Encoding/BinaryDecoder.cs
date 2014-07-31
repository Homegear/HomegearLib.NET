using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomegearLib.RPC.Encoding
{
    internal class BinaryDecoder
    {
        public int DecodeInteger(byte[] encodedData, ref uint position)
        {
            if (position + 4 > encodedData.Length) return 0;
            int value = (encodedData[position] << 24) + (encodedData[position + 1] << 16) + (encodedData[position + 2] << 8) + encodedData[position + 3];
            position += 4;
            return value;
        }

        public byte DecodeByte(byte[] encodedData, ref uint position)
        {
            if (position + 1 > encodedData.Length) return 0;
            byte value = encodedData[position];
            position++;
            return value;
        }

        public string DecodeString(byte[] encodedData, ref uint position)
        {
            int stringLength = DecodeInteger(encodedData, ref position);
            if (position + stringLength > encodedData.Length || stringLength == 0) return "";
            String value = System.Text.UTF8Encoding.UTF8.GetString(encodedData, (int)position, stringLength);
            position += (uint)stringLength;
            return value;
        }

        public bool DecodeBoolean(byte[] encodedData, ref uint position)
        {
            if (position + 1 > encodedData.Length) return false;
            bool value = encodedData[position] != 0;
            position++;
            return value;
        }

        public double DecodeFloat(byte[] encodedData, ref uint position)
        {
            if (position + 8 > encodedData.Length) return 0;
            double mantissa = (double)DecodeInteger(encodedData, ref position);
            double exponent = (double)DecodeInteger(encodedData, ref position);
            double result = (mantissa / (double)0x40000000) * Math.Pow(2, exponent);
            if (result != 0)
            {
                Int32 digits = (Int32)Math.Floor(Math.Log10(Math.Abs(result)) + 1);
                double factor = Math.Pow(10, 9 - digits);
                result = Math.Floor(result * factor + 0.5) / factor;
            }
            return result;
        }
    }
}
