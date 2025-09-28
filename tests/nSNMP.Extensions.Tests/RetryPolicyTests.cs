using nSNMP.Extensions;
using Xunit;

namespace nSNMP.Extensions.Tests
{
    public class RetryPolicyTests
    {
        [Fact]
        public void LinearRetryPolicy_ShouldRetryWithFixedDelay()
        {
            var policy = RetryPolicy.Linear(maxAttempts: 3, delay: TimeSpan.FromSeconds(1));

            var delay1 = policy.ShouldRetry(1, new TimeoutException());
            var delay2 = policy.ShouldRetry(2, new TimeoutException());
            var delay3 = policy.ShouldRetry(3, new TimeoutException());
            var delay4 = policy.ShouldRetry(4, new TimeoutException());

            Assert.Equal(TimeSpan.FromSeconds(1), delay1);
            Assert.Equal(TimeSpan.FromSeconds(1), delay2);
            Assert.Equal(TimeSpan.FromSeconds(1), delay3);
            Assert.Null(delay4);
        }

        [Fact]
        public void ExponentialBackoffRetryPolicy_ShouldIncreaseDelay()
        {
            var policy = RetryPolicy.ExponentialBackoff(
                maxAttempts: 3,
                baseDelay: TimeSpan.FromMilliseconds(100),
                maxDelay: TimeSpan.FromSeconds(10));

            var delay1 = policy.ShouldRetry(1, new TimeoutException());
            var delay2 = policy.ShouldRetry(2, new TimeoutException());
            var delay3 = policy.ShouldRetry(3, new TimeoutException());
            var delay4 = policy.ShouldRetry(4, new TimeoutException());

            Assert.NotNull(delay1);
            Assert.NotNull(delay2);
            Assert.NotNull(delay3);
            Assert.Null(delay4);

            // Exponential backoff should increase delays (with jitter, so approximate)
            Assert.True(delay1 >= TimeSpan.FromMilliseconds(75)); // Allow for jitter
            Assert.True(delay2 > delay1);
        }

        [Fact]
        public void RetryPolicy_ShouldNotRetryNonRetryableExceptions()
        {
            var policy = RetryPolicy.Linear(maxAttempts: 3, delay: TimeSpan.FromSeconds(1));

            var delay = policy.ShouldRetry(1, new ArgumentException());

            Assert.Null(delay);
        }

        [Fact]
        public void RetryPolicy_ShouldRetryTimeoutExceptions()
        {
            var policy = RetryPolicy.Linear(maxAttempts: 3, delay: TimeSpan.FromSeconds(1));

            var delay1 = policy.ShouldRetry(1, new TimeoutException());
            var delay2 = policy.ShouldRetry(1, new TaskCanceledException());

            Assert.NotNull(delay1);
            Assert.NotNull(delay2);
        }
    }
}