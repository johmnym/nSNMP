using System.IO;
using nSNMP.SMI.V1.DataTypes.SimpleDataTypes;

namespace nSNMP.SMI.Message
{
    public class SnmpMessage : SimpleDataType
    {
        public Version Version { get; set; }
        public string CommunityString { get; set; }
        public SnmpPdu PDU { get; set; }

        private SnmpMessage(byte[] data) : base(data)
        {
            PDU = new SnmpPdu();
        }

        public static SnmpMessage Create(byte[] data)
        {
            if (data == null) return null;

            var stream = new MemoryStream(data);

            var message = new SnmpMessage(data);

            message.Version = ReadVersion(stream);

            message.CommunityString = ReadCommintyString(stream);

            return message;
        }

        private static string ReadCommintyString(MemoryStream stream)
        {
            var data = (OctetString)SMIDataFactory.Create(stream);

            return data.ToString();
        }

        private static Version ReadVersion(MemoryStream stream)
        {
            var data = (Integer)SMIDataFactory.Create(stream);

            return (Version) data.Value;
        }
    }

    public enum Version
    {
        none = 0,
        V1 = 1,
        V2 = 2,
        V3 = 3
    }
}
