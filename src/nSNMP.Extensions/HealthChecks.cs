using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using nSNMP.Abstractions;
using Microsoft.Extensions.Logging;

namespace nSNMP.Extensions
{
    /// <summary>
    /// Health check result
    /// </summary>
    public enum HealthStatus
    {
        Healthy,
        Degraded,
        Unhealthy
    }

    /// <summary>
    /// Health check result details
    /// </summary>
    public class HealthCheckResult
    {
        public HealthStatus Status { get; init; }
        public string? Description { get; init; }
        public Exception? Exception { get; init; }
        public TimeSpan Duration { get; init; }
        public Dictionary<string, object> Data { get; init; } = new();

        public static HealthCheckResult Healthy(string? description = null, Dictionary<string, object>? data = null) =>
            new() { Status = HealthStatus.Healthy, Description = description, Data = data ?? new() };

        public static HealthCheckResult Degraded(string? description = null, Dictionary<string, object>? data = null) =>
            new() { Status = HealthStatus.Degraded, Description = description, Data = data ?? new() };

        public static HealthCheckResult Unhealthy(string? description = null, Exception? exception = null, Dictionary<string, object>? data = null) =>
            new() { Status = HealthStatus.Unhealthy, Description = description, Exception = exception, Data = data ?? new() };
    }

    /// <summary>
    /// SNMP health check implementation
    /// </summary>
    public class SnmpHealthCheck
    {
        private readonly ISnmpClient _client;
        private readonly SnmpHealthCheckOptions _options;
        private readonly ILogger? _logger;

        public SnmpHealthCheck(ISnmpClient client, SnmpHealthCheckOptions? options = null, ILogger? logger = null)
        {
            _client = client;
            _options = options ?? new SnmpHealthCheckOptions();
            _logger = logger;
        }

        /// <summary>
        /// Performs a health check
        /// </summary>
        public async Task<HealthCheckResult> CheckHealthAsync(CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();
            var data = new Dictionary<string, object>
            {
                ["endpoint"] = _client.Endpoint.ToString(),
                ["version"] = _client.Version.ToString()
            };

            try
            {
                _logger?.LogDebug("Starting SNMP health check for {Endpoint}", _client.Endpoint);

                // Perform basic connectivity test
                var testOids = _options.TestOids.Any() ? _options.TestOids.ToArray() : new[] { "1.3.6.1.2.1.1.3.0" }; // sysUpTime
                var results = await _client.GetAsync(testOids, cancellationToken);

                stopwatch.Stop();
                data["duration_ms"] = stopwatch.ElapsedMilliseconds;
                data["response_count"] = results.Count;
                data["test_oids"] = testOids;

                // Check if we got expected responses
                if (results.Count == 0)
                {
                    return HealthCheckResult.Unhealthy("No response received from SNMP agent", data: data);
                }

                // Check response time threshold
                if (_options.MaxResponseTime.HasValue && stopwatch.Elapsed > _options.MaxResponseTime.Value)
                {
                    data["threshold_ms"] = _options.MaxResponseTime.Value.TotalMilliseconds;
                    return HealthCheckResult.Degraded($"Response time {stopwatch.ElapsedMilliseconds}ms exceeds threshold", data: data);
                }

                // Validate specific responses if configured
                if (_options.ExpectedValues.Any())
                {
                    foreach (var expected in _options.ExpectedValues)
                    {
                        var result = results.FirstOrDefault(r => r.Oid == expected.Key);
                        if (result == null)
                        {
                            return HealthCheckResult.Degraded($"Expected OID {expected.Key} not found in response", data: data);
                        }

                        if (!ValidateValue(result.Data, expected.Value))
                        {
                            data["expected_value"] = expected.Value;
                            data["actual_value"] = result.Data.Value;
                            return HealthCheckResult.Degraded($"OID {expected.Key} value mismatch", data: data);
                        }
                    }
                }

                _logger?.LogDebug("SNMP health check completed successfully in {Duration}ms", stopwatch.ElapsedMilliseconds);
                return HealthCheckResult.Healthy("SNMP agent is responding normally", data);
            }
            catch (TaskCanceledException)
            {
                stopwatch.Stop();
                data["duration_ms"] = stopwatch.ElapsedMilliseconds;
                return HealthCheckResult.Unhealthy("Health check timed out", data: data);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                data["duration_ms"] = stopwatch.ElapsedMilliseconds;
                data["error_type"] = ex.GetType().Name;

                _logger?.LogWarning(ex, "SNMP health check failed for {Endpoint}", _client.Endpoint);
                return HealthCheckResult.Unhealthy("SNMP health check failed", ex, data);
            }
        }

        private static bool ValidateValue(IDataType actual, object expected)
        {
            if (expected is string expectedStr)
            {
                return actual.ToString() == expectedStr;
            }

            if (expected is IDataType expectedData)
            {
                return actual.Value.Equals(expectedData.Value);
            }

            return actual.Value.Equals(expected);
        }
    }

    /// <summary>
    /// Options for SNMP health checks
    /// </summary>
    public class SnmpHealthCheckOptions
    {
        /// <summary>
        /// OIDs to test during health check (defaults to sysUpTime)
        /// </summary>
        public List<string> TestOids { get; set; } = new();

        /// <summary>
        /// Maximum acceptable response time
        /// </summary>
        public TimeSpan? MaxResponseTime { get; set; }

        /// <summary>
        /// Expected values for specific OIDs
        /// </summary>
        public Dictionary<string, object> ExpectedValues { get; set; } = new();

        /// <summary>
        /// Timeout for health check operations
        /// </summary>
        public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(10);
    }

    /// <summary>
    /// Aggregated health check for multiple SNMP endpoints
    /// </summary>
    public class SnmpAggregateHealthCheck
    {
        private readonly Dictionary<string, SnmpHealthCheck> _healthChecks;
        private readonly ILogger? _logger;

        public SnmpAggregateHealthCheck(Dictionary<string, SnmpHealthCheck> healthChecks, ILogger? logger = null)
        {
            _healthChecks = healthChecks;
            _logger = logger;
        }

        /// <summary>
        /// Performs health checks on all configured endpoints
        /// </summary>
        public async Task<AggregateHealthCheckResult> CheckHealthAsync(CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();
            var results = new Dictionary<string, HealthCheckResult>();
            var tasks = _healthChecks.Select(async kvp =>
            {
                try
                {
                    var result = await kvp.Value.CheckHealthAsync(cancellationToken).ConfigureAwait(false);
                    lock (results)
                    {
                        results[kvp.Key] = result;
                    }
                }
                catch (Exception ex)
                {
                    lock (results)
                    {
                        results[kvp.Key] = HealthCheckResult.Unhealthy($"Health check failed: {ex.Message}", ex);
                    }
                }
            });

            await Task.WhenAll(tasks).ConfigureAwait(false);
            stopwatch.Stop();

            var overallStatus = DetermineOverallStatus(results.Values);
            var totalEndpoints = results.Count;
            var healthyEndpoints = results.Values.Count(r => r.Status == HealthStatus.Healthy);
            var degradedEndpoints = results.Values.Count(r => r.Status == HealthStatus.Degraded);
            var unhealthyEndpoints = results.Values.Count(r => r.Status == HealthStatus.Unhealthy);

            return new AggregateHealthCheckResult
            {
                Status = overallStatus,
                Duration = stopwatch.Elapsed,
                Results = results,
                Summary = new Dictionary<string, object>
                {
                    ["total_endpoints"] = totalEndpoints,
                    ["healthy_endpoints"] = healthyEndpoints,
                    ["degraded_endpoints"] = degradedEndpoints,
                    ["unhealthy_endpoints"] = unhealthyEndpoints,
                    ["health_percentage"] = totalEndpoints > 0 ? (double)healthyEndpoints / totalEndpoints * 100 : 0
                }
            };
        }

        private static HealthStatus DetermineOverallStatus(IEnumerable<HealthCheckResult> results)
        {
            var resultList = results.ToList();

            if (!resultList.Any())
                return HealthStatus.Unhealthy;

            if (resultList.All(r => r.Status == HealthStatus.Healthy))
                return HealthStatus.Healthy;

            if (resultList.Any(r => r.Status == HealthStatus.Unhealthy))
                return HealthStatus.Unhealthy;

            return HealthStatus.Degraded;
        }
    }

    /// <summary>
    /// Aggregate health check result
    /// </summary>
    public class AggregateHealthCheckResult
    {
        public HealthStatus Status { get; init; }
        public TimeSpan Duration { get; init; }
        public Dictionary<string, HealthCheckResult> Results { get; init; } = new();
        public Dictionary<string, object> Summary { get; init; } = new();
    }

    /// <summary>
    /// Extension methods for SNMP health checks
    /// </summary>
    public static class HealthCheckExtensions
    {
        /// <summary>
        /// Creates a health check for the SNMP client
        /// </summary>
        public static SnmpHealthCheck CreateHealthCheck(this ISnmpClient client, SnmpHealthCheckOptions? options = null, ILogger? logger = null)
        {
            return new SnmpHealthCheck(client, options, logger);
        }

        /// <summary>
        /// Performs a quick health check
        /// </summary>
        public static async Task<bool> IsHealthyAsync(this ISnmpClient client, CancellationToken cancellationToken = default)
        {
            try
            {
                var healthCheck = client.CreateHealthCheck();
                var result = await healthCheck.CheckHealthAsync(cancellationToken);
                return result.Status == HealthStatus.Healthy;
            }
            catch
            {
                return false;
            }
        }
    }
}