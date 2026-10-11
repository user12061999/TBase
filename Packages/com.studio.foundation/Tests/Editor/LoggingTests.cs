using System;
using System.Collections.Generic;
using System.Threading;
using NUnit.Framework;
using Studio.Foundation.Logging;

namespace Studio.Foundation.Tests.Editor
{
    public sealed class LoggingTests
    {
        private sealed class Sink : ILogSink, IDisposable
        {
            public Action<LogLevel, string, string, Exception> OnWrite;
            public int Calls;
            public int DisposeCalls;
            public void Write(LogLevel level, string category, string message, Exception exception)
            {
                Calls++;
                OnWrite?.Invoke(level, category, message, exception);
            }
            public void Dispose() { DisposeCalls++; }
        }

        [TestCase(LogLevel.Trace)]
        [TestCase(LogLevel.Debug)]
        [TestCase(LogLevel.Information)]
        [TestCase(LogLevel.Warning)]
        [TestCase(LogLevel.Error)]
        [TestCase(LogLevel.Critical)]
        [TestCase(LogLevel.None)]
        public void ThresholdFiltersEverySeverityBeforeCallingSinks(LogLevel minimum)
        {
            var sink = new Sink();
            using (var logger = new SinkLogger(minimum, sink))
            {
                var expected = 0;
                for (var value = 0; value <= (int)LogLevel.Critical; value++)
                {
                    var level = (LogLevel)value;
                    var enabled = value >= (int)minimum;
                    Assert.That(logger.IsEnabled(level), Is.EqualTo(enabled));
                    logger.Log(level, "category", "message");
                    if (enabled) expected++;
                }
                Assert.That(logger.IsEnabled(LogLevel.None), Is.False);
                Assert.That(sink.Calls, Is.EqualTo(expected));
            }
        }

        [Test]
        public void DeliversUnchangedContextInSinkOrder()
        {
            var order = new List<int>();
            var error = new InvalidOperationException("original");
            var first = new Sink { OnWrite = (level, category, message, exception) =>
            {
                Assert.That(level, Is.EqualTo(LogLevel.Error));
                Assert.That(category, Is.EqualTo("network"));
                Assert.That(message, Is.EqualTo("failed"));
                Assert.That(exception, Is.SameAs(error));
                order.Add(1);
            } };
            var second = new Sink { OnWrite = (a, b, c, d) => order.Add(2) };
            using (var logger = new SinkLogger(LogLevel.Trace, first, second))
            {
                logger.Log(LogLevel.Error, "network", "failed", error);
                Assert.That(order, Is.EqualTo(new[] { 1, 2 }));
                // Assertions inside a sink would be isolated too; this prevents false positives.
                Assert.That(logger.SinkFailureCount, Is.Zero);
            }
        }

        [Test]
        public void SinkFailuresAreIsolatedAndLaterLogsStillWork()
        {
            var broken = new Sink { OnWrite = (a, b, c, d) => throw new Exception("sink failed") };
            var healthy = new Sink();
            using (var logger = new SinkLogger(LogLevel.Trace, broken, healthy, broken))
            {
                Assert.DoesNotThrow(() => logger.Log(LogLevel.Warning, "", ""));
                Assert.DoesNotThrow(() => logger.Log(LogLevel.Error, "", "again"));
                Assert.That(healthy.Calls, Is.EqualTo(2));
                Assert.That(logger.SinkFailureCount, Is.EqualTo(4));
            }
        }

        [Test]
        public void ConstructorCopiesSinkSlotsAndDoesNotAcquireOwnership()
        {
            var original = new Sink();
            var replacement = new Sink();
            var slots = new ILogSink[] { original, original };
            var logger = new SinkLogger(LogLevel.Trace, slots);
            slots[0] = replacement;
            logger.Log(LogLevel.Information, "", "");
            logger.Dispose();
            logger.Dispose();
            Assert.That(original.Calls, Is.EqualTo(2));
            Assert.That(replacement.Calls, Is.Zero);
            Assert.That(original.DisposeCalls, Is.Zero);
        }

        [Test]
        public void ConstructorRejectsNullSinksAndUndefinedThresholds()
        {
            Assert.Throws<ArgumentNullException>(() => new SinkLogger(LogLevel.Trace, (ILogSink[])null));
            Assert.Throws<ArgumentNullException>(() => new SinkLogger(LogLevel.Trace, new ILogSink[] { null }));
            Assert.Throws<ArgumentOutOfRangeException>(() => new SinkLogger((LogLevel)(-1)));
            Assert.Throws<ArgumentOutOfRangeException>(() => new SinkLogger((LogLevel)7));
            using (var empty = new SinkLogger(LogLevel.Trace))
                Assert.DoesNotThrow(() => empty.Log(LogLevel.Error, "", ""));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void FilteredAndNullLoggersStillValidateInputs(bool useNullLogger)
        {
            ILogger logger = useNullLogger ? (ILogger)new NullLogger() : new SinkLogger(LogLevel.None);
            try
            {
                Assert.Throws<ArgumentOutOfRangeException>(() => logger.IsEnabled((LogLevel)(-1)));
                Assert.Throws<ArgumentOutOfRangeException>(() => logger.IsEnabled((LogLevel)7));
                Assert.Throws<ArgumentOutOfRangeException>(() => logger.Log(LogLevel.None, "", ""));
                Assert.Throws<ArgumentOutOfRangeException>(() => logger.Log((LogLevel)99, "", ""));
                Assert.Throws<ArgumentNullException>(() => logger.Log(LogLevel.Error, null, ""));
                Assert.Throws<ArgumentNullException>(() => logger.Log(LogLevel.Error, "", null));
                Assert.DoesNotThrow(() => logger.Log(LogLevel.Error, "", ""));
                Assert.That(logger.IsEnabled(LogLevel.Error), Is.False);
                Assert.That(logger.IsEnabled(LogLevel.None), Is.False);
            }
            finally { (logger as IDisposable)?.Dispose(); }
        }

        [Test]
        public void ReentrantLogsAreSuppressedWithoutSkippingOuterSinks()
        {
            SinkLogger logger = null;
            var recursive = new Sink { OnWrite = (a, b, c, d) => logger.Log(LogLevel.Error, "", "nested") };
            var healthy = new Sink();
            using (logger = new SinkLogger(LogLevel.Trace, recursive, healthy))
            {
                logger.Log(LogLevel.Error, "", "outer");
                logger.Log(LogLevel.Error, "", "next");
                Assert.That(recursive.Calls, Is.EqualTo(2));
                Assert.That(healthy.Calls, Is.EqualTo(2));
                Assert.That(logger.SuppressedReentrantLogCount, Is.EqualTo(2));
                Assert.That(logger.SinkFailureCount, Is.Zero);
            }
        }

        [Test]
        public void DisposalDuringDeliveryFinishesSnapshotAndClosesFutureCalls()
        {
            SinkLogger logger = null;
            var closing = new Sink { OnWrite = (a, b, c, d) => logger.Dispose() };
            var tail = new Sink();
            logger = new SinkLogger(LogLevel.Trace, closing, tail);
            logger.Log(LogLevel.Error, "", "");
            Assert.That(tail.Calls, Is.EqualTo(1));
            Assert.That(closing.DisposeCalls + tail.DisposeCalls, Is.Zero);
            Assert.Throws<ObjectDisposedException>(() => logger.Log(LogLevel.Error, "", ""));
            Assert.Throws<ObjectDisposedException>(() => logger.IsEnabled(LogLevel.Error));
            Assert.DoesNotThrow(() => logger.Dispose());
            Assert.That(logger.SinkFailureCount, Is.Zero);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void WrongThreadLoggingIsRejected(bool useNullLogger)
        {
            var sink = new Sink();
            ILogger logger = useNullLogger ? (ILogger)new NullLogger() : new SinkLogger(LogLevel.Trace, sink);
            try
            {
                Assert.That(OnOtherThread(() => logger.Log(LogLevel.Error, "", "")), Is.TypeOf<InvalidOperationException>());
                Assert.That(OnOtherThread(() => logger.IsEnabled(LogLevel.Error)), Is.TypeOf<InvalidOperationException>());
                if (logger is SinkLogger concrete)
                {
                    Assert.That(OnOtherThread(() => concrete.Dispose()), Is.TypeOf<InvalidOperationException>());
                    Assert.That(OnOtherThread(() => { var unused = concrete.SinkFailureCount; }), Is.TypeOf<InvalidOperationException>());
                }
                Assert.That(sink.Calls, Is.Zero);
                Assert.DoesNotThrow(() => logger.Log(LogLevel.Error, "", "valid owner call"));
            }
            finally { (logger as IDisposable)?.Dispose(); }
        }

        private static Exception OnOtherThread(Action action)
        {
            Exception error = null;
            var worker = new Thread(() => { try { action(); } catch (Exception ex) { error = ex; } });
            worker.IsBackground = true;
            worker.Start();
            Assert.That(worker.Join(5000), Is.True, "Worker did not finish.");
            return error;
        }
    }
}
