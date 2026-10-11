using System;
using Studio.Foundation.Services;

namespace Studio.Foundation.Lifecycle
{
    /// <summary>Registration/enrollment capability valid only during one module's configuration callback.</summary>
    /// <remarks>
    /// Access on the host's creating thread. No resolution or lifetime transitions are exposed.
    /// Retaining this context does not extend its lifetime; later access throws InvalidOperationException.
    /// A separately retained Registry view is frozen by Start after all configuration callbacks.
    /// </remarks>
    public sealed class ModuleContext
    {
        private readonly ApplicationHost host;
        private bool active = true;
        internal ModuleContext(ApplicationHost host) { this.host = host; }
        internal void Close() { active = false; }

        private void Check() { host.CheckConfiguration(active); }

        /// <summary>Borrowed registration-only access. Registering a service never enrolls or owns it.</summary>
        public IServiceRegistry Registry { get { Check(); return host.ConfigurationRegistry; } }

        /// <summary>Enrolls one reference-type lifecycle participant once by identity; no ownership transfer.</summary>
        /// <exception cref="ArgumentException">Value type or no supported lifecycle capability.</exception>
        /// <exception cref="InvalidOperationException">Duplicate participant or expired context.</exception>
        public void Enroll(object participant) { Check(); host.EnrollParticipant(participant); }

        /// <summary>Transfers disposal responsibility to the host; repeat enrollment by identity is a no-op.</summary>
        /// <remarks>Disposal uses reverse first-enrollment order. This does not enroll lifecycle callbacks.</remarks>
        public void Own(IDisposable resource) { Check(); host.EnrollOwned(resource); }
    }
}
