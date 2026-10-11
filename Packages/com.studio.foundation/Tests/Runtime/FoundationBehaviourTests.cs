using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using Studio.Foundation.Lifecycle;
using Studio.Foundation.Modules;
using Studio.Foundation.Unity;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Studio.Foundation.Tests.Runtime
{
    public sealed class FoundationBehaviourTests
    {
        private readonly List<GameObject> objects = new List<GameObject>();
        private sealed class Module : IModule
        {
            public string Id => "test";
            public IReadOnlyList<string> Dependencies => Array.Empty<string>();
        }
        private sealed class Probe : IInitializable, IStoppable, IUpdatable, IFixedUpdatable, ILateUpdatable, IDisposable
        {
            public int Starts, Stops, Disposals, Updates, Fixed, Late;
            public Action OnStart, OnUpdate, OnStop;
            public void Initialize() { Starts++; OnStart?.Invoke(); }
            public void Stop() { Stops++; OnStop?.Invoke(); }
            public void Dispose() { Disposals++; }
            public void Update(float delta) { Assert.That(delta, Is.GreaterThanOrEqualTo(0)); Updates++; OnUpdate?.Invoke(); }
            public void FixedUpdate(float delta) { Assert.That(delta, Is.GreaterThanOrEqualTo(0)); Fixed++; }
            public void LateUpdate(float delta) { Assert.That(delta, Is.GreaterThanOrEqualTo(0)); Late++; }
        }
        private FoundationBehaviour Create()
        {
            var go = new GameObject("Foundation test host");
            objects.Add(go);
            return go.AddComponent<FoundationBehaviour>();
        }
        private static void Configure(ApplicationHost host, Probe probe)
        {
            host.AddModule(new Module(), context => { context.Registry.Register(probe); context.Enroll(probe); context.Own(probe); });
        }
        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            foreach (var go in objects) if (go != null) Object.Destroy(go);
            objects.Clear();
            yield return null;
        }

        [UnityTest]
        public IEnumerator ComponentIsInertUntilExplicitInitializationAndForwardsTicks()
        {
            var adapter = Create();
            yield return null;
            Assert.That(adapter.State, Is.Null);
            var probe = new Probe();
            adapter.Initialize(h => Configure(h, probe), false);
            Assert.That(adapter.Services.GetRequired<Probe>(), Is.SameAs(probe));
            yield return new WaitForFixedUpdate();
            yield return null;
            Assert.That(probe.Starts, Is.EqualTo(1));
            Assert.That(probe.Updates, Is.GreaterThan(0));
            Assert.That(probe.Fixed, Is.GreaterThan(0));
            Assert.That(probe.Late, Is.GreaterThan(0));
            adapter.Shutdown();
            adapter.Shutdown();
            Assert.That(probe.Stops, Is.EqualTo(1));
            Assert.That(probe.Disposals, Is.EqualTo(1));
            Assert.Throws<ObjectDisposedException>(() => adapter.Services.GetRequired<Probe>());
        }

        [Test]
        public void DuplicateRejectsBeforeCompositionAndCannotStealGuard()
        {
            var first = Create();
            var duplicate = Create();
            first.Initialize(_ => { }, false);
            var calls = 0;
            Assert.Throws<InvalidOperationException>(() => duplicate.Initialize(_ => calls++, false));
            duplicate.Shutdown();
            Assert.Throws<InvalidOperationException>(() => duplicate.Initialize(_ => calls++, false));
            Assert.That(calls, Is.Zero);
            first.Shutdown();
            duplicate.Initialize(_ => calls++, false);
            Assert.That(calls, Is.EqualTo(1));
        }

        [Test]
        public void CompositionFailureCleansOwnedResourcesAndReleasesSlot()
        {
            var adapter = Create();
            var owned = new Probe();
            var original = new Exception("composition");
            var error = Assert.Throws<Exception>(() => adapter.Initialize(h => { h.Own(owned); throw original; }, false));
            Assert.That(error, Is.SameAs(original));
            Assert.That(owned.Disposals, Is.EqualTo(1));
            Assert.Throws<InvalidOperationException>(() => adapter.Initialize(_ => { }, false));
            Create().Initialize(_ => { }, false);
        }

        [Test]
        public void StartupFailureRollsBackAndReleasesSlot()
        {
            var probe = new Probe { OnStart = () => throw new Exception("startup") };
            var adapter = Create();
            Assert.Throws<Exception>(() => adapter.Initialize(h => Configure(h, probe), false));
            Assert.That(adapter.State, Is.EqualTo(ApplicationState.Faulted));
            Assert.That(probe.Stops, Is.Zero);
            Assert.That(probe.Disposals, Is.EqualTo(1));
            Create().Initialize(_ => { }, false);
        }

        [UnityTest]
        public IEnumerator DisableStopsOnceAndEnableDoesNotRestart()
        {
            var adapter = Create();
            var probe = new Probe();
            adapter.Initialize(h => Configure(h, probe), false);
            adapter.enabled = false;
            adapter.enabled = true;
            yield return null;
            Assert.That(adapter.State, Is.EqualTo(ApplicationState.Stopped));
            Assert.That(probe.Starts, Is.EqualTo(1));
            Assert.That(probe.Stops, Is.EqualTo(1));
            Assert.That(probe.Disposals, Is.EqualTo(1));
            Assert.Throws<InvalidOperationException>(() => adapter.Initialize(_ => { }, false));
        }

        [UnityTest]
        public IEnumerator DestroyCleansAndAllowsReplacement()
        {
            var adapter = Create();
            var probe = new Probe();
            adapter.Initialize(h => Configure(h, probe), false);
            Object.Destroy(adapter.gameObject);
            yield return null;
            Assert.That(probe.Stops, Is.EqualTo(1));
            Assert.That(probe.Disposals, Is.EqualTo(1));
            Create().Initialize(_ => { }, false);
        }

        [UnityTest]
        public IEnumerator TickFailureLogsOnceCleansAndStopsDispatch()
        {
            var adapter = Create();
            var probe = new Probe { OnUpdate = () => throw new InvalidOperationException("foundation tick failure") };
            adapter.Initialize(h => Configure(h, probe), false);
            LogAssert.Expect(LogType.Exception, new Regex("InvalidOperationException: foundation tick failure"));
            yield return null;
            yield return null;
            Assert.That(adapter.State, Is.EqualTo(ApplicationState.Faulted));
            Assert.That(probe.Updates, Is.EqualTo(1));
            Assert.That(probe.Stops, Is.EqualTo(1));
            Assert.That(probe.Disposals, Is.EqualTo(1));
            Create().Initialize(_ => { }, false);
        }

        [UnityTest]
        public IEnumerator DisableInsideTickDefersCleanupAndKeepsGuardUntilReturn()
        {
            var adapter = Create();
            var next = Create();
            var probe = new Probe();
            probe.OnUpdate = () =>
            {
                adapter.enabled = false;
                Assert.That(probe.Stops, Is.Zero);
                Assert.Throws<InvalidOperationException>(() => next.Initialize(_ => { }, false));
            };
            adapter.Initialize(h => Configure(h, probe), false);
            yield return null;
            yield return null;
            Assert.That(probe.Stops, Is.EqualTo(1));
            Assert.That(probe.Disposals, Is.EqualTo(1));
            next.Initialize(_ => { }, false);
        }

        [Test]
        public void DisableInsideStartupFinishesInitializationThenCleans()
        {
            var adapter = Create();
            var probe = new Probe { OnStart = () => adapter.enabled = false };
            adapter.Initialize(h => Configure(h, probe), false);
            Assert.That(probe.Starts, Is.EqualTo(1));
            Assert.That(probe.Stops, Is.EqualTo(1));
            Assert.That(probe.Disposals, Is.EqualTo(1));
            Create().Initialize(_ => { }, false);
        }

        [Test]
        public void PersistentChildIsRejectedBeforeComposition()
        {
            var parent = Create();
            var child = Create();
            child.transform.SetParent(parent.transform);
            var configured = false;
            Assert.Throws<InvalidOperationException>(() => child.Initialize(_ => configured = true));
            Assert.That(configured, Is.False);
            child.Initialize(_ => { }, false);
        }

        [UnityTest]
        public IEnumerator PersistenceSurvivesSourceSceneUnload()
        {
            var source = SceneManager.CreateScene("Foundation temporary scene");
            var adapter = Create();
            SceneManager.MoveGameObjectToScene(adapter.gameObject, source);
            var probe = new Probe();
            adapter.Initialize(h => Configure(h, probe));
            yield return SceneManager.UnloadSceneAsync(source);
            Assert.That(adapter != null, Is.True);
            Assert.That(adapter.State, Is.EqualTo(ApplicationState.Running));
            Assert.That(probe.Stops, Is.Zero);
            adapter.Shutdown();
        }

        [Test]
        public void ReentrantShutdownCannotReleaseGuardDuringCleanup()
        {
            var adapter = Create();
            var replacement = Create();
            var probe = new Probe { OnStop = () =>
            {
                Assert.Throws<InvalidOperationException>(adapter.Shutdown);
                Assert.Throws<InvalidOperationException>(() => replacement.Initialize(_ => { }, false));
            } };
            adapter.Initialize(h => Configure(h, probe), false);
            Assert.DoesNotThrow(adapter.Shutdown);
            replacement.Initialize(_ => { }, false);
        }

        [Test]
        public void CleanupFailuresStillReleaseApplicationSlot()
        {
            var adapter = Create();
            var probe = new Probe { OnStop = () => throw new Exception("cleanup") };
            adapter.Initialize(h => Configure(h, probe), false);
            Assert.Throws<AggregateException>(adapter.Shutdown);
            Assert.That(probe.Disposals, Is.EqualTo(1));
            Assert.DoesNotThrow(adapter.Shutdown);
            Create().Initialize(_ => { }, false);
        }
    }
}
