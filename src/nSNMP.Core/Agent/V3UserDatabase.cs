using System.Collections.Concurrent;
using nSNMP.Security;

namespace nSNMP.Agent
{
    /// <summary>
    /// User database for SNMPv3 agent with localized keys
    /// </summary>
    public class V3UserDatabase
    {
        private readonly ConcurrentDictionary<string, V3User> _users = new();
        private readonly SnmpEngine _engine;

        public V3UserDatabase(SnmpEngine engine)
        {
            _engine = engine ?? throw new ArgumentNullException(nameof(engine));
        }

        /// <summary>
        /// Add user with credentials (keys will be localized automatically)
        /// </summary>
        public void AddUser(V3Credentials credentials)
        {
            credentials.Validate();

            var authKey = credentials.GetAuthKey(_engine.EngineId);
            var privKey = credentials.GetPrivKey(_engine.EngineId);

            var user = new V3User(
                credentials.UserName,
                credentials.AuthProtocol,
                authKey,
                credentials.PrivProtocol,
                privKey
            );

            _users[credentials.UserName] = user;
        }

        /// <summary>
        /// Get user by username
        /// </summary>
        public V3User? GetUser(string userName)
        {
            _users.TryGetValue(userName, out var user);
            return user;
        }

        /// <summary>
        /// Remove user
        /// </summary>
        public bool RemoveUser(string userName)
        {
            return _users.TryRemove(userName, out _);
        }

        /// <summary>
        /// Check if user exists
        /// </summary>
        public bool HasUser(string userName)
        {
            return _users.ContainsKey(userName);
        }

        /// <summary>
        /// Get all usernames
        /// </summary>
        public IEnumerable<string> GetUserNames()
        {
            return _users.Keys;
        }

        /// <summary>
        /// Clear all users
        /// </summary>
        public void Clear()
        {
            _users.Clear();
        }

        /// <summary>
        /// Get user count
        /// </summary>
        public int Count => _users.Count;
    }

    /// <summary>
    /// SNMPv3 user with localized keys
    /// </summary>
    public record V3User(
        string UserName,
        AuthProtocol AuthProtocol,
        byte[] AuthKey,
        PrivProtocol PrivProtocol,
        byte[] PrivKey)
    {
        /// <summary>
        /// Security level for this user
        /// </summary>
        public SecurityLevel SecurityLevel => (AuthProtocol, PrivProtocol) switch
        {
            (AuthProtocol.None, _) => SecurityLevel.NoAuthNoPriv,
            (_, PrivProtocol.None) => SecurityLevel.AuthNoPriv,
            _ => SecurityLevel.AuthPriv
        };

        /// <summary>
        /// Check if user has authentication enabled
        /// </summary>
        public bool HasAuth => AuthProtocol != AuthProtocol.None;

        /// <summary>
        /// Check if user has privacy enabled
        /// </summary>
        public bool HasPriv => PrivProtocol != PrivProtocol.None;

        /// <summary>
        /// Validate authentication parameters
        /// </summary>
        public bool ValidateAuth(byte[] message, byte[] providedDigest)
        {
            if (!HasAuth)
                return providedDigest.Length == 0;

            return KeyLocalization.VerifyDigest(message, AuthKey, providedDigest, AuthProtocol);
        }

        /// <summary>
        /// Calculate authentication digest for response
        /// </summary>
        public byte[] CalculateAuthDigest(byte[] message)
        {
            if (!HasAuth)
                return Array.Empty<byte>();

            return KeyLocalization.CalculateDigest(message, AuthKey, AuthProtocol);
        }

        public override string ToString()
        {
            return $"User: {UserName}, Security: {SecurityLevel}";
        }
    }
}