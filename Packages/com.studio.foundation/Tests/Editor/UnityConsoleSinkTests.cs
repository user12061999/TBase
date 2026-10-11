using System;
using System.Threading;
using NUnit.Framework;
using Studio.Foundation.Logging;
using Studio.Foundation.Unity;
using UnityEngine;
using UnityEngine.TestTools;

namespace Studio.Foundation.Tests.Editor
{
    public sealed class UnityConsoleSinkTests
    {
        [TestCase(LogLevel.Trace, LogType.Log)]
        [TestCase(LogLevel.Debug, LogType.Log)]
        [TestCase(LogLevel.Information, LogType.Log)]
        [TestCase(LogLevel.Warning, LogType.Warning)]
        [TestCase(LogLevel.Error, LogType.Error)]
        [TestCase(LogLevel.Critical, LogType.Error)]
        public void SeverityAndContextMapToOneConsoleEntry(LogLevel level, LogType type)
        {
            var error = new InvalidOperationException("cause");
            LogAssert.Expect(type, "[category] message\n" + error);
            new UnityConsoleSink().Write(level, "category", "message", error);
        }

        [Test]
        public void RouterFiltersBeforeConsoleOutput()
        {
            using (var logger = new SinkLogger(LogLevel.Warning, new UnityConsoleSink()))
            {
                logger.Log(LogLevel.Information, "test", "filtered");
                LogAssert.Expect(LogType.Warning, "[test] visible");
                logger.Log(LogLevel.Warning, "test", "visible");
                Assert.That(logger.SinkFailureCount, Is.Zero);
                LogAssert.NoUnexpectedReceived();
            }
        }

        [Test]
        public void InvalidInputsAndWrongThreadFailBeforeLogging()
        {
            var sink = new UnityConsoleSink();
            Assert.Throws<ArgumentOutOfRangeException>(() => sink.Write(LogLevel.None, "", "", null));
            Assert.Throws<ArgumentOutOfRangeException>(() => sink.Write((LogLevel)(-1), "", "", null));
            Assert.Throws<ArgumentNullException>(() => sink.Write(LogLevel.Error, null, "", null));
            Assert.Throws<ArgumentNullException>(() => sink.Write(LogLevel.Error, "", null, null));
            Exception failure = null;
            var worker = new Thread(() => { try { sink.Write(LogLevel.Error, "", "", null); } catch (Exception e) { failure = e; } });
            worker.Start(); worker.Join();
            Assert.That(failure, Is.TypeOf<InvalidOperationException>());
        }
    }
}
