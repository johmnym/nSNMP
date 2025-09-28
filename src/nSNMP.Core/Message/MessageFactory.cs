
using System.Collections.Generic;
using nSNMP.SMI.DataTypes;
using nSNMP.SMI.DataTypes.V1.Constructed;
using nSNMP.SMI.DataTypes.V1.Primitive;
using nSNMP.SMI.PDUs;

namespace nSNMP.Message
{
    public class MessageFactory
    {
        private SnmpVersion? _version;
        private string? _community;
        private PDU? _pdu;
        private readonly List<Sequence> _varbinds = new();

        public MessageFactory CreateGetRequest()
        {
            _pdu = new GetRequest();
            return this;
        }

        public MessageFactory WithVersion(SnmpVersion version)
        {
            _version = version;
            return this;
        }

        public MessageFactory WithCommunity(string community)
        {
            _community = community;
            return this;
        }

        public MessageFactory WithVarbind(Sequence varbind)
        {
            _varbinds.Add(varbind);
            return this;
        }

        public SnmpMessage Message()
        {
            var varbindList = new Sequence(_varbinds);

            var pdu = _pdu switch
            {
                GetRequest => new GetRequest(null, null, null, null, varbindList),
                _ => _pdu
            };

            return new SnmpMessage(_version, OctetString.Create(_community ?? ""), pdu);
        }
    }

    public class Varbind
    {
        public static Sequence Create(string oid)
        {
            var objectId = ObjectIdentifier.Create(oid);
            var nullValue = new Null();
            
            return new Sequence(new IDataType[] { objectId, nullValue });
        }
    }
}
