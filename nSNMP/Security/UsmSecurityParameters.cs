using nSNMP.SMI.DataTypes;
using nSNMP.SMI.DataTypes.V1.Constructed;
using nSNMP.SMI.DataTypes.V1.Primitive;

namespace nSNMP.Security
{
    /// <summary>
    /// User-based Security Model (USM) security parameters for SNMPv3
    /// </summary>
    public record UsmSecurityParameters(
        OctetString AuthoritativeEngineId,
        Integer AuthoritativeEngineBoots,
        Integer AuthoritativeEngineTime,
        OctetString UserName,
        OctetString AuthenticationParameters,
        OctetString PrivacyParameters)
    {
        /// <summary>
        /// Create USM parameters with default values
        /// </summary>
        public static UsmSecurityParameters Create(
            string authoritativeEngineId = "",
            int authoritativeEngineBoots = 0,
            int authoritativeEngineTime = 0,
            string userName = "",
            byte[] authenticationParameters = null,
            byte[] privacyParameters = null)
        {
            return new UsmSecurityParameters(
                OctetString.Create(authoritativeEngineId),
                Integer.Create(authoritativeEngineBoots),
                Integer.Create(authoritativeEngineTime),
                OctetString.Create(userName),
                new OctetString(authenticationParameters ?? Array.Empty<byte>()),
                new OctetString(privacyParameters ?? Array.Empty<byte>())
            );
        }

        /// <summary>
        /// Serialize to bytes for embedding in SNMPv3 message
        /// </summary>
        public byte[] ToBytes()
        {
            var sequence = new Sequence(new IDataType[]
            {
                AuthoritativeEngineId,
                AuthoritativeEngineBoots,
                AuthoritativeEngineTime,
                UserName,
                AuthenticationParameters,
                PrivacyParameters
            });

            return sequence.ToBytes();
        }

        /// <summary>
        /// Parse USM parameters from bytes
        /// </summary>
        public static UsmSecurityParameters Parse(byte[] data)
        {
            if (data == null || data.Length == 0)
            {
                return Create(); // Empty parameters for discovery
            }

            var sequence = (Sequence)nSNMP.SMI.SMIDataFactory.Create(data);

            return new UsmSecurityParameters(
                (OctetString)sequence.Elements[0],
                (Integer)sequence.Elements[1],
                (Integer)sequence.Elements[2],
                (OctetString)sequence.Elements[3],
                (OctetString)sequence.Elements[4],
                (OctetString)sequence.Elements[5]
            );
        }

        /// <summary>
        /// Create discovery parameters (empty engine ID for discovery requests)
        /// </summary>
        public static UsmSecurityParameters CreateDiscovery(string userName = "")
        {
            return Create(
                authoritativeEngineId: "", // Empty for discovery
                userName: userName
            );
        }

        /// <summary>
        /// Check if this represents a discovery request
        /// </summary>
        public bool IsDiscovery => string.IsNullOrEmpty(AuthoritativeEngineId.Value);

        /// <summary>
        /// Get engine ID as hex string for display
        /// </summary>
        public string EngineIdHex => Convert.ToHexString(AuthoritativeEngineId.Data);

        /// <summary>
        /// Check if authentication parameters are present
        /// </summary>
        public bool HasAuthParams => AuthenticationParameters.Data.Length > 0;

        /// <summary>
        /// Check if privacy parameters are present
        /// </summary>
        public bool HasPrivParams => PrivacyParameters.Data.Length > 0;
    }
}