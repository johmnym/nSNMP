namespace nSNMP.SMI.Message
{
    public class GetResponseSnmpPdu : SnmpPdu
    {
        private GetResponseSnmpPdu(byte[] data) : base(data)
        {
        }

        public static GetResponseSnmpPdu Create(byte[] data)
        {
            var pdu = new GetResponseSnmpPdu(data);

            pdu.Initialize();

            return pdu;
        }
    }
}