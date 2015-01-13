
namespace nSNMP.SMI.PDUs
{
    public class GetRequest : PDU
    {
        private GetRequest(byte[] data) : base(data)
        {
        }

        public GetRequest() : base(null)
        {
            
        }

        public static GetRequest Create(byte[] data)
        {
            var pdu = new GetRequest(data);

            pdu.Initialize();

            return pdu;
        }
    }
}
