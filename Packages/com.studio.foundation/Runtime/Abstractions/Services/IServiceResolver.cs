namespace Studio.Foundation.Services
{
    /// <summary>Read-only access to exact-contract registrations after composition freezes.</summary>
    /// <remarks>
    /// Owning-thread only. Returned services are borrowed; callers must not dispose them
    /// merely because they resolved them. The resolver does not own the services.
    /// A disposed owning scope invalidates this capability.
    /// </remarks>
    public interface IServiceResolver
    {
        /// <summary>Returns the non-null instance registered under exactly TService.</summary>
        /// <exception cref="System.Collections.Generic.KeyNotFoundException">No registration; the message names TService.</exception>
        /// <exception cref="System.InvalidOperationException">Composition is not frozen or the thread is incorrect.</exception>
        /// <exception cref="System.ObjectDisposedException">The owning scope is disposed.</exception>
        TService GetRequired<TService>() where TService : class;

        /// <summary>Returns true and the registered instance, or false and null if absent.</summary>
        /// <remarks>False means absence only, not an invalid lifecycle state or a suppressed error.</remarks>
        /// <exception cref="System.InvalidOperationException">Composition is not frozen or the thread is incorrect.</exception>
        /// <exception cref="System.ObjectDisposedException">The owning scope is disposed.</exception>
        bool TryGet<TService>(out TService service) where TService : class;
    }
}
