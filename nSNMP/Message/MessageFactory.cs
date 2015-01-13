
using nSNMP.SMI.DataTypes.V1.Constructed;
using nSNMP.SMI.DataTypes.V1.Primitive;
using nSNMP.SMI.PDUs;

namespace nSNMP.Message
{
    public class MessageFactory
    {
        private SnmpMessage _message;

        public MessageFactory CreateGetRequest()
        {
            _message = new SnmpMessage {PDU = new GetRequest()};

            return this;
        }

        public MessageFactory Create()
        {
            _message = new SnmpMessage();

            _message.PDU = new GetRequest();

            return this;
        }

        public MessageFactory WithVersion(SnmpVersion version)
        {
            _message.Version = version;

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
            _message.PDU.VarbindList.Add(varbind);

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
