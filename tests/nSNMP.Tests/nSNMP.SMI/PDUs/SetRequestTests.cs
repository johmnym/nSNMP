using nSNMP.SMI;
using nSNMP.SMI.DataTypes.V1.Primitive;
using nSNMP.SMI.PDUs;
using Xunit;

namespace nSNMP.Tests.nSNMP.SMI.PDUs
{
    public class SetRequestTests
    {
        [Fact]
        public void CanCreateSetRequest()
        {
            var setRequest = new SetRequest();

            Assert.NotNull(setRequest);
            Assert.NotNull(setRequest.VarbindList);
        }

        [Fact]
        public void CanParseSetRequestPDU()
        {
            // A3 is the SetRequest PDU tag
            // This is a minimal SetRequest PDU with request-id=1, error=0, error-index=0, empty varbind list
            byte[] data = new byte[]
            {
                0xA3, 0x0B,  // SetRequest PDU, length 11
                0x02, 0x01, 0x01,  // Integer (request-id) = 1
                0x02, 0x01, 0x00,  // Integer (error) = 0
                0x02, 0x01, 0x00,  // Integer (error-index) = 0
                0x30, 0x00   // Sequence (varbind list) empty
            };

            var result = SMIDataFactory.Create(data);

            Assert.NotNull(result);
            Assert.IsType<SetRequest>(result);

            var setRequest = (SetRequest)result;
            Assert.NotNull(setRequest.RequestId);
            Assert.Equal(1, (int)setRequest.RequestId);
            Assert.NotNull(setRequest.Error);
            Assert.Equal(0, (int)setRequest.Error);
            Assert.NotNull(setRequest.ErrorIndex);
            Assert.Equal(0, (int)setRequest.ErrorIndex);
            Assert.NotNull(setRequest.VarbindList);
        }

        [Fact]
        public void SetRequestCreate_ReturnsCorrectInstance()
        {
            byte[] data = new byte[]
            {
                0x02, 0x01, 0x01,  // Integer (request-id) = 1
                0x02, 0x01, 0x00,  // Integer (error) = 0
                0x02, 0x01, 0x00,  // Integer (error-index) = 0
                0x30, 0x00   // Sequence (varbind list) empty
            };

            var setRequest = SetRequest.Create(data);

            Assert.NotNull(setRequest);
            Assert.IsType<SetRequest>(setRequest);
            Assert.NotNull(setRequest.RequestId);
            Assert.NotNull(setRequest.Error);
            Assert.NotNull(setRequest.ErrorIndex);
            Assert.NotNull(setRequest.VarbindList);
        }
    }
}