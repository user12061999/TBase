using System;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using Studio.Foundation.Internal;
using Studio.Foundation.Modules;
using Studio.Foundation.Services;

namespace Studio.Foundation.Lifecycle
{
    /// <summary>Single-use, synchronous, engine-independent module and lifecycle host.</summary>
    /// <remarks>
    /// All calls use the creating thread. Module graph validation precedes configuration.
    /// Registered services and enrolled participants are borrowed unless explicitly passed to Own.
    /// Tick methods allocate no dispatch lists or delegates per call. No global host is created.
    /// </remarks>
    public sealed class ApplicationHost : IDisposable
    {
        private readonly ThreadGuard thread = new ThreadGuard();
        private readonly ServiceRegistryBuilder registry = new ServiceRegistryBuilder();
        private readonly List<ModuleRegistration> modules = new List<ModuleRegistration>();
        private readonly List<object> participants = new List<object>();
        private readonly HashSet<object> participantSet = new HashSet<object>(new ReferenceIdentityComparer());
        private readonly List<IStoppable> started = new List<IStoppable>();
        private readonly List<IDisposable> owned = new List<IDisposable>();
        private readonly HashSet<object> ownedSet = new HashSet<object>(new ReferenceIdentityComparer());
        private IUpdatable[] updates = Array.Empty<IUpdatable>();
        private IFixedUpdatable[] fixedUpdates = Array.Empty<IFixedUpdatable>();
        private ILateUpdatable[] lateUpdates = Array.Empty<ILateUpdatable>();
        private IServiceResolver services;
        private ApplicationState state;
        private bool configuring;
        private bool dispatching;
        private bool stopRequested;
        private bool disposeRequested;
        private bool disposed;

        /// <summary>Current state; remains readable after disposal on the creating thread.</summary>
        public ApplicationState State { get { thread.Check(); return state; } }

        /// <summary>Borrowed resolver available after freeze, including participant cleanup until final invalidation.</summary>
        /// <exception cref="InvalidOperationException">Registration has not finished.</exception>
        /// <exception cref="ObjectDisposedException">The host scope has ended.</exception>
        public IServiceResolver Services
        {
            get
            {
                CheckAlive();
                if (state == ApplicationState.Stopped || state == ApplicationState.Faulted)
                    throw new ObjectDisposedException(nameof(ApplicationHost));
                if (services == null) throw new InvalidOperationException("Service registration is not frozen.");
                return services;
            }
        }

        /// <summary>Adds borrowed metadata and an optional configuration callback while Created.</summary>
        /// <remarks>Metadata is snapshotted at Start; callbacks execute in validated topological order.</remarks>
        public void AddModule(IModule module, Action<ModuleContext> configure = null)
        {
            RequireCreated();
            if (module == null) throw new ArgumentNullException(nameof(module));
            modules.Add(new ModuleRegistration(module, configure));
        }

        /// <summary>Enrolls an already-created owned resource before startup. Idempotent by reference identity.</summary>
        /// <remarks>Module configuration uses ModuleContext.Own instead. No lifecycle enrollment is implied.</remarks>
        public void Own(IDisposable resource)
        {
            RequireCreated();
            EnrollOwned(resource);
        }

        /// <summary>Validates modules, configures services, freezes the registry, then initializes participants once.</summary>
        /// <remarks>
        /// Startup failure cleans up and ends Faulted. With no cleanup error, the original exception
        /// is rethrown with its stack; otherwise an AggregateException contains it first, then cleanup errors.
        /// Stop/Dispose during startup is rejected; it is not an asynchronous cancellation facility.
        /// </remarks>
        public void Start()
        {
            RequireCreated();
            state = ApplicationState.Starting;
            try
            {
                var ordered = ModuleGraph.Sort(modules);
                foreach (var module in ordered)
                {
                    if (module.Configure == null) continue;
                    var context = new ModuleContext(this);
                    configuring = true;
                    try { module.Configure(context); }
                    finally { configuring = false; context.Close(); }
                }
                services = registry.Build();

                var updateList = new List<IUpdatable>();
                var fixedList = new List<IFixedUpdatable>();
                var lateList = new List<ILateUpdatable>();
                foreach (var participant in participants)
                {
                    if (participant is IInitializable initializer) initializer.Initialize();
                    if (participant is IStoppable stoppable) started.Add(stoppable);
                    if (participant is IUpdatable update) updateList.Add(update);
                    if (participant is IFixedUpdatable fixedUpdate) fixedList.Add(fixedUpdate);
                    if (participant is ILateUpdatable lateUpdate) lateList.Add(lateUpdate);
                }
                updates = updateList.ToArray();
                fixedUpdates = fixedList.ToArray();
                lateUpdates = lateList.ToArray();
                participants.Clear();
                participantSet.Clear();
                modules.Clear();
                state = ApplicationState.Running;
            }
            catch (Exception failure)
            {
                var errors = Cleanup(ApplicationState.Faulted);
                RethrowFailure(failure, errors);
            }
        }

        /// <summary>Dispatches Update while Running using finite non-negative scaled seconds.</summary>
        public void Update(float deltaTime) => Tick(deltaTime, 0);
        /// <summary>Dispatches FixedUpdate while Running using finite non-negative fixed-step seconds.</summary>
        public void FixedUpdate(float fixedDeltaTime) => Tick(fixedDeltaTime, 1);
        /// <summary>Dispatches LateUpdate while Running using finite non-negative scaled seconds.</summary>
        public void LateUpdate(float deltaTime) => Tick(deltaTime, 2);

        /// <summary>Stops and cleans up once. During a tick, defers until the current callback returns.</summary>
        /// <remarks>Later callbacks in that tick are skipped. Stopped/Faulted hosts ignore repeated Stop.</remarks>
        public void Stop()
        {
            thread.Check();
            if (disposed || state == ApplicationState.Stopped || state == ApplicationState.Faulted) return;
            RejectTransition();
            if (dispatching) { stopRequested = true; return; }
            ThrowCleanupErrors(Cleanup(ApplicationState.Stopped));
        }

        /// <summary>Stops and closes the host. Idempotent; never disposes borrowed objects.</summary>
        public void Dispose()
        {
            thread.Check();
            if (disposed) return;
            RejectTransition();
            if (dispatching) { stopRequested = true; disposeRequested = true; return; }
            try
            {
                if (state != ApplicationState.Stopped && state != ApplicationState.Faulted)
                    ThrowCleanupErrors(Cleanup(ApplicationState.Stopped));
            }
            finally { disposed = true; }
        }

        internal IServiceRegistry ConfigurationRegistry => registry.Registry;

        internal void CheckConfiguration(bool active)
        {
            thread.Check();
            if (!active || !configuring || state != ApplicationState.Starting)
                throw new InvalidOperationException("Module configuration capability has expired.");
        }

        internal void EnrollParticipant(object participant)
        {
            if (participant == null) throw new ArgumentNullException(nameof(participant));
            if (participant.GetType().IsValueType ||
                !(participant is IInitializable || participant is IStoppable || participant is IUpdatable ||
                  participant is IFixedUpdatable || participant is ILateUpdatable))
                throw new ArgumentException("Expected a reference-type lifecycle participant.", nameof(participant));
            if (!participantSet.Add(participant))
                throw new InvalidOperationException("Participant is already enrolled.");
            participants.Add(participant);
        }

        internal void EnrollOwned(IDisposable resource)
        {
            if (resource == null) throw new ArgumentNullException(nameof(resource));
            if (ReferenceEquals(resource, this))
                throw new ArgumentException("A host cannot own itself.", nameof(resource));
            if (resource.GetType().IsValueType)
                throw new ArgumentException("Owned resources must be reference types.", nameof(resource));
            if (ownedSet.Add(resource)) owned.Add(resource);
        }

        private void Tick(float deltaTime, int phase)
        {
            CheckAlive();
            if (state != ApplicationState.Running || dispatching)
                throw new InvalidOperationException("Ticks require a Running host and cannot be nested.");
            if (float.IsNaN(deltaTime) || float.IsInfinity(deltaTime) || deltaTime < 0)
                throw new ArgumentOutOfRangeException(nameof(deltaTime), "Time must be finite and non-negative.");
            dispatching = true;
            Exception failure = null;
            try
            {
                var count = phase == 0 ? updates.Length : phase == 1 ? fixedUpdates.Length : lateUpdates.Length;
                for (var i = 0; i < count; i++)
                {
                    if (phase == 0) updates[i].Update(deltaTime);
                    else if (phase == 1) fixedUpdates[i].FixedUpdate(deltaTime);
                    else lateUpdates[i].LateUpdate(deltaTime);
                    if (stopRequested) break;
                }
            }
            catch (Exception error) { failure = error; }
            finally { dispatching = false; }

            if (failure != null) RethrowFailure(failure, Cleanup(ApplicationState.Faulted));
            else if (stopRequested) ThrowCleanupErrors(Cleanup(ApplicationState.Stopped));
        }

        private List<Exception> Cleanup(ApplicationState terminal)
        {
            state = ApplicationState.Stopping;
            var errors = new List<Exception>();
            try
            {
                for (var i = started.Count - 1; i >= 0; i--)
                    try { started[i].Stop(); } catch (Exception error) { errors.Add(error); }
                for (var i = owned.Count - 1; i >= 0; i--)
                    try { owned[i].Dispose(); } catch (Exception error) { errors.Add(error); }
            }
            finally
            {
                registry.Dispose();
                services = null;
                modules.Clear();
                participants.Clear();
                participantSet.Clear();
                started.Clear();
                owned.Clear();
                ownedSet.Clear();
                updates = Array.Empty<IUpdatable>();
                fixedUpdates = Array.Empty<IFixedUpdatable>();
                lateUpdates = Array.Empty<ILateUpdatable>();
                stopRequested = false;
                if (disposeRequested) disposed = true;
                disposeRequested = false;
                state = terminal;
            }
            return errors;
        }

        private void CheckAlive()
        {
            thread.Check();
            if (disposed) throw new ObjectDisposedException(nameof(ApplicationHost));
        }

        private void RequireCreated()
        {
            CheckAlive();
            if (state != ApplicationState.Created) throw new InvalidOperationException("Host is single-use; expected Created.");
        }

        private void RejectTransition()
        {
            if (state == ApplicationState.Starting || state == ApplicationState.Stopping)
                throw new InvalidOperationException("Reentrant startup/shutdown transitions are not supported.");
        }

        private static void ThrowCleanupErrors(List<Exception> errors)
        {
            if (errors.Count != 0) throw new AggregateException("Host cleanup failed.", errors);
        }

        private static void RethrowFailure(Exception failure, List<Exception> cleanupErrors)
        {
            if (cleanupErrors.Count == 0) ExceptionDispatchInfo.Capture(failure).Throw();
            cleanupErrors.Insert(0, failure);
            throw new AggregateException("Host failed; cleanup also reported errors.", cleanupErrors);
        }
    }
}
