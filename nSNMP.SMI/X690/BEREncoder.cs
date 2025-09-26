using System;
using System.Collections.Generic;

namespace nSNMP.SMI.X690
{
    public static class BEREncoder
    {
        /// <summary>
        /// Encodes a complete BER TLV (Type-Length-Value) structure
        /// </summary>
        public static byte[] EncodeTLV(byte tag, ReadOnlySpan<byte> value)
        {
            var lengthBytes = EncodeLength(value.Length);
            var result = new byte[1 + lengthBytes.Length + value.Length];

            result[0] = tag;
            lengthBytes.CopyTo(result.AsSpan(1));
            value.CopyTo(result.AsSpan(1 + lengthBytes.Length));

            return result;
        }

        /// <summary>
        /// Encodes length in BER definite form
        /// </summary>
        public static byte[] EncodeLength(int length)
        {
            if (length < 0)
                throw new ArgumentException("Length cannot be negative", nameof(length));

            // Short form (0-127): single byte with bit 7 = 0
            if (length <= 127)
            {
                return new byte[] { (byte)length };
            }

            // Long form: first byte has bit 7 = 1 and bits 0-6 = number of length octets
            var lengthBytes = new List<byte>();

            // Convert length to bytes (big-endian)
            var temp = length;
            while (temp > 0)
            {
                lengthBytes.Insert(0, (byte)(temp & 0xFF));
                temp >>= 8;
            }

            // First byte: 0x80 | number of length octets
            var result = new byte[1 + lengthBytes.Count];
            result[0] = (byte)(0x80 | lengthBytes.Count);

            for (int i = 0; i < lengthBytes.Count; i++)
            {
                result[i + 1] = lengthBytes[i];
            }

            return result;
        }

        /// <summary>
        /// Encodes a 32-bit integer in two's complement form (minimal octets, big-endian)
        /// </summary>
        public static byte[] EncodeInteger(int value)
        {
            if (value == 0)
                return new byte[] { 0x00 };

            var bytes = new List<byte>();
            bool negative = value < 0;

            // Convert to unsigned for bit manipulation
            uint unsignedValue = (uint)value;

            // Extract bytes from least to most significant
            while (unsignedValue != 0 || bytes.Count == 0)
            {
                bytes.Insert(0, (byte)(unsignedValue & 0xFF));
                unsignedValue >>= 8;
            }

            // Handle sign bit for positive numbers
            if (!negative && (bytes[0] & 0x80) != 0)
            {
                bytes.Insert(0, 0x00);
            }

            return bytes.ToArray();
        }

        /// <summary>
        /// Encodes a 64-bit integer in two's complement form (minimal octets, big-endian)
        /// </summary>
        public static byte[] EncodeInteger64(long value)
        {
            if (value == 0)
                return new byte[] { 0x00 };

            var bytes = new List<byte>();
            bool negative = value < 0;

            // Convert to unsigned for bit manipulation
            ulong unsignedValue = (ulong)value;

            // Extract bytes from least to most significant
            while (unsignedValue != 0 || bytes.Count == 0)
            {
                bytes.Insert(0, (byte)(unsignedValue & 0xFF));
                unsignedValue >>= 8;
            }

            // Handle sign bit for positive numbers
            if (!negative && (bytes[0] & 0x80) != 0)
            {
                bytes.Insert(0, 0x00);
            }

            return bytes.ToArray();
        }

        /// <summary>
        /// Encodes an OID using base-128 encoding
        /// </summary>
        public static byte[] EncodeOID(ReadOnlySpan<uint> subIds)
        {
            if (subIds.Length < 2)
                throw new ArgumentException("OID must have at least 2 sub-identifiers", nameof(subIds));

            var result = new List<byte>();

            // First sub-identifier: 40 * first + second
            uint firstEncoded = 40 * subIds[0] + subIds[1];
            result.AddRange(EncodeSubIdentifier(firstEncoded));

            // Remaining sub-identifiers
            for (int i = 2; i < subIds.Length; i++)
            {
                result.AddRange(EncodeSubIdentifier(subIds[i]));
            }

            return result.ToArray();
        }

        /// <summary>
        /// Encodes a single OID sub-identifier using base-128 with continuation bits
        /// </summary>
        private static byte[] EncodeSubIdentifier(uint value)
        {
            if (value == 0)
                return new byte[] { 0x00 };

            var bytes = new List<byte>();

            // Extract 7-bit groups from least to most significant
            bytes.Add((byte)(value & 0x7F)); // Last byte has no continuation bit
            value >>= 7;

            while (value > 0)
            {
                bytes.Insert(0, (byte)((value & 0x7F) | 0x80)); // Continuation bit set
                value >>= 7;
            }

            return bytes.ToArray();
        }
    }
}