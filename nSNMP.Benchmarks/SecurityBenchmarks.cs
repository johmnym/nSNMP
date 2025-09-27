using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using nSNMP.Security;
using nSNMP.SMI.DataTypes.V1.Primitive;
using nSNMP.SMI.PDUs;
using System.Security.Cryptography;

namespace nSNMP.Benchmarks
{
    /// <summary>
    /// Benchmarks for USM security operations and crypto performance
    /// </summary>
    [SimpleJob(RuntimeMoniker.Net90)]
    [MemoryDiagnoser]
    [MinColumn, MaxColumn, MeanColumn, MedianColumn]
    public class SecurityBenchmarks
    {
        private V3Credentials _credentials = null!;
        private byte[] _engineId = null!;
        private byte[] _testData = null!;
        private byte[] _testKey = null!;
        private byte[] _authKey = null!;
        private byte[] _privKey = null!;
        private GetRequest _testPdu = null!;
        private byte[] _testMessage = null!;

        [GlobalSetup]
        public void Setup()
        {
            // Create test credentials
            _credentials = V3Credentials.AuthNoPriv("testuser", AuthProtocol.SHA256, "authpassword12345678");
            _engineId = new byte[] { 0x80, 0x00, 0x13, 0x70, 0x01, 0x02, 0x03, 0x04 };

            // Test data for encryption/decryption
            _testData = System.Text.Encoding.UTF8.GetBytes("This is test data for encryption benchmarks. It should be long enough to measure meaningful performance.");
            _testKey = new byte[32]; // 256-bit key
            Random.Shared.NextBytes(_testKey);

            // Pre-compute localized keys
            _authKey = _credentials.GetAuthKey(_engineId);
            _privKey = _credentials.GetPrivKey(_engineId);

            // Create test PDU and message
            _testPdu = new GetRequest(
                null,
                Integer.Create(12345),
                Integer.Create(0),
                Integer.Create(0),
                new nSNMP.SMI.DataTypes.V1.Constructed.Sequence(Array.Empty<nSNMP.SMI.DataTypes.IDataType>())
            );
            _testMessage = _testPdu.ToBytes();
        }

        [Benchmark]
        public byte[] KeyLocalizationMD5()
        {
            return KeyLocalization.LocalizeKey("password12345678", _engineId, AuthProtocol.MD5);
        }

        [Benchmark]
        public byte[] KeyLocalizationSHA1()
        {
            return KeyLocalization.LocalizeKey("password12345678", _engineId, AuthProtocol.SHA1);
        }

        [Benchmark]
        public byte[] KeyLocalizationSHA256()
        {
            return KeyLocalization.LocalizeKey("password12345678", _engineId, AuthProtocol.SHA256);
        }

        [Benchmark]
        public byte[] AuthenticationMD5()
        {
            return KeyLocalization.CalculateDigest(_testMessage, _authKey, AuthProtocol.MD5);
        }

        [Benchmark]
        public byte[] AuthenticationSHA1()
        {
            return KeyLocalization.CalculateDigest(_testMessage, _authKey, AuthProtocol.SHA1);
        }

        [Benchmark]
        public byte[] AuthenticationSHA256()
        {
            return KeyLocalization.CalculateDigest(_testMessage, _authKey, AuthProtocol.SHA256);
        }

        [Benchmark]
        public (byte[], byte[]) EncryptionDES()
        {
            return PrivacyProvider.Encrypt(_testData, _privKey, PrivProtocol.DES, 1, 1);
        }

        [Benchmark]
        public (byte[], byte[]) EncryptionAES128()
        {
            return PrivacyProvider.Encrypt(_testData, _privKey, PrivProtocol.AES128, 1, 1);
        }

        [Benchmark]
        public (byte[], byte[]) EncryptionAES256()
        {
            return PrivacyProvider.Encrypt(_testData, _privKey, PrivProtocol.AES256, 1, 1);
        }

        [Benchmark]
        public byte[] DecryptionDES()
        {
            var (encrypted, salt) = PrivacyProvider.Encrypt(_testData, _privKey, PrivProtocol.DES, 1, 1);
            return PrivacyProvider.Decrypt(encrypted, _privKey, salt, PrivProtocol.DES, 1, 1);
        }

        [Benchmark]
        public byte[] DecryptionAES128()
        {
            var (encrypted, salt) = PrivacyProvider.Encrypt(_testData, _privKey, PrivProtocol.AES128, 1, 1);
            return PrivacyProvider.Decrypt(encrypted, _privKey, salt, PrivProtocol.AES128, 1, 1);
        }

        [Benchmark]
        public byte[] DecryptionAES256()
        {
            var (encrypted, salt) = PrivacyProvider.Encrypt(_testData, _privKey, PrivProtocol.AES256, 1, 1);
            return PrivacyProvider.Decrypt(encrypted, _privKey, salt, PrivProtocol.AES256, 1, 1);
        }

        /// <summary>
        /// End-to-end USM message processing benchmark
        /// </summary>
        [Benchmark]
        public UsmSecurityParameters UsmParameterCreation()
        {
            return UsmSecurityParameters.Create(
                Convert.ToHexString(_engineId),
                1,
                123456,
                "testuser",
                new byte[12],
                new byte[8]
            );
        }

        /// <summary>
        /// Random number generation for cryptographic operations
        /// </summary>
        [Benchmark]
        public byte[] CryptoRandomBytes()
        {
            var buffer = new byte[16];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(buffer);
            return buffer;
        }

        /// <summary>
        /// Memory-intensive key stretching benchmark
        /// </summary>
        [Benchmark]
        public byte[] KeyStretching()
        {
            const string password = "testpassword12345678";
            var passwordBytes = System.Text.Encoding.UTF8.GetBytes(password);

            // Key stretching similar to what's done in key localization
            var md5 = MD5.Create();
            var hash = passwordBytes;

            for (int i = 0; i < 1048576; i++) // 1MB of hashing
            {
                hash = md5.ComputeHash(hash);
            }

            return hash;
        }
    }
}