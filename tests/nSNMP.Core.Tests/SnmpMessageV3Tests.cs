using nSNMP.Message;
using nSNMP.SMI.DataTypes.V1.Primitive;
using nSNMP.SMI.PDUs;
using nSNMP.SMI.DataTypes.V1.Constructed;
using nSNMP.SMI.DataTypes;
using Xunit;

namespace nSNMP.Core.Tests
{
    public class SnmpMessageV3Tests
    {
        [Fact]
        public void Create_WithDefaults_CreatesCorrectMessage()
        {
            var message = SnmpMessageV3.Create(12345);

            Assert.Equal(12345, message.MessageId.Value);
            Assert.Equal(65507, message.MaxSize.Value);
            Assert.False(message.AuthFlag);
            Assert.False(message.PrivFlag);
            Assert.True(message.ReportableFlag);
            Assert.Equal(3, message.SecurityModel.Value);
            Assert.Empty(message.SecurityParameters.Data ?? Array.Empty<byte>());
        }

        [Fact]
        public void Create_WithFlags_SetsCorrectFlags()
        {
            var message = SnmpMessageV3.Create(12345, authFlag: true, privFlag: true, reportableFlag: false);

            Assert.True(message.AuthFlag);
            Assert.True(message.PrivFlag);
            Assert.False(message.ReportableFlag);
        }

        [Fact]
        public void AuthFlag_ReadsCorrectly()
        {
            var messageWithAuth = SnmpMessageV3.Create(1, authFlag: true);
            var messageWithoutAuth = SnmpMessageV3.Create(1, authFlag: false);

            Assert.True(messageWithAuth.AuthFlag);
            Assert.False(messageWithoutAuth.AuthFlag);
        }

        [Fact]
        public void PrivFlag_ReadsCorrectly()
        {
            var messageWithPriv = SnmpMessageV3.Create(1, privFlag: true);
            var messageWithoutPriv = SnmpMessageV3.Create(1, privFlag: false);

            Assert.True(messageWithPriv.PrivFlag);
            Assert.False(messageWithoutPriv.PrivFlag);
        }

        [Fact]
        public void ReportableFlag_ReadsCorrectly()
        {
            var messageReportable = SnmpMessageV3.Create(1, reportableFlag: true);
            var messageNotReportable = SnmpMessageV3.Create(1, reportableFlag: false);

            Assert.True(messageReportable.ReportableFlag);
            Assert.False(messageNotReportable.ReportableFlag);
        }

        [Fact]
        public void ToBytes_RoundTrip_WorksCorrectly()
        {
            var varbindList = new Sequence(Array.Empty<IDataType>());
            var getRequest = new GetRequest(null, Integer.Create(1), Integer.Create(0), Integer.Create(0), varbindList);
            var scopedPdu = ScopedPdu.Create(getRequest, "contextEngine", "contextName");

            var original = SnmpMessageV3.Create(
                messageId: 12345,
                maxSize: 32768,
                authFlag: true,
                privFlag: false,
                reportableFlag: true,
                securityParameters: new byte[] { 0x01, 0x02, 0x03 },
                scopedPdu: scopedPdu
            );

            var bytes = original.ToBytes();
            var parsed = SnmpMessageV3.Parse(bytes);

            Assert.Equal(original.MessageId.Value, parsed.MessageId.Value);
            Assert.Equal(original.MaxSize.Value, parsed.MaxSize.Value);
            Assert.Equal(original.AuthFlag, parsed.AuthFlag);
            Assert.Equal(original.PrivFlag, parsed.PrivFlag);
            Assert.Equal(original.ReportableFlag, parsed.ReportableFlag);
            Assert.Equal(original.SecurityModel.Value, parsed.SecurityModel.Value);
            Assert.Equal(original.SecurityParameters.Data, parsed.SecurityParameters.Data);
        }

        [Fact]
        public void Parse_InvalidVersion_ThrowsException()
        {
            // Create a message with version 2 instead of 3
            var messageData = new Sequence(new IDataType[]
            {
                Integer.Create(2), // Wrong version
                new Sequence(new IDataType[]
                {
                    Integer.Create(1),
                    Integer.Create(65507),
                    new OctetString(new byte[] { 0x04 }),
                    Integer.Create(3)
                }),
                new OctetString(Array.Empty<byte>()),
                new ScopedPdu()
            });

            var bytes = messageData.ToBytes();

            Assert.Throws<ArgumentException>(() => SnmpMessageV3.Parse(bytes));
        }

        [Theory]
        [InlineData(0x01, true, false, false)]   // Auth only
        [InlineData(0x02, false, true, false)]   // Priv only
        [InlineData(0x04, false, false, true)]   // Reportable only
        [InlineData(0x03, true, true, false)]    // Auth + Priv
        [InlineData(0x05, true, false, true)]    // Auth + Reportable
        [InlineData(0x07, true, true, true)]     // All flags
        [InlineData(0x00, false, false, false)]  // No flags
        public void Flags_ParseCorrectly(byte flagsByte, bool expectedAuth, bool expectedPriv, bool expectedReportable)
        {
            var message = new SnmpMessageV3(
                Integer.Create(1),
                Integer.Create(65507),
                new OctetString(new[] { flagsByte }),
                Integer.Create(3),
                new OctetString(Array.Empty<byte>()),
                new ScopedPdu()
            );

            Assert.Equal(expectedAuth, message.AuthFlag);
            Assert.Equal(expectedPriv, message.PrivFlag);
            Assert.Equal(expectedReportable, message.ReportableFlag);
        }

        [Fact]
        public void Create_WithSecurityParameters_IncludesParameters()
        {
            var securityParams = new byte[] { 0xAA, 0xBB, 0xCC, 0xDD };
            var message = SnmpMessageV3.Create(1, securityParameters: securityParams);

            Assert.Equal(securityParams, message.SecurityParameters.Data);
        }

        [Fact]
        public void Create_WithScopedPdu_IncludesPdu()
        {
            var varbindList = new Sequence(Array.Empty<IDataType>());
            var getRequest = new GetRequest(null, Integer.Create(1), Integer.Create(0), Integer.Create(0), varbindList);
            var scopedPdu = ScopedPdu.Create(getRequest);

            var message = SnmpMessageV3.Create(1, scopedPdu: scopedPdu);

            Assert.Same(scopedPdu, message.ScopedPdu);
        }
    }
}