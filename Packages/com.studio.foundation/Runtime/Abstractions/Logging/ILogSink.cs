using System;

namespace Studio.Foundation.Logging
{
    /// <summary>Output boundary called by a logger after filtering and input validation.</summary>
    /// <remarks>
    /// Owning-thread only. Receives defined severities except None and non-null category/message.
    /// A sink may throw on output failure; the logger must catch it, continue other sinks and
    /// record bounded failure accounting without recursive logging. Direct callers do not
    /// receive that isolation guarantee. Registration does not transfer sink ownership;
    /// composition explicitly enrolls disposable sinks as owned resources when appropriate.
    /// Calls after a disposable sink's disposal throw ObjectDisposedException.
    /// </remarks>
    public interface ILogSink
    {
        /// <summary>Writes a validated, enabled entry to the destination synchronously.</summary>
        void Write(LogLevel level, string category, string message, Exception exception);
    }
}
