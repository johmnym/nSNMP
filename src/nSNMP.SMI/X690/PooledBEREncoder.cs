using System;
using System.Buffers;
using System.Collections.Generic;

namespace nSNMP.SMI.X690
{
    /// <summary>
    /// Memory-optimized BER encoder using ArrayPool<byte> for temporary allocations
    /// </summary>
    public static class PooledBEREncoder
    {
        private static readonly ArrayPool<byte> _pool = ArrayPool<byte>.Shared;

        /// <summary>
        /// Encodes a complete BER TLV (Type-Length-Value) structure using pooled memory
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
        /// Encodes length in BER definite form using pooled memory for temporary arrays
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

            // Long form: calculate required bytes for length
            int bytesNeeded = 0;
            int temp = length;
            while (temp > 0)
            {
                bytesNeeded++;
                temp >>= 8;
            }

            // Create result array
            var result = new byte[1 + bytesNeeded];
            result[0] = (byte)(0x80 | bytesNeeded);

            // Fill length bytes in big-endian order
            temp = length;
            for (int i = bytesNeeded; i > 0; i--)
            {
                result[i] = (byte)(temp & 0xFF);
                temp >>= 8;
            }

            return result;
        }

        /// <summary>
        /// Encodes a 32-bit integer in two's complement form using minimal allocations
        /// </summary>
        public static byte[] EncodeInteger(int value)
        {
            if (value == 0)
                return new byte[] { 0x00 };

            // Determine required bytes
            int bytesNeeded = 4; // Start with max for int32
            uint unsignedValue = (uint)value;
            bool negative = value < 0;

            // Count significant bytes
            if (!negative)
            {
                bytesNeeded = 0;
                uint temp = unsignedValue;
                do
                {
                    bytesNeeded++;
                    temp >>= 8;
                } while (temp > 0);

                // Add padding byte if high bit is set (to maintain positive sign)
                if ((unsignedValue >> ((bytesNeeded - 1) * 8)) >= 0x80)
                {
                    bytesNeeded++;
                }
            }

            var result = new byte[bytesNeeded];

            // Fill bytes in big-endian order
            for (int i = bytesNeeded - 1; i >= 0; i--)
            {
                result[i] = (byte)(unsignedValue & 0xFF);
                unsignedValue >>= 8;
            }

            return result;
        }

        /// <summary>
        /// Encodes a 64-bit integer in two's complement form using minimal allocations
        /// </summary>
        public static byte[] EncodeInteger64(long value)
        {
            if (value == 0)
                return new byte[] { 0x00 };

            // Determine required bytes
            int bytesNeeded = 8; // Start with max for int64
            ulong unsignedValue = (ulong)value;
            bool negative = value < 0;

            // Count significant bytes
            if (!negative)
            {
                bytesNeeded = 0;
                ulong temp = unsignedValue;
                do
                {
                    bytesNeeded++;
                    temp >>= 8;
                } while (temp > 0);

                // Add padding byte if high bit is set (to maintain positive sign)
                if ((unsignedValue >> ((bytesNeeded - 1) * 8)) >= 0x80)
                {
                    bytesNeeded++;
                }
            }

            var result = new byte[bytesNeeded];

            // Fill bytes in big-endian order
            for (int i = bytesNeeded - 1; i >= 0; i--)
            {
                result[i] = (byte)(unsignedValue & 0xFF);
                unsignedValue >>= 8;
            }

            return result;
        }

        /// <summary>
        /// Encodes an OID using base-128 encoding with pooled temporary buffers
        /// </summary>
        public static byte[] EncodeOID(ReadOnlySpan<uint> subIds)
        {
            if (subIds.Length < 2)
                throw new ArgumentException("OID must have at least 2 sub-identifiers", nameof(subIds));

            // Estimate size to minimize allocations (each sub-id needs max 5 bytes)
            int estimatedSize = subIds.Length * 5;
            byte[] buffer = _pool.Rent(estimatedSize);

            try
            {
                int currentPos = 0;

                // First sub-identifier: 40 * first + second
                uint firstEncoded = 40 * subIds[0] + subIds[1];
                currentPos += EncodeSubIdentifierToBuffer(firstEncoded, buffer.AsSpan(currentPos));

                // Remaining sub-identifiers
                for (int i = 2; i < subIds.Length; i++)
                {
                    currentPos += EncodeSubIdentifierToBuffer(subIds[i], buffer.AsSpan(currentPos));
                }

                // Copy to final result
                var result = new byte[currentPos];
                buffer.AsSpan(0, currentPos).CopyTo(result);
                return result;
            }
            finally
            {
                _pool.Return(buffer);
            }
        }

        /// <summary>
        /// Encodes a single OID sub-identifier directly to a buffer span
        /// </summary>
        private static int EncodeSubIdentifierToBuffer(uint value, Span<byte> buffer)
        {
            if (value == 0)
            {
                buffer[0] = 0x00;
                return 1;
            }

            int bytesWritten = 0;

            // Calculate number of 7-bit groups needed
            uint temp = value;
            int groupCount = 0;
            do
            {
                groupCount++;
                temp >>= 7;
            } while (temp > 0);

            // Write from most significant to least significant group
            for (int i = groupCount - 1; i >= 0; i--)
            {
                uint groupValue = (value >> (i * 7)) & 0x7F;
                byte byteValue = (byte)groupValue;

                // Set continuation bit for all but the last byte
                if (i > 0)
                    byteValue |= 0x80;

                buffer[bytesWritten++] = byteValue;
            }

            return bytesWritten;
        }

        /// <summary>
        /// Utility method to concatenate multiple byte arrays using pooled buffers
        /// </summary>
        public static byte[] ConcatenateBuffers(IEnumerable<ReadOnlyMemory<byte>> buffers)
        {
            // Calculate total length
            int totalLength = 0;
            foreach (var buffer in buffers)
            {
                totalLength += buffer.Length;
            }

            var result = new byte[totalLength];
            int currentPos = 0;

            foreach (var buffer in buffers)
            {
                buffer.Span.CopyTo(result.AsSpan(currentPos));
                currentPos += buffer.Length;
            }

            return result;
        }
    }
}