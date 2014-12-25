using System;
using System.IO;

namespace nSNMP.SMI.X690
{
    public static class BERParser
    {
        public static SnmpDataType ParseType(byte data)
        {
            return (SnmpDataType) data;
        }

        public static SnmpDataType ParseType(MemoryStream stream)
        {
            var data = (byte)stream.ReadByte();
            
            return ParseType(data);
        }

        public static BERClass ParseClass(byte data)
        {
            int classValue = data & 192;

            return (BERClass) classValue;
        }

        public static byte[] ParseDataField(MemoryStream stream, int length)
        {
            var buffer = new byte[length];

            stream.Read(buffer, 0, length);

            return buffer;
        }

        public static int ParseLengthOfNextDataField(MemoryStream stream)
        {
            var firstLengthOctet = (byte) stream.ReadByte();

            if (LengthIsInShortForm(firstLengthOctet))
            {
                return firstLengthOctet;
            }

            int length = 0;

            int numberOfLengthOctets = firstLengthOctet & 0x7f;
            
            for (int octetIndex = 0; octetIndex < numberOfLengthOctets; octetIndex++)
            {
                var nextByte = (byte)stream.ReadByte();

                length = (length << 8) + nextByte;
            }

            return length;
        }

        private static bool LengthIsInShortForm(int firstLengthOctet)
        {
            return (firstLengthOctet & 0x80) == 0;
        }
    }
}
