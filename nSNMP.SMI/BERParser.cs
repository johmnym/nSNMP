
using System;
using System.Collections.Generic;
using System.IO;

namespace nSNMP.SMI
{
    public static class BERParser
    {
        public static SnmpDataType ParseType(byte data)
        {
            int tagNumber = data & 0x3F; //63
            
            return (SnmpDataType) tagNumber;
        }

        public static BERClass ParseClass(byte data)
        {
            int classValue = data & 192;

            return (BERClass) classValue;
        }

        public static int ParseLength(byte[] data)
        {
            var stream = new MemoryStream(data) {Position = 1};

            var firstLengthOctet = (byte) stream.ReadByte();

            if (LengthIsInShortForm(firstLengthOctet))
            {
                return firstLengthOctet;
            }

            var list = new List<byte> {firstLengthOctet};

            var length = 0;

            var numberOfLengthOctets = firstLengthOctet & 0x7f;
            
            for (var octetIndex = 0; octetIndex < numberOfLengthOctets; octetIndex++)
            {
                var octet = stream.ReadByte();

                if (octet == -1)
                {
                    throw new Exception("BER end of file");
                }

                var nextByte = (byte)octet;

                length = (length << 8) + nextByte;
                
                list.Add(nextByte);
            }

            return length;
        }

        private static bool LengthIsInShortForm(int firstLengthOctet)
        {
            return (firstLengthOctet & 0x80) == 0;
        }
    }

    public enum BERClass
    {
        Universal = 0,
        Application = 64,
        ContextSpecific = 128,
        Private = 192
    }
}
