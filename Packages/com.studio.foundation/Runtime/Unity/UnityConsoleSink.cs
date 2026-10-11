using System;
using System.Threading;
using Studio.Foundation.Logging;
using UnityEngine;

namespace Studio.Foundation.Unity
{
    /// <summary>Borrowed console sink; construct and call on the Unity main thread.</summary>
    /// <remarks>Use SinkLogger for threshold filtering and failure isolation. Exceptions append full text to one entry.</remarks>
    public sealed class UnityConsoleSink : ILogSink
    {
        private readonly int thread = Thread.CurrentThread.ManagedThreadId;

        /// <summary>Trace/Debug/Information map to Log, Warning to Warning, Error/Critical to Error.</summary>
        public void Write(LogLevel level, string category, string message, Exception exception)
        {
            if (thread != Thread.CurrentThread.ManagedThreadId)
                throw new InvalidOperationException("Console sink must be used on its creating main thread.");
            if (level < LogLevel.Trace || level > LogLevel.Critical) throw new ArgumentOutOfRangeException(nameof(level));
            if (category == null) throw new ArgumentNullException(nameof(category));
            if (message == null) throw new ArgumentNullException(nameof(message));
            var text = "[" + category + "] " + message;
            if (exception != null) text += "\n" + exception;
            if (level >= LogLevel.Error) Debug.LogError(text);
            else if (level == LogLevel.Warning) Debug.LogWarning(text);
            else Debug.Log(text);
        }
    }
}
