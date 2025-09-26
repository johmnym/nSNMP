using System.Security.Cryptography;
using System.Text;

namespace nSNMP.Security
{
    /// <summary>
    /// Implementation of USM key localization algorithms (RFC 3414)
    /// </summary>
    public static class KeyLocalization
    {
        /// <summary>
        /// Localize authentication key for specific engine
        /// </summary>
        public static byte[] LocalizeKey(string password, byte[] engineId, AuthProtocol authProtocol)
        {
            if (authProtocol == AuthProtocol.None)
                return Array.Empty<byte>();

            // Step 1: Generate Ku (user key) from password
            var ku = GenerateKu(password, authProtocol);

            // Step 2: Localize Ku to specific engine ID to get Kul
            return LocalizeKu(ku, engineId, authProtocol);
        }

        /// <summary>
        /// Localize privacy key for specific engine
        /// </summary>
        public static byte[] LocalizeKey(string password, byte[] engineId, AuthProtocol authProtocol, PrivProtocol privProtocol)
        {
            if (privProtocol == PrivProtocol.None)
                return Array.Empty<byte>();

            // Privacy key is derived from auth key localization + salt
            var authKey = LocalizeKey(password, engineId, authProtocol);

            return privProtocol switch
            {
                PrivProtocol.DES => authKey.Take(16).ToArray(), // DES uses first 16 bytes
                PrivProtocol.AES128 => authKey.Take(16).ToArray(), // AES-128 uses first 16 bytes
                PrivProtocol.AES192 => authKey.Take(24).ToArray(), // AES-192 uses first 24 bytes
                PrivProtocol.AES256 => authKey.Take(32).ToArray(), // AES-256 uses first 32 bytes
                _ => throw new NotSupportedException($"Privacy protocol {privProtocol} not supported")
            };
        }

        /// <summary>
        /// Generate user key (Ku) from password using RFC 3414 algorithm
        /// </summary>
        private static byte[] GenerateKu(string password, AuthProtocol authProtocol)
        {
            var passwordBytes = Encoding.UTF8.GetBytes(password);
            var extendedPassword = new byte[1048576]; // 1MB buffer

            // Extend password to 1MB by repeating
            for (int i = 0; i < extendedPassword.Length; i++)
            {
                extendedPassword[i] = passwordBytes[i % passwordBytes.Length];
            }

            // Hash the extended password
            return authProtocol switch
            {
                AuthProtocol.MD5 => MD5.HashData(extendedPassword),
                AuthProtocol.SHA1 => SHA1.HashData(extendedPassword),
                AuthProtocol.SHA224 => SHA256.HashData(extendedPassword).Take(28).ToArray(), // Truncated SHA-256
                AuthProtocol.SHA256 => SHA256.HashData(extendedPassword),
                AuthProtocol.SHA384 => SHA384.HashData(extendedPassword),
                AuthProtocol.SHA512 => SHA512.HashData(extendedPassword),
                _ => throw new NotSupportedException($"Authentication protocol {authProtocol} not supported")
            };
        }

        /// <summary>
        /// Localize user key (Ku) to engine-specific key (Kul)
        /// </summary>
        private static byte[] LocalizeKu(byte[] ku, byte[] engineId, AuthProtocol authProtocol)
        {
            // Kul = H(Ku || engineID || Ku)
            var buffer = new byte[ku.Length + engineId.Length + ku.Length];

            Array.Copy(ku, 0, buffer, 0, ku.Length);
            Array.Copy(engineId, 0, buffer, ku.Length, engineId.Length);
            Array.Copy(ku, 0, buffer, ku.Length + engineId.Length, ku.Length);

            return authProtocol switch
            {
                AuthProtocol.MD5 => MD5.HashData(buffer),
                AuthProtocol.SHA1 => SHA1.HashData(buffer),
                AuthProtocol.SHA224 => SHA256.HashData(buffer).Take(28).ToArray(),
                AuthProtocol.SHA256 => SHA256.HashData(buffer),
                AuthProtocol.SHA384 => SHA384.HashData(buffer),
                AuthProtocol.SHA512 => SHA512.HashData(buffer),
                _ => throw new NotSupportedException($"Authentication protocol {authProtocol} not supported")
            };
        }

        /// <summary>
        /// Calculate authentication digest for message
        /// </summary>
        public static byte[] CalculateDigest(byte[] message, byte[] key, AuthProtocol authProtocol)
        {
            if (authProtocol == AuthProtocol.None)
                return Array.Empty<byte>();

            byte[] digest = authProtocol switch
            {
                AuthProtocol.MD5 => HMACMD5.HashData(key, message),
                AuthProtocol.SHA1 => HMACSHA1.HashData(key, message),
                AuthProtocol.SHA256 => HMACSHA256.HashData(key, message),
                AuthProtocol.SHA384 => HMACSHA384.HashData(key, message),
                AuthProtocol.SHA512 => HMACSHA512.HashData(key, message),
                _ => throw new NotSupportedException($"Authentication protocol {authProtocol} not supported")
            };

            // USM uses first 12 bytes of digest as authentication parameter
            return digest.Take(12).ToArray();
        }

        /// <summary>
        /// Verify authentication digest
        /// </summary>
        public static bool VerifyDigest(byte[] message, byte[] key, byte[] providedDigest, AuthProtocol authProtocol)
        {
            if (authProtocol == AuthProtocol.None)
                return providedDigest.Length == 0;

            var calculatedDigest = CalculateDigest(message, key, authProtocol);
            return calculatedDigest.SequenceEqual(providedDigest);
        }

        /// <summary>
        /// Get hash algorithm output length
        /// </summary>
        public static int GetDigestLength(AuthProtocol authProtocol) => authProtocol switch
        {
            AuthProtocol.None => 0,
            AuthProtocol.MD5 => 16,
            AuthProtocol.SHA1 => 20,
            AuthProtocol.SHA224 => 28,
            AuthProtocol.SHA256 => 32,
            AuthProtocol.SHA384 => 48,
            AuthProtocol.SHA512 => 64,
            _ => throw new NotSupportedException($"Authentication protocol {authProtocol} not supported")
        };
    }
}