namespace Studio.Foundation.Services
{
    /// <summary>Registration-only capability for explicit service instances.</summary>
    /// <remarks>
    /// Composition-thread only. Registration is allowed before the host freezes composition.
    /// Registration neither initializes nor owns/disposes the instance. Consumers receive
    /// IServiceResolver after freeze; they must not retain registration access.
    /// The future Core composition builder owns the freeze operation.
    /// </remarks>
    public interface IServiceRegistry
    {
        /// <summary>Registers a non-null reference under the exact TService contract.</summary>
        /// <typeparam name="TService">Reference-type contract; no assignable-type lookup is implied.</typeparam>
        /// <param name="instance">Borrowed instance; ownership is enrolled separately with the host.</param>
        /// <exception cref="System.ArgumentNullException">The instance is null.</exception>
        /// <exception cref="System.InvalidOperationException">
        /// The contract is already registered, composition is frozen, or the call is on the wrong thread.
        /// </exception>
        /// <exception cref="System.ObjectDisposedException">The owning composition scope is disposed.</exception>
        void Register<TService>(TService instance) where TService : class;
    }
}
