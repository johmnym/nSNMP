using System;

namespace nSNMP.SMI.X690
{
    public static class BERParser
    {
        public static SnmpDataType ParseType(ref ReadOnlyMemory<byte> memory)
        {
            if (memory.IsEmpty)
            {
                throw new ArgumentException("Cannot parse type from empty memory.");
            }

            var type = (SnmpDataType)memory.Span[0];
            memory = memory.Slice(1);
            return type;
        }

        public static ReadOnlyMemory<byte> ParseDataField(ref ReadOnlyMemory<byte> memory, int length)
        {
            if (length < 0)
            {
                throw new ArgumentException($"Invalid data field length: {length}", nameof(length));
            }

            if (length == 0)
            {
                return ReadOnlyMemory<byte>.Empty;
            }

            if (length > memory.Length)
            {
                throw new ArgumentException("Not enough data in memory to parse field.");
            }

            var data = memory.Slice(0, length);
            memory = memory.Slice(length);
            return data;
        }

        public static int ParseLengthOfNextDataField(ref ReadOnlyMemory<byte> memory)
        {
            if (memory.IsEmpty)
            {
                throw new ArgumentException("Cannot parse length from empty memory.");
            }

            byte firstLengthOctet = memory.Span[0];
            memory = memory.Slice(1);

            if (LengthIsInShortForm(firstLengthOctet))
            {
                return firstLengthOctet;
            }

            int length = 0;
            int numberOfLengthOctets = firstLengthOctet & 0x7f;

            if (numberOfLengthOctets > memory.Length)
            {
                throw new ArgumentException("Not enough data in memory to parse length.");
            }

            for (int i = 0; i < numberOfLengthOctets; i++)
            {
                length = (length << 8) + memory.Span[i];
            }

            memory = memory.Slice(numberOfLengthOctets);
            return length;
        }

        private static bool LengthIsInShortForm(int firstLengthOctet)
        {
            return (firstLengthOctet & 0x80) == 0;
        }
    }
}
