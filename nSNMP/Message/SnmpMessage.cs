using nSNMP.SMI;
using nSNMP.SMI.DataTypes.V1.Constructed;
using nSNMP.SMI.DataTypes.V1.Primitive;
using nSNMP.SMI.PDUs;

namespace nSNMP.Message
{
    public class SnmpMessage
    {
        private readonly Sequence _message;
        
        public SnmpVersion? Version 
        {
            get { return (SnmpVersion)(int)(Integer)_message.Elements[0]; } 
            set { _message.Elements[0] = Integer.Create((int)value!); }
        }

        public OctetString? CommunityString
        {
            get { return (OctetString) _message.Elements[1]; }
            set { _message.Elements[1] = value!; }
        }

        public PDU? PDU
        {
            get { return (PDU) _message.Elements[2]; }
            set { _message.Elements[2] = value!; }
        }

        private SnmpMessage(Sequence message)
        {
            _message = message;
        }

        public SnmpMessage()
        {
            _message = new Sequence();
            _message.Add(default!);
            _message.Add(default!);
            _message.Add(default!);
        }

        public static SnmpMessage Create(byte[] data)
        {
            var sequence = (Sequence)SMIDataFactory.Create(data);
            
            var message = new SnmpMessage(sequence);

            return message;
        }
    }
}