using nSNMP.Telemetry;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using Xunit;

namespace nSNMP.Core.Tests
{
    public class SnmpTelemetryTests
    {
        [Fact]
        public void SnmpTelemetry_Initialization_ShouldWork()
        {
            Assert.NotNull(SnmpTelemetry.ActivitySource);
            Assert.NotNull(SnmpTelemetry.Meter);
            Assert.Equal("nSNMP", SnmpTelemetry.InstrumentationName);
            Assert.Equal("1.0.0", SnmpTelemetry.InstrumentationVersion);
        }

        [Fact]
        public void SnmpTelemetry_Metrics_ShouldBeCreated()
        {
            Assert.NotNull(SnmpTelemetry.RequestsTotal);
            Assert.NotNull(SnmpTelemetry.ResponsesTotal);
            Assert.NotNull(SnmpTelemetry.ErrorsTotal);
            Assert.NotNull(SnmpTelemetry.SecurityOperationsTotal);
            Assert.NotNull(SnmpTelemetry.TransportOperationsTotal);
            Assert.NotNull(SnmpTelemetry.RequestDuration);
            Assert.NotNull(SnmpTelemetry.SecurityOperationDuration);
            Assert.NotNull(SnmpTelemetry.TransportDuration);
            Assert.NotNull(SnmpTelemetry.ActiveConnections);
            Assert.NotNull(SnmpTelemetry.MessageSize);
            Assert.NotNull(SnmpTelemetry.ResponseItemCount);
        }

        [Fact]
        public void StartActivity_ShouldCreateActivityWithCorrectTags()
        {
            var operation = "GET";
            var endpoint = "127.0.0.1:161";
            var community = "public";

            using var activity = SnmpTelemetry.StartActivity(operation, endpoint, community);

            if (activity != null)
            {
                Assert.Equal("snmp.get", activity.OperationName);
                Assert.Contains(activity.Tags, tag => tag.Key == "snmp.operation" && tag.Value == operation);
                Assert.Contains(activity.Tags, tag => tag.Key == "snmp.endpoint" && tag.Value == endpoint);
                Assert.Contains(activity.Tags, tag => tag.Key == "snmp.community" && tag.Value == community);
                Assert.Contains(activity.Tags, tag => tag.Key == "snmp.library" && tag.Value == "nSNMP");
            }
        }

        [Fact]
        public void StartActivity_WithUserName_ShouldIncludeUserTag()
        {
            var operation = "GET";
            var endpoint = "127.0.0.1:161";
            var userName = "testuser";

            using var activity = SnmpTelemetry.StartActivity(operation, endpoint, userName: userName);

            if (activity != null)
            {
                Assert.Contains(activity.Tags, tag => tag.Key == "snmp.user" && tag.Value == userName);
            }
        }

        [Fact]
        public void RecordRequest_ShouldNotThrow()
        {
            // This test verifies that recording metrics doesn't throw exceptions
            // The actual metric values would be tested with a proper telemetry test framework

            var operation = "GET";
            var endpoint = "127.0.0.1:161";
            var duration = TimeSpan.FromMilliseconds(100);
            var itemCount = 5;
            var messageSize = 1024L;

            SnmpTelemetry.RecordRequest(operation, endpoint, duration, itemCount, messageSize);

            // If we get here without exception, the test passes
            Assert.True(true);
        }

        [Fact]
        public void RecordError_ShouldNotThrow()
        {
            var operation = "GET";
            var endpoint = "127.0.0.1:161";
            var duration = TimeSpan.FromMilliseconds(50);
            var errorType = "TimeoutException";
            var messageSize = 512L;

            SnmpTelemetry.RecordError(operation, endpoint, duration, errorType, messageSize);

            // If we get here without exception, the test passes
            Assert.True(true);
        }

        [Fact]
        public void RecordSecurityOperation_ShouldNotThrow()
        {
            var operation = "AUTH";
            var userName = "testuser";
            var securityLevel = "authPriv";
            var duration = TimeSpan.FromMilliseconds(25);

            SnmpTelemetry.RecordSecurityOperation(operation, userName, securityLevel, duration, true);
            SnmpTelemetry.RecordSecurityOperation(operation, userName, securityLevel, duration, false);

            // If we get here without exception, the test passes
            Assert.True(true);
        }

        [Fact]
        public void RecordTransportOperation_ShouldNotThrow()
        {
            var operation = "SEND";
            var endpoint = "127.0.0.1:161";
            var messageSize = 2048L;
            var duration = TimeSpan.FromMilliseconds(10);

            SnmpTelemetry.RecordTransportOperation(operation, endpoint, messageSize, duration);
            SnmpTelemetry.RecordTransportOperation(operation, endpoint, messageSize);

            // If we get here without exception, the test passes
            Assert.True(true);
        }

        [Fact]
        public void SetActivityError_ShouldSetCorrectTags()
        {
            using var activity = SnmpTelemetry.StartActivity("TEST");
            var exception = new InvalidOperationException("Test error");

            SnmpTelemetry.SetActivityError(activity, exception);

            if (activity != null)
            {
                Assert.Equal(ActivityStatusCode.Error, activity.Status);
                Assert.Contains(activity.Tags, tag => tag.Key == "error" && tag.Value == "True");
                Assert.Contains(activity.Tags, tag => tag.Key == "error.type" && tag.Value == "InvalidOperationException");
                Assert.Contains(activity.Tags, tag => tag.Key == "error.message" && tag.Value == "Test error");
            }
        }

        [Fact]
        public void SetActivitySuccess_ShouldSetCorrectTags()
        {
            using var activity = SnmpTelemetry.StartActivity("TEST");
            var itemCount = 10;

            SnmpTelemetry.SetActivitySuccess(activity, itemCount);

            if (activity != null)
            {
                Assert.Equal(ActivityStatusCode.Ok, activity.Status);
                Assert.Contains(activity.Tags, tag => tag.Key == "snmp.item_count" && tag.Value == "10");
            }
        }

        [Fact]
        public void TelemetryExtensions_StartTimer_ShouldWork()
        {
            var tags = new TagList { { "test", "value" } };

            using var timer = SnmpTelemetry.RequestDuration.StartTimer(tags);

            // Timer should dispose without error
            Assert.NotNull(timer);
        }

        [Fact]
        public void ActivitySource_Name_ShouldBeCorrect()
        {
            Assert.Equal("nSNMP", SnmpTelemetry.ActivitySource.Name);
            Assert.Equal("1.0.0", SnmpTelemetry.ActivitySource.Version);
        }

        [Fact]
        public void Meter_Name_ShouldBeCorrect()
        {
            Assert.Equal("nSNMP", SnmpTelemetry.Meter.Name);
            Assert.Equal("1.0.0", SnmpTelemetry.Meter.Version);
        }

        [Fact]
        public void ActivityTags_ShouldSupportNullValues()
        {
            // Test that null values are handled gracefully
            using var activity = SnmpTelemetry.StartActivity("TEST", null, null, null);

            if (activity != null)
            {
                Assert.Contains(activity.Tags, tag => tag.Key == "snmp.operation" && tag.Value == "TEST");
                Assert.DoesNotContain(activity.Tags, tag => tag.Key == "snmp.endpoint");
                Assert.DoesNotContain(activity.Tags, tag => tag.Key == "snmp.community");
                Assert.DoesNotContain(activity.Tags, tag => tag.Key == "snmp.user");
            }
        }
    }
}