using nSNMP.Security;
using Xunit;

namespace nSNMP.Tests.nSNMP.Security
{
    public class KeyLocalizationTests
    {
        [Fact]
        public void LocalizeKey_ReturnsEmptyForNoAuth()
        {
            var password = "testpassword";
            var engineId = new byte[] { 0x01, 0x02, 0x03 };

            var key = KeyLocalization.LocalizeKey(password, engineId, AuthProtocol.None);

            Assert.Empty(key);
        }

        [Fact]
        public void LocalizeKey_SHA1_ProducesCorrectLength()
        {
            var password = "testpassword";
            var engineId = new byte[] { 0x01, 0x02, 0x03 };

            var key = KeyLocalization.LocalizeKey(password, engineId, AuthProtocol.SHA1);

            Assert.Equal(20, key.Length); // SHA1 produces 20 bytes
        }

        [Fact]
        public void LocalizeKey_SHA256_ProducesCorrectLength()
        {
            var password = "testpassword";
            var engineId = new byte[] { 0x01, 0x02, 0x03 };

            var key = KeyLocalization.LocalizeKey(password, engineId, AuthProtocol.SHA256);

            Assert.Equal(32, key.Length); // SHA256 produces 32 bytes
        }

        [Fact]
        public void LocalizeKey_MD5_ProducesCorrectLength()
        {
            var password = "testpassword";
            var engineId = new byte[] { 0x01, 0x02, 0x03 };

            var key = KeyLocalization.LocalizeKey(password, engineId, AuthProtocol.MD5);

            Assert.Equal(16, key.Length); // MD5 produces 16 bytes
        }

        [Fact]
        public void LocalizeKey_DifferentEngineIds_ProduceDifferentKeys()
        {
            var password = "testpassword";
            var engineId1 = new byte[] { 0x01, 0x02, 0x03 };
            var engineId2 = new byte[] { 0x04, 0x05, 0x06 };

            var key1 = KeyLocalization.LocalizeKey(password, engineId1, AuthProtocol.SHA1);
            var key2 = KeyLocalization.LocalizeKey(password, engineId2, AuthProtocol.SHA1);

            Assert.NotEqual(key1, key2);
        }

        [Fact]
        public void LocalizeKey_SameInputs_ProduceSameKey()
        {
            var password = "testpassword";
            var engineId = new byte[] { 0x01, 0x02, 0x03 };

            var key1 = KeyLocalization.LocalizeKey(password, engineId, AuthProtocol.SHA1);
            var key2 = KeyLocalization.LocalizeKey(password, engineId, AuthProtocol.SHA1);

            Assert.Equal(key1, key2);
        }

        [Fact]
        public void CalculateDigest_ProducesCorrectLength()
        {
            var message = "test message"u8.ToArray();
            var key = new byte[20]; // SHA1 key

            var digest = KeyLocalization.CalculateDigest(message, key, AuthProtocol.SHA1);

            Assert.Equal(12, digest.Length); // USM uses first 12 bytes
        }

        [Fact]
        public void CalculateDigest_ReturnsEmptyForNoAuth()
        {
            var message = "test message"u8.ToArray();
            var key = Array.Empty<byte>();

            var digest = KeyLocalization.CalculateDigest(message, key, AuthProtocol.None);

            Assert.Empty(digest);
        }

        [Fact]
        public void VerifyDigest_ValidatesCorrectly()
        {
            var message = "test message"u8.ToArray();
            var key = new byte[32]; // SHA256 key
            Array.Fill<byte>(key, 0x42);

            var digest = KeyLocalization.CalculateDigest(message, key, AuthProtocol.SHA256);
            var isValid = KeyLocalization.VerifyDigest(message, key, digest, AuthProtocol.SHA256);

            Assert.True(isValid);
        }

        [Fact]
        public void VerifyDigest_RejectsIncorrectDigest()
        {
            var message = "test message"u8.ToArray();
            var key = new byte[32];
            Array.Fill<byte>(key, 0x42);
            var wrongDigest = new byte[12];

            var isValid = KeyLocalization.VerifyDigest(message, key, wrongDigest, AuthProtocol.SHA256);

            Assert.False(isValid);
        }

        [Fact]
        public void GetDigestLength_ReturnsCorrectLengths()
        {
            Assert.Equal(0, KeyLocalization.GetDigestLength(AuthProtocol.None));
            Assert.Equal(16, KeyLocalization.GetDigestLength(AuthProtocol.MD5));
            Assert.Equal(20, KeyLocalization.GetDigestLength(AuthProtocol.SHA1));
            Assert.Equal(32, KeyLocalization.GetDigestLength(AuthProtocol.SHA256));
            Assert.Equal(48, KeyLocalization.GetDigestLength(AuthProtocol.SHA384));
            Assert.Equal(64, KeyLocalization.GetDigestLength(AuthProtocol.SHA512));
        }

        [Theory]
        [InlineData(AuthProtocol.MD5)]
        [InlineData(AuthProtocol.SHA1)]
        [InlineData(AuthProtocol.SHA256)]
        [InlineData(AuthProtocol.SHA384)]
        [InlineData(AuthProtocol.SHA512)]
        public void LocalizeKey_SupportedProtocols_WorkWithoutException(AuthProtocol protocol)
        {
            var password = "testpassword";
            var engineId = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 };

            // Should not throw
            var key = KeyLocalization.LocalizeKey(password, engineId, protocol);
            Assert.NotEmpty(key);
        }
    }
}