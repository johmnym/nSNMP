
namespace nSNMP.Message
{
    public class GetRequest : Pdu
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
