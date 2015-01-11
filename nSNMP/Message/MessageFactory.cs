
using nSNMP.SMI.DataTypes.V1.Constructed;
using nSNMP.SMI.DataTypes.V1.Primitive;

namespace nSNMP.Message
{
    public class MessageFactory
    {
        private SnmpMessage _message;

        public MessageFactory Create(PduType type)
        {
            _message = new SnmpMessage();

            switch (type)
            {
                case PduType.GetRequest:
                    _message.Pdu = new GetRequest();
                    break;
                case PduType.GetResponse:
                    _message.Pdu = new GetResponse();
                    break;
                default:
                    _message.Pdu = new GetRequest();
                    break;
            }
            
            return this;
        }

        public MessageFactory Create()
        {
            _message = new SnmpMessage();

            _message.Pdu = new GetRequest();

            return this;
        }

        public MessageFactory WithVersion(SnmpVersion version)
        {
            var integer = Version.Create((int) version);

            _message.Version = integer;

            return this;
        }

        public MessageFactory WithCommunity(string community)
        {
            var communityString = OctetString.Create(community);

            _message.CommunityString = communityString;

            return this;
        }

        public MessageFactory WithVarbind(Sequence varbind)
        {
            _message.Pdu.VarbindList.Add(varbind);

            return this;
        }
    }

    public class Varbind
    {
        public static Sequence Create(string oid)
        {
            var varbind = new Sequence();

            var objectId = ObjectIdentifier.Create(oid);
            varbind.Add(objectId);
            varbind.Add(new Null());

            return varbind;
        }
    }
}
