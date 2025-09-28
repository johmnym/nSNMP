using nSNMP.Security;
using Xunit;

namespace nSNMP.Core.Tests
{
    public class V3CredentialsTests
    {
        [Fact]
        public void NoAuthNoPriv_CreatesCorrectSecurityLevel()
        {
            var credentials = V3Credentials.NoAuthNoPriv("testuser");

            Assert.Equal("testuser", credentials.UserName);
            Assert.Equal(AuthProtocol.None, credentials.AuthProtocol);
            Assert.Equal(PrivProtocol.None, credentials.PrivProtocol);
            Assert.Equal(SecurityLevel.NoAuthNoPriv, credentials.SecurityLevel);
        }

        [Fact]
        public void AuthNoPriv_CreatesCorrectSecurityLevel()
        {
            var credentials = V3Credentials.AuthNoPriv("testuser", AuthProtocol.SHA1, "authpassword");

            Assert.Equal("testuser", credentials.UserName);
            Assert.Equal(AuthProtocol.SHA1, credentials.AuthProtocol);
            Assert.Equal("authpassword", credentials.AuthPassword);
            Assert.Equal(PrivProtocol.None, credentials.PrivProtocol);
            Assert.Equal(SecurityLevel.AuthNoPriv, credentials.SecurityLevel);
        }

        [Fact]
        public void AuthPriv_CreatesCorrectSecurityLevel()
        {
            var credentials = V3Credentials.AuthPriv("testuser", AuthProtocol.SHA256, "authpassword", PrivProtocol.AES128, "privpassword");

            Assert.Equal("testuser", credentials.UserName);
            Assert.Equal(AuthProtocol.SHA256, credentials.AuthProtocol);
            Assert.Equal("authpassword", credentials.AuthPassword);
            Assert.Equal(PrivProtocol.AES128, credentials.PrivProtocol);
            Assert.Equal("privpassword", credentials.PrivPassword);
            Assert.Equal(SecurityLevel.AuthPriv, credentials.SecurityLevel);
        }

        [Fact]
        public void Validate_ThrowsForEmptyUsername()
        {
            var credentials = new V3Credentials("");

            Assert.Throws<ArgumentException>(() => credentials.Validate());
        }

        [Fact]
        public void Validate_ThrowsForAuthWithoutPassword()
        {
            var credentials = new V3Credentials("testuser", AuthProtocol.SHA1, "");

            Assert.Throws<ArgumentException>(() => credentials.Validate());
        }

        [Fact]
        public void Validate_ThrowsForPrivWithoutAuth()
        {
            var credentials = new V3Credentials("testuser", AuthProtocol.None, "", PrivProtocol.AES128, "privpassword");

            Assert.Throws<ArgumentException>(() => credentials.Validate());
        }

        [Fact]
        public void Validate_ThrowsForPrivWithoutPassword()
        {
            var credentials = new V3Credentials("testuser", AuthProtocol.SHA1, "authpassword", PrivProtocol.AES128, "");

            Assert.Throws<ArgumentException>(() => credentials.Validate());
        }

        [Fact]
        public void Validate_ThrowsForShortPasswords()
        {
            var credentials1 = new V3Credentials("testuser", AuthProtocol.SHA1, "short");
            var credentials2 = new V3Credentials("testuser", AuthProtocol.SHA1, "authpassword", PrivProtocol.AES128, "short");

            Assert.Throws<ArgumentException>(() => credentials1.Validate());
            Assert.Throws<ArgumentException>(() => credentials2.Validate());
        }

        [Fact]
        public void Validate_PassesForValidCredentials()
        {
            var credentials1 = V3Credentials.NoAuthNoPriv("testuser");
            var credentials2 = V3Credentials.AuthNoPriv("testuser", AuthProtocol.SHA1, "authpassword");
            var credentials3 = V3Credentials.AuthPriv("testuser", AuthProtocol.SHA256, "authpassword", PrivProtocol.AES128, "privpassword");

            // Should not throw
            credentials1.Validate();
            credentials2.Validate();
            credentials3.Validate();
        }

        [Fact]
        public void GetAuthKey_ReturnsEmptyForNoAuth()
        {
            var credentials = V3Credentials.NoAuthNoPriv("testuser");
            var engineId = new byte[] { 0x01, 0x02, 0x03 };

            var authKey = credentials.GetAuthKey(engineId);

            Assert.Empty(authKey);
        }

        [Fact]
        public void GetAuthKey_ReturnsKeyForAuth()
        {
            var credentials = V3Credentials.AuthNoPriv("testuser", AuthProtocol.SHA1, "authpassword");
            var engineId = new byte[] { 0x01, 0x02, 0x03 };

            var authKey = credentials.GetAuthKey(engineId);

            Assert.NotEmpty(authKey);
            Assert.Equal(20, authKey.Length); // SHA1 produces 20 bytes
        }

        [Fact]
        public void GetPrivKey_ReturnsEmptyForNoPriv()
        {
            var credentials = V3Credentials.AuthNoPriv("testuser", AuthProtocol.SHA1, "authpassword");
            var engineId = new byte[] { 0x01, 0x02, 0x03 };

            var privKey = credentials.GetPrivKey(engineId);

            Assert.Empty(privKey);
        }

        [Fact]
        public void GetPrivKey_ReturnsKeyForPriv()
        {
            var credentials = V3Credentials.AuthPriv("testuser", AuthProtocol.SHA256, "authpassword", PrivProtocol.AES128, "privpassword");
            var engineId = new byte[] { 0x01, 0x02, 0x03 };

            var privKey = credentials.GetPrivKey(engineId);

            Assert.NotEmpty(privKey);
            Assert.Equal(16, privKey.Length); // AES128 uses 16 bytes
        }
    }
}