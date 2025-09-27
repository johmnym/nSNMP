using System;

namespace nSNMP.Abstractions
{
    /// <summary>
    /// Logging abstraction for SNMP operations
    /// </summary>
    public interface ISnmpLogger
    {
        /// <summary>
        /// Logs a debug message
        /// </summary>
        /// <param name="message">Message to log</param>
        /// <param name="args">Message arguments</param>
        void LogDebug(string message, params object[] args);

        /// <summary>
        /// Logs an informational message
        /// </summary>
        /// <param name="message">Message to log</param>
        /// <param name="args">Message arguments</param>
        void LogInformation(string message, params object[] args);

        /// <summary>
        /// Logs a warning message
        /// </summary>
        /// <param name="message">Message to log</param>
        /// <param name="args">Message arguments</param>
        void LogWarning(string message, params object[] args);

        /// <summary>
        /// Logs an error message
        /// </summary>
        /// <param name="message">Message to log</param>
        /// <param name="args">Message arguments</param>
        void LogError(string message, params object[] args);

        /// <summary>
        /// Logs an error message with exception
        /// </summary>
        /// <param name="exception">Exception to log</param>
        /// <param name="message">Message to log</param>
        /// <param name="args">Message arguments</param>
        void LogError(Exception exception, string message, params object[] args);

        /// <summary>
        /// Gets whether debug logging is enabled
        /// </summary>
        bool IsDebugEnabled { get; }

        /// <summary>
        /// Gets whether information logging is enabled
        /// </summary>
        bool IsInformationEnabled { get; }
    }
}