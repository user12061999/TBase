using System;

namespace Studio.Foundation.Logging
{
    /// <summary>Consumer-facing logger; does not expose sink ownership or configuration.</summary>
    /// <remarks>
    /// Owning-thread only. The composition owner configures the threshold and sinks.
    /// Sink failures are isolated and must not escape Log or recursively enter a failing sink.
    /// Invalid caller arguments and lifecycle/thread violations are still errors.
    /// Consumers borrow the logger and do not dispose it. Owned implementations that require
    /// cleanup use IDisposable separately; calls after owner disposal throw ObjectDisposedException.
    /// </remarks>
    public interface ILogger
    {
        /// <summary>Reports whether a severity passes the threshold; None always returns false.</summary>
        /// <exception cref="ArgumentOutOfRangeException">The enum value is undefined.</exception>
        /// <exception cref="InvalidOperationException">The call is on the wrong thread.</exception>
        /// <exception cref="ObjectDisposedException">The owner has disposed this logger.</exception>
        bool IsEnabled(LogLevel level);

        /// <summary>Writes an entry if enabled; validates arguments even when filtered out.</summary>
        /// <param name="level">A defined severity other than None.</param>
        /// <param name="category">Non-null category; empty means uncategorized.</param>
        /// <param name="message">Non-null message; empty is allowed.</param>
        /// <param name="exception">Optional diagnostic exception; no ownership transfer.</param>
        /// <exception cref="ArgumentNullException">Category or message is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Level is undefined or None.</exception>
        /// <exception cref="InvalidOperationException">The call is on the wrong thread.</exception>
        /// <exception cref="ObjectDisposedException">The owner has disposed this logger.</exception>
        void Log(LogLevel level, string category, string message, Exception exception = null);
    }
}
