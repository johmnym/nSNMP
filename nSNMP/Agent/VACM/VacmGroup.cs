using nSNMP.Security;

namespace nSNMP.Agent.VACM
{
    /// <summary>
    /// VACM Group definition for grouping security principals
    /// </summary>
    public record VacmGroup(
        string GroupName,
        VacmSecurityModel SecurityModel,
        string SecurityName)
    {
        /// <summary>
        /// Check if this group matches the given security parameters
        /// </summary>
        public bool Matches(VacmSecurityModel securityModel, string securityName)
        {
            return SecurityModel == securityModel && SecurityName == securityName;
        }

        /// <summary>
        /// Create group for community string (v1/v2c)
        /// </summary>
        public static VacmGroup CreateCommunityGroup(string groupName, string communityString)
        {
            return new VacmGroup(groupName, VacmSecurityModel.SNMPv2c, communityString);
        }

        /// <summary>
        /// Create group for SNMPv3 user
        /// </summary>
        public static VacmGroup CreateUserGroup(string groupName, string userName)
        {
            return new VacmGroup(groupName, VacmSecurityModel.USM, userName);
        }

        public override string ToString()
        {
            return $"Group: {GroupName}, Model: {SecurityModel}, Name: {SecurityName}";
        }
    }

    /// <summary>
    /// VACM Access entry for permission control
    /// </summary>
    public record VacmAccess(
        string GroupName,
        string ContextPrefix,
        VacmSecurityModel SecurityModel,
        SecurityLevel SecurityLevel,
        VacmAccessMatch ContextMatch,
        string? ReadView = null,
        string? WriteView = null,
        string? NotifyView = null)
    {
        /// <summary>
        /// Check if this access entry matches the request parameters
        /// </summary>
        public bool Matches(string groupName, string contextName, VacmSecurityModel securityModel, SecurityLevel securityLevel)
        {
            if (GroupName != groupName || SecurityModel != securityModel)
                return false;

            if (SecurityLevel > securityLevel) // Request must meet minimum security level
                return false;

            return ContextMatch switch
            {
                VacmAccessMatch.Exact => ContextPrefix == contextName,
                VacmAccessMatch.Prefix => contextName.StartsWith(ContextPrefix),
                _ => false
            };
        }

        /// <summary>
        /// Get the view name for the specified access type
        /// </summary>
        public string? GetView(VacmAccessType accessType)
        {
            return accessType switch
            {
                VacmAccessType.Read => ReadView,
                VacmAccessType.Write => WriteView,
                VacmAccessType.Notify => NotifyView,
                _ => null
            };
        }

        public override string ToString()
        {
            return $"Access: {GroupName}, Context: {ContextPrefix} ({ContextMatch}), Model: {SecurityModel}, Level: {SecurityLevel}";
        }
    }

    /// <summary>
    /// VACM security models
    /// </summary>
    public enum VacmSecurityModel
    {
        Any = 0,
        SNMPv1 = 1,
        SNMPv2c = 2,
        USM = 3
    }

    /// <summary>
    /// VACM context matching modes
    /// </summary>
    public enum VacmAccessMatch
    {
        Exact = 1,
        Prefix = 2
    }

    /// <summary>
    /// VACM access types
    /// </summary>
    public enum VacmAccessType
    {
        Read,
        Write,
        Notify
    }
}