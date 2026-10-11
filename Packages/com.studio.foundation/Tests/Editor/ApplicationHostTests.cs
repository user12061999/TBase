using System;
using System.Collections.Generic;
using System.Threading;
using NUnit.Framework;
using Studio.Foundation.Lifecycle;
using Studio.Foundation.Modules;
using Studio.Foundation.Services;

namespace Studio.Foundation.Tests.Editor
{
    public sealed class ApplicationHostTests
    {
        private sealed class Module : IModule
        {
            public string Id { get; set; }
            public IReadOnlyList<string> Dependencies { get; set; }
            public Module(string id, params string[] dependencies) { Id = id; Dependencies = dependencies; }
        }

        private sealed class Participant : IInitializable, IStoppable, IUpdatable, IFixedUpdatable, ILateUpdatable, IDisposable
        {
            public Action InitializeAction;
            public Action StopAction;
            public Action DisposeAction;
            public Action<float> TickAction;
            public readonly List<string> Calls = new List<string>();
            public void Initialize() { Calls.Add("init"); InitializeAction?.Invoke(); }
            public void Stop() { Calls.Add("stop"); StopAction?.Invoke(); }
            public void Dispose() { Calls.Add("dispose"); DisposeAction?.Invoke(); }
            public void Update(float delta) { Calls.Add("update"); TickAction?.Invoke(delta); }
            public void FixedUpdate(float delta) { Calls.Add("fixed"); TickAction?.Invoke(delta); }
            public void LateUpdate(float delta) { Calls.Add("late"); TickAction?.Invoke(delta); }
            // Deliberately hostile value equality: host enrollment must use reference identity.
            public override bool Equals(object other) => other is Participant;
            public override int GetHashCode() => 1;
        }

        private sealed class StopOnly : IStoppable
        {
            public int Calls;
            public void Stop() { Calls++; }
        }

        private sealed class Resource : IDisposable
        {
            public int Calls;
            public Action Action;
            public void Dispose() { Calls++; Action?.Invoke(); }
        }

        private sealed class ThrowingMetadata : IModule
        {
            public string Id => throw new FormatException("metadata");
            public IReadOnlyList<string> Dependencies => Array.Empty<string>();
        }

        private sealed class EmptyTicks : IUpdatable, IFixedUpdatable, ILateUpdatable
        {
            public void Update(float deltaTime) { }
            public void FixedUpdate(float fixedDeltaTime) { }
            public void LateUpdate(float deltaTime) { }
        }

        [TestCase(false)] [TestCase(true)]
        public void WarmTickDispatchHasNoManagedAllocations(bool withParticipant)
        {
            using (var host = new ApplicationHost())
            {
                if (withParticipant) host.AddModule(new Module("test"), c => c.Enroll(new EmptyTicks()));
                host.Start();
                for (var i = 0; i < 1000; i++) { host.Update(0); host.FixedUpdate(0); host.LateUpdate(0); }
                var before = GC.GetAllocatedBytesForCurrentThread();
                for (var i = 0; i < 1000; i++) { host.Update(0); host.FixedUpdate(0); host.LateUpdate(0); }
                var bytes = GC.GetAllocatedBytesForCurrentThread() - before;
                Assert.That(bytes, Is.Zero, "Measured 3000 warmed host calls on the test thread; excludes startup/shutdown.");
            }
        }

        [Test]
        public void EmptyHostIsSingleUseAndTerminalStopDisposeAreIdempotent()
        {
            var host = new ApplicationHost();
            Assert.That(host.State, Is.EqualTo(ApplicationState.Created));
            Assert.Throws<InvalidOperationException>(() => host.Update(0));
            Assert.Throws<InvalidOperationException>(() => { var unused = host.Services; });
            host.Start();
            Assert.That(host.State, Is.EqualTo(ApplicationState.Running));
            host.Update(0); host.FixedUpdate(0); host.LateUpdate(0);
            Assert.Throws<InvalidOperationException>(() => host.Start());
            host.Stop(); host.Stop();
            Assert.That(host.State, Is.EqualTo(ApplicationState.Stopped));
            Assert.Throws<InvalidOperationException>(() => host.Start());
            Assert.Throws<InvalidOperationException>(() => host.Update(0));
            host.Dispose(); host.Dispose(); host.Stop();
            Assert.Throws<ObjectDisposedException>(() => host.Start());
        }

        [Test]
        public void DependencyOrderUsesOrdinalReadyQueueThenEnrollmentOrder()
        {
            var sequence = new List<string>();
            using (var host = new ApplicationHost())
            {
                Action<string, string[]> add = (id, dependencies) => host.AddModule(new Module(id, dependencies), context =>
                {
                    sequence.Add("config:" + id);
                    for (var i = 0; i < 2; i++)
                    {
                        var name = id + i;
                        var participant = new Participant
                        {
                            InitializeAction = () => sequence.Add("init:" + name),
                            StopAction = () => sequence.Add("stop:" + name),
                            DisposeAction = () => sequence.Add("dispose:" + name)
                        };
                        context.Enroll(participant); context.Own(participant);
                    }
                });
                add("c", Array.Empty<string>()); add("a", new[] { "b" }); add("b", Array.Empty<string>());
                host.Start(); host.Stop();
                Assert.That(sequence, Is.EqualTo(new[] {
                    "config:b", "config:a", "config:c",
                    "init:b0", "init:b1", "init:a0", "init:a1", "init:c0", "init:c1",
                    "stop:c1", "stop:c0", "stop:a1", "stop:a0", "stop:b1", "stop:b0",
                    "dispose:c1", "dispose:c0", "dispose:a1", "dispose:a0", "dispose:b1", "dispose:b0" }));
            }
        }

        [TestCase("duplicate")]
        [TestCase("missing")]
        [TestCase("self")]
        [TestCase("cycle")]
        [TestCase("blankId")]
        [TestCase("nullList")]
        [TestCase("blankDependency")]
        [TestCase("duplicateDependency")]
        public void InvalidGraphsFailBeforeAnyConfigurationAndDisposePreownedResources(string kind)
        {
            using (var host = new ApplicationHost())
            {
                var resource = new Resource(); host.Own(resource);
                var configured = 0;
                host.AddModule(new Module("root"), _ => configured++);
                var bad = new Module("bad");
                switch (kind)
                {
                    case "duplicate": bad.Id = "root"; break;
                    case "missing": bad.Dependencies = new[] { "absent" }; break;
                    case "self": bad.Dependencies = new[] { "bad" }; break;
                    case "cycle":
                        bad.Dependencies = new[] { "other" };
                        host.AddModule(new Module("other", "bad"), _ => configured++); break;
                    case "blankId": bad.Id = " "; break;
                    case "nullList": bad.Dependencies = null; break;
                    case "blankDependency": bad.Dependencies = new[] { " " }; break;
                    case "duplicateDependency": bad.Dependencies = new[] { "root", "root" }; break;
                }
                host.AddModule(bad, _ => configured++);
                Assert.Throws<InvalidOperationException>(() => host.Start());
                Assert.That(host.State, Is.EqualTo(ApplicationState.Faulted));
                Assert.That(configured, Is.Zero);
                Assert.That(resource.Calls, Is.EqualTo(1));
                host.Stop(); Assert.That(resource.Calls, Is.EqualTo(1));
            }
        }

        [Test]
        public void MetadataExceptionsAlsoTriggerCleanup()
        {
            using (var host = new ApplicationHost())
            {
                var resource = new Resource(); host.Own(resource);
                host.AddModule(new ThrowingMetadata());
                Assert.Throws<FormatException>(() => host.Start());
                Assert.That(resource.Calls, Is.EqualTo(1));
                Assert.That(host.State, Is.EqualTo(ApplicationState.Faulted));
            }
        }

        [Test]
        public void MetadataIsSnapshottedAndIdsAreCaseSensitive()
        {
            using (var host = new ApplicationHost())
            {
                var order = new List<string>();
                var later = new Module("b", "A");
                host.AddModule(later, _ => order.Add("b"));
                host.AddModule(new Module("a"), _ => order.Add("a"));
                host.AddModule(new Module("A"), _ => { later.Dependencies = new[] { "missing" }; order.Add("A"); });
                host.Start();
                Assert.That(order, Is.EqualTo(new[] { "A", "a", "b" }));
            }
        }

        [Test]
        public void RegistrationFreezesBeforeInitAndRemainsUsableThroughoutCleanup()
        {
            IServiceRegistry registration = null;
            IServiceResolver retained = null;
            ModuleContext expired = null;
            var service = new object();
            using (var host = new ApplicationHost())
            {
                var participant = new Participant
                {
                    InitializeAction = () => { retained = host.Services; Assert.That(retained.GetRequired<object>(), Is.SameAs(service)); },
                    StopAction = () => Assert.That(host.Services.GetRequired<object>(), Is.SameAs(service)),
                    DisposeAction = () => Assert.That(host.Services.GetRequired<object>(), Is.SameAs(service))
                };
                host.AddModule(new Module("test"), context =>
                {
                    Assert.Throws<InvalidOperationException>(() => { var unused = host.Services; });
                    expired = context; registration = context.Registry;
                    registration.Register<object>(service);
                    context.Enroll(participant); context.Own(participant);
                });
                host.Start();
                Assert.Throws<InvalidOperationException>(() => registration.Register<string>("late"));
                Assert.Throws<InvalidOperationException>(() => expired.Enroll(new StopOnly()));
                Assert.Throws<InvalidOperationException>(() => expired.Own(new Resource()));
                Assert.Throws<InvalidOperationException>(() => { var unused = expired.Registry; });
                host.Stop();
                Assert.Throws<ObjectDisposedException>(() => retained.GetRequired<object>());
                Assert.Throws<ObjectDisposedException>(() => { var unused = host.Services; });
            }
        }

        [Test]
        public void RegistrationFailureDisposesEnrolledResourcesWithoutStartingOrStoppingParticipants()
        {
            using (var host = new ApplicationHost())
            {
                ModuleContext expired = null;
                var participant = new Participant();
                host.AddModule(new Module("a"), context =>
                {
                    expired = context;
                    context.Enroll(participant); context.Own(participant);
                    context.Registry.Register<object>(new object());
                    context.Registry.Register<object>(new object());
                });
                Assert.Throws<InvalidOperationException>(() => host.Start());
                Assert.That(participant.Calls, Is.EqualTo(new[] { "dispose" }));
                Assert.Throws<InvalidOperationException>(() => expired.Enroll(new StopOnly()));
                Assert.That(host.State, Is.EqualTo(ApplicationState.Faulted));
            }
        }

        [Test]
        public void StartupFailureRetainsOriginalAndCollectsEveryCleanupFailureInOrder()
        {
            var original = new Exception("init"); var stopError = new Exception("stop"); var disposeError = new Exception("dispose");
            var first = new Participant { StopAction = () => throw stopError, DisposeAction = () => throw disposeError };
            var failing = new Participant { InitializeAction = () => throw original };
            var unreached = new Participant();
            using (var host = new ApplicationHost())
            {
                host.AddModule(new Module("test"), context =>
                {
                    foreach (var participant in new[] { first, failing, unreached })
                    { context.Enroll(participant); context.Own(participant); }
                });
                var aggregate = Assert.Throws<AggregateException>(() => host.Start());
                Assert.That(aggregate.InnerExceptions, Is.EqualTo(new[] { original, stopError, disposeError }));
                Assert.That(first.Calls, Is.EqualTo(new[] { "init", "stop", "dispose" }));
                Assert.That(failing.Calls, Is.EqualTo(new[] { "init", "dispose" }));
                Assert.That(unreached.Calls, Is.EqualTo(new[] { "dispose" }));
                Assert.That(host.State, Is.EqualTo(ApplicationState.Faulted));
                Assert.DoesNotThrow(() => host.Stop());
            }
        }

        [Test]
        public void OriginalStartupExceptionIsRethrownWhenCleanupSucceeds()
        {
            var failure = new FormatException("original");
            using (var host = new ApplicationHost())
            {
                host.AddModule(new Module("test"), c => c.Enroll(new Participant { InitializeAction = () => throw failure }));
                Assert.That(Assert.Throws<FormatException>(() => host.Start()), Is.SameAs(failure));
                Assert.That(failure.StackTrace, Does.Contain("Initialize"));
            }
        }

        [Test]
        public void StopOnlyParticipantsAndBorrowedServicesHaveIndependentOwnership()
        {
            var stopOnly = new StopOnly(); var borrowed = new Participant(); var owned = new Participant();
            using (var host = new ApplicationHost())
            {
                host.AddModule(new Module("test"), c =>
                {
                    c.Registry.Register<IDisposable>(borrowed);
                    c.Enroll(borrowed); c.Enroll(stopOnly); c.Enroll(owned);
                    c.Own(owned); c.Own(owned);
                });
                host.Start(); host.Stop(); host.Dispose();
                Assert.That(stopOnly.Calls, Is.EqualTo(1));
                Assert.That(borrowed.Calls, Is.EqualTo(new[] { "init", "stop" }));
                Assert.That(owned.Calls, Is.EqualTo(new[] { "init", "stop", "dispose" }));
            }
        }

        [Test]
        public void StopBeforeStartSkipsConfigurationAndDisposesInReverseFirstEnrollmentOrder()
        {
            var order = new List<int>();
            var first = new Resource { Action = () => order.Add(1) };
            var second = new Resource { Action = () => order.Add(2) };
            using (var host = new ApplicationHost())
            {
                host.AddModule(new Module("test"), _ => Assert.Fail("Must not configure"));
                host.Own(first); host.Own(second); host.Own(first);
                host.Stop(); host.Stop();
                Assert.That(order, Is.EqualTo(new[] { 2, 1 }));
                Assert.That(host.State, Is.EqualTo(ApplicationState.Stopped));
                Assert.Throws<InvalidOperationException>(() => host.Start());
            }
        }

        [Test]
        public void NormalCleanupAttemptsEverythingAndEndsStoppedEvenWithFailures()
        {
            var p = new Participant { StopAction = () => throw new Exception("stop") };
            var r = new Resource { Action = () => throw new Exception("dispose") };
            using (var host = new ApplicationHost())
            {
                host.Own(r);
                host.AddModule(new Module("test"), c => c.Enroll(p));
                host.Start();
                Assert.That(Assert.Throws<AggregateException>(() => host.Stop()).InnerExceptions.Count, Is.EqualTo(2));
                Assert.That(host.State, Is.EqualTo(ApplicationState.Stopped));
                host.Stop(); host.Dispose(); Assert.That(r.Calls, Is.EqualTo(1));
            }
        }

        [TestCase(0)] [TestCase(1)] [TestCase(2)]
        public void EachTickPhasePreservesOrderAndExplicitDelta(int phase)
        {
            var order = new List<int>();
            var first = new Participant { TickAction = d => { Assert.That(d, Is.EqualTo(0.25f)); order.Add(1); } };
            var second = new Participant { TickAction = d => { Assert.That(d, Is.EqualTo(0.25f)); order.Add(2); } };
            using (var host = HostWith(first, second))
            {
                host.Start(); Tick(host, phase, 0.25f);
                Assert.That(order, Is.EqualTo(new[] { 1, 2 }));
                Assert.That(first.Calls[1], Is.EqualTo(new[] { "update", "fixed", "late" }[phase]));
            }
        }

        [TestCase(0)] [TestCase(1)] [TestCase(2)]
        public void InvalidDeltaIsRejectedBeforeDispatchWithoutFaulting(int phase)
        {
            var p = new Participant();
            using (var host = HostWith(p))
            {
                host.Start();
                foreach (var invalid in new[] { -1f, float.NaN, float.PositiveInfinity, float.NegativeInfinity })
                    Assert.Throws<ArgumentOutOfRangeException>(() => Tick(host, phase, invalid));
                Assert.That(p.Calls, Is.EqualTo(new[] { "init" }));
                Assert.That(host.State, Is.EqualTo(ApplicationState.Running));
                Tick(host, phase, 0);
            }
        }

        [TestCase(0)] [TestCase(1)] [TestCase(2)]
        public void TickFailureStopsFurtherDispatchAndPreservesOriginalError(int phase)
        {
            var failure = new FormatException("tick");
            var first = new Participant { TickAction = _ => throw failure };
            var second = new Participant();
            using (var host = HostWith(first, second))
            {
                host.Start();
                Assert.That(Assert.Throws<FormatException>(() => Tick(host, phase, 0)), Is.SameAs(failure));
                Assert.That(second.Calls, Is.EqualTo(new[] { "init", "stop" }));
                Assert.That(host.State, Is.EqualTo(ApplicationState.Faulted));
                Assert.Throws<InvalidOperationException>(() => Tick(host, phase, 0));
            }
        }

        [TestCase(false)] [TestCase(true)]
        public void StopOrDisposeInsideTickDefersUntilReturnAndSkipsFollowingParticipants(bool dispose)
        {
            ApplicationHost host = null;
            var first = new Participant { TickAction = _ =>
            {
                if (dispose) host.Dispose(); else host.Stop();
                Assert.That(host.State, Is.EqualTo(ApplicationState.Running));
            } };
            var second = new Participant();
            using (host = HostWith(first, second))
            {
                host.Start(); host.Update(0);
                Assert.That(first.Calls, Is.EqualTo(new[] { "init", "update", "stop" }));
                Assert.That(second.Calls, Is.EqualTo(new[] { "init", "stop" }));
                Assert.That(host.State, Is.EqualTo(ApplicationState.Stopped));
            }
        }

        [Test]
        public void StopThenThrowInsideTickStillFaultsAndAggregatesCleanupError()
        {
            ApplicationHost host = null;
            var original = new FormatException("tick"); var cleanup = new Exception("cleanup");
            var p = new Participant { TickAction = _ => { host.Dispose(); throw original; }, StopAction = () => throw cleanup };
            using (host = HostWith(p))
            {
                host.Start();
                var result = Assert.Throws<AggregateException>(() => host.Update(0));
                Assert.That(result.InnerExceptions, Is.EqualTo(new Exception[] { original, cleanup }));
                Assert.That(host.State, Is.EqualTo(ApplicationState.Faulted));
                Assert.DoesNotThrow(() => host.Dispose());
            }
        }

        [Test]
        public void ReentrantStartupShutdownAndTickTransitionsAreRejected()
        {
            ApplicationHost host = null;
            var p = new Participant
            {
                InitializeAction = () =>
                {
                    Assert.Throws<InvalidOperationException>(() => host.Start());
                    Assert.Throws<InvalidOperationException>(() => host.Stop());
                    Assert.Throws<InvalidOperationException>(() => host.Dispose());
                },
                TickAction = _ => Assert.Throws<InvalidOperationException>(() => host.LateUpdate(0)),
                StopAction = () => Assert.Throws<InvalidOperationException>(() => host.Stop())
            };
            using (host = HostWith(p)) { host.Start(); host.Update(0); host.Stop(); }
        }

        [Test]
        public void InvalidEnrollmentAndLateCompositionFailWithoutCorruptingHost()
        {
            using (var host = new ApplicationHost())
            {
                Assert.Throws<ArgumentNullException>(() => host.AddModule(null));
                Assert.Throws<ArgumentNullException>(() => host.Own(null));
                Assert.Throws<ArgumentException>(() => host.Own(host));
                host.AddModule(new Module("test"), c =>
                {
                    Assert.Throws<ArgumentNullException>(() => c.Enroll(null));
                    Assert.Throws<ArgumentException>(() => c.Enroll(new object()));
                    var p = new Participant(); c.Enroll(p);
                    Assert.Throws<InvalidOperationException>(() => c.Enroll(p));
                    Assert.Throws<InvalidOperationException>(() => host.AddModule(new Module("late")));
                });
                host.Start();
                Assert.Throws<InvalidOperationException>(() => host.Own(new Resource()));
            }
        }

        [Test]
        public void WrongThreadOperationsAndRetainedContextAreRejected()
        {
            using (var host = new ApplicationHost())
            {
                AssertWrongThread(() => host.AddModule(new Module("test")));
                AssertWrongThread(() => host.Own(new Resource()));
                AssertWrongThread(() => host.Start()); AssertWrongThread(() => host.Stop()); AssertWrongThread(() => host.Dispose());
                host.AddModule(new Module("test"), c => AssertWrongThread(() => c.Enroll(new StopOnly())));
                host.Start();
                AssertWrongThread(() => host.Update(0));
                AssertWrongThread(() => { var unused = host.Services; });
                AssertWrongThread(() => { var unused = host.State; });
                Assert.That(host.State, Is.EqualTo(ApplicationState.Running));
            }
        }

        private static ApplicationHost HostWith(params object[] participants)
        {
            var host = new ApplicationHost();
            host.AddModule(new Module("test"), context => { foreach (var p in participants) context.Enroll(p); });
            return host;
        }

        private static void Tick(ApplicationHost host, int phase, float delta)
        {
            if (phase == 0) host.Update(delta); else if (phase == 1) host.FixedUpdate(delta); else host.LateUpdate(delta);
        }

        private static void AssertWrongThread(Action action)
        {
            Exception error = null;
            var worker = new Thread(() => { try { action(); } catch (Exception caught) { error = caught; } }) { IsBackground = true };
            worker.Start(); Assert.That(worker.Join(5000), Is.True);
            Assert.That(error, Is.TypeOf<InvalidOperationException>());
        }
    }
}
