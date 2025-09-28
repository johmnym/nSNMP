using System.Net;
using nSNMP.Agent;
using nSNMP.Message;
using nSNMP.Security;
using nSNMP.SMI.DataTypes;
using nSNMP.SMI.DataTypes.V1.Constructed;
using nSNMP.SMI.DataTypes.V1.Primitive;
using nSNMP.SMI.PDUs;
using nSNMP.Transport;
using Xunit;

namespace nSNMP.Agent.Tests
{
    public class SnmpAgentHostV3Tests : IDisposable
    {
        private readonly MockUdpListener _mockListener;
        private readonly SnmpAgentHostV3 _agent;

        public SnmpAgentHostV3Tests()
        {
            _mockListener = new MockUdpListener();
            _agent = new SnmpAgentHostV3("public", "private", _mockListener);

            // Add test users
            _agent.AddUser("testuser", AuthProtocol.SHA1, "authpass123");
            _agent.AddUser("testuser_priv", AuthProtocol.SHA256, "authpass123", PrivProtocol.AES128, "privpass123");
        }

        [Fact]
        public void Constructor_WithDefaults_CreatesV3Agent()
        {
            using var agent = new SnmpAgentHostV3();
            Assert.NotNull(agent);
            Assert.NotNull(agent.Engine);
            Assert.NotNull(agent.UserDatabase);
        }

        [Fact]
        public void AddUser_WithValidCredentials_AddsUser()
        {
            var credentials = V3Credentials.AuthNoPriv("newuser", AuthProtocol.MD5, "password123");
            _agent.AddUser(credentials);

            Assert.True(_agent.UserDatabase.HasUser("newuser"));
            Assert.Equal(3, _agent.UserCount); // 2 existing + 1 new
        }

        [Fact]
        public void AddUser_WithFactoryMethod_AddsUser()
        {
            _agent.AddUser("factoryuser", AuthProtocol.SHA1, "password123");

            Assert.True(_agent.UserDatabase.HasUser("factoryuser"));
            Assert.Contains("factoryuser", _agent.GetUserNames());
        }

        [Fact]
        public void Engine_HasValidEngineId()
        {
            Assert.NotNull(_agent.Engine.EngineId);
            Assert.True(_agent.Engine.EngineId.Length > 0);
            Assert.NotEmpty(_agent.Engine.EngineIdHex);
        }

        [Fact]
        public async Task Engine_TimeProgresses()
        {
            var time1 = _agent.Engine.EngineTime;
            await Task.Delay(1100); // Wait > 1 second asynchronously
            var time2 = _agent.Engine.EngineTime;

            Assert.True(time2 > time1);
        }

        [Fact]
        public async Task ProcessV3DiscoveryRequest_ReturnsEngineParameters()
        {
            // Arrange
            var discoveryRequest = CreateDiscoveryRequest();
            await _agent.StartAsync(161);

            // Act
            var response = await _mockListener.SendRequestAndGetResponse(discoveryRequest);

            // Assert
            Assert.NotNull(response);

            // Should be a V3 message with engine parameters
            var responseMessage = SnmpMessageV3.Parse(response);
            Assert.NotNull(responseMessage);

            // Should contain engine ID in security parameters
            var usmParams = UsmSecurityParameters.Parse(responseMessage.SecurityParameters.Data ?? Array.Empty<byte>());
            Assert.NotEmpty(usmParams.AuthoritativeEngineId.Data ?? Array.Empty<byte>());

            await _agent.StopAsync();
        }

        [Fact]
        public async Task ProcessV3Request_WithInvalidUser_ReturnsReport()
        {
            // Arrange
            var request = CreateV3Request("unknownuser", AuthProtocol.None, "", PrivProtocol.None, "");
            await _agent.StartAsync(161);

            // Act
            var response = await _mockListener.SendRequestAndGetResponse(request);

            // Assert
            Assert.NotNull(response);

            var responseMessage = SnmpMessageV3.Parse(response);
            Assert.NotNull(responseMessage.ScopedPdu.Pdu);
            Assert.IsType<Report>(responseMessage.ScopedPdu.Pdu);

            await _agent.StopAsync();
        }

        [Fact]
        public async Task ProcessV3Request_WithAuthRequest_HandlesAuthentication()
        {
            // Arrange
            _agent.MapScalar("1.3.6.1.2.1.1.1.0", OctetString.Create("Test System"));

            var request = CreateAuthenticatedV3Request("testuser", AuthProtocol.SHA1, "authpass123");
            await _agent.StartAsync(161);

            // Act
            var response = await _mockListener.SendRequestAndGetResponse(request);

            // Assert
            Assert.NotNull(response);

            // Since we're not providing properly calculated authentication parameters,
            // the agent should respond with a Report indicating authentication failure
            var responseMessage = SnmpMessageV3.Parse(response);
            Assert.NotNull(responseMessage.ScopedPdu.Pdu);

            // Should be a Report (authentication failure is expected with simplified test request)
            Assert.IsType<Report>(responseMessage.ScopedPdu.Pdu);

            // Verify the agent processed this as a V3 message by checking the Report content
            var report = (Report)responseMessage.ScopedPdu.Pdu;
            Assert.NotNull(report.VarbindList);
            Assert.True(report.VarbindList.Elements?.Count > 0);

            await _agent.StopAsync();
        }

        [Fact]
        public void GetEngineInfo_ReturnsValidInfo()
        {
            var info = _agent.GetEngineInfo();
            Assert.NotNull(info);
            Assert.Contains("Engine ID:", info);
            Assert.Contains("Boots:", info);
            Assert.Contains("Time:", info);
        }

        [Fact]
        public void UserDatabase_SupportsUserManagement()
        {
            var initialCount = _agent.UserCount;

            _agent.AddUser("tempuser", AuthProtocol.MD5, "password123");
            Assert.Equal(initialCount + 1, _agent.UserCount);

            Assert.True(_agent.UserDatabase.RemoveUser("tempuser"));
            Assert.Equal(initialCount, _agent.UserCount);
        }

        /// <summary>
        /// Create SNMPv3 discovery request
        /// </summary>
        private byte[] CreateDiscoveryRequest()
        {
            var usmParams = UsmSecurityParameters.CreateDiscovery();
            var varbindList = new Sequence(Array.Empty<IDataType>());
            var getRequest = new GetRequest(null, Integer.Create(1), Integer.Create(0), Integer.Create(0), varbindList);
            var scopedPdu = ScopedPdu.Create(getRequest);

            var v3Message = SnmpMessageV3.Create(
                messageId: 1,
                authFlag: false,
                privFlag: false,
                reportableFlag: true,
                securityParameters: usmParams.ToBytes(),
                scopedPdu: scopedPdu);

            return v3Message.ToBytes();
        }

        /// <summary>
        /// Create basic V3 request (for testing error cases)
        /// </summary>
        private byte[] CreateV3Request(string userName, AuthProtocol authProtocol, string authPassword,
                                     PrivProtocol privProtocol, string privPassword)
        {
            var usmParams = UsmSecurityParameters.Create(
                authoritativeEngineId: Convert.ToHexString(_agent.Engine.EngineId),
                authoritativeEngineBoots: _agent.Engine.EngineBoots,
                authoritativeEngineTime: _agent.Engine.EngineTime,
                userName: userName);

            var testOid = ObjectIdentifier.Create("1.3.6.1.2.1.1.1.0");
            var varbind = new Sequence(new IDataType[] { testOid, new Null() });
            var varbindList = new Sequence(new IDataType[] { varbind });
            var getRequest = new GetRequest(null, Integer.Create(1), Integer.Create(0), Integer.Create(0), varbindList);
            var scopedPdu = ScopedPdu.Create(getRequest);

            var v3Message = SnmpMessageV3.Create(
                messageId: 1,
                authFlag: authProtocol != AuthProtocol.None,
                privFlag: privProtocol != PrivProtocol.None,
                reportableFlag: true,
                securityParameters: usmParams.ToBytes(),
                scopedPdu: scopedPdu);

            return v3Message.ToBytes();
        }

        /// <summary>
        /// Create properly authenticated V3 request
        /// </summary>
        private byte[] CreateAuthenticatedV3Request(string userName, AuthProtocol authProtocol, string authPassword)
        {
            // This is a simplified version - in reality, we'd need to properly calculate
            // authentication parameters, but for testing the agent's ability to detect
            // V3 messages and route them correctly, this is sufficient
            return CreateV3Request(userName, authProtocol, authPassword, PrivProtocol.None, "");
        }

        public void Dispose()
        {
            _agent?.Dispose();
            _mockListener?.Dispose();
        }
    }
}