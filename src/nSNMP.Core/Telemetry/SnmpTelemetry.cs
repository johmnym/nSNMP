using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace nSNMP.Telemetry
{
    /// <summary>
    /// OpenTelemetry instrumentation for SNMP operations
    /// Provides metrics and distributed tracing for SNMP library
    /// </summary>
    public static class SnmpTelemetry
    {
        /// <summary>
        /// The name of the SNMP telemetry instrumentation
        /// </summary>
        public const string InstrumentationName = "nSNMP";

        /// <summary>
        /// The version of the SNMP telemetry instrumentation
        /// </summary>
        public const string InstrumentationVersion = "1.0.0";

        /// <summary>
        /// ActivitySource for distributed tracing
        /// </summary>
        public static readonly ActivitySource ActivitySource = new(InstrumentationName, InstrumentationVersion);

        /// <summary>
        /// Meter for metrics collection
        /// </summary>
        public static readonly Meter Meter = new(InstrumentationName, InstrumentationVersion);

        // Counters for operation tracking
        public static readonly Counter<long> RequestsTotal = Meter.CreateCounter<long>(
            "snmp_requests_total",
            "operation",
            "Total number of SNMP requests");

        public static readonly Counter<long> ResponsesTotal = Meter.CreateCounter<long>(
            "snmp_responses_total",
            "operation",
            "Total number of SNMP responses");

        public static readonly Counter<long> ErrorsTotal = Meter.CreateCounter<long>(
            "snmp_errors_total",
            "operation",
            "Total number of SNMP errors");

        public static readonly Counter<long> SecurityOperationsTotal = Meter.CreateCounter<long>(
            "snmp_security_operations_total",
            "operation",
            "Total number of SNMPv3 security operations");

        public static readonly Counter<long> TransportOperationsTotal = Meter.CreateCounter<long>(
            "snmp_transport_operations_total",
            "operation",
            "Total number of transport operations");

        // Histograms for latency tracking
        public static readonly Histogram<double> RequestDuration = Meter.CreateHistogram<double>(
            "snmp_request_duration_seconds",
            "s",
            "Duration of SNMP requests");

        public static readonly Histogram<double> SecurityOperationDuration = Meter.CreateHistogram<double>(
            "snmp_security_operation_duration_seconds",
            "s",
            "Duration of SNMPv3 security operations");

        public static readonly Histogram<double> TransportDuration = Meter.CreateHistogram<double>(
            "snmp_transport_duration_seconds",
            "s",
            "Duration of transport operations");

        // Gauges for current state - use a callback to provide current value
        public static readonly ObservableGauge<int> ActiveConnections = Meter.CreateObservableGauge<int>(
            "snmp_active_connections",
            unit: "connection",
            description: "Number of active SNMP connections",
            observeValue: () => GetActiveConnectionCount());

        public static readonly Histogram<long> MessageSize = Meter.CreateHistogram<long>(
            "snmp_message_size_bytes",
            "bytes",
            "Size of SNMP messages");

        public static readonly Histogram<long> ResponseItemCount = Meter.CreateHistogram<long>(
            "snmp_response_item_count",
            "items",
            "Number of items in SNMP responses");

        /// <summary>
        /// Creates a new activity for an SNMP operation
        /// </summary>
        /// <param name="operationName">The name of the operation</param>
        /// <param name="endpoint">The target endpoint</param>
        /// <param name="community">The community string (for v1/v2c)</param>
        /// <param name="userName">The user name (for v3)</param>
        /// <returns>The created activity or null if tracing is not enabled</returns>
        public static Activity? StartActivity(string operationName, string? endpoint = null, string? community = null, string? userName = null)
        {
            var activity = ActivitySource.StartActivity($"snmp.{operationName.ToLowerInvariant()}");

            if (activity != null)
            {
                activity.SetTag("snmp.operation", operationName);

                if (!string.IsNullOrEmpty(endpoint))
                    activity.SetTag("snmp.endpoint", endpoint);

                if (!string.IsNullOrEmpty(community))
                    activity.SetTag("snmp.community", community);

                if (!string.IsNullOrEmpty(userName))
                    activity.SetTag("snmp.user", userName);

                activity.SetTag("snmp.library", "nSNMP");
                activity.SetTag("snmp.version", InstrumentationVersion);
            }

            return activity;
        }

        /// <summary>
        /// Records a successful SNMP request
        /// </summary>
        /// <param name="operation">The operation type</param>
        /// <param name="endpoint">The target endpoint</param>
        /// <param name="duration">The operation duration</param>
        /// <param name="itemCount">The number of items processed</param>
        /// <param name="messageSize">The size of the message</param>
        public static void RecordRequest(string operation, string endpoint, TimeSpan duration, int itemCount = 1, long messageSize = 0)
        {
            var tags = new TagList
            {
                { "operation", operation },
                { "endpoint", endpoint },
                { "status", "success" }
            };

            RequestsTotal.Add(1, tags);
            ResponsesTotal.Add(1, tags);
            RequestDuration.Record(duration.TotalSeconds, tags);

            if (itemCount > 0)
                ResponseItemCount.Record(itemCount, tags);

            if (messageSize > 0)
                MessageSize.Record(messageSize, tags);
        }

        /// <summary>
        /// Records a failed SNMP request
        /// </summary>
        /// <param name="operation">The operation type</param>
        /// <param name="endpoint">The target endpoint</param>
        /// <param name="duration">The operation duration</param>
        /// <param name="errorType">The type of error</param>
        /// <param name="messageSize">The size of the message</param>
        public static void RecordError(string operation, string endpoint, TimeSpan duration, string errorType, long messageSize = 0)
        {
            var tags = new TagList
            {
                { "operation", operation },
                { "endpoint", endpoint },
                { "status", "error" },
                { "error_type", errorType }
            };

            RequestsTotal.Add(1, tags);
            ErrorsTotal.Add(1, tags);
            RequestDuration.Record(duration.TotalSeconds, tags);

            if (messageSize > 0)
                MessageSize.Record(messageSize, tags);
        }

        /// <summary>
        /// Records a security operation
        /// </summary>
        /// <param name="operation">The security operation type</param>
        /// <param name="userName">The user name</param>
        /// <param name="securityLevel">The security level</param>
        /// <param name="duration">The operation duration</param>
        /// <param name="success">Whether the operation succeeded</param>
        public static void RecordSecurityOperation(string operation, string userName, string securityLevel, TimeSpan duration, bool success)
        {
            var tags = new TagList
            {
                { "operation", operation },
                { "user", userName },
                { "security_level", securityLevel },
                { "status", success ? "success" : "failure" }
            };

            SecurityOperationsTotal.Add(1, tags);
            SecurityOperationDuration.Record(duration.TotalSeconds, tags);
        }

        /// <summary>
        /// Records a transport operation
        /// </summary>
        /// <param name="operation">The transport operation type</param>
        /// <param name="endpoint">The target endpoint</param>
        /// <param name="messageSize">The size of the message</param>
        /// <param name="duration">The operation duration</param>
        public static void RecordTransportOperation(string operation, string endpoint, long messageSize, TimeSpan? duration = null)
        {
            var tags = new TagList
            {
                { "operation", operation },
                { "endpoint", endpoint }
            };

            TransportOperationsTotal.Add(1, tags);
            MessageSize.Record(messageSize, tags);

            if (duration.HasValue)
                TransportDuration.Record(duration.Value.TotalSeconds, tags);
        }

        /// <summary>
        /// Sets the error status on an activity
        /// </summary>
        /// <param name="activity">The activity to update</param>
        /// <param name="exception">The exception that occurred</param>
        public static void SetActivityError(Activity? activity, Exception exception)
        {
            if (activity != null)
            {
                activity.SetStatus(ActivityStatusCode.Error, exception.Message);
                activity.SetTag("error", true);
                activity.SetTag("error.type", exception.GetType().Name);
                activity.SetTag("error.message", exception.Message);
            }
        }

        /// <summary>
        /// Sets the success status on an activity
        /// </summary>
        /// <param name="activity">The activity to update</param>
        /// <param name="itemCount">The number of items processed</param>
        public static void SetActivitySuccess(Activity? activity, int itemCount = 1)
        {
            if (activity != null)
            {
                activity.SetStatus(ActivityStatusCode.Ok);
                activity.SetTag("snmp.item_count", itemCount);
            }
        }

        /// <summary>
        /// Dispose of telemetry resources
        /// </summary>
        public static void Dispose()
        {
            ActivitySource?.Dispose();
            Meter?.Dispose();
        }

        /// <summary>
        /// Get the current number of active connections
        /// This is a placeholder implementation - in a real scenario this would track actual connections
        /// </summary>
        private static int GetActiveConnectionCount()
        {
            // For now, return 0 as a placeholder
            // In a real implementation, this would track active UDP channels/connections
            return 0;
        }
    }

    /// <summary>
    /// Extension methods for integrating telemetry with SNMP operations
    /// </summary>
    public static class TelemetryExtensions
    {
        /// <summary>
        /// Records the duration of an operation using a timer pattern
        /// </summary>
        /// <param name="histogram">The histogram to record to</param>
        /// <param name="tags">The tags to apply</param>
        /// <returns>A disposable timer that records the duration when disposed</returns>
        public static IDisposable StartTimer(this Histogram<double> histogram, TagList tags)
        {
            return new HistogramTimer(histogram, tags);
        }

        private class HistogramTimer : IDisposable
        {
            private readonly Histogram<double> _histogram;
            private readonly TagList _tags;
            private readonly DateTime _startTime;

            public HistogramTimer(Histogram<double> histogram, TagList tags)
            {
                _histogram = histogram;
                _tags = tags;
                _startTime = DateTime.UtcNow;
            }

            public void Dispose()
            {
                var duration = DateTime.UtcNow - _startTime;
                _histogram.Record(duration.TotalSeconds, _tags);
            }
        }
    }
}