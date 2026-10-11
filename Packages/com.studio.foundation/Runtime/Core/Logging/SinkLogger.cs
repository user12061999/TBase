using System;
using Studio.Foundation.Internal;

namespace Studio.Foundation.Logging
{
    /// <summary>Synchronous, filtered logger over a fixed snapshot of borrowed sinks.</summary>
    /// <remarks>
    /// Owns only its sink references, not the sinks. Threshold and sink order are fixed at construction.
    /// Sink exceptions are isolated. Reentrant valid Log calls are dropped and counted, avoiding
    /// cycles even when a sink forwards back to this logger. Disposal during delivery closes the
    /// logger immediately but finishes the current sink snapshot before releasing references.
    /// All access, including disposal and counters, belongs to the creating thread.
    /// </remarks>
    public sealed class SinkLogger : ILogger, IDisposable
    {
        private readonly ThreadGuard thread = new ThreadGuard();
        private readonly LogLevel minimumLevel;
        private ILogSink[] sinks;
        private bool disposed;
        private bool dispatching;
        private int sinkFailureCount;
        private int suppressedReentrantLogCount;

        /// <summary>Copies sink slots; empty arrays are valid and repeated sinks are separate slots.</summary>
        /// <exception cref="ArgumentNullException">The array or a sink element is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Threshold is undefined.</exception>
        public SinkLogger(LogLevel minimumLevel, params ILogSink[] sinks)
        {
            LogValidation.Level(minimumLevel, true);
            if (sinks == null) throw new ArgumentNullException(nameof(sinks));
            this.sinks = (ILogSink[])sinks.Clone();
            for (var i = 0; i < this.sinks.Length; i++)
                if (this.sinks[i] == null) throw new ArgumentNullException(nameof(sinks), "Sink elements cannot be null.");
            this.minimumLevel = minimumLevel;
        }

        /// <summary>Number of failed sink invocations, saturating at Int32.MaxValue; readable after disposal.</summary>
        public int SinkFailureCount { get { thread.Check(); return sinkFailureCount; } }

        /// <summary>Number of enabled reentrant log calls dropped, saturating at Int32.MaxValue.</summary>
        /// <remarks>Readable after disposal on the owning thread.</remarks>
        public int SuppressedReentrantLogCount { get { thread.Check(); return suppressedReentrantLogCount; } }

        /// <inheritdoc />
        public bool IsEnabled(LogLevel level)
        {
            CheckAlive();
            LogValidation.Level(level, true);
            return level != LogLevel.None && level >= minimumLevel;
        }

        /// <inheritdoc />
        public void Log(LogLevel level, string category, string message, Exception exception = null)
        {
            CheckAlive();
            LogValidation.Entry(level, category, message);
            if (level < minimumLevel) return;
            if (dispatching)
            {
                if (suppressedReentrantLogCount < int.MaxValue) suppressedReentrantLogCount++;
                return;
            }

            dispatching = true;
            try
            {
                for (var i = 0; i < sinks.Length; i++)
                {
                    try { sinks[i].Write(level, category, message, exception); }
                    catch (Exception)
                    {
                        if (sinkFailureCount < int.MaxValue) sinkFailureCount++;
                    }
                }
            }
            finally
            {
                dispatching = false;
                if (disposed) sinks = Array.Empty<ILogSink>();
            }
        }

        /// <summary>Closes this logger without disposing sinks. Idempotent on the owning thread.</summary>
        public void Dispose()
        {
            thread.Check();
            if (disposed) return;
            disposed = true;
            if (!dispatching) sinks = Array.Empty<ILogSink>();
        }

        private void CheckAlive()
        {
            thread.Check();
            if (disposed) throw new ObjectDisposedException(nameof(SinkLogger));
        }
    }
}
