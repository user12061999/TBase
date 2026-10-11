using System.Collections;
using NUnit.Framework;
using Studio.Foundation.Lifecycle;
using Studio.Foundation.Sample;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;

namespace Studio.Foundation.Validation
{
    public sealed class SceneHostReloadTests
    {
        private const string Key = "Foundation.SceneReload.";
        private static SessionCounters previous;

        [UnityTest]
        public IEnumerator SceneAuthoredHostRestartsAcrossAllReloadSettings()
        {
            PlayerBuild.RequireFixture();
            if (!SessionState.GetBool(Key + "saved", false))
            {
                SessionState.SetBool(Key + "enabled", EditorSettings.enterPlayModeOptionsEnabled);
                SessionState.SetInt(Key + "options", (int)EditorSettings.enterPlayModeOptions);
                SessionState.SetBool(Key + "saved", true);
                SessionState.SetInt(Key + "completed", 0);
                SessionState.SetInt(Key + "stops", 0);
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                new GameObject("Scene authored sample").AddComponent<MinimalBootstrap>();
                new GameObject("Camera").AddComponent<Camera>();
                new GameObject("Light").AddComponent<Light>().type = LightType.Directional;
                EditorSceneManager.SaveScene(scene, "Assets/SceneReload.unity");
            }
            while (SessionState.GetInt(Key + "completed", 0) < 12)
            {
                EditorSettings.enterPlayModeOptionsEnabled = true;
                EditorSettings.enterPlayModeOptions = (EnterPlayModeOptions)(SessionState.GetInt(Key + "completed", 0) / 3);
                SessionState.SetInt(Key + "instance", Sample().GetInstanceID());
                yield return new EnterPlayMode((EditorSettings.enterPlayModeOptions & EnterPlayModeOptions.DisableDomainReload) == 0);
                VerifyStartup();
                yield return null;
                yield return null;
                VerifyTicks();
                yield return new ExitPlayMode();
                Assert.That(SessionState.GetInt(Key + "stops", 0), Is.EqualTo(SessionState.GetInt(Key + "completed", 0) + 1));
                SessionState.SetInt(Key + "completed", SessionState.GetInt(Key + "completed", 0) + 1);
                TestContext.WriteLine("Scene-host reload cycle passed: " + SessionState.GetInt(Key + "completed", 0));
            }
        }

        private static MinimalBootstrap Sample()
        {
            var samples = Object.FindObjectsByType<MinimalBootstrap>(FindObjectsSortMode.None);
            Assert.That(samples.Length, Is.EqualTo(1), "Saved scene must retain exactly one sample marker.");
            return samples[0];
        }
        private static void VerifyStartup()
        {
            var sample = Sample();
            Assert.That(sample.Foundation.State, Is.EqualTo(ApplicationState.Running));
            Assert.That(sample.Current.Starts, Is.EqualTo(1));
            Assert.That(sample.Current.Stops, Is.Zero);
            if ((EditorSettings.enterPlayModeOptions & EnterPlayModeOptions.DisableSceneReload) != 0)
                Assert.That(sample.GetInstanceID(), Is.EqualTo(SessionState.GetInt(Key + "instance", 0)), "Scene reload disabled must reuse the authored component.");
            if (previous != null)
            {
                Assert.That(previous.Stops, Is.EqualTo(1));
                Assert.That(sample.Current, Is.Not.SameAs(previous));
            }
            previous = sample.Current;
            Application.logMessageReceived -= ObserveStop;
            Application.logMessageReceived += ObserveStop;
        }
        private static void VerifyTicks()
        {
            var status = Sample().Current;
            Assert.That(status.Updates, Is.GreaterThan(0));
            Assert.That(status.Events, Is.EqualTo(status.Updates));
        }
        private static void ObserveStop(string condition, string stack, LogType type)
        {
            if (condition == "[Foundation sample] Stopped")
                SessionState.SetInt(Key + "stops", SessionState.GetInt(Key + "stops", 0) + 1);
        }
        [UnityTearDown]
        public IEnumerator Restore()
        {
            if (EditorApplication.isPlaying) yield return new ExitPlayMode();
            Application.logMessageReceived -= ObserveStop;
            previous = null;
            if (SessionState.GetBool(Key + "saved", false))
            {
                EditorSettings.enterPlayModeOptions = (EnterPlayModeOptions)SessionState.GetInt(Key + "options", 0);
                EditorSettings.enterPlayModeOptionsEnabled = SessionState.GetBool(Key + "enabled", false);
                SessionState.EraseBool(Key + "saved");
            }
        }
    }
}
