using System;
using nSNMP.SMI.DataTypes.V1.Primitive;

namespace nSNMP.Fuzz
{
    /// <summary>
    /// Simplified BER fuzzer focusing on the most critical parsing functions
    /// </summary>
    public static class SimpleBerFuzzer
    {
        public static void FuzzBerDecoder(System.IO.Stream stream)
        {
            var data = new byte[stream.Length];
            var bytesRead = stream.Read(data, 0, data.Length);

            // Resize array to actual bytes read
            if (bytesRead < data.Length)
            {
                Array.Resize(ref data, bytesRead);
            }

            if (data.Length == 0)
                return;

            try
            {
                // Focus on the most critical BER parsing operations
                // These are the primary attack surface for SNMP

                // 1. ObjectIdentifier parsing - critical for all SNMP operations
                TryParseObjectIdentifier(data.AsSpan());

                // 2. Integer parsing - used in all message fields
                TryParseInteger(data.AsSpan());

                // 3. OctetString parsing - used for community strings and data
                TryParseOctetString(data.AsSpan());

                // 4. Counter types parsing - common in SNMP responses
                TryParseCounterTypes(data.AsSpan());

                // 5. Test various length combinations for length encoding attacks
                TryParseDifferentLengths(data.AsSpan());
            }
            catch (Exception ex) when (IsExpectedException(ex))
            {
                // Expected exceptions from malformed input - not security issues
            }
        }

        private static void TryParseObjectIdentifier(ReadOnlySpan<byte> data)
        {
            try
            {
                var oid = new ObjectIdentifier(data.ToArray());

                // Exercise all operations that could trigger vulnerabilities
                var value = oid.Value;       // Array access, potential bounds issues
                var str = oid.ToString();    // String formatting, potential overflows
                var bytes = oid.ToBytes();   // Re-encoding, potential corruption
                var hash = oid.GetHashCode(); // Hash calculation

                // Test comparison operations (potential for infinite loops)
                try
                {
                    var standardOid = ObjectIdentifier.Create("1.3.6.1.2.1.1.1.0");
                    var comparison = oid.CompareTo(standardOid);
                    var equality = oid.Equals(standardOid);
                }
                catch
                {
                    // Comparison might fail for malformed OIDs
                }
            }
            catch
            {
                // Expected for malformed input
            }
        }

        private static void TryParseInteger(ReadOnlySpan<byte> data)
        {
            try
            {
                var integer = new Integer(data.ToArray());

                var value = integer.Value;    // Integer conversion, potential overflows
                var str = integer.ToString(); // String formatting
                var bytes = integer.ToBytes(); // Re-encoding
                var hash = integer.GetHashCode();
            }
            catch
            {
                // Expected for malformed input
            }
        }

        private static void TryParseOctetString(ReadOnlySpan<byte> data)
        {
            try
            {
                var octetString = new OctetString(data.ToArray());

                var value = octetString.Value; // Byte array access
                var str = octetString.ToString(); // String conversion
                var bytes = octetString.ToBytes(); // Re-encoding
            }
            catch
            {
                // Expected for malformed input
            }
        }

        private static void TryParseCounterTypes(ReadOnlySpan<byte> data)
        {
            try
            {
                var counter32 = new Counter32(data.ToArray());
                var _ = counter32.Value;
                var __ = counter32.ToBytes();
            }
            catch { }

            try
            {
                var gauge32 = new Gauge32(data.ToArray());
                var _ = gauge32.Value;
                var __ = gauge32.ToBytes();
            }
            catch { }

            try
            {
                var timeTicks = new TimeTicks(data.ToArray());
                var _ = timeTicks.Value;
                var __ = timeTicks.ToBytes();
            }
            catch { }

            try
            {
                var counter64 = new Counter64(data.ToArray());
                var _ = counter64.Value;
                var __ = counter64.ToBytes();
            }
            catch { }

            try
            {
                var ipAddress = new IpAddress(data.ToArray());
                var _ = ipAddress.Value;
                var __ = ipAddress.ToBytes();
            }
            catch { }
        }

        private static void TryParseDifferentLengths(ReadOnlySpan<byte> data)
        {
            // Test parsing with different data lengths to find boundary conditions
            for (int len = 1; len <= Math.Min(data.Length, 20); len++)
            {
                try
                {
                    var truncated = data.Slice(0, len);
                    var oid = new ObjectIdentifier(truncated.ToArray());
                    var _ = oid.ToString();
                }
                catch { }

                try
                {
                    var truncated = data.Slice(0, len);
                    var integer = new Integer(truncated.ToArray());
                    var _ = integer.Value;
                }
                catch { }
            }

            // Test parsing with various prefixes that might confuse length decoding
            if (data.Length > 1)
            {
                var prefixes = new byte[] { 0x00, 0x01, 0x30, 0x02, 0x04, 0x06, 0x80, 0x81, 0x82, 0xFF };

                foreach (var prefix in prefixes)
                {
                    try
                    {
                        var prefixedData = new byte[data.Length + 1];
                        prefixedData[0] = prefix;
                        data.CopyTo(prefixedData.AsSpan(1));

                        var oid = new ObjectIdentifier(prefixedData);
                        var _ = oid.ToString();
                    }
                    catch { }
                }
            }
        }

        private static bool IsExpectedException(Exception ex)
        {
            // These are expected exceptions from malformed input and not security vulnerabilities
            return ex is ArgumentException ||
                   ex is ArgumentOutOfRangeException ||
                   ex is FormatException ||
                   ex is InvalidOperationException ||
                   ex is NotSupportedException ||
                   ex is IndexOutOfRangeException ||
                   ex is ArgumentNullException ||
                   ex is OverflowException;
        }
    }
}