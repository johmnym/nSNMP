
namespace nSNMP.Message
{
    public class GetRequest : Pdu
    {
        public GetRequest(byte[] data) : base(data)
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
