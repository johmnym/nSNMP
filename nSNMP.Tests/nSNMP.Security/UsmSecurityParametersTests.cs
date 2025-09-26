using nSNMP.Security;
using nSNMP.SMI.DataTypes.V1.Primitive;
using Xunit;

namespace nSNMP.Tests.nSNMP.Security
{
    public class UsmSecurityParametersTests
    {
        [Fact]
        public void Create_WithDefaults_CreatesEmptyParameters()
        {
            var parameters = UsmSecurityParameters.Create();

            Assert.Equal("", parameters.AuthoritativeEngineId.Value);
            Assert.Equal(0, parameters.AuthoritativeEngineBoots.Value);
            Assert.Equal(0, parameters.AuthoritativeEngineTime.Value);
            Assert.Equal("", parameters.UserName.Value);
            Assert.Empty(parameters.AuthenticationParameters.Data);
            Assert.Empty(parameters.PrivacyParameters.Data);
        }

        [Fact]
        public void Create_WithValues_CreatesCorrectParameters()
        {
            var engineId = "80001f888059dc486145a26322";
            var boots = 42;
            var time = 12345;
            var userName = "testuser";
            var authParams = new byte[] { 0x01, 0x02, 0x03 };
            var privParams = new byte[] { 0x04, 0x05, 0x06 };

            var parameters = UsmSecurityParameters.Create(engineId, boots, time, userName, authParams, privParams);

            Assert.Equal(engineId, parameters.AuthoritativeEngineId.Value);
            Assert.Equal(boots, parameters.AuthoritativeEngineBoots.Value);
            Assert.Equal(time, parameters.AuthoritativeEngineTime.Value);
            Assert.Equal(userName, parameters.UserName.Value);
            Assert.Equal(authParams, parameters.AuthenticationParameters.Data);
            Assert.Equal(privParams, parameters.PrivacyParameters.Data);
        }

        [Fact]
        public void CreateDiscovery_CreatesDiscoveryParameters()
        {
            var parameters = UsmSecurityParameters.CreateDiscovery("discoveryuser");

            Assert.Equal("", parameters.AuthoritativeEngineId.Value);
            Assert.Equal(0, parameters.AuthoritativeEngineBoots.Value);
            Assert.Equal(0, parameters.AuthoritativeEngineTime.Value);
            Assert.Equal("discoveryuser", parameters.UserName.Value);
            Assert.Empty(parameters.AuthenticationParameters.Data);
            Assert.Empty(parameters.PrivacyParameters.Data);
            Assert.True(parameters.IsDiscovery);
        }

        [Fact]
        public void ToBytes_RoundTrip_WorksCorrectly()
        {
            var original = UsmSecurityParameters.Create(
                "80001f888059dc486145a26322",
                42,
                12345,
                "testuser",
                new byte[] { 0x01, 0x02, 0x03 },
                new byte[] { 0x04, 0x05, 0x06 }
            );

            var bytes = original.ToBytes();
            var parsed = UsmSecurityParameters.Parse(bytes);

            Assert.Equal(original.AuthoritativeEngineId.Value, parsed.AuthoritativeEngineId.Value);
            Assert.Equal(original.AuthoritativeEngineBoots.Value, parsed.AuthoritativeEngineBoots.Value);
            Assert.Equal(original.AuthoritativeEngineTime.Value, parsed.AuthoritativeEngineTime.Value);
            Assert.Equal(original.UserName.Value, parsed.UserName.Value);
            Assert.Equal(original.AuthenticationParameters.Data, parsed.AuthenticationParameters.Data);
            Assert.Equal(original.PrivacyParameters.Data, parsed.PrivacyParameters.Data);
        }

        [Fact]
        public void Parse_EmptyData_ReturnsEmptyParameters()
        {
            var parameters = UsmSecurityParameters.Parse(Array.Empty<byte>());

            Assert.True(parameters.IsDiscovery);
            Assert.Equal("", parameters.AuthoritativeEngineId.Value);
            Assert.Equal("", parameters.UserName.Value);
        }

        [Fact]
        public void Parse_NullData_ReturnsEmptyParameters()
        {
            var parameters = UsmSecurityParameters.Parse(null);

            Assert.True(parameters.IsDiscovery);
            Assert.Equal("", parameters.AuthoritativeEngineId.Value);
            Assert.Equal("", parameters.UserName.Value);
        }

        [Fact]
        public void IsDiscovery_DetectsDiscoveryCorrectly()
        {
            var discoveryParams = UsmSecurityParameters.CreateDiscovery();
            var normalParams = UsmSecurityParameters.Create("engineid", 1, 100, "user");

            Assert.True(discoveryParams.IsDiscovery);
            Assert.False(normalParams.IsDiscovery);
        }

        [Fact]
        public void EngineIdHex_FormatsCorrectly()
        {
            var engineIdBytes = new byte[] { 0x80, 0x00, 0x1F, 0x88 };
            var parameters = new UsmSecurityParameters(
                new OctetString(engineIdBytes),
                Integer.Create(0),
                Integer.Create(0),
                OctetString.Create("user"),
                new OctetString(Array.Empty<byte>()),
                new OctetString(Array.Empty<byte>())
            );

            var expectedHex = "80001F88";
            Assert.Equal(expectedHex, parameters.EngineIdHex);
        }

        [Fact]
        public void HasAuthParams_DetectsCorrectly()
        {
            var withAuth = UsmSecurityParameters.Create("", 0, 0, "", new byte[] { 0x01 });
            var withoutAuth = UsmSecurityParameters.Create("", 0, 0, "", Array.Empty<byte>());

            Assert.True(withAuth.HasAuthParams);
            Assert.False(withoutAuth.HasAuthParams);
        }

        [Fact]
        public void HasPrivParams_DetectsCorrectly()
        {
            var withPriv = UsmSecurityParameters.Create("", 0, 0, "", null, new byte[] { 0x01 });
            var withoutPriv = UsmSecurityParameters.Create("", 0, 0, "", null, Array.Empty<byte>());

            Assert.True(withPriv.HasPrivParams);
            Assert.False(withoutPriv.HasPrivParams);
        }
    }
}