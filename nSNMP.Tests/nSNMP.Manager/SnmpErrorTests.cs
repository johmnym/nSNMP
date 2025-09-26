using System.Net;
using nSNMP.Manager;
using nSNMP.Message;
using nSNMP.SMI.DataTypes.V1.Primitive;
using nSNMP.SMI.DataTypes.V1.Constructed;
using nSNMP.SMI.PDUs;
using nSNMP.SMI.DataTypes;
using Xunit;

namespace nSNMP.Tests.nSNMP.Manager
{
    public class SnmpErrorTests
    {
        private readonly IPEndPoint _testEndpoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 161);

        [Fact]
        public async Task GetAsync_SnmpError_ThrowsSnmpErrorException()
        {
            // Arrange
            var mockTransport = new MockUdpChannel();
            var client = new SnmpClient(_testEndpoint, SnmpVersion.V2c, "public", TimeSpan.FromSeconds(5), mockTransport);

            var testOid = ObjectIdentifier.Create("1.3.6.1.2.1.1.1.0");

            // Create error response (noSuchName = 2)
            var responseVarbind = new Sequence(new IDataType[] { testOid, new Null() });
            var responseVarbindList = new Sequence(new IDataType[] { responseVarbind });
            var responsePdu = new GetResponse(null, Integer.Create(1), Integer.Create(2), Integer.Create(1), responseVarbindList);
            var responseMessage = new SnmpMessage(SnmpVersion.V2c, OctetString.Create("public"), responsePdu);

            // Create matching request
            var requestVarbind = new Sequence(new IDataType[] { testOid, new Null() });
            var requestVarbindList = new Sequence(new IDataType[] { requestVarbind });
            var requestPdu = new GetRequest(null, Integer.Create(1), Integer.Create(0), Integer.Create(0), requestVarbindList);
            var requestMessage = new SnmpMessage(SnmpVersion.V2c, OctetString.Create("public"), requestPdu);

            mockTransport.SetResponse(requestMessage.ToBytes(), responseMessage.ToBytes());

            // Act & Assert
            var exception = await Assert.ThrowsAsync<SnmpErrorException>(() => client.GetAsync("1.3.6.1.2.1.1.1.0"));

            Assert.Equal(2, exception.ErrorStatus);
            Assert.Equal(1, exception.ErrorIndex);
            Assert.Contains("noSuchName", exception.Message);

            client.Dispose();
        }

        [Fact]
        public async Task GetAsync_Timeout_ThrowsSnmpTimeoutException()
        {
            // Arrange
            var mockTransport = new MockUdpChannel();
            var client = new SnmpClient(_testEndpoint, SnmpVersion.V2c, "public", TimeSpan.FromMilliseconds(100), mockTransport);

            // Don't set any response - this will cause timeout

            // Act & Assert
            var exception = await Assert.ThrowsAsync<SnmpTimeoutException>(() => client.GetAsync("1.3.6.1.2.1.1.1.0"));

            Assert.Equal(TimeSpan.FromMilliseconds(100), exception.Timeout);

            client.Dispose();
        }

        [Fact]
        public void SnmpErrorException_FromErrorStatus_CreatesCorrectMessages()
        {
            var ex1 = SnmpErrorException.FromErrorStatus(1, 0);
            Assert.Contains("tooBig", ex1.Message);

            var ex2 = SnmpErrorException.FromErrorStatus(2, 1);
            Assert.Contains("noSuchName", ex2.Message);

            var ex3 = SnmpErrorException.FromErrorStatus(3, 2);
            Assert.Contains("badValue", ex3.Message);

            var ex4 = SnmpErrorException.FromErrorStatus(4, 3);
            Assert.Contains("readOnly", ex4.Message);

            var ex5 = SnmpErrorException.FromErrorStatus(5, 4);
            Assert.Contains("genErr", ex5.Message);

            var ex6 = SnmpErrorException.FromErrorStatus(99, 5);
            Assert.Contains("Unknown error status: 99", ex6.Message);
        }

        [Fact]
        public void Dispose_CalledMultipleTimes_DoesNotThrow()
        {
            var mockTransport = new MockUdpChannel();
            var client = new SnmpClient(_testEndpoint, SnmpVersion.V2c, "public", TimeSpan.FromSeconds(5), mockTransport);

            client.Dispose();
            client.Dispose(); // Should not throw
        }

        [Fact]
        public async Task GetAsync_AfterDispose_ThrowsObjectDisposedException()
        {
            var mockTransport = new MockUdpChannel();
            var client = new SnmpClient(_testEndpoint, SnmpVersion.V2c, "public", TimeSpan.FromSeconds(5), mockTransport);

            client.Dispose();

            await Assert.ThrowsAsync<ObjectDisposedException>(() => client.GetAsync("1.3.6.1.2.1.1.1.0"));
        }
    }
}