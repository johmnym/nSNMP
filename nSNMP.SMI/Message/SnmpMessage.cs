using System.IO;
using nSNMP.SMI.V1.DataTypes.ApplicationWideDataTypes;
using nSNMP.SMI.V1.DataTypes.SimpleDataTypes;

namespace nSNMP.SMI.Message
{
    public class SnmpMessage : Sequence
    {
        public Version Version { get { return (Version) Elements[0]; }}
        public OctetString CommunityString { get { return (OctetString) Elements[1]; } }
        public GetResponseSnmpPdu Pdu { get { return (GetResponseSnmpPdu) Elements[2]; } }

        public SnmpMessage(byte[] data) : base(data)
        {
            
        }

        public static SnmpMessage Create(byte[] data)
        {
            if (data == null) return null;

            SnmpMessage message;

            using (var stream = new MemoryStream(data))
            {
                message = ReadSnmpMesasge(stream);
            }

            using (var stream = new MemoryStream(message.Data))
            {
                message.Elements.Add(ReadVersion(stream));

                message.Elements.Add(ReadCommintyString(stream));

                message.Elements.Add(ReadPdu(stream));
            }

            return message;
        }

        private static SnmpMessage ReadSnmpMesasge(MemoryStream stream)
        {
            var sequence = (Sequence) SMIDataFactory.Create(stream);

            return sequence.ToSnmpMessage();
        }

        private static GetResponseSnmpPdu ReadPdu(MemoryStream stream)
        {
            return (GetResponseSnmpPdu) SMIDataFactory.Create(stream);
        }

        private static OctetString ReadCommintyString(MemoryStream stream)
        {
            return (OctetString) SMIDataFactory.Create(stream);
        }

        private static Version ReadVersion(MemoryStream stream)
        {
            var data = (Integer)SMIDataFactory.Create(stream);

            return new Version(data.Data);
        }
    }
}
