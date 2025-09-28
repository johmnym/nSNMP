using System.Collections.Concurrent;
using nSNMP.Security;
using nSNMP.SMI.DataTypes.V1.Primitive;

namespace nSNMP.Agent.VACM
{
    /// <summary>
    /// VACM (View-based Access Control Model) processor for SNMP agents
    /// Implements RFC 3415 access control
    /// </summary>
    public class VacmProcessor
    {
        private readonly ConcurrentDictionary<string, VacmGroup> _groups = new();
        private readonly ConcurrentDictionary<string, VacmAccess> _accessEntries = new();
        private readonly ConcurrentDictionary<string, VacmView> _views = new();

        /// <summary>
        /// Add a security-to-group mapping
        /// </summary>
        public void AddGroup(VacmGroup group)
        {
            var key = CreateGroupKey(group.SecurityModel, group.SecurityName);
            _groups[key] = group;
        }

        /// <summary>
        /// Add an access control entry
        /// </summary>
        public void AddAccess(VacmAccess access)
        {
            var key = CreateAccessKey(access.GroupName, access.ContextPrefix, access.SecurityModel, access.SecurityLevel);
            _accessEntries[key] = access;
        }

        /// <summary>
        /// Add a view definition
        /// </summary>
        public void AddView(VacmView view)
        {
            _views[view.ViewName] = view;
        }

        /// <summary>
        /// Check if access is allowed for the given parameters
        /// </summary>
        public VacmResult CheckAccess(
            VacmSecurityModel securityModel,
            string securityName,
            SecurityLevel securityLevel,
            string contextName,
            VacmAccessType accessType,
            ObjectIdentifier oid)
        {
            try
            {
                // Step 1: Find the group for this security principal
                var group = FindGroup(securityModel, securityName);
                if (group == null)
                {
                    return VacmResult.CreateDenied("No group found for security principal");
                }

                // Step 2: Find matching access entry
                var access = FindAccess(group.GroupName, contextName, securityModel, securityLevel);
                if (access == null)
                {
                    return VacmResult.CreateDenied("No access entry found");
                }

                // Step 3: Get the appropriate view for this access type
                var viewName = access.GetView(accessType);
                if (string.IsNullOrEmpty(viewName))
                {
                    return VacmResult.CreateDenied($"No view configured for {accessType} access");
                }

                // Step 4: Check view permissions
                var viewResult = CheckViewAccess(viewName, oid);
                if (!viewResult.IsAllowed)
                {
                    return VacmResult.CreateDenied($"OID {oid.Value} not accessible in view {viewName}");
                }

                return VacmResult.CreateAllowed(group.GroupName, viewName);
            }
            catch (Exception ex)
            {
                return VacmResult.CreateDenied($"VACM error: {ex.Message}");
            }
        }

        /// <summary>
        /// Find group for security principal
        /// </summary>
        private VacmGroup? FindGroup(VacmSecurityModel securityModel, string securityName)
        {
            var key = CreateGroupKey(securityModel, securityName);
            return _groups.TryGetValue(key, out var group) ? group : null;
        }

        /// <summary>
        /// Find access entry for group and context
        /// </summary>
        private VacmAccess? FindAccess(string groupName, string contextName, VacmSecurityModel securityModel, SecurityLevel securityLevel)
        {
            // Try exact match first
            var exactKey = CreateAccessKey(groupName, contextName, securityModel, securityLevel);
            if (_accessEntries.TryGetValue(exactKey, out var exactAccess))
                return exactAccess;

            // Try to find matching entries (considering prefix matching and security level)
            return _accessEntries.Values
                .Where(a => a.Matches(groupName, contextName, securityModel, securityLevel))
                .OrderByDescending(a => a.ContextPrefix.Length) // Prefer more specific context prefixes
                .FirstOrDefault();
        }

        /// <summary>
        /// Check if OID is accessible in the specified view
        /// </summary>
        private VacmResult CheckViewAccess(string viewName, ObjectIdentifier oid)
        {
            if (!_views.TryGetValue(viewName, out var view))
            {
                return VacmResult.CreateDenied($"View {viewName} not found");
            }

            // Check all views with the same name (there can be multiple with different subtrees)
            var matchingViews = _views.Values.Where(v => v.ViewName == viewName).ToList();

            bool isIncluded = false;
            bool isExcluded = false;

            foreach (var v in matchingViews)
            {
                if (v.IsOidIncluded(oid))
                {
                    if (v.ViewType == VacmViewType.Included)
                        isIncluded = true;
                    else if (v.ViewType == VacmViewType.Excluded)
                        isExcluded = true;
                }
            }

            // Excluded views take precedence
            if (isExcluded)
            {
                return VacmResult.CreateDenied($"OID excluded by view {viewName}");
            }

            if (isIncluded)
            {
                return VacmResult.CreateAllowed("", viewName);
            }

            return VacmResult.CreateDenied($"OID not included in view {viewName}");
        }

        /// <summary>
        /// Configure default VACM settings for basic operation
        /// </summary>
        public void ConfigureDefaults()
        {
            // Default groups
            AddGroup(VacmGroup.CreateCommunityGroup("public", "public"));
            AddGroup(VacmGroup.CreateCommunityGroup("private", "private"));

            // Default views
            AddView(VacmView.Create("internet", "1.3.6.1", null, VacmViewType.Included));
            AddView(VacmView.Create("system", "1.3.6.1.2.1.1", null, VacmViewType.Included));
            AddView(VacmView.Create("readonly", "1.3.6.1", null, VacmViewType.Included));

            // Default access entries
            AddAccess(new VacmAccess(
                "public", "", VacmSecurityModel.SNMPv2c, SecurityLevel.NoAuthNoPriv,
                VacmAccessMatch.Exact, "readonly", null, null));

            AddAccess(new VacmAccess(
                "private", "", VacmSecurityModel.SNMPv2c, SecurityLevel.NoAuthNoPriv,
                VacmAccessMatch.Exact, "internet", "internet", null));
        }

        /// <summary>
        /// Get statistics about configured VACM entries
        /// </summary>
        public VacmStatistics GetStatistics()
        {
            return new VacmStatistics(_groups.Count, _accessEntries.Count, _views.Count);
        }

        // Helper methods for creating keys
        private static string CreateGroupKey(VacmSecurityModel securityModel, string securityName)
        {
            return $"{securityModel}:{securityName}";
        }

        private static string CreateAccessKey(string groupName, string contextPrefix, VacmSecurityModel securityModel, SecurityLevel securityLevel)
        {
            return $"{groupName}:{contextPrefix}:{securityModel}:{securityLevel}";
        }
    }

    /// <summary>
    /// Result of VACM access control check
    /// </summary>
    public record VacmResult(bool IsAllowed, string? GroupName = null, string? ViewName = null, string? Reason = null)
    {
        public static VacmResult CreateAllowed(string groupName, string viewName)
        {
            return new VacmResult(true, groupName, viewName);
        }

        public static VacmResult CreateDenied(string reason)
        {
            return new VacmResult(false, Reason: reason);
        }

        public override string ToString()
        {
            return IsAllowed
                ? $"Allowed - Group: {GroupName}, View: {ViewName}"
                : $"Denied - {Reason}";
        }
    }

    /// <summary>
    /// VACM configuration statistics
    /// </summary>
    public record VacmStatistics(int Groups, int AccessEntries, int Views)
    {
        public override string ToString()
        {
            return $"VACM Stats - Groups: {Groups}, Access: {AccessEntries}, Views: {Views}";
        }
    }
}