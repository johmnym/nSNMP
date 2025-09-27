using System.Net;
using nSNMP.Manager;
using nSNMP.Message;
using nSNMP.SMI.DataTypes.V1.Primitive;
using nSNMP.SMI.DataTypes.V1.Constructed;
using nSNMP.SMI.PDUs;
using nSNMP.SMI.DataTypes;
using Xunit;

namespace nSNMP.Core.Tests
{
    public class SnmpClientTests
    {
        private readonly IPEndPoint _testEndpoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 161);

        [Fact]
        public void CreateCommunity_CreatesClientWithCorrectSettings()
        {
            var client = SnmpClient.CreateCommunity("192.168.1.1", 161, SnmpVersion.V2c, "test-community");

            Assert.NotNull(client);
            client.Dispose();
        }

        [Fact]
        public async Task GetAsync_SingleOid_ReturnsCorrectResult()
        {
            // Arrange
            var mockTransport = new MockUdpChannel();
            var client = new SnmpClient(_testEndpoint, SnmpVersion.V2c, "public", TimeSpan.FromSeconds(5), mockTransport);

            var testOid = ObjectIdentifier.Create("1.3.6.1.2.1.1.1.0");
            var testValue = OctetString.Create("Test System Description");

            // Create expected response
            var responseVarbind = new Sequence(new IDataType[] { testOid, testValue });
            var responseVarbindList = new Sequence(new IDataType[] { responseVarbind });
            var responsePdu = new GetResponse(null, Integer.Create(1), Integer.Create(0), Integer.Create(0), responseVarbindList);
            var responseMessage = new SnmpMessage(SnmpVersion.V2c, OctetString.Create("public"), responsePdu);

            // We need to create a request to match against
            var requestVarbind = new Sequence(new IDataType[] { testOid, new Null() });
            var requestVarbindList = new Sequence(new IDataType[] { requestVarbind });
            var requestPdu = new GetRequest(null, Integer.Create(1), Integer.Create(0), Integer.Create(0), requestVarbindList);
            var requestMessage = new SnmpMessage(SnmpVersion.V2c, OctetString.Create("public"), requestPdu);

            mockTransport.SetResponse(requestMessage.ToBytes(), responseMessage.ToBytes());

            // Act
            var results = await client.GetAsync("1.3.6.1.2.1.1.1.0");

            // Assert
            Assert.Single(results);
            Assert.Equal(testOid.ToString(), results[0].OidString);
            Assert.Equal(testValue.Value, ((OctetString)results[0].Value).Value);

            client.Dispose();
        }

        [Fact]
        public async Task GetAsync_MultipleOids_ReturnsAllResults()
        {
            // Arrange
            var mockTransport = new MockUdpChannel();
            var client = new SnmpClient(_testEndpoint, SnmpVersion.V2c, "public", TimeSpan.FromSeconds(5), mockTransport);

            var oid1 = ObjectIdentifier.Create("1.3.6.1.2.1.1.1.0");
            var oid2 = ObjectIdentifier.Create("1.3.6.1.2.1.1.2.0");
            var value1 = OctetString.Create("System Description");
            var value2 = OctetString.Create("System ObjectID");

            // Create response with multiple varbinds
            var varbind1 = new Sequence(new IDataType[] { oid1, value1 });
            var varbind2 = new Sequence(new IDataType[] { oid2, value2 });
            var responseVarbindList = new Sequence(new IDataType[] { varbind1, varbind2 });
            var responsePdu = new GetResponse(null, Integer.Create(1), Integer.Create(0), Integer.Create(0), responseVarbindList);
            var responseMessage = new SnmpMessage(SnmpVersion.V2c, OctetString.Create("public"), responsePdu);

            // Create matching request
            var requestVarbind1 = new Sequence(new IDataType[] { oid1, new Null() });
            var requestVarbind2 = new Sequence(new IDataType[] { oid2, new Null() });
            var requestVarbindList = new Sequence(new IDataType[] { requestVarbind1, requestVarbind2 });
            var requestPdu = new GetRequest(null, Integer.Create(1), Integer.Create(0), Integer.Create(0), requestVarbindList);
            var requestMessage = new SnmpMessage(SnmpVersion.V2c, OctetString.Create("public"), requestPdu);

            mockTransport.SetResponse(requestMessage.ToBytes(), responseMessage.ToBytes());

            // Act
            var results = await client.GetAsync("1.3.6.1.2.1.1.1.0", "1.3.6.1.2.1.1.2.0");

            // Assert
            Assert.Equal(2, results.Length);
            Assert.Equal(oid1.ToString(), results[0].OidString);
            Assert.Equal(oid2.ToString(), results[1].OidString);

            client.Dispose();
        }

        [Fact]
        public async Task SetAsync_SingleVarBind_SendsCorrectRequest()
        {
            // Arrange
            var mockTransport = new MockUdpChannel();
            var client = new SnmpClient(_testEndpoint, SnmpVersion.V2c, "public", TimeSpan.FromSeconds(5), mockTransport);

            var testOid = ObjectIdentifier.Create("1.3.6.1.2.1.1.4.0");
            var testValue = OctetString.Create("New Contact Info");
            var varBind = new VarBind(testOid, testValue);

            // Create expected response (echo back the set value)
            var responseVarbind = new Sequence(new IDataType[] { testOid, testValue });
            var responseVarbindList = new Sequence(new IDataType[] { responseVarbind });
            var responsePdu = new GetResponse(null, Integer.Create(1), Integer.Create(0), Integer.Create(0), responseVarbindList);
            var responseMessage = new SnmpMessage(SnmpVersion.V2c, OctetString.Create("public"), responsePdu);

            // Create matching request
            var requestVarbind = new Sequence(new IDataType[] { testOid, testValue });
            var requestVarbindList = new Sequence(new IDataType[] { requestVarbind });
            var requestPdu = new SetRequest(null, Integer.Create(1), Integer.Create(0), Integer.Create(0), requestVarbindList);
            var requestMessage = new SnmpMessage(SnmpVersion.V2c, OctetString.Create("public"), requestPdu);

            mockTransport.SetResponse(requestMessage.ToBytes(), responseMessage.ToBytes());

            // Act
            var results = await client.SetAsync(varBind);

            // Assert
            Assert.Single(results);
            Assert.Equal(testOid.ToString(), results[0].OidString);
            Assert.Equal(testValue.Value, ((OctetString)results[0].Value).Value);

            client.Dispose();
        }

        [Fact]
        public async Task GetNextAsync_ReturnsNextOid()
        {
            // Arrange
            var mockTransport = new MockUdpChannel();
            var client = new SnmpClient(_testEndpoint, SnmpVersion.V2c, "public", TimeSpan.FromSeconds(5), mockTransport);

            var requestOid = ObjectIdentifier.Create("1.3.6.1.2.1.1.1");
            var responseOid = ObjectIdentifier.Create("1.3.6.1.2.1.1.1.0");
            var responseValue = OctetString.Create("System Description");

            // Create expected response
            var responseVarbind = new Sequence(new IDataType[] { responseOid, responseValue });
            var responseVarbindList = new Sequence(new IDataType[] { responseVarbind });
            var responsePdu = new GetResponse(null, Integer.Create(1), Integer.Create(0), Integer.Create(0), responseVarbindList);
            var responseMessage = new SnmpMessage(SnmpVersion.V2c, OctetString.Create("public"), responsePdu);

            // Create matching request
            var requestVarbind = new Sequence(new IDataType[] { requestOid, new Null() });
            var requestVarbindList = new Sequence(new IDataType[] { requestVarbind });
            var requestPdu = new GetNextRequest(null, Integer.Create(1), Integer.Create(0), Integer.Create(0), requestVarbindList);
            var requestMessage = new SnmpMessage(SnmpVersion.V2c, OctetString.Create("public"), requestPdu);

            mockTransport.SetResponse(requestMessage.ToBytes(), responseMessage.ToBytes());

            // Act
            var results = await client.GetNextAsync("1.3.6.1.2.1.1.1");

            // Assert
            Assert.Single(results);
            Assert.Equal(responseOid.ToString(), results[0].OidString);
            Assert.Equal(responseValue.Value, ((OctetString)results[0].Value).Value);

            client.Dispose();
        }

        [Fact]
        public async Task GetBulkAsync_V2c_SendsGetBulkRequest()
        {
            // Arrange
            var mockTransport = new MockUdpChannel();
            var client = new SnmpClient(_testEndpoint, SnmpVersion.V2c, "public", TimeSpan.FromSeconds(5), mockTransport);

            var requestOid = ObjectIdentifier.Create("1.3.6.1.2.1.1");
            var responseOid1 = ObjectIdentifier.Create("1.3.6.1.2.1.1.1.0");
            var responseOid2 = ObjectIdentifier.Create("1.3.6.1.2.1.1.2.0");
            var responseValue1 = OctetString.Create("System Description");
            var responseValue2 = OctetString.Create("System ObjectID");

            // Create response with multiple varbinds
            var varbind1 = new Sequence(new IDataType[] { responseOid1, responseValue1 });
            var varbind2 = new Sequence(new IDataType[] { responseOid2, responseValue2 });
            var responseVarbindList = new Sequence(new IDataType[] { varbind1, varbind2 });
            var responsePdu = new GetResponse(null, Integer.Create(1), Integer.Create(0), Integer.Create(0), responseVarbindList);
            var responseMessage = new SnmpMessage(SnmpVersion.V2c, OctetString.Create("public"), responsePdu);

            // Create matching request (GetBulk with non-repeaters=0, max-repetitions=10)
            var requestVarbind = new Sequence(new IDataType[] { requestOid, new Null() });
            var requestVarbindList = new Sequence(new IDataType[] { requestVarbind });
            var requestPdu = new GetBulkRequest(null, Integer.Create(1), Integer.Create(0), Integer.Create(10), requestVarbindList);
            var requestMessage = new SnmpMessage(SnmpVersion.V2c, OctetString.Create("public"), requestPdu);

            mockTransport.SetResponse(requestMessage.ToBytes(), responseMessage.ToBytes());

            // Act
            var results = await client.GetBulkAsync(0, 10, "1.3.6.1.2.1.1");

            // Assert
            Assert.Equal(2, results.Length);
            Assert.Equal(responseOid1.ToString(), results[0].OidString);
            Assert.Equal(responseOid2.ToString(), results[1].OidString);

            client.Dispose();
        }

        [Fact]
        public async Task GetBulkAsync_V1_ThrowsNotSupportedException()
        {
            // Arrange
            var mockTransport = new MockUdpChannel();
            var client = new SnmpClient(_testEndpoint, SnmpVersion.V1, "public", TimeSpan.FromSeconds(5), mockTransport);

            // Act & Assert
            await Assert.ThrowsAsync<NotSupportedException>(() => client.GetBulkAsync(0, 10, "1.3.6.1.2.1.1"));

            client.Dispose();
        }

        [Fact]
        public async Task WalkAsync_StopsAtEndOfSubtree()
        {
            // Arrange
            var mockTransport = new MockUdpChannel();
            var client = new SnmpClient(_testEndpoint, SnmpVersion.V2c, "public", TimeSpan.FromSeconds(5), mockTransport);

            var baseOid = ObjectIdentifier.Create("1.3.6.1.2.1.1");

            // First GetNext: 1.3.6.1.2.1.1 -> 1.3.6.1.2.1.1.1.0
            var request1Oid = baseOid;
            var response1Oid = ObjectIdentifier.Create("1.3.6.1.2.1.1.1.0");
            var response1Value = OctetString.Create("System Description");

            var response1Varbind = new Sequence(new IDataType[] { response1Oid, response1Value });
            var response1VarbindList = new Sequence(new IDataType[] { response1Varbind });
            var response1Pdu = new GetResponse(null, Integer.Create(1), Integer.Create(0), Integer.Create(0), response1VarbindList);
            var response1Message = new SnmpMessage(SnmpVersion.V2c, OctetString.Create("public"), response1Pdu);

            var request1Varbind = new Sequence(new IDataType[] { request1Oid, new Null() });
            var request1VarbindList = new Sequence(new IDataType[] { request1Varbind });
            var request1Pdu = new GetNextRequest(null, Integer.Create(1), Integer.Create(0), Integer.Create(0), request1VarbindList);
            var request1Message = new SnmpMessage(SnmpVersion.V2c, OctetString.Create("public"), request1Pdu);

            // Second GetNext: 1.3.6.1.2.1.1.1.0 -> 1.3.6.1.2.1.2.1.0 (out of subtree)
            var request2Oid = response1Oid;
            var response2Oid = ObjectIdentifier.Create("1.3.6.1.2.1.2.1.0"); // Different subtree
            var response2Value = OctetString.Create("Interface Number");

            var response2Varbind = new Sequence(new IDataType[] { response2Oid, response2Value });
            var response2VarbindList = new Sequence(new IDataType[] { response2Varbind });
            var response2Pdu = new GetResponse(null, Integer.Create(2), Integer.Create(0), Integer.Create(0), response2VarbindList);
            var response2Message = new SnmpMessage(SnmpVersion.V2c, OctetString.Create("public"), response2Pdu);

            var request2Varbind = new Sequence(new IDataType[] { request2Oid, new Null() });
            var request2VarbindList = new Sequence(new IDataType[] { request2Varbind });
            var request2Pdu = new GetNextRequest(null, Integer.Create(2), Integer.Create(0), Integer.Create(0), request2VarbindList);
            var request2Message = new SnmpMessage(SnmpVersion.V2c, OctetString.Create("public"), request2Pdu);

            mockTransport.SetResponse(request1Message.ToBytes(), response1Message.ToBytes());
            mockTransport.SetResponse(request2Message.ToBytes(), response2Message.ToBytes());

            // Act
            var results = new List<VarBind>();
            await foreach (var result in client.WalkAsync("1.3.6.1.2.1.1"))
            {
                results.Add(result);
            }

            // Assert
            Assert.Single(results); // Should only return the first result, stop when moving to different subtree
            Assert.Equal(response1Oid.ToString(), results[0].OidString);

            client.Dispose();
        }

        [Fact]
        public void VarBind_CreationMethods_WorkCorrectly()
        {
            // Test various VarBind creation methods
            var oid = ObjectIdentifier.Create("1.3.6.1.2.1.1.1.0");
            var value = OctetString.Create("test");

            var vb1 = new VarBind(oid);
            var vb2 = new VarBind(oid, value);
            var vb3 = new VarBind("1.3.6.1.2.1.1.1.0");
            var vb4 = new VarBind("1.3.6.1.2.1.1.1.0", value);

            Assert.Equal(oid.ToString(), vb1.OidString);
            Assert.True(vb1.IsEndOfMibView); // Null value
            Assert.False(vb2.IsEndOfMibView);
            Assert.Equal(oid.ToString(), vb3.OidString);
            Assert.Equal(oid.ToString(), vb4.OidString);
            Assert.Equal(value, vb4.Value);
        }
    }
}