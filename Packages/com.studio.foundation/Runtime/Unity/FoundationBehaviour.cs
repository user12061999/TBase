using System;
using System.Threading;
using Studio.Foundation.Lifecycle;
using Studio.Foundation.Services;
using UnityEngine;

namespace Studio.Foundation.Unity
{
    /// <summary>Explicit, single-session Unity owner of an ApplicationHost. Installation never starts it.</summary>
    /// <remarks>
    /// Call Initialize on the main thread every Play session. Disabling ends the session; enabling
    /// does not restart it. The static field only guards duplicates and never exposes services.
    /// Startup/shutdown errors propagate from explicit calls; Unity callbacks log them once.
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class FoundationBehaviour : MonoBehaviour
    {
        private static FoundationBehaviour active;
        private static int mainThread;
        private static object session;
        private object initializedSession;
        private ApplicationHost host;
        private bool invoking;
        private bool cleaning;
        private bool shutdownRequested;

        /// <summary>Null before initialization; terminal state remains inspectable afterward.</summary>
        public ApplicationState? State { get { CheckThread(); return host?.State; } }
        /// <summary>Borrowed resolver; unavailable before startup or after shutdown.</summary>
        public IServiceResolver Services
        {
            get
            {
                CheckThread();
                if (host == null) throw new InvalidOperationException("Foundation is not initialized.");
                return host.Services;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetSession()
        {
            mainThread = Thread.CurrentThread.ManagedThreadId;
            // No-domain-reload sessions must never inherit an old guard or owned scope.
            if (!ReferenceEquals(active, null)) active.ShutdownFromUnity();
            active = null;
            session = new object();
        }

        /// <summary>Claims the application slot, configures a fresh host and starts it synchronously.</summary>
        /// <param name="configure">Explicit composition; enroll resources before doing fallible work.</param>
        /// <param name="persistAcrossScenes">Requires a root object; moves it to DontDestroyOnLoad after startup.</param>
        /// <remarks>One attempt per component per session, including failed startup. Duplicate rejection makes no changes.</remarks>
        public void Initialize(Action<ApplicationHost> configure, bool persistAcrossScenes = true)
        {
            CheckThread();
            if (configure == null) throw new ArgumentNullException(nameof(configure));
            if (!Application.isPlaying || !isActiveAndEnabled)
                throw new InvalidOperationException("Initialize requires an enabled component in Play Mode.");
            if (ReferenceEquals(initializedSession, session))
                throw new InvalidOperationException("This component already attempted startup in this session.");
            if (!ReferenceEquals(active, null))
                throw new InvalidOperationException("A Foundation application host is already active.");
            if (persistAcrossScenes && transform.parent != null)
                throw new InvalidOperationException("A persistent Foundation host must be a root GameObject.");

            active = this;
            initializedSession = session;
            shutdownRequested = false;
            host = new ApplicationHost();
            invoking = true;
            try
            {
                configure(host);
                if (!shutdownRequested) host.Start();
                if (!shutdownRequested && persistAcrossScenes) DontDestroyOnLoad(gameObject);
            }
            catch (Exception failure)
            {
                shutdownRequested = true;
                invoking = false;
                try { Shutdown(); }
                catch (Exception cleanup) { throw new AggregateException("Composition and cleanup failed.", failure, cleanup); }
                throw;
            }
            finally
            {
                invoking = false;
                if (shutdownRequested) Shutdown();
            }
        }

        /// <summary>Ends ownership once. During a tick, defers cleanup until the current callback returns.</summary>
        public void Shutdown()
        {
            CheckThread();
            if (cleaning) throw new InvalidOperationException("Reentrant Foundation shutdown is not supported.");
            shutdownRequested = true;
            if (invoking)
            {
                if (host?.State == ApplicationState.Running) host.Stop();
                return;
            }
            cleaning = true;
            try { host?.Dispose(); }
            finally
            {
                cleaning = false;
                if (ReferenceEquals(active, this)) active = null;
            }
        }

        private static void CheckThread()
        {
            if (mainThread == 0 || Thread.CurrentThread.ManagedThreadId != mainThread)
                throw new InvalidOperationException("Foundation Unity operations require the Play session main thread.");
        }

        private void Update() { Tick(0, Time.deltaTime); }
        private void FixedUpdate() { Tick(1, Time.fixedDeltaTime); }
        private void LateUpdate() { Tick(2, Time.deltaTime); }

        private void Tick(int phase, float delta)
        {
            if (host == null || host.State != ApplicationState.Running) return;
            invoking = true;
            try
            {
                if (phase == 0) host.Update(delta);
                else if (phase == 1) host.FixedUpdate(delta);
                else host.LateUpdate(delta);
            }
            catch (Exception error) { Debug.LogException(error, this); }
            finally
            {
                invoking = false;
                if (shutdownRequested || host.State != ApplicationState.Running) ShutdownFromUnity();
            }
        }

        private void OnDisable() { ShutdownFromUnity(); }
        private void OnDestroy() { ShutdownFromUnity(); }
        private void OnApplicationQuit() { ShutdownFromUnity(); }

        private void ShutdownFromUnity()
        {
            // Uninitialized edit-time components have no owned resources or session.
            if (host == null) return;
            try { Shutdown(); }
            catch (Exception error) { Debug.LogException(error, this); }
        }
    }
}
