using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using nSNMP.Abstractions;

namespace nSNMP.Extensions
{
    /// <summary>
    /// Retry policy for SNMP operations
    /// </summary>
    public abstract class RetryPolicy
    {
        /// <summary>
        /// Determines if a retry should be attempted
        /// </summary>
        /// <param name="attempt">Current attempt number (1-based)</param>
        /// <param name="exception">Exception that occurred</param>
        /// <returns>Delay before retry, or null if no retry should be attempted</returns>
        public abstract TimeSpan? ShouldRetry(int attempt, Exception exception);
        /// <summary>
        /// Creates a linear retry policy
        /// </summary>
        /// <param name="maxAttempts">Maximum number of attempts</param>
        /// <param name="delay">Delay between attempts</param>
        /// <returns>Linear retry policy</returns>
        public static RetryPolicy Linear(int maxAttempts, TimeSpan delay) => new LinearRetryPolicy(maxAttempts, delay);

        /// <summary>
        /// Creates an exponential backoff retry policy
        /// </summary>
        /// <param name="maxAttempts">Maximum number of attempts</param>
        /// <param name="baseDelay">Base delay for exponential calculation</param>
        /// <param name="maxDelay">Maximum delay between attempts</param>
        /// <returns>Exponential backoff retry policy</returns>
        public static RetryPolicy ExponentialBackoff(int maxAttempts, TimeSpan baseDelay, TimeSpan? maxDelay = null) =>
            new ExponentialBackoffRetryPolicy(maxAttempts, baseDelay, maxDelay ?? TimeSpan.FromMinutes(1));
    }

    /// <summary>
    /// Linear retry policy with fixed delay between attempts
    /// </summary>
    internal class LinearRetryPolicy : RetryPolicy
    {
        private readonly int _maxAttempts;
        private readonly TimeSpan _delay;

        public LinearRetryPolicy(int maxAttempts, TimeSpan delay)
        {
            _maxAttempts = maxAttempts;
            _delay = delay;
        }

        public override TimeSpan? ShouldRetry(int attempt, Exception exception)
        {
            return attempt <= _maxAttempts && IsRetryableException(exception) ? _delay : null;
        }

        private static bool IsRetryableException(Exception exception)
        {
            return exception is TimeoutException ||
                   exception is TaskCanceledException ||
                   (exception is InvalidOperationException && exception.Message.Contains("timeout"));
        }
    }

    /// <summary>
    /// Exponential backoff retry policy
    /// </summary>
    internal class ExponentialBackoffRetryPolicy : RetryPolicy
    {
        private readonly int _maxAttempts;
        private readonly TimeSpan _baseDelay;
        private readonly TimeSpan _maxDelay;
        private readonly Random _random = new();

        public ExponentialBackoffRetryPolicy(int maxAttempts, TimeSpan baseDelay, TimeSpan maxDelay)
        {
            _maxAttempts = maxAttempts;
            _baseDelay = baseDelay;
            _maxDelay = maxDelay;
        }

        public override TimeSpan? ShouldRetry(int attempt, Exception exception)
        {
            if (attempt > _maxAttempts || !IsRetryableException(exception))
                return null;

            // Calculate exponential delay with jitter
            var delay = TimeSpan.FromMilliseconds(
                Math.Min(_baseDelay.TotalMilliseconds * Math.Pow(2, attempt - 1), _maxDelay.TotalMilliseconds)
            );

            // Add jitter (±25%)
            var jitter = delay.TotalMilliseconds * 0.25 * (_random.NextDouble() * 2 - 1);
            delay = TimeSpan.FromMilliseconds(Math.Max(0, delay.TotalMilliseconds + jitter));

            return delay;
        }

        private static bool IsRetryableException(Exception exception)
        {
            return exception is TimeoutException ||
                   exception is TaskCanceledException ||
                   (exception is InvalidOperationException && exception.Message.Contains("timeout"));
        }
    }

    /// <summary>
    /// Extension methods for adding retry policies to SNMP operations
    /// </summary>
    public static class RetryExtensions
    {
        /// <summary>
        /// Executes an SNMP operation with retry policy
        /// </summary>
        /// <typeparam name="T">Return type</typeparam>
        /// <param name="operation">Operation to execute</param>
        /// <param name="retryPolicy">Retry policy</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Operation result</returns>
        public static async Task<T> WithRetryAsync<T>(
            this Func<CancellationToken, Task<T>> operation,
            RetryPolicy retryPolicy,
            CancellationToken cancellationToken = default)
        {
            int attempt = 1;
            Exception? lastException = null;

            while (true)
            {
                try
                {
                    return await operation(cancellationToken);
                }
                catch (Exception ex) when (!(ex is OperationCanceledException))
                {
                    lastException = ex;
                    var delay = retryPolicy.ShouldRetry(attempt, ex);

                    if (delay == null)
                    {
                        throw;
                    }

                    await Task.Delay(delay.Value, cancellationToken);
                    attempt++;
                }
            }
        }

        /// <summary>
        /// Executes a GET operation with retry policy
        /// </summary>
        public static Task<IReadOnlyList<IVarBind>> GetWithRetryAsync(
            this ISnmpClient client,
            IEnumerable<string> oids,
            RetryPolicy retryPolicy,
            CancellationToken cancellationToken = default)
        {
            return ((Func<CancellationToken, Task<IReadOnlyList<IVarBind>>>)(ct => client.GetAsync(oids, ct)))
                .WithRetryAsync(retryPolicy, cancellationToken);
        }

        /// <summary>
        /// Executes a SET operation with retry policy
        /// </summary>
        public static Task<IReadOnlyList<IVarBind>> SetWithRetryAsync(
            this ISnmpClient client,
            IEnumerable<IVarBind> varBinds,
            RetryPolicy retryPolicy,
            CancellationToken cancellationToken = default)
        {
            return ((Func<CancellationToken, Task<IReadOnlyList<IVarBind>>>)(ct => client.SetAsync(varBinds, ct)))
                .WithRetryAsync(retryPolicy, cancellationToken);
        }

        /// <summary>
        /// Executes a GET-BULK operation with retry policy
        /// </summary>
        public static Task<IReadOnlyList<IVarBind>> GetBulkWithRetryAsync(
            this ISnmpClient client,
            int nonRepeaters,
            int maxRepetitions,
            IEnumerable<string> oids,
            RetryPolicy retryPolicy,
            CancellationToken cancellationToken = default)
        {
            return ((Func<CancellationToken, Task<IReadOnlyList<IVarBind>>>)(ct =>
                client.GetBulkAsync(nonRepeaters, maxRepetitions, oids, ct)))
                .WithRetryAsync(retryPolicy, cancellationToken);
        }
    }
}