using nSNMP.SMI.V1.DataTypes.ApplicationWideDataTypes;
using nSNMP.SMI.V1.DataTypes.SimpleDataTypes;
namespace nSNMP.SMI.Message
{
    public class SnmpMessage : Sequence
    {
        public Version Version { get { return Version.Create((Integer) Elements[0]); }}
        public OctetString CommunityString { get { return (OctetString) Elements[1]; } }
        public GetResponseSnmpPdu Pdu { get { return (GetResponseSnmpPdu) Elements[2]; } }

        public SnmpMessage(byte[] data) : base(data)
        {
            
        }

        public new static SnmpMessage Create(byte[] data)
        {
            var message = new SnmpMessage(data);

            message.Initialize();

            return message;
        }

    }
}
