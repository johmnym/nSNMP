using Microsoft.Extensions.Logging;

namespace nSNMP.Logging
{
    /// <summary>
    /// SNMP-specific logging interface with structured logging support
    /// </summary>
    public interface ISnmpLogger
    {
        /// <summary>
        /// Log SNMP request operations
        /// </summary>
        void LogRequest(string operation, string oid, string? community = null, TimeSpan? timeout = null);

        /// <summary>
        /// Log SNMP response operations
        /// </summary>
        void LogResponse(string operation, string oid, object? value, TimeSpan elapsed);

        /// <summary>
        /// Log SNMP errors with context
        /// </summary>
        void LogError(string operation, Exception exception, string? context = null);

        /// <summary>
        /// Log SNMPv3 security operations
        /// </summary>
        void LogSecurityOperation(string operation, string userName, string securityLevel, bool success);

        /// <summary>
        /// Log network transport operations
        /// </summary>
        void LogTransport(string operation, string endpoint, int messageSize, TimeSpan? elapsed = null);

        /// <summary>
        /// Log agent operations
        /// </summary>
        void LogAgent(string operation, string? context = null);

        /// <summary>
        /// Log performance metrics
        /// </summary>
        void LogPerformance(string operation, TimeSpan duration, int itemCount = 1);

        /// <summary>
        /// Check if logging is enabled for a specific level
        /// </summary>
        bool IsEnabled(LogLevel logLevel);
    }

    /// <summary>
    /// Default implementation of ISnmpLogger using Microsoft.Extensions.Logging
    /// </summary>
    public class SnmpLogger : ISnmpLogger
    {
        private readonly ILogger _logger;

        public SnmpLogger(ILogger<SnmpLogger> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public SnmpLogger(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public void LogRequest(string operation, string oid, string? community = null, TimeSpan? timeout = null)
        {
            if (!_logger.IsEnabled(LogLevel.Debug))
                return;

            _logger.LogDebug("SNMP {Operation} request for OID {Oid} with community {Community} and timeout {Timeout}",
                operation, oid, community ?? "(none)", timeout?.TotalMilliseconds);
        }

        public void LogResponse(string operation, string oid, object? value, TimeSpan elapsed)
        {
            if (!_logger.IsEnabled(LogLevel.Debug))
                return;

            _logger.LogDebug("SNMP {Operation} response for OID {Oid}: {Value} (elapsed: {ElapsedMs}ms)",
                operation, oid, value?.ToString() ?? "(null)", elapsed.TotalMilliseconds);
        }

        public void LogError(string operation, Exception exception, string? context = null)
        {
            _logger.LogError(exception, "SNMP {Operation} failed: {Context}",
                operation, context ?? "No additional context");
        }

        public void LogSecurityOperation(string operation, string userName, string securityLevel, bool success)
        {
            var logLevel = success ? LogLevel.Information : LogLevel.Warning;

            _logger.Log(logLevel, "SNMPv3 {Operation} for user {UserName} with security level {SecurityLevel}: {Result}",
                operation, userName, securityLevel, success ? "Success" : "Failed");
        }

        public void LogTransport(string operation, string endpoint, int messageSize, TimeSpan? elapsed = null)
        {
            if (!_logger.IsEnabled(LogLevel.Debug))
                return;

            _logger.LogDebug("SNMP transport {Operation} to {Endpoint}: {MessageSize} bytes{Elapsed}",
                operation, endpoint, messageSize,
                elapsed.HasValue ? $" in {elapsed.Value.TotalMilliseconds}ms" : "");
        }

        public void LogAgent(string operation, string? context = null)
        {
            if (!_logger.IsEnabled(LogLevel.Information))
                return;

            _logger.LogInformation("SNMP Agent {Operation}{Context}",
                operation, !string.IsNullOrEmpty(context) ? $": {context}" : "");
        }

        public void LogPerformance(string operation, TimeSpan duration, int itemCount = 1)
        {
            if (!_logger.IsEnabled(LogLevel.Information))
                return;

            var throughput = itemCount / duration.TotalSeconds;
            _logger.LogInformation("SNMP {Operation} performance: {ItemCount} items in {DurationMs}ms ({Throughput:F2} items/sec)",
                operation, itemCount, duration.TotalMilliseconds, throughput);
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return _logger.IsEnabled(logLevel);
        }
    }

    /// <summary>
    /// Null object pattern implementation for when logging is disabled
    /// </summary>
    public class NullSnmpLogger : ISnmpLogger
    {
        public static readonly ISnmpLogger Instance = new NullSnmpLogger();

        private NullSnmpLogger() { }

        public void LogRequest(string operation, string oid, string? community = null, TimeSpan? timeout = null) { }
        public void LogResponse(string operation, string oid, object? value, TimeSpan elapsed) { }
        public void LogError(string operation, Exception exception, string? context = null) { }
        public void LogSecurityOperation(string operation, string userName, string securityLevel, bool success) { }
        public void LogTransport(string operation, string endpoint, int messageSize, TimeSpan? elapsed = null) { }
        public void LogAgent(string operation, string? context = null) { }
        public void LogPerformance(string operation, TimeSpan duration, int itemCount = 1) { }
        public bool IsEnabled(LogLevel logLevel) => false;
    }
}