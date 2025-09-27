using nSNMP.SMI.DataTypes;
using nSNMP.SMI.DataTypes.V1.Constructed;
using nSNMP.SMI.DataTypes.V1.Primitive;
using nSNMP.SMI.PDUs;

namespace nSNMP.Message
{
    /// <summary>
    /// SNMPv3 Scoped PDU with context information
    /// </summary>
    public record ScopedPdu(OctetString? ContextEngineId, OctetString? ContextName, PDU? Pdu) : IDataType
    {
        /// <summary>
        /// Data property required by IDataType interface
        /// </summary>
        public byte[] Data => ToBytes();

        public ScopedPdu() : this(null, null, null) { }

        /// <summary>
        /// Create scoped PDU with default context
        /// </summary>
        public static ScopedPdu Create(PDU pdu, string contextEngineId = "", string contextName = "")
        {
            return new ScopedPdu(
                OctetString.Create(contextEngineId),
                OctetString.Create(contextName),
                pdu
            );
        }

        /// <summary>
        /// Serialize to bytes for transmission
        /// </summary>
        public byte[] ToBytes()
        {
            // If we have encrypted data, return it directly (for privacy)
            if (EncryptedData != null)
                return EncryptedData;

            var elements = new List<IDataType>();
            elements.Add(ContextEngineId ?? OctetString.Create(""));
            elements.Add(ContextName ?? OctetString.Create(""));
            if (Pdu != null)
                elements.Add(Pdu);

            var sequence = new Sequence(elements);
            return sequence.ToBytes();
        }

        /// <summary>
        /// Parse scoped PDU from bytes
        /// </summary>
        public static ScopedPdu Parse(byte[] data)
        {
            var sequence = (Sequence)nSNMP.SMI.SMIDataFactory.Create(data);

            var contextEngineId = (OctetString)sequence.Elements[0];
            var contextName = (OctetString)sequence.Elements[1];
            var pdu = sequence.Elements.Count > 2 ? (PDU)sequence.Elements[2] : null;

            return new ScopedPdu(contextEngineId, contextName, pdu);
        }

        /// <summary>
        /// Parse scoped PDU from sequence
        /// </summary>
        public static ScopedPdu Parse(Sequence sequence)
        {
            var contextEngineId = (OctetString)sequence.Elements[0];
            var contextName = (OctetString)sequence.Elements[1];
            var pdu = sequence.Elements.Count > 2 ? (PDU)sequence.Elements[2] : null;

            return new ScopedPdu(contextEngineId, contextName, pdu);
        }

        /// <summary>
        /// Create from sequence (for factory parsing)
        /// </summary>
        public static ScopedPdu Create(byte[] data)
        {
            return Parse(data);
        }

        /// <summary>
        /// Create scoped PDU with encrypted data (for privacy)
        /// </summary>
        public static ScopedPdu CreateEncrypted(byte[] encryptedData)
        {
            return new ScopedPdu(
                OctetString.Create(""),
                OctetString.Create(""),
                null  // No PDU, just encrypted data
            )
            {
                EncryptedData = encryptedData
            };
        }

        /// <summary>
        /// Encrypted data for privacy (when PDU is null)
        /// </summary>
        public byte[]? EncryptedData { get; init; }
    }
}