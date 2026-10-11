using System;

namespace Studio.Foundation.Events
{
    /// <summary>Owner-facing bus capability; consumers should receive narrower event interfaces.</summary>
    /// <remarks>
    /// Dispose on the owning thread; repeated disposal is a no-op. Disposal releases all retained
    /// handlers. During dispatch, disposal is deferred until the current snapshot finishes;
    /// subsequent publish/subscribe operations then throw ObjectDisposedException.
    /// A dispose request during dispatch prevents new subscriptions immediately. Existing
    /// subscription tokens remain safe to dispose. The bus never disposes handler targets.
    /// </remarks>
    public interface IEventBus : IEventPublisher, IEventSubscriber, IDisposable
    {
    }
}
