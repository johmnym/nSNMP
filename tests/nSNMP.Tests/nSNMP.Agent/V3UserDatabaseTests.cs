using nSNMP.Agent;
using nSNMP.Security;
using Xunit;

namespace nSNMP.Tests.nSNMP.Agent
{
    public class V3UserDatabaseTests
    {
        private readonly SnmpEngine _engine;
        private readonly V3UserDatabase _database;

        public V3UserDatabaseTests()
        {
            _engine = new SnmpEngine();
            _database = new V3UserDatabase(_engine);
        }

        [Fact]
        public void Constructor_WithNullEngine_ThrowsException()
        {
            Assert.Throws<ArgumentNullException>(() => new V3UserDatabase(null!));
        }

        [Fact]
        public void AddUser_WithValidCredentials_AddsUser()
        {
            var credentials = V3Credentials.AuthNoPriv("testuser", AuthProtocol.SHA1, "password123");

            _database.AddUser(credentials);

            Assert.True(_database.HasUser("testuser"));
            Assert.Equal(1, _database.Count);
        }

        [Fact]
        public void AddUser_WithInvalidCredentials_ThrowsException()
        {
            var invalidCredentials = new V3Credentials("", AuthProtocol.SHA1, "password123");

            Assert.Throws<ArgumentException>(() => _database.AddUser(invalidCredentials));
        }

        [Fact]
        public void GetUser_WithExistingUser_ReturnsUser()
        {
            var credentials = V3Credentials.AuthPriv("testuser", AuthProtocol.MD5, "authpass123",
                                                   PrivProtocol.AES128, "privpass123");
            _database.AddUser(credentials);

            var user = _database.GetUser("testuser");

            Assert.NotNull(user);
            Assert.Equal("testuser", user.UserName);
            Assert.Equal(AuthProtocol.MD5, user.AuthProtocol);
            Assert.Equal(PrivProtocol.AES128, user.PrivProtocol);
            Assert.Equal(SecurityLevel.AuthPriv, user.SecurityLevel);
        }

        [Fact]
        public void GetUser_WithNonExistentUser_ReturnsNull()
        {
            var user = _database.GetUser("nonexistent");
            Assert.Null(user);
        }

        [Fact]
        public void RemoveUser_WithExistingUser_RemovesUser()
        {
            var credentials = V3Credentials.NoAuthNoPriv("testuser");
            _database.AddUser(credentials);

            var removed = _database.RemoveUser("testuser");

            Assert.True(removed);
            Assert.False(_database.HasUser("testuser"));
            Assert.Equal(0, _database.Count);
        }

        [Fact]
        public void RemoveUser_WithNonExistentUser_ReturnsFalse()
        {
            var removed = _database.RemoveUser("nonexistent");
            Assert.False(removed);
        }

        [Fact]
        public void HasUser_WithExistingUser_ReturnsTrue()
        {
            var credentials = V3Credentials.NoAuthNoPriv("testuser");
            _database.AddUser(credentials);

            Assert.True(_database.HasUser("testuser"));
        }

        [Fact]
        public void HasUser_WithNonExistentUser_ReturnsFalse()
        {
            Assert.False(_database.HasUser("nonexistent"));
        }

        [Fact]
        public void GetUserNames_ReturnsAllUsernames()
        {
            _database.AddUser(V3Credentials.NoAuthNoPriv("user1"));
            _database.AddUser(V3Credentials.AuthNoPriv("user2", AuthProtocol.SHA1, "password123"));
            _database.AddUser(V3Credentials.AuthPriv("user3", AuthProtocol.MD5, "authpass123",
                                                    PrivProtocol.DES, "privpass123"));

            var userNames = _database.GetUserNames().ToList();

            Assert.Equal(3, userNames.Count);
            Assert.Contains("user1", userNames);
            Assert.Contains("user2", userNames);
            Assert.Contains("user3", userNames);
        }

        [Fact]
        public void Clear_RemovesAllUsers()
        {
            _database.AddUser(V3Credentials.NoAuthNoPriv("user1"));
            _database.AddUser(V3Credentials.NoAuthNoPriv("user2"));

            _database.Clear();

            Assert.Equal(0, _database.Count);
            Assert.False(_database.HasUser("user1"));
            Assert.False(_database.HasUser("user2"));
        }

        [Fact]
        public void AddUser_LocalizesKeysCorrectly()
        {
            var credentials = V3Credentials.AuthPriv("testuser", AuthProtocol.SHA256, "authpassword123",
                                                   PrivProtocol.AES256, "privpassword123");
            _database.AddUser(credentials);

            var user = _database.GetUser("testuser");
            Assert.NotNull(user);

            // Keys should be localized (not empty)
            Assert.NotEmpty(user.AuthKey);
            Assert.NotEmpty(user.PrivKey);

            // Keys should be proper length for algorithms
            Assert.True(user.AuthKey.Length >= 32); // SHA256 produces 32-byte hash
            Assert.Equal(32, user.PrivKey.Length); // AES256 uses 32-byte key
        }

        [Fact]
        public void V3User_SecurityLevels_CalculatedCorrectly()
        {
            // NoAuthNoPriv
            var credentials1 = V3Credentials.NoAuthNoPriv("user1");
            _database.AddUser(credentials1);
            var user1 = _database.GetUser("user1")!;
            Assert.Equal(SecurityLevel.NoAuthNoPriv, user1.SecurityLevel);
            Assert.False(user1.HasAuth);
            Assert.False(user1.HasPriv);

            // AuthNoPriv
            var credentials2 = V3Credentials.AuthNoPriv("user2", AuthProtocol.MD5, "password123");
            _database.AddUser(credentials2);
            var user2 = _database.GetUser("user2")!;
            Assert.Equal(SecurityLevel.AuthNoPriv, user2.SecurityLevel);
            Assert.True(user2.HasAuth);
            Assert.False(user2.HasPriv);

            // AuthPriv
            var credentials3 = V3Credentials.AuthPriv("user3", AuthProtocol.SHA1, "authpass123",
                                                     PrivProtocol.AES128, "privpass123");
            _database.AddUser(credentials3);
            var user3 = _database.GetUser("user3")!;
            Assert.Equal(SecurityLevel.AuthPriv, user3.SecurityLevel);
            Assert.True(user3.HasAuth);
            Assert.True(user3.HasPriv);
        }

        [Fact]
        public void V3User_AuthenticationMethods_Work()
        {
            var credentials = V3Credentials.AuthNoPriv("testuser", AuthProtocol.SHA1, "testpassword123");
            _database.AddUser(credentials);
            var user = _database.GetUser("testuser")!;

            // Test message and digest
            var testMessage = "test message"u8.ToArray();
            var digest = user.CalculateAuthDigest(testMessage);

            Assert.NotEmpty(digest);
            Assert.Equal(12, digest.Length); // USM uses 12-byte digests

            // Validation should succeed with correct digest
            Assert.True(user.ValidateAuth(testMessage, digest));

            // Validation should fail with wrong digest
            var wrongDigest = new byte[12];
            Assert.False(user.ValidateAuth(testMessage, wrongDigest));
        }

        [Fact]
        public void V3User_ToString_ReturnsUserInfo()
        {
            var credentials = V3Credentials.AuthPriv("testuser", AuthProtocol.SHA256, "authpass123",
                                                   PrivProtocol.AES192, "privpass123");
            _database.AddUser(credentials);
            var user = _database.GetUser("testuser")!;

            var userInfo = user.ToString();

            Assert.Contains("testuser", userInfo);
            Assert.Contains("AuthPriv", userInfo);
        }
    }
}