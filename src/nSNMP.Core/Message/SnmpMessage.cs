using System.Collections.Generic;
using nSNMP.SMI;
using nSNMP.SMI.DataTypes;
using nSNMP.SMI.DataTypes.V1.Constructed;
using nSNMP.SMI.DataTypes.V1.Primitive;
using nSNMP.SMI.PDUs;

namespace nSNMP.Message
{
    public record SnmpMessage(SnmpVersion? Version, OctetString? CommunityString, PDU? PDU)
    {
        public SnmpMessage() : this(null, null, null) { }

        public static SnmpMessage Create(byte[] data)
        {
            var sequence = (Sequence)SMIDataFactory.Create(data);

            var version = (SnmpVersion)(int)(Integer)sequence.Elements[0];
            var communityString = (OctetString)sequence.Elements[1];
            var pdu = (PDU)sequence.Elements[2];

            return new SnmpMessage(version, communityString, pdu);
        }

        public Sequence ToSequence()
        {
            var elements = new List<IDataType>();
            elements.Add(Integer.Create((int)Version!));
            elements.Add(CommunityString!);
            elements.Add(PDU!);

            return new Sequence(elements);
        }

        public byte[] ToBytes()
        {
            return ToSequence().ToBytes();
        }
    }
}