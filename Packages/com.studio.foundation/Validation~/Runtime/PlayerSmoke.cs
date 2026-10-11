using System;
using System.IO;
using Studio.Foundation.Events;
using Studio.Foundation.Lifecycle;
using Studio.Foundation.Sample;
using Studio.Foundation.Unity;
using UnityEngine;

namespace Studio.Foundation.Validation
{
    /// <summary>Fixture-only executable assertions; never import into a real application.</summary>
    public sealed class PlayerSmoke : MonoBehaviour
    {
        private int frames;
        private bool finished;
        private void Update()
        {
            if (finished || ++frames < 10) return;
            finished = true;
            try
            {
                var samples = FindObjectsByType<MinimalBootstrap>(FindObjectsSortMode.None);
                Require(samples.Length == 1, "Expected exactly one sample marker.");
                var sample = samples[0];
                var counters = sample.Current;
                Require(sample.Foundation.State == ApplicationState.Running, "Host not Running.");
                Require(counters.Starts == 1 && counters.Updates > 0 && counters.Events == counters.Updates, "Lifecycle/event counters invalid.");
                Require(ReferenceEquals(sample.Foundation.Services.GetRequired<SessionCounters>(), counters), "Service identity mismatch.");
                var publisher = sample.Foundation.Services.GetRequired<IEventPublisher>();
                var duplicate = new GameObject("Duplicate smoke host").AddComponent<FoundationBehaviour>();
                var configured = false;
                try { duplicate.Initialize(_ => configured = true, false); throw new Exception("Duplicate accepted."); }
                catch (InvalidOperationException) { }
                Require(!configured, "Duplicate ran composition.");
                sample.Foundation.Shutdown();
                sample.Foundation.Shutdown();
                Require(counters.Stops == 1 && sample.Foundation.State == ApplicationState.Stopped, "Cleanup mismatch.");
                try { publisher.Publish(1); throw new Exception("Disposed bus accepted publish."); }
                catch (ObjectDisposedException) { }
                duplicate.Initialize(_ => { }, false);
                duplicate.Shutdown();
                WriteResult("PASS: startup, ticks, exact service identity, events, duplicate guard, cleanup, bus invalidation, replacement");
                Application.Quit(0);
            }
            catch (Exception failure)
            {
                Debug.LogException(failure);
                WriteResult("FAIL: " + failure);
                Application.Quit(1);
            }
        }
        private static void Require(bool value, string message) { if (!value) throw new Exception(message); }
        private static void WriteResult(string text)
        {
            var args = Environment.GetCommandLineArgs();
            for (var i = 0; i + 1 < args.Length; i++)
                if (args[i] == "-foundationResult") { File.WriteAllText(args[i + 1], text); return; }
            throw new InvalidOperationException("Missing -foundationResult output path.");
        }
    }
}
