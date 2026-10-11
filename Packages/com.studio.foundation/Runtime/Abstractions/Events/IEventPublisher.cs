namespace Studio.Foundation.Events
{
    /// <summary>Publish-only capability for synchronous, exact-type event delivery.</summary>
    /// <remarks>
    /// Owning-thread only. TEvent is the dispatch key even if the payload has a derived runtime type.
    /// Handlers run in subscription order against a fixed snapshot. Subscription changes affect
    /// subsequent publishes. All snapshot handlers are attempted before their errors are aggregated.
    /// No queue, replay, event retention, implicit main-thread marshaling or ownership transfer.
    /// </remarks>
    public interface IEventPublisher
    {
        /// <summary>Delivers a non-null payload to subscribers of exactly TEvent.</summary>
        /// <exception cref="System.ArgumentNullException">A reference or nullable payload is null.</exception>
        /// <exception cref="System.InvalidOperationException">Publish is reentrant or called from the wrong thread.</exception>
        /// <exception cref="System.ObjectDisposedException">The bus is disposed.</exception>
        /// <exception cref="System.AggregateException">One or more snapshot handlers threw; all were attempted.</exception>
        void Publish<TEvent>(TEvent payload);
    }
}
