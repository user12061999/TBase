using System;
using Studio.Foundation.Internal;

namespace Studio.Foundation.Logging
{
    /// <summary>Instance-based no-op logger that still enforces argument and owning-thread contracts.</summary>
    /// <remarks>No sinks or resources are retained; no disposal lifecycle is required.</remarks>
    public sealed class NullLogger : ILogger
    {
        private readonly ThreadGuard thread = new ThreadGuard();

        /// <inheritdoc />
        public bool IsEnabled(LogLevel level)
        {
            thread.Check();
            LogValidation.Level(level, true);
            return false;
        }

        /// <inheritdoc />
        public void Log(LogLevel level, string category, string message, Exception exception = null)
        {
            thread.Check();
            LogValidation.Entry(level, category, message);
        }
    }
}
