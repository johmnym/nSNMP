using nSNMP.SMI.DataTypes.V1.Primitive;

namespace nSNMP.Agent.VACM
{
    /// <summary>
    /// VACM View definition for OID subtree access control
    /// </summary>
    public record VacmView(
        string ViewName,
        ObjectIdentifier Subtree,
        byte[]? Mask = null,
        VacmViewType ViewType = VacmViewType.Included)
    {
        /// <summary>
        /// Check if an OID is included in this view
        /// </summary>
        public bool IsOidIncluded(ObjectIdentifier oid)
        {
            if (!IsOidInSubtree(oid))
                return false;

            return ViewType == VacmViewType.Included;
        }

        /// <summary>
        /// Check if OID is within the subtree (considering mask if present)
        /// </summary>
        private bool IsOidInSubtree(ObjectIdentifier oid)
        {
            var oidElements = oid.Value;
            var subtreeElements = Subtree.Value;

            // OID must be at least as long as subtree
            if (oidElements.Length < subtreeElements.Length)
                return false;

            // Check each element of subtree
            for (int i = 0; i < subtreeElements.Length; i++)
            {
                if (Mask != null && i < Mask.Length * 8)
                {
                    // Check if this bit is masked
                    int byteIndex = i / 8;
                    int bitIndex = 7 - (i % 8); // MSB first
                    bool isMasked = (Mask[byteIndex] & (1 << bitIndex)) != 0;

                    if (!isMasked)
                        continue; // Skip comparison for masked bits
                }

                if (oidElements[i] != subtreeElements[i])
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Create view with string OID
        /// </summary>
        public static VacmView Create(string viewName, string subtreeOid, byte[]? mask = null, VacmViewType viewType = VacmViewType.Included)
        {
            return new VacmView(viewName, ObjectIdentifier.Create(subtreeOid), mask, viewType);
        }

        public override string ToString()
        {
            var maskStr = Mask != null ? $", Mask: {Convert.ToHexString(Mask)}" : "";
            return $"View: {ViewName}, Subtree: {Subtree.Value}, Type: {ViewType}{maskStr}";
        }
    }

    /// <summary>
    /// VACM view type (included or excluded)
    /// </summary>
    public enum VacmViewType
    {
        Included = 1,
        Excluded = 2
    }
}