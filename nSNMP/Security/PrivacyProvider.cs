using System.Security.Cryptography;

namespace nSNMP.Security
{
    /// <summary>
    /// Privacy (encryption/decryption) implementation for SNMPv3 USM
    /// </summary>
    public static class PrivacyProvider
    {
        /// <summary>
        /// Encrypt scoped PDU data using specified privacy protocol
        /// </summary>
        public static (byte[] encryptedData, byte[] privacyParameters) Encrypt(
            byte[] data,
            byte[] privacyKey,
            PrivProtocol privProtocol,
            int engineBoots,
            int engineTime)
        {
            return privProtocol switch
            {
                PrivProtocol.None => (data, Array.Empty<byte>()),
                PrivProtocol.DES => EncryptDES(data, privacyKey, engineBoots, engineTime),
                PrivProtocol.AES128 => EncryptAES(data, privacyKey, 16, engineBoots, engineTime),
                PrivProtocol.AES192 => EncryptAES(data, privacyKey, 24, engineBoots, engineTime),
                PrivProtocol.AES256 => EncryptAES(data, privacyKey, 32, engineBoots, engineTime),
                _ => throw new NotSupportedException($"Privacy protocol {privProtocol} not supported")
            };
        }

        /// <summary>
        /// Decrypt scoped PDU data using specified privacy protocol
        /// </summary>
        public static byte[] Decrypt(
            byte[] encryptedData,
            byte[] privacyKey,
            byte[] privacyParameters,
            PrivProtocol privProtocol,
            int engineBoots,
            int engineTime)
        {
            return privProtocol switch
            {
                PrivProtocol.None => encryptedData,
                PrivProtocol.DES => DecryptDES(encryptedData, privacyKey, privacyParameters, engineBoots, engineTime),
                PrivProtocol.AES128 => DecryptAES(encryptedData, privacyKey, privacyParameters, 16, engineBoots, engineTime),
                PrivProtocol.AES192 => DecryptAES(encryptedData, privacyKey, privacyParameters, 24, engineBoots, engineTime),
                PrivProtocol.AES256 => DecryptAES(encryptedData, privacyKey, privacyParameters, 32, engineBoots, engineTime),
                _ => throw new NotSupportedException($"Privacy protocol {privProtocol} not supported")
            };
        }

        /// <summary>
        /// DES-CFB encryption (legacy, RFC 3414)
        /// </summary>
        private static (byte[] encryptedData, byte[] privacyParameters) EncryptDES(
            byte[] data,
            byte[] privacyKey,
            int engineBoots,
            int engineTime)
        {
            if (privacyKey.Length < 16)
                throw new ArgumentException("DES privacy key must be at least 16 bytes");

            // DES key is first 8 bytes, IV salt is last 8 bytes
            var desKey = privacyKey.Take(8).ToArray();
            var ivSalt = privacyKey.Skip(8).Take(8).ToArray();

            // Privacy parameters = salt (random 8 bytes)
            var salt = new byte[8];
            RandomNumberGenerator.Fill(salt);

            // IV = ivSalt XOR salt
            var iv = new byte[8];
            for (int i = 0; i < 8; i++)
            {
                iv[i] = (byte)(ivSalt[i] ^ salt[i]);
            }

            // Encrypt using DES-CFB
            using var des = DES.Create();
            des.Mode = CipherMode.CFB;
            des.Padding = PaddingMode.None;
            des.Key = desKey;
            des.IV = iv;

            using var encryptor = des.CreateEncryptor();
            var encrypted = encryptor.TransformFinalBlock(data, 0, data.Length);

            return (encrypted, salt);
        }

        /// <summary>
        /// DES-CFB decryption
        /// </summary>
        private static byte[] DecryptDES(
            byte[] encryptedData,
            byte[] privacyKey,
            byte[] privacyParameters,
            int engineBoots,
            int engineTime)
        {
            if (privacyKey.Length < 16)
                throw new ArgumentException("DES privacy key must be at least 16 bytes");

            if (privacyParameters.Length != 8)
                throw new ArgumentException("DES privacy parameters must be 8 bytes");

            var desKey = privacyKey.Take(8).ToArray();
            var ivSalt = privacyKey.Skip(8).Take(8).ToArray();
            var salt = privacyParameters;

            // IV = ivSalt XOR salt
            var iv = new byte[8];
            for (int i = 0; i < 8; i++)
            {
                iv[i] = (byte)(ivSalt[i] ^ salt[i]);
            }

            using var des = DES.Create();
            des.Mode = CipherMode.CFB;
            des.Padding = PaddingMode.None;
            des.Key = desKey;
            des.IV = iv;

            using var decryptor = des.CreateDecryptor();
            return decryptor.TransformFinalBlock(encryptedData, 0, encryptedData.Length);
        }

        /// <summary>
        /// AES-CFB encryption (RFC 3826)
        /// </summary>
        private static (byte[] encryptedData, byte[] privacyParameters) EncryptAES(
            byte[] data,
            byte[] privacyKey,
            int keySize,
            int engineBoots,
            int engineTime)
        {
            if (privacyKey.Length < keySize)
                throw new ArgumentException($"AES privacy key must be at least {keySize} bytes");

            var aesKey = privacyKey.Take(keySize).ToArray();

            // AES IV = engineBoots + engineTime + salt (16 bytes total)
            var salt = new byte[8];
            RandomNumberGenerator.Fill(salt);

            var iv = new byte[16];
            BitConverter.GetBytes(engineBoots).CopyTo(iv, 0);
            BitConverter.GetBytes(engineTime).CopyTo(iv, 4);
            salt.CopyTo(iv, 8);

            // Encrypt using AES-CFB
            using var aes = Aes.Create();
            aes.Mode = CipherMode.CFB;
            aes.Padding = PaddingMode.None;
            aes.Key = aesKey;
            aes.IV = iv;

            using var encryptor = aes.CreateEncryptor();
            var encrypted = encryptor.TransformFinalBlock(data, 0, data.Length);

            return (encrypted, salt);
        }

        /// <summary>
        /// AES-CFB decryption
        /// </summary>
        private static byte[] DecryptAES(
            byte[] encryptedData,
            byte[] privacyKey,
            byte[] privacyParameters,
            int keySize,
            int engineBoots,
            int engineTime)
        {
            if (privacyKey.Length < keySize)
                throw new ArgumentException($"AES privacy key must be at least {keySize} bytes");

            if (privacyParameters.Length != 8)
                throw new ArgumentException("AES privacy parameters must be 8 bytes (salt)");

            var aesKey = privacyKey.Take(keySize).ToArray();
            var salt = privacyParameters;

            // Reconstruct IV = engineBoots + engineTime + salt
            var iv = new byte[16];
            BitConverter.GetBytes(engineBoots).CopyTo(iv, 0);
            BitConverter.GetBytes(engineTime).CopyTo(iv, 4);
            salt.CopyTo(iv, 8);

            using var aes = Aes.Create();
            aes.Mode = CipherMode.CFB;
            aes.Padding = PaddingMode.None;
            aes.Key = aesKey;
            aes.IV = iv;

            using var decryptor = aes.CreateDecryptor();
            return decryptor.TransformFinalBlock(encryptedData, 0, encryptedData.Length);
        }

        /// <summary>
        /// Get required key size for privacy protocol
        /// </summary>
        public static int GetKeySize(PrivProtocol privProtocol) => privProtocol switch
        {
            PrivProtocol.None => 0,
            PrivProtocol.DES => 16,
            PrivProtocol.AES128 => 16,
            PrivProtocol.AES192 => 24,
            PrivProtocol.AES256 => 32,
            _ => throw new NotSupportedException($"Privacy protocol {privProtocol} not supported")
        };

        /// <summary>
        /// Get privacy parameters size for protocol
        /// </summary>
        public static int GetPrivacyParametersSize(PrivProtocol privProtocol) => privProtocol switch
        {
            PrivProtocol.None => 0,
            PrivProtocol.DES => 8,
            PrivProtocol.AES128 => 8,
            PrivProtocol.AES192 => 8,
            PrivProtocol.AES256 => 8,
            _ => throw new NotSupportedException($"Privacy protocol {privProtocol} not supported")
        };
    }
}