using System;

namespace Studio.Foundation.Events
{
    /// <summary>Subscribe-only capability; the subscriber owns each returned token.</summary>
    /// <remarks>
    /// Owning-thread only, including token disposal. Each call is independent even for the same
    /// delegate. A token's Dispose is idempotent and harmless after bus disposal. Disposal removes
    /// the subscription from future publishes, never from an in-progress dispatch snapshot.
    /// Tokens do not own the bus or handler target. Dispose tokens when their owning participant stops.
    /// </remarks>
    public interface IEventSubscriber
    {
        /// <summary>Subscribes a handler for exactly TEvent and returns its non-null removal token.</summary>
        /// <exception cref="ArgumentNullException">The handler is null.</exception>
        /// <exception cref="InvalidOperationException">The call is on the wrong thread.</exception>
        /// <exception cref="ObjectDisposedException">The bus is disposed.</exception>
        IDisposable Subscribe<TEvent>(Action<TEvent> handler);
    }
}
