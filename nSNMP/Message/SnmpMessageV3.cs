using nSNMP.SMI.DataTypes;
using nSNMP.SMI.DataTypes.V1.Constructed;
using nSNMP.SMI.DataTypes.V1.Primitive;
using nSNMP.SMI.PDUs;

namespace nSNMP.Message
{
    /// <summary>
    /// SNMPv3 message structure with security parameters
    /// </summary>
    public record SnmpMessageV3(
        Integer MessageId,
        Integer MaxSize,
        OctetString Flags,
        Integer SecurityModel,
        OctetString SecurityParameters,
        ScopedPdu ScopedPdu)
    {
        /// <summary>
        /// Authentication flag (bit 0)
        /// </summary>
        public bool AuthFlag => (Flags.Data[0] & 0x01) != 0;

        /// <summary>
        /// Privacy flag (bit 1)
        /// </summary>
        public bool PrivFlag => (Flags.Data[0] & 0x02) != 0;

        /// <summary>
        /// Reportable flag (bit 2)
        /// </summary>
        public bool ReportableFlag => (Flags.Data[0] & 0x04) != 0;

        /// <summary>
        /// Create SNMPv3 message with default values
        /// </summary>
        public static SnmpMessageV3 Create(
            int messageId,
            int maxSize = 65507,
            bool authFlag = false,
            bool privFlag = false,
            bool reportableFlag = true,
            byte[] securityParameters = null,
            ScopedPdu scopedPdu = null)
        {
            byte flags = 0;
            if (authFlag) flags |= 0x01;
            if (privFlag) flags |= 0x02;
            if (reportableFlag) flags |= 0x04;

            return new SnmpMessageV3(
                Integer.Create(messageId),
                Integer.Create(maxSize),
                new OctetString(new[] { flags }),
                Integer.Create(3), // USM security model
                new OctetString(securityParameters ?? Array.Empty<byte>()),
                scopedPdu ?? new ScopedPdu()
            );
        }

        /// <summary>
        /// Serialize to complete SNMP message
        /// </summary>
        public byte[] ToBytes()
        {
            var headerData = new Sequence(new IDataType[]
            {
                MessageId,
                MaxSize,
                Flags,
                SecurityModel
            });

            // Convert scoped PDU to a sequence for proper BER encoding
            var scopedPduElements = new List<IDataType>();
            scopedPduElements.Add(ScopedPdu.ContextEngineId ?? OctetString.Create(""));
            scopedPduElements.Add(ScopedPdu.ContextName ?? OctetString.Create(""));
            if (ScopedPdu.Pdu != null)
                scopedPduElements.Add(ScopedPdu.Pdu);

            var scopedPduSequence = new Sequence(scopedPduElements);

            var messageData = new Sequence(new IDataType[]
            {
                Integer.Create(3), // Version
                headerData,
                SecurityParameters,
                scopedPduSequence
            });

            return messageData.ToBytes();
        }

        /// <summary>
        /// Parse SNMPv3 message from bytes
        /// </summary>
        public static SnmpMessageV3 Parse(byte[] data)
        {
            var sequence = (Sequence)nSNMP.SMI.SMIDataFactory.Create(data);

            var version = (Integer)sequence.Elements[0];
            if (version.Value != 3)
                throw new ArgumentException($"Expected SNMPv3, got version {version.Value}");

            var headerData = (Sequence)sequence.Elements[1];
            var messageId = (Integer)headerData.Elements[0];
            var maxSize = (Integer)headerData.Elements[1];
            var flags = (OctetString)headerData.Elements[2];
            var securityModel = (Integer)headerData.Elements[3];

            var securityParameters = (OctetString)sequence.Elements[2];
            var scopedPduSequence = (Sequence)sequence.Elements[3];
            var scopedPdu = ScopedPdu.Parse(scopedPduSequence);

            return new SnmpMessageV3(messageId, maxSize, flags, securityModel, securityParameters, scopedPdu);
        }
    }
}