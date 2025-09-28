namespace nSNMP.SMI.Configuration
{
    /// <summary>
    /// Global settings for memory optimization features
    /// </summary>
    public static class MemoryOptimizationSettings
    {
        private static bool _useArrayPooling = true;
        private static bool _useStringInterning = true;

        /// <summary>
        /// Enable/disable ArrayPool<byte> usage for temporary allocations
        /// Default: true (enabled for better performance)
        /// </summary>
        public static bool UseArrayPooling
        {
            get => _useArrayPooling;
            set => _useArrayPooling = value;
        }

        /// <summary>
        /// Enable/disable string interning for common OIDs
        /// Default: true (enabled for better memory usage)
        /// </summary>
        public static bool UseStringInterning
        {
            get => _useStringInterning;
            set => _useStringInterning = value;
        }

        /// <summary>
        /// Reset all settings to their default values
        /// </summary>
        public static void ResetToDefaults()
        {
            _useArrayPooling = true;
            _useStringInterning = true;
        }
    }
}