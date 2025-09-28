using Microsoft.Extensions.Logging;
using nSNMP.Logging;
using nSNMP.Manager;
using System.Net;
using Xunit;

namespace nSNMP.Tests.nSNMP.Logging
{
    public class SnmpLoggerTests
    {
        [Fact]
        public void SnmpLogger_Creation_ShouldWork()
        {
            using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            var logger = loggerFactory.CreateLogger<SnmpLogger>();
            var snmpLogger = new SnmpLogger(logger);

            Assert.NotNull(snmpLogger);
        }

        [Fact]
        public void NullSnmpLogger_ShouldNotThrow()
        {
            var logger = NullSnmpLogger.Instance;

            // These should all be no-ops and not throw
            logger.LogRequest("GET", "1.3.6.1.2.1.1.1.0", "public", TimeSpan.FromSeconds(5));
            logger.LogResponse("GET", "1.3.6.1.2.1.1.1.0", "System Description", TimeSpan.FromMilliseconds(100));
            logger.LogError("GET", new Exception("Test exception"), "Test context");
            logger.LogSecurityOperation("AUTH", "testuser", "authPriv", true);
            logger.LogTransport("SEND", "127.0.0.1:161", 1024, TimeSpan.FromMilliseconds(50));
            logger.LogAgent("START", "Agent started");
            logger.LogPerformance("BULK_GET", TimeSpan.FromSeconds(1), 100);

            Assert.False(logger.IsEnabled(LogLevel.Debug));
            Assert.False(logger.IsEnabled(LogLevel.Information));
        }

        [Fact]
        public void SnmpClient_WithLogger_ShouldWork()
        {
            using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            var logger = loggerFactory.CreateLogger<SnmpLogger>();
            var snmpLogger = new SnmpLogger(logger);

            var endpoint = new IPEndPoint(IPAddress.Loopback, 161);
            using var client = new SnmpClient(endpoint, logger: snmpLogger);

            Assert.NotNull(client);
        }

        [Fact]
        public void SnmpLogger_LoggingMethods_ShouldWork()
        {
            var testLogger = new TestLogger();
            var snmpLogger = new SnmpLogger(testLogger);

            snmpLogger.LogRequest("GET", "1.3.6.1.2.1.1.1.0", "public", TimeSpan.FromSeconds(5));
            snmpLogger.LogResponse("GET", "1.3.6.1.2.1.1.1.0", "System Description", TimeSpan.FromMilliseconds(100));
            snmpLogger.LogError("GET", new Exception("Test exception"), "Test context");
            snmpLogger.LogSecurityOperation("AUTH", "testuser", "authPriv", true);
            snmpLogger.LogTransport("SEND", "127.0.0.1:161", 1024, TimeSpan.FromMilliseconds(50));
            snmpLogger.LogAgent("START", "Agent started");
            snmpLogger.LogPerformance("BULK_GET", TimeSpan.FromSeconds(1), 100);

            Assert.True(testLogger.LoggedMessages.Count > 0);
        }

        [Fact]
        public void SnmpLogger_IsEnabled_ShouldWork()
        {
            var testLogger = new TestLogger();
            var snmpLogger = new SnmpLogger(testLogger);

            // TestLogger returns true for all levels by default
            Assert.True(snmpLogger.IsEnabled(LogLevel.Debug));
            Assert.True(snmpLogger.IsEnabled(LogLevel.Information));
            Assert.True(snmpLogger.IsEnabled(LogLevel.Warning));
            Assert.True(snmpLogger.IsEnabled(LogLevel.Error));
        }

        private class TestLogger : ILogger
        {
            public List<string> LoggedMessages { get; } = new();

            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            {
                LoggedMessages.Add(formatter(state, exception));
            }
        }
    }
}