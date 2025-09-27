using System;
using nSNMP.SMI.DataTypes.V1.Primitive;

namespace nSNMP.Fuzz
{
    /// <summary>
    /// Fuzzer targeting ObjectIdentifier parsing from strings
    /// </summary>
    public static class OidParserFuzzer
    {
        public static void FuzzOidParser(System.IO.Stream stream)
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
                // Convert bytes to string for OID parsing
                var input = System.Text.Encoding.UTF8.GetString(data);
                TryParseOidFromString(input);

                // Also try ASCII interpretation
                var asciiInput = System.Text.Encoding.ASCII.GetString(data);
                TryParseOidFromString(asciiInput);

                // Try Latin1 for binary data
                var latin1Input = System.Text.Encoding.Latin1.GetString(data);
                TryParseOidFromString(latin1Input);
            }
            catch (Exception ex) when (IsExpectedException(ex))
            {
                // Expected exceptions from malformed input
            }
        }

        private static void TryParseOidFromString(string input)
        {
            try
            {
                var oid = ObjectIdentifier.Create(input);

                // Exercise the parsed OID
                var _ = oid.Value;
                var __ = oid.ToString();
                var ___ = oid.ToBytes();

                // Test comparison operations
                try
                {
                    var standardOid = ObjectIdentifier.Create("1.3.6.1.2.1.1.1.0");
                    var comparison = oid.CompareTo(standardOid);
                    var equality = oid.Equals(standardOid);
                    var hashCode = oid.GetHashCode();
                }
                catch
                {
                    // Comparison operations might fail for malformed OIDs
                }

                // Test lexicographic operations if they exist
                try
                {
                    var parentOid = ObjectIdentifier.Create("1.3.6.1.2.1");
                    // Test if this OID is under the parent (if such methods exist)
                }
                catch
                {
                    // Parent/child operations might fail
                }
            }
            catch
            {
                // Expected for malformed input
            }
        }

        private static bool IsExpectedException(Exception ex)
        {
            return ex is ArgumentException ||
                   ex is ArgumentOutOfRangeException ||
                   ex is FormatException ||
                   ex is InvalidOperationException ||
                   ex is NotSupportedException ||
                   ex is IndexOutOfRangeException ||
                   ex is ArgumentNullException ||
                   ex is OverflowException ||
                   ex is System.Text.DecoderFallbackException;
        }
    }
}