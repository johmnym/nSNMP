using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using nSNMP.Abstractions;

namespace nSNMP.Extensions
{
    /// <summary>
    /// Circuit breaker states
    /// </summary>
    public enum CircuitBreakerState
    {
        Closed,
        Open,
        HalfOpen
    }

    /// <summary>
    /// Circuit breaker for SNMP operations to prevent cascade failures
    /// </summary>
    public class CircuitBreaker
    {
        private readonly int _failureThreshold;
        private readonly TimeSpan _timeout;
        private readonly TimeSpan _retryTimeout;
        private readonly object _lock = new();

        private CircuitBreakerState _state = CircuitBreakerState.Closed;
        private int _failureCount = 0;
        private DateTime _lastFailureTime = DateTime.MinValue;
        private DateTime _nextRetryTime = DateTime.MinValue;

        /// <summary>
        /// Initializes a new circuit breaker
        /// </summary>
        /// <param name="failureThreshold">Number of failures before opening circuit</param>
        /// <param name="timeout">Timeout for operations</param>
        /// <param name="retryTimeout">Time to wait before retrying when circuit is open</param>
        public CircuitBreaker(int failureThreshold = 5, TimeSpan? timeout = null, TimeSpan? retryTimeout = null)
        {
            _failureThreshold = failureThreshold;
            _timeout = timeout ?? TimeSpan.FromSeconds(30);
            _retryTimeout = retryTimeout ?? TimeSpan.FromMinutes(1);
        }

        /// <summary>
        /// Gets the current state of the circuit breaker
        /// </summary>
        public CircuitBreakerState State
        {
            get
            {
                lock (_lock)
                {
                    return _state;
                }
            }
        }

        /// <summary>
        /// Gets the current failure count
        /// </summary>
        public int FailureCount
        {
            get
            {
                lock (_lock)
                {
                    return _failureCount;
                }
            }
        }

        /// <summary>
        /// Executes an operation through the circuit breaker
        /// </summary>
        /// <typeparam name="T">Return type</typeparam>
        /// <param name="operation">Operation to execute</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Operation result</returns>
        /// <exception cref="CircuitBreakerOpenException">Thrown when circuit is open</exception>
        public async Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken = default)
        {
            CheckState();

            try
            {
                using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                timeoutCts.CancelAfter(_timeout);

                var result = await operation(timeoutCts.Token);
                OnSuccess();
                return result;
            }
            catch (Exception ex)
            {
                OnFailure(ex);
                throw;
            }
        }

        private void CheckState()
        {
            lock (_lock)
            {
                if (_state == CircuitBreakerState.Open)
                {
                    if (DateTime.UtcNow >= _nextRetryTime)
                    {
                        _state = CircuitBreakerState.HalfOpen;
                    }
                    else
                    {
                        throw new CircuitBreakerOpenException(
                            $"Circuit breaker is open. Next retry at {_nextRetryTime:yyyy-MM-dd HH:mm:ss} UTC");
                    }
                }
            }
        }

        private void OnSuccess()
        {
            lock (_lock)
            {
                _failureCount = 0;
                _state = CircuitBreakerState.Closed;
            }
        }

        private void OnFailure(Exception exception)
        {
            lock (_lock)
            {
                _failureCount++;
                _lastFailureTime = DateTime.UtcNow;

                if (_failureCount >= _failureThreshold)
                {
                    _state = CircuitBreakerState.Open;
                    _nextRetryTime = DateTime.UtcNow.Add(_retryTimeout);
                }
            }
        }

        /// <summary>
        /// Resets the circuit breaker to closed state
        /// </summary>
        public void Reset()
        {
            lock (_lock)
            {
                _failureCount = 0;
                _state = CircuitBreakerState.Closed;
                _lastFailureTime = DateTime.MinValue;
                _nextRetryTime = DateTime.MinValue;
            }
        }
    }

    /// <summary>
    /// Exception thrown when circuit breaker is open
    /// </summary>
    public class CircuitBreakerOpenException : Exception
    {
        public CircuitBreakerOpenException(string message) : base(message) { }
        public CircuitBreakerOpenException(string message, Exception innerException) : base(message, innerException) { }
    }

    /// <summary>
    /// Circuit breaker factory for managing multiple circuit breakers
    /// </summary>
    public class CircuitBreakerFactory
    {
        private readonly ConcurrentDictionary<string, CircuitBreaker> _circuitBreakers = new();
        private readonly Func<CircuitBreaker> _circuitBreakerFactory;

        /// <summary>
        /// Initializes a new circuit breaker factory
        /// </summary>
        /// <param name="circuitBreakerFactory">Factory function for creating circuit breakers</param>
        public CircuitBreakerFactory(Func<CircuitBreaker>? circuitBreakerFactory = null)
        {
            _circuitBreakerFactory = circuitBreakerFactory ?? (() => new CircuitBreaker());
        }

        /// <summary>
        /// Gets or creates a circuit breaker for the specified key
        /// </summary>
        /// <param name="key">Circuit breaker key (e.g., endpoint)</param>
        /// <returns>Circuit breaker instance</returns>
        public CircuitBreaker GetOrCreate(string key)
        {
            return _circuitBreakers.GetOrAdd(key, _ => _circuitBreakerFactory());
        }

        /// <summary>
        /// Removes a circuit breaker
        /// </summary>
        /// <param name="key">Circuit breaker key</param>
        /// <returns>True if removed, false if not found</returns>
        public bool Remove(string key)
        {
            return _circuitBreakers.TryRemove(key, out _);
        }

        /// <summary>
        /// Resets all circuit breakers
        /// </summary>
        public void ResetAll()
        {
            foreach (var circuitBreaker in _circuitBreakers.Values)
            {
                circuitBreaker.Reset();
            }
        }

        /// <summary>
        /// Gets all circuit breaker keys and their states
        /// </summary>
        public IReadOnlyDictionary<string, CircuitBreakerState> GetStates()
        {
            return _circuitBreakers.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value.State
            );
        }
    }

    /// <summary>
    /// Extension methods for adding circuit breaker protection to SNMP operations
    /// </summary>
    public static class CircuitBreakerExtensions
    {
        private static readonly CircuitBreakerFactory _factory = new();

        /// <summary>
        /// Executes a GET operation with circuit breaker protection
        /// </summary>
        public static Task<IReadOnlyList<IVarBind>> GetWithCircuitBreakerAsync(
            this ISnmpClient client,
            IEnumerable<string> oids,
            string? circuitBreakerKey = null,
            CancellationToken cancellationToken = default)
        {
            var key = circuitBreakerKey ?? client.Endpoint.ToString();
            var circuitBreaker = _factory.GetOrCreate(key);

            return circuitBreaker.ExecuteAsync(ct => client.GetAsync(oids, ct), cancellationToken);
        }

        /// <summary>
        /// Executes a SET operation with circuit breaker protection
        /// </summary>
        public static Task<IReadOnlyList<IVarBind>> SetWithCircuitBreakerAsync(
            this ISnmpClient client,
            IEnumerable<IVarBind> varBinds,
            string? circuitBreakerKey = null,
            CancellationToken cancellationToken = default)
        {
            var key = circuitBreakerKey ?? client.Endpoint.ToString();
            var circuitBreaker = _factory.GetOrCreate(key);

            return circuitBreaker.ExecuteAsync(ct => client.SetAsync(varBinds, ct), cancellationToken);
        }

        /// <summary>
        /// Gets the circuit breaker state for a client
        /// </summary>
        public static CircuitBreakerState GetCircuitBreakerState(this ISnmpClient client, string? circuitBreakerKey = null)
        {
            var key = circuitBreakerKey ?? client.Endpoint.ToString();
            var circuitBreaker = _factory.GetOrCreate(key);
            return circuitBreaker.State;
        }

        /// <summary>
        /// Resets the circuit breaker for a client
        /// </summary>
        public static void ResetCircuitBreaker(this ISnmpClient client, string? circuitBreakerKey = null)
        {
            var key = circuitBreakerKey ?? client.Endpoint.ToString();
            var circuitBreaker = _factory.GetOrCreate(key);
            circuitBreaker.Reset();
        }
    }
}