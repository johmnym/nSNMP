using nSNMP.SMI.V1.DataTypes.ApplicationWideDataTypes;
using nSNMP.SMI.V1.DataTypes.SimpleDataTypes;

namespace nSNMP.SMI.Message
{
    public class SnmpMessage : DataType
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

            var message = new SnmpMessage(data);

            //message.Version = ParseVersion()
            

            return message;
        }

        private Version ParseVersion(byte[] data)
        {
           return (Version) new Integer(data).Value;
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
