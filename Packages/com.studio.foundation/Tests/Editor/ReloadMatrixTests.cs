using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Studio.Foundation.Lifecycle;
using Studio.Foundation.Modules;
using Studio.Foundation.Unity;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace Studio.Foundation.Tests.Editor
{
    // Explicit and batch-only: this changes Editor play options and creates runtime-only hosts.
    // Run only in a disposable consumer, never the application's working project.
    [Explicit("Run by exact test filter in an isolated batchmode fixture.")]
    public sealed class ReloadMatrixTests
    {
        private const string Key = "Studio.Foundation.ReloadMatrix.";
        private sealed class Probe : IModule, IInitializable, IStoppable, IDisposable
        {
            public string Id => "reload";
            public IReadOnlyList<string> Dependencies => Array.Empty<string>();
            public void Initialize() { SessionState.SetInt(Key + "starts", SessionState.GetInt(Key + "starts", 0) + 1); }
            public void Stop() { SessionState.SetInt(Key + "stops", SessionState.GetInt(Key + "stops", 0) + 1); }
            public void Dispose() { SessionState.SetInt(Key + "disposes", SessionState.GetInt(Key + "disposes", 0) + 1); }
        }

        [UnityTest]
        public IEnumerator AllReloadCombinationsStartFreshAndCleanThreeTimes()
        {
            if (!Application.isBatchMode) Assert.Ignore("Requires an isolated batchmode fixture.");
            if (!SessionState.GetBool(Key + "saved", false))
            {
                SessionState.SetBool(Key + "enabled", EditorSettings.enterPlayModeOptionsEnabled);
                SessionState.SetInt(Key + "options", (int)EditorSettings.enterPlayModeOptions);
                SessionState.SetBool(Key + "saved", true);
                SessionState.SetInt(Key + "starts", 0);
                SessionState.SetInt(Key + "completed", 0);
                SessionState.SetInt(Key + "stops", 0);
                SessionState.SetInt(Key + "disposes", 0);
            }
            while (SessionState.GetInt(Key + "completed", 0) < 12)
            {
                EditorSettings.enterPlayModeOptionsEnabled = true;
                EditorSettings.enterPlayModeOptions = (EnterPlayModeOptions)(SessionState.GetInt(Key + "completed", 0) / 3);
                yield return new EnterPlayMode((EditorSettings.enterPlayModeOptions & EnterPlayModeOptions.DisableDomainReload) == 0);
                StartSessionHost();
                Assert.That(SessionState.GetInt(Key + "starts", 0), Is.EqualTo(SessionState.GetInt(Key + "completed", 0) + 1));
                yield return null;
                yield return new ExitPlayMode();
                Assert.That(SessionState.GetInt(Key + "stops", 0), Is.EqualTo(SessionState.GetInt(Key + "completed", 0) + 1));
                Assert.That(SessionState.GetInt(Key + "disposes", 0), Is.EqualTo(SessionState.GetInt(Key + "completed", 0) + 1));
                SessionState.SetInt(Key + "completed", SessionState.GetInt(Key + "completed", 0) + 1);
                TestContext.WriteLine("Completed reload cycle " + SessionState.GetInt(Key + "completed", 0));
            }
        }
        // Keep captured composition locals outside the coroutine: its compiler-generated closure
        // cannot survive Unity Test Framework's domain-reload continuation serialization.
        private static void StartSessionHost()
        {
            var adapter = new GameObject("Foundation reload host").AddComponent<FoundationBehaviour>();
            var probe = new Probe();
            adapter.Initialize(h => h.AddModule(probe, c => { c.Enroll(probe); c.Own(probe); }), false);
            Assert.That(adapter.State, Is.EqualTo(ApplicationState.Running));
        }

        [UnityTearDown]
        public IEnumerator RestoreSettings()
        {
            if (EditorApplication.isPlaying) yield return new ExitPlayMode();
            if (SessionState.GetBool(Key + "saved", false))
            {
                EditorSettings.enterPlayModeOptions = (EnterPlayModeOptions)SessionState.GetInt(Key + "options", 0);
                EditorSettings.enterPlayModeOptionsEnabled = SessionState.GetBool(Key + "enabled", false);
                SessionState.EraseBool(Key + "saved");
            }
        }
    }
}
