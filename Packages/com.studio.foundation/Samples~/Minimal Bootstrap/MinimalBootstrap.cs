using System;
using System.Collections.Generic;
using Studio.Foundation.Events;
using Studio.Foundation.Lifecycle;
using Studio.Foundation.Logging;
using Studio.Foundation.Modules;
using Studio.Foundation.Unity;
using UnityEngine;

namespace Studio.Foundation.Sample
{
    /// <summary>Opt-in scene composition sample. Attach to exactly one active scene root.</summary>
    [RequireComponent(typeof(FoundationBehaviour))]
    [DisallowMultipleComponent]
    public sealed class MinimalBootstrap : MonoBehaviour
    {
        /// <summary>Per-session counters; replaced rather than reused on each Play session.</summary>
        public SessionCounters Current { get; private set; }
        public FoundationBehaviour Foundation => GetComponent<FoundationBehaviour>();

        // This explicit sample-only scan handles scene-authored objects even when Awake/Start
        // do not repeat with scene reload disabled. Installing the base package does not run it.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void StartSceneApplications()
        {
            foreach (var sample in FindObjectsByType<MinimalBootstrap>(FindObjectsSortMode.None))
                if (sample.isActiveAndEnabled) sample.StartApplication();
        }

        public void StartApplication()
        {
            Foundation.Initialize(host =>
            {
                var counters = new SessionCounters();
                Current = counters;
                host.AddModule(new SampleModule(), context =>
                {
                    var logger = new SinkLogger(LogLevel.Information, new UnityConsoleSink());
                    context.Own(logger);
                    var events = new EventBus();
                    context.Own(events);
                    context.Registry.Register(counters);
                    context.Registry.Register<IEventPublisher>(events);
                    context.Registry.Register<IEventSubscriber>(events);
                    context.Registry.Register<Studio.Foundation.Logging.ILogger>(logger);
                    context.Enroll(new Session(events, logger, counters));
                });
            }, persistAcrossScenes: false);
        }

        private sealed class SampleModule : IModule
        {
            public string Id => "foundation.sample";
            public IReadOnlyList<string> Dependencies => Array.Empty<string>();
        }

        private readonly struct TickEvent { }

        private sealed class Session : IInitializable, IUpdatable, IStoppable
        {
            private readonly IEventBus events;
            private readonly Studio.Foundation.Logging.ILogger logger;
            private readonly SessionCounters counters;
            private IDisposable subscription;
            public Session(IEventBus events, Studio.Foundation.Logging.ILogger logger, SessionCounters counters)
            { this.events = events; this.logger = logger; this.counters = counters; }
            public void Initialize()
            {
                subscription = events.Subscribe<TickEvent>(_ => counters.Events++);
                counters.Starts++;
                logger.Log(LogLevel.Information, "Foundation sample", "Started");
            }
            public void Update(float deltaTime)
            {
                counters.Updates++;
                events.Publish(new TickEvent());
            }
            public void Stop()
            {
                subscription?.Dispose();
                subscription = null;
                counters.Stops++;
                logger.Log(LogLevel.Information, "Foundation sample", "Stopped");
            }
        }
    }

    /// <summary>Sample-only observable state, not a foundation API.</summary>
    public sealed class SessionCounters
    {
        public int Starts { get; internal set; }
        public int Updates { get; internal set; }
        public int Events { get; internal set; }
        public int Stops { get; internal set; }
    }
}
