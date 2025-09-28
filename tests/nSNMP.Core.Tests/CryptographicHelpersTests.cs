using nSNMP.Security;
using Xunit;

namespace nSNMP.Core.Tests
{
    public class CryptographicHelpersTests
    {
        [Fact]
        public void SecureClear_ShouldZeroByteArray()
        {
            // Arrange
            var sensitiveData = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 };
            var originalData = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 };

            // Act
            CryptographicHelpers.SecureClear(sensitiveData);

            // Assert
            Assert.All(sensitiveData, b => Assert.Equal(0, b));
            Assert.NotEqual(originalData, sensitiveData);
        }

        [Fact]
        public void SecureClear_WithSpan_ShouldZeroData()
        {
            // Arrange
            var sensitiveData = new byte[] { 0xFF, 0xEE, 0xDD, 0xCC };

            // Act
            CryptographicHelpers.SecureClear(sensitiveData.AsSpan());

            // Assert
            Assert.All(sensitiveData, b => Assert.Equal(0, b));
        }

        [Fact]
        public void SecureByteArray_DisposeShouldClearData()
        {
            // Arrange
            var originalData = new byte[] { 0x12, 0x34, 0x56, 0x78 };
            var dataToSecure = new byte[] { 0x12, 0x34, 0x56, 0x78 };

            // Act
            SecureByteArray secureArray;
            using (secureArray = CryptographicHelpers.CreateSecureByteArray(dataToSecure))
            {
                // Verify data is accessible while not disposed
                Assert.Equal(originalData, secureArray.Data);
                Assert.Equal(4, secureArray.Length);
                Assert.False(secureArray.IsDisposed);
            }

            // Assert - data should be cleared after disposal
            Assert.All(dataToSecure, b => Assert.Equal(0, b));
            Assert.True(secureArray.IsDisposed);
            Assert.Equal(0, secureArray.Length);
        }

        [Fact]
        public void SecureByteArray_AccessAfterDispose_ShouldThrow()
        {
            // Arrange
            var data = new byte[] { 0x01, 0x02, 0x03 };
            var secureArray = CryptographicHelpers.CreateSecureByteArray(data);

            // Act
            secureArray.Dispose();

            // Assert
            Assert.Throws<ObjectDisposedException>(() => secureArray.Data);
            Assert.Throws<ObjectDisposedException>(() => secureArray.AsSpan());
            Assert.Throws<ObjectDisposedException>(() => secureArray.AsReadOnlySpan());
            Assert.Throws<ObjectDisposedException>(() => secureArray.ToArray());
        }

        [Fact]
        public void SecureCopy_ShouldCopyAndClearOriginal()
        {
            // Arrange
            var original = new byte[] { 0xAA, 0xBB, 0xCC };
            var expectedCopy = new byte[] { 0xAA, 0xBB, 0xCC };

            // Act
            var copy = CryptographicHelpers.SecureCopy(original);

            // Assert
            Assert.Equal(expectedCopy, copy);
            Assert.All(original, b => Assert.Equal(0, b)); // Original should be cleared
        }

        [Fact]
        public void SecureStringToBytes_ShouldConvertAndWrapSecurely()
        {
            // Arrange
            var testString = "TestPassword123";
            var expectedBytes = System.Text.Encoding.UTF8.GetBytes(testString);

            // Act
            using var secureBytes = CryptographicHelpers.SecureStringToBytes(testString);

            // Assert
            Assert.Equal(expectedBytes, secureBytes.Data);
            Assert.Equal(expectedBytes.Length, secureBytes.Length);
            Assert.False(secureBytes.IsDisposed);
        }

        [Fact]
        public void ConstantTimeEquals_ShouldWorkCorrectly()
        {
            // Arrange
            var data1 = new byte[] { 0x01, 0x02, 0x03 };
            var data2 = new byte[] { 0x01, 0x02, 0x03 };
            var data3 = new byte[] { 0x01, 0x02, 0x04 };
            var data4 = new byte[] { 0x01, 0x02 }; // Different length

            // Act & Assert
            Assert.True(CryptographicHelpers.ConstantTimeEquals(data1, data2));
            Assert.False(CryptographicHelpers.ConstantTimeEquals(data1, data3));
            Assert.False(CryptographicHelpers.ConstantTimeEquals(data1, data4));
        }

        [Fact]
        public void ConstantTimeHashEquals_ShouldWorkCorrectly()
        {
            // Arrange
            var hash1 = new byte[] { 0x12, 0x34, 0x56, 0x78 };
            var hash2 = new byte[] { 0x12, 0x34, 0x56, 0x78 };
            var hash3 = new byte[] { 0x12, 0x34, 0x56, 0x79 };
            var hash4 = new byte[] { 0x12, 0x34, 0x56 }; // Different length

            // Act & Assert
            Assert.True(CryptographicHelpers.ConstantTimeHashEquals(hash1, hash2));
            Assert.False(CryptographicHelpers.ConstantTimeHashEquals(hash1, hash3));
            Assert.False(CryptographicHelpers.ConstantTimeHashEquals(hash1, hash4));
        }
    }
}