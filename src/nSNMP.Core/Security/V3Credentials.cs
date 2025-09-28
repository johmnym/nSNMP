using System.Security.Cryptography;

namespace nSNMP.Security
{
    /// <summary>
    /// SNMPv3 User credentials with authentication and privacy settings
    /// </summary>
    public record V3Credentials(
        string UserName,
        AuthProtocol AuthProtocol = AuthProtocol.None,
        string AuthPassword = "",
        PrivProtocol PrivProtocol = PrivProtocol.None,
        string PrivPassword = "")
    {
        /// <summary>
        /// Security level based on auth/priv configuration
        /// </summary>
        public SecurityLevel SecurityLevel => (AuthProtocol, PrivProtocol) switch
        {
            (AuthProtocol.None, _) => SecurityLevel.NoAuthNoPriv,
            (_, PrivProtocol.None) => SecurityLevel.AuthNoPriv,
            _ => SecurityLevel.AuthPriv
        };

        /// <summary>
        /// Create credentials for noAuthNoPriv
        /// </summary>
        public static V3Credentials NoAuthNoPriv(string userName) => new(userName);

        /// <summary>
        /// Create credentials for authNoPriv
        /// </summary>
        public static V3Credentials AuthNoPriv(string userName, AuthProtocol authProtocol, string authPassword) =>
            new(userName, authProtocol, authPassword);

        /// <summary>
        /// Create credentials for authPriv
        /// </summary>
        public static V3Credentials AuthPriv(string userName, AuthProtocol authProtocol, string authPassword,
            PrivProtocol privProtocol, string privPassword) =>
            new(userName, authProtocol, authPassword, privProtocol, privPassword);

        /// <summary>
        /// Generate authentication key using key localization algorithm
        /// </summary>
        public byte[] GetAuthKey(byte[] engineId)
        {
            if (AuthProtocol == AuthProtocol.None)
                return Array.Empty<byte>();

            return KeyLocalization.LocalizeKey(AuthPassword, engineId, AuthProtocol);
        }

        /// <summary>
        /// Generate privacy key using key localization algorithm
        /// </summary>
        public byte[] GetPrivKey(byte[] engineId)
        {
            if (PrivProtocol == PrivProtocol.None)
                return Array.Empty<byte>();

            return KeyLocalization.LocalizeKey(PrivPassword, engineId, AuthProtocol, PrivProtocol);
        }

        /// <summary>
        /// Validate credential configuration
        /// </summary>
        public void Validate()
        {
            if (string.IsNullOrEmpty(UserName))
                throw new ArgumentException("Username is required");

            if (AuthProtocol != AuthProtocol.None && string.IsNullOrEmpty(AuthPassword))
                throw new ArgumentException("Authentication password is required when auth protocol is specified");

            if (PrivProtocol != PrivProtocol.None && AuthProtocol == AuthProtocol.None)
                throw new ArgumentException("Authentication is required when privacy protocol is specified");

            if (PrivProtocol != PrivProtocol.None && string.IsNullOrEmpty(PrivPassword))
                throw new ArgumentException("Privacy password is required when privacy protocol is specified");

            // Validate password lengths
            if (AuthProtocol != AuthProtocol.None && AuthPassword.Length < 8)
                throw new ArgumentException("Authentication password must be at least 8 characters");

            if (PrivProtocol != PrivProtocol.None && PrivPassword.Length < 8)
                throw new ArgumentException("Privacy password must be at least 8 characters");
        }
    }

    /// <summary>
    /// Authentication protocols supported by USM
    /// </summary>
    public enum AuthProtocol
    {
        None,
        MD5,
        SHA1,
        SHA224,
        SHA256,
        SHA384,
        SHA512
    }

    /// <summary>
    /// Privacy protocols supported by USM
    /// </summary>
    public enum PrivProtocol
    {
        None,
        DES,
        AES128,
        AES192,
        AES256
    }

    /// <summary>
    /// Security levels for SNMPv3
    /// </summary>
    public enum SecurityLevel
    {
        NoAuthNoPriv,
        AuthNoPriv,
        AuthPriv
    }
}