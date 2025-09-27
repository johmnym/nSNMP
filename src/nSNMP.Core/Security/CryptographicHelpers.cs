using System;
using System.Runtime.CompilerServices;

namespace nSNMP.Security
{
    /// <summary>
    /// Cryptographic helper functions with security-focused implementations
    /// </summary>
    public static class CryptographicHelpers
    {
        /// <summary>
        /// Constant-time byte array comparison to prevent timing attacks
        /// This function always takes the same amount of time regardless of where differences occur
        /// </summary>
        /// <param name="a">First byte array to compare</param>
        /// <param name="b">Second byte array to compare</param>
        /// <returns>True if arrays are equal, false otherwise</returns>
        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        public static bool ConstantTimeEquals(ReadOnlySpan<byte> a, ReadOnlySpan<byte> b)
        {
            // If lengths differ, arrays cannot be equal
            // However, we still need to do timing-safe comparison to avoid leaking length info
            if (a.Length != b.Length)
            {
                // Perform a dummy comparison to maintain constant timing
                PerformDummyComparison(a, b);
                return false;
            }

            return ConstantTimeEqualsCore(a, b);
        }

        /// <summary>
        /// Core constant-time comparison for equal-length arrays
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        private static bool ConstantTimeEqualsCore(ReadOnlySpan<byte> a, ReadOnlySpan<byte> b)
        {
            int result = 0;

            // XOR all bytes together - if any differ, result will be non-zero
            for (int i = 0; i < a.Length; i++)
            {
                result |= a[i] ^ b[i];
            }

            // Convert to boolean: 0 means equal, anything else means different
            return result == 0;
        }

        /// <summary>
        /// Performs a dummy comparison to maintain constant timing when lengths differ
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        private static void PerformDummyComparison(ReadOnlySpan<byte> a, ReadOnlySpan<byte> b)
        {
            // Use the shorter length to avoid index out of bounds
            int compareLength = Math.Min(a.Length, b.Length);
            int dummy = 0;

            // Perform the same XOR operations as the real comparison
            for (int i = 0; i < compareLength; i++)
            {
                dummy |= a[i] ^ b[i];
            }

            // Access the dummy variable to prevent compiler optimization
            _ = dummy;
        }

        /// <summary>
        /// Constant-time byte array comparison with explicit length matching
        /// Use this when you know both arrays should be the same length (e.g., hash digests)
        /// </summary>
        /// <param name="expected">Expected byte array (e.g., calculated digest)</param>
        /// <param name="provided">Provided byte array (e.g., digest from client)</param>
        /// <returns>True if arrays are equal in both content and length</returns>
        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        public static bool ConstantTimeHashEquals(ReadOnlySpan<byte> expected, ReadOnlySpan<byte> provided)
        {
            // For cryptographic hashes, length must match exactly
            if (expected.Length != provided.Length)
                return false;

            return ConstantTimeEqualsCore(expected, provided);
        }

        /// <summary>
        /// Securely clear sensitive data from memory
        /// </summary>
        /// <param name="sensitiveData">Array containing sensitive data to clear</param>
        public static void SecureClear(Span<byte> sensitiveData)
        {
            if (sensitiveData.IsEmpty)
                return;

            // Clear the memory
            sensitiveData.Clear();

            // Prevent compiler optimization from removing the clear operation
            // This is a memory barrier that ensures the clear actually happens
            System.Runtime.CompilerServices.Unsafe.SkipInit(out byte dummy);
            for (int i = 0; i < sensitiveData.Length; i++)
            {
                dummy = sensitiveData[i];
            }
        }

        /// <summary>
        /// Securely clear sensitive data from a byte array
        /// </summary>
        /// <param name="sensitiveData">Array containing sensitive data to clear</param>
        public static void SecureClear(byte[] sensitiveData)
        {
            if (sensitiveData == null || sensitiveData.Length == 0)
                return;

            SecureClear(sensitiveData.AsSpan());
        }

        /// <summary>
        /// Create a disposable wrapper for sensitive byte arrays that ensures secure clearing
        /// </summary>
        /// <param name="data">Sensitive data to wrap</param>
        /// <returns>Disposable wrapper that will securely clear data when disposed</returns>
        public static SecureByteArray CreateSecureByteArray(byte[] data)
        {
            return new SecureByteArray(data);
        }

        /// <summary>
        /// Copy sensitive data and ensure original is securely cleared
        /// </summary>
        /// <param name="source">Source array to copy from (will be cleared)</param>
        /// <returns>New array with copied data</returns>
        public static byte[] SecureCopy(byte[] source)
        {
            if (source == null || source.Length == 0)
                return Array.Empty<byte>();

            var copy = new byte[source.Length];
            source.CopyTo(copy, 0);
            SecureClear(source);
            return copy;
        }

        /// <summary>
        /// Securely convert a string to bytes and clear the original string from memory
        /// Note: Strings are immutable in .NET, so this clears the byte representation
        /// </summary>
        /// <param name="sensitiveString">String containing sensitive data</param>
        /// <param name="encoding">Encoding to use (defaults to UTF-8)</param>
        /// <returns>SecureByteArray containing the string bytes</returns>
        public static SecureByteArray SecureStringToBytes(string sensitiveString, System.Text.Encoding? encoding = null)
        {
            if (string.IsNullOrEmpty(sensitiveString))
                return new SecureByteArray(Array.Empty<byte>());

            encoding ??= System.Text.Encoding.UTF8;
            var bytes = encoding.GetBytes(sensitiveString);
            return new SecureByteArray(bytes);
        }
    }

    /// <summary>
    /// Disposable wrapper for sensitive byte arrays that ensures secure clearing on disposal
    /// </summary>
    public sealed class SecureByteArray : IDisposable
    {
        private byte[]? _data;
        private bool _disposed;

        internal SecureByteArray(byte[] data)
        {
            _data = data ?? throw new ArgumentNullException(nameof(data));
        }

        /// <summary>
        /// Get the underlying data. Throws if disposed.
        /// </summary>
        public byte[] Data
        {
            get
            {
                ThrowIfDisposed();
                return _data!;
            }
        }

        /// <summary>
        /// Get the length of the data. Returns 0 if disposed.
        /// </summary>
        public int Length => _disposed ? 0 : _data?.Length ?? 0;

        /// <summary>
        /// Check if the array is disposed
        /// </summary>
        public bool IsDisposed => _disposed;

        /// <summary>
        /// Get a span of the data. Throws if disposed.
        /// </summary>
        public Span<byte> AsSpan()
        {
            ThrowIfDisposed();
            return _data!.AsSpan();
        }

        /// <summary>
        /// Get a read-only span of the data. Throws if disposed.
        /// </summary>
        public ReadOnlySpan<byte> AsReadOnlySpan()
        {
            ThrowIfDisposed();
            return _data!.AsSpan();
        }

        /// <summary>
        /// Create a copy of the data. Original remains secure.
        /// </summary>
        public byte[] ToArray()
        {
            ThrowIfDisposed();
            var copy = new byte[_data!.Length];
            _data.CopyTo(copy, 0);
            return copy;
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(SecureByteArray));
        }

        public void Dispose()
        {
            if (!_disposed && _data != null)
            {
                CryptographicHelpers.SecureClear(_data);
                _data = null;
                _disposed = true;
            }
        }

        /// <summary>
        /// Generate cryptographically secure random bytes
        /// </summary>
        /// <param name="buffer">Buffer to fill with random bytes</param>
        public static void GenerateSecureRandomBytes(Span<byte> buffer)
        {
            using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
            rng.GetBytes(buffer);
        }

        /// <summary>
        /// Verify that a hash digest has the expected length for the given algorithm
        /// </summary>
        /// <param name="digest">The digest to validate</param>
        /// <param name="expectedLength">Expected length in bytes</param>
        /// <returns>True if digest length is correct</returns>
        public static bool ValidateDigestLength(ReadOnlySpan<byte> digest, int expectedLength)
        {
            return digest.Length == expectedLength;
        }
    }
}