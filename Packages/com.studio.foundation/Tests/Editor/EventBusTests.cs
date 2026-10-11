using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using NUnit.Framework;
using Studio.Foundation.Events;

namespace Studio.Foundation.Tests.Editor
{
    public sealed class EventBusTests
    {
        private class Message { }
        private sealed class Derived : Message { }
        private sealed class Target : IDisposable
        {
            public int Calls;
            public bool Disposed;
            public void Handle(int value) { Calls += value; }
            public void Dispose() { Disposed = true; }
        }

        [Test]
        public void DispatchUsesExactGenericTypeAndPreservesPayload()
        {
            using (var bus = new EventBus())
            {
                var payload = new Derived();
                Message received = null;
                var derivedCalls = 0;
                var objectCalls = 0;
                bus.Subscribe<Message>(x => received = x);
                bus.Subscribe<Derived>(x => derivedCalls++);
                bus.Subscribe<object>(x => objectCalls++);
                bus.Publish<Message>(payload);
                Assert.That(received, Is.SameAs(payload));
                Assert.That(derivedCalls + objectCalls, Is.Zero);
            }
        }

        [Test]
        public void ValueAndNullablePayloadsAreDeliveredWithoutConversion()
        {
            using (var bus = new EventBus())
            {
                var values = new List<int>();
                bus.Subscribe<int>(values.Add);
                bus.Subscribe<int?>(x => values.Add(x.Value));
                bus.Publish(7);
                bus.Publish<int?>(9);
                Assert.That(values, Is.EqualTo(new[] { 7, 9 }));
            }
        }

        [Test]
        public void EmptyBusStillRejectsNullInputs()
        {
            using (var bus = new EventBus())
            {
                Assert.DoesNotThrow(() => bus.Publish(1));
                Assert.Throws<ArgumentNullException>(() => bus.Publish<string>(null));
                Assert.Throws<ArgumentNullException>(() => bus.Publish<int?>(null));
                Assert.Throws<ArgumentNullException>(() => bus.Subscribe<int>(null));
            }
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        public void RemovalPreservesOrderAndResubscriptionAppends(int removed)
        {
            using (var bus = new EventBus())
            {
                var calls = new List<int>();
                var tokens = new IDisposable[3];
                for (var i = 0; i < 3; i++)
                {
                    var index = i;
                    tokens[i] = bus.Subscribe<int>(_ => calls.Add(index));
                }
                tokens[removed].Dispose();
                tokens[removed].Dispose();
                bus.Subscribe<int>(_ => calls.Add(removed));
                bus.Publish(0);
                var expected = new List<int>();
                for (var i = 0; i < 3; i++) if (i != removed) expected.Add(i);
                expected.Add(removed);
                Assert.That(calls, Is.EqualTo(expected));
            }
        }

        [Test]
        public void DuplicateDelegatesHaveIndependentTokens()
        {
            using (var bus = new EventBus())
            {
                var calls = 0;
                Action<int> handler = _ => calls++;
                var first = bus.Subscribe(handler);
                var second = bus.Subscribe(handler);
                bus.Publish(0);
                first.Dispose();
                bus.Publish(0);
                second.Dispose();
                bus.Publish(0);
                Assert.That(calls, Is.EqualTo(3));
            }
        }

        [Test]
        public void CallbackMutationsOnlyAffectFutureSnapshots()
        {
            using (var bus = new EventBus())
            {
                var calls = new List<string>();
                IDisposable self = null;
                IDisposable tail = null;
                self = bus.Subscribe<int>(_ =>
                {
                    calls.Add("first");
                    self.Dispose();
                    tail.Dispose();
                    bus.Subscribe<int>(x => calls.Add("added"));
                    bus.Subscribe<int>(x => calls.Add("transient")).Dispose();
                });
                tail = bus.Subscribe<int>(_ => calls.Add("tail"));
                bus.Publish(0);
                bus.Publish(0);
                Assert.That(calls, Is.EqualTo(new[] { "first", "tail", "added" }));
            }
        }

        [Test]
        public void ExceptionsAreCollectedInOrderAndBusRecovers()
        {
            using (var bus = new EventBus())
            {
                var first = new Exception("first");
                var second = new Exception("second");
                var a = bus.Subscribe<int>(_ => throw first);
                var calls = 0;
                bus.Subscribe<int>(_ => calls++);
                var b = bus.Subscribe<int>(_ => throw second);
                var error = Assert.Throws<AggregateException>(() => bus.Publish(1));
                Assert.That(error.InnerExceptions, Is.EqualTo(new[] { first, second }));
                a.Dispose();
                b.Dispose();
                bus.Publish(2);
                Assert.That(calls, Is.EqualTo(2));
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void ReentrantPublishIsRejectedAcrossAllChannels(bool otherType)
        {
            using (var bus = new EventBus())
            {
                var token = bus.Subscribe<int>(_ => { if (otherType) bus.Publish("nested"); else bus.Publish(1); });
                var error = Assert.Throws<AggregateException>(() => bus.Publish(0));
                Assert.That(error.InnerExceptions.Count, Is.EqualTo(1));
                Assert.That(error.InnerExceptions[0], Is.TypeOf<InvalidOperationException>());
                token.Dispose();
                Assert.DoesNotThrow(() => bus.Publish(0));
            }
        }

        [Test]
        public void SeparateBusesCanPublishInsideCallbacks()
        {
            using (var a = new EventBus())
            using (var b = new EventBus())
            {
                var value = 0;
                b.Subscribe<int>(x => value = x);
                a.Subscribe<int>(b.Publish);
                a.Publish(3);
                Assert.That(value, Is.EqualTo(3));
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void DisposeDuringDispatchClosesImmediatelyButFinishesSnapshot(bool fail)
        {
            var bus = new EventBus();
            var calls = 0;
            var token = bus.Subscribe<int>(_ =>
            {
                bus.Dispose();
                Assert.Throws<ObjectDisposedException>(() => bus.Publish(1));
                Assert.Throws<ObjectDisposedException>(() => bus.Subscribe<int>(x => { }));
                if (fail) throw new InvalidOperationException("handler");
            });
            bus.Subscribe<int>(_ => calls++);
            if (fail)
            {
                var error = Assert.Throws<AggregateException>(() => bus.Publish(0));
                Assert.That(error.InnerExceptions.Count, Is.EqualTo(1));
                Assert.That(error.InnerExceptions[0].Message, Is.EqualTo("handler"));
            }
            else Assert.DoesNotThrow(() => bus.Publish(0));
            Assert.That(calls, Is.EqualTo(1));
            Assert.DoesNotThrow(() => { bus.Dispose(); token.Dispose(); token.Dispose(); });
        }

        [Test]
        public void DisposalDoesNotOwnHandlerTargets()
        {
            var target = new Target();
            var bus = new EventBus();
            var token = bus.Subscribe<int>(target.Handle);
            bus.Publish(2);
            bus.Dispose();
            token.Dispose();
            Assert.That(target.Calls, Is.EqualTo(2));
            Assert.That(target.Disposed, Is.False);
            Assert.Throws<ObjectDisposedException>(() => bus.Publish(1));
            Assert.Throws<ObjectDisposedException>(() => bus.Subscribe<int>(target.Handle));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void ThreadAffinityAppliesEvenAfterDisposal(bool disposed)
        {
            var bus = new EventBus();
            var calls = 0;
            var token = bus.Subscribe<int>(_ => calls++);
            if (disposed) { token.Dispose(); bus.Dispose(); }
            foreach (var operation in new Action[] { () => bus.Publish(1), () => bus.Subscribe<int>(_ => { }), bus.Dispose, token.Dispose })
            {
                Exception failure = null;
                var worker = new Thread(() => { try { operation(); } catch (Exception error) { failure = error; } });
                worker.Start();
                worker.Join();
                Assert.That(failure, Is.TypeOf<InvalidOperationException>());
            }
            if (!disposed) { bus.Publish(1); Assert.That(calls, Is.EqualTo(1)); }
            token.Dispose();
            bus.Dispose();
        }

        [TestCase(false)]
        [TestCase(true)]
        public void RetainedTokenDoesNotRetainReleasedHandler(bool disposeBus)
        {
            var bus = new EventBus();
            var weak = SubscribeTarget(bus, out var token);
            if (disposeBus) bus.Dispose(); else token.Dispose();
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            Assert.That(weak.IsAlive, Is.False);
            GC.KeepAlive(token);
            GC.KeepAlive(bus);
            bus.Dispose();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static WeakReference SubscribeTarget(EventBus bus, out IDisposable token)
        {
            var target = new Target();
            token = bus.Subscribe<int>(target.Handle);
            return new WeakReference(target);
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(10)]
        [TestCase(100)]
        public void RecordWarmedPublishAllocations(int listeners)
        {
            using (var bus = new EventBus())
            {
                var calls = 0;
                Action<int> handler = _ => calls++;
                for (var i = 0; i < listeners; i++) bus.Subscribe(handler);
                for (var i = 0; i < 100; i++) bus.Publish(1);
                calls = 0;
                var before = GC.GetAllocatedBytesForCurrentThread();
                for (var i = 0; i < 1000; i++) bus.Publish(1);
                var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
                Assert.That(calls, Is.EqualTo(1000 * listeners));
                TestContext.WriteLine($"listeners={listeners}; publishes=1000; allocatedBytes={allocated}");
            }
        }
    }
}
