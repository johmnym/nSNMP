using nSNMP.SMI;
using nSNMP.SMI.DataTypes.V1.Constructed;
using nSNMP.SMI.DataTypes.V1.Primitive;

namespace nSNMP.Message
{
    public class SnmpMessage
    {
        private readonly Sequence _message;
        
        public Version Version { get { return Version.Create((Integer) _message.Elements[0]); }}
        public OctetString CommunityString { get { return (OctetString) _message.Elements[1]; } }
        public Pdu Pdu { get { return (Pdu) _message.Elements[2]; } }

        private SnmpMessage(Sequence message)
        {
            _message = message;
        }

        public static SnmpMessage Create(byte[] data)
        {
            var sequence = (Sequence)PDUDataFactory.Create(data);
            
            var message = new SnmpMessage(sequence);

            return message;
        }
    }
}