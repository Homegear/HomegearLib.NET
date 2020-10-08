using System;

namespace HomegearLib.RPC.Encoding
{
    internal class BinaryDecoder
    {
        public static int DecodeInteger32(byte[] encodedData, ref uint position)
        {
            if (position + 4 > encodedData.Length)
            {
                return 0;
            }

            int value = (encodedData[position] << 24) | (encodedData[position + 1] << 16) | (encodedData[position + 2] << 8) | encodedData[position + 3];
            position += 4;
            return value;
        }

        public static long DecodeInteger64(byte[] encodedData, ref uint position)
        {
            if (position + 8 > encodedData.Length)
            {
                return 0;
            }

            long value = ((long)encodedData[position] << 56) + ((long)encodedData[position + 1] << 48) + ((long)encodedData[position + 2] << 40) + ((long)encodedData[position + 3] << 32) + ((long)encodedData[position + 4] << 24) + ((long)encodedData[position + 5] << 16) + ((long)encodedData[position + 6] << 8) + (long)encodedData[position + 7];
            position += 8;
            return value;
        }

        public static byte DecodeByte(byte[] encodedData, ref uint position)
        {
            if (position + 1 > encodedData.Length)
            {
                return 0;
            }

            byte value = encodedData[position];
            position++;
            return value;
        }

        public static string DecodeString(byte[] encodedData, ref uint position)
        {
            int stringLength = DecodeInteger32(encodedData, ref position);
            if (position + stringLength > encodedData.Length || stringLength == 0)
            {
                return "";
            }

            string value = System.Text.UTF8Encoding.UTF8.GetString(encodedData, (int)position, stringLength);
            position += (uint)stringLength;
            return value;
        }

        public static byte[] DecodeBinary(byte[] encodedData, ref uint position)
        {
            int binaryLength = DecodeInteger32(encodedData, ref position);
            if (position + binaryLength > encodedData.Length || binaryLength == 0)
            {
                return new byte[0];
            }

            byte[] result = new byte[binaryLength];
            Array.Copy(encodedData, position, result, 0, binaryLength);
            return result;
        }

        public static bool DecodeBoolean(byte[] encodedData, ref uint position)
        {
            if (position + 1 > encodedData.Length)
            {
                return false;
            }

            bool value = encodedData[position] != 0;
            position++;
            return value;
        }

        public static double DecodeFloat(byte[] encodedData, ref uint position)
        {
            if (position + 8 > encodedData.Length)
            {
                return 0;
            }

            double mantissa = (double)DecodeInteger32(encodedData, ref position);
            double exponent = (double)DecodeInteger32(encodedData, ref position);
            double result = (mantissa / (double)0x40000000) * Math.Pow(2, exponent);
            if (result != 0)
            {
                int digits = (int)Math.Floor(Math.Log10(Math.Abs(result)) + 1);
                double factor = Math.Pow(10, 9 - digits);
                result = Math.Floor(result * factor + 0.5) / factor;
            }
            return result;
        }
    }
}