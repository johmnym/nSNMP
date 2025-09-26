
using nSNMP.SMI.DataTypes.V1.Constructed;
using nSNMP.SMI.DataTypes.V1.Primitive;
using nSNMP.SMI.PDUs;

namespace nSNMP.Message
{
    public class MessageFactory
    {
        private SnmpMessage? _message;

        public MessageFactory CreateGetRequest()
        {
            _message = new SnmpMessage { PDU = new GetRequest() };

            return this;
        }

        public MessageFactory WithVersion(SnmpVersion version)
        {
            if (_message == null) throw new InvalidOperationException();
            _message.Version = version;

            return this;
        }

        public MessageFactory WithCommunity(string community)
        {
            if (_message == null) throw new InvalidOperationException();
            var communityString = OctetString.Create(community);

            _message.CommunityString = communityString;

            return this;
        }

        public MessageFactory WithVarbind(Sequence varbind)
        {
            if (_message == null) throw new InvalidOperationException();
            if (_message.PDU == null) throw new InvalidOperationException();
            if (_message.PDU.VarbindList == null) throw new InvalidOperationException();
            _message.PDU.VarbindList.Add(varbind);

            return this;
        }

        public SnmpMessage Message()
        {
            if (_message == null) throw new InvalidOperationException();
            return _message;
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
