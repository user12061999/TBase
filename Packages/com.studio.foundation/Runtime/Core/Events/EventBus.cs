using System;
using System.Collections.Generic;
using Studio.Foundation.Internal;

namespace Studio.Foundation.Events
{
    /// <summary>Instance-owned, synchronous event bus dispatching by the exact generic event type.</summary>
    /// <remarks>
    /// Bind all operations, including subscription-token disposal, to the creating thread.
    /// Subscription changes copy the channel's ordered snapshot; steady subscription sets reuse it.
    /// Every registered delegate is attempted before handler exceptions are aggregated in order.
    /// No global instance, queue, reflection invocation or event retention is used.
    /// </remarks>
    public sealed class EventBus : IEventBus
    {
        private readonly ThreadGuard thread = new ThreadGuard();
        private readonly Dictionary<Type, IChannel> channels = new Dictionary<Type, IChannel>();
        private bool dispatching;
        private bool disposed;

        /// <inheritdoc />
        public IDisposable Subscribe<TEvent>(Action<TEvent> handler)
        {
            CheckAlive();
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            if (!channels.TryGetValue(typeof(TEvent), out var entry))
            {
                entry = new Channel<TEvent>();
                channels.Add(typeof(TEvent), entry);
            }
            return ((Channel<TEvent>)entry).Add(handler, thread);
        }

        /// <inheritdoc />
        public void Publish<TEvent>(TEvent payload)
        {
            CheckAlive();
            if (payload is null) throw new ArgumentNullException(nameof(payload));
            if (dispatching) throw new InvalidOperationException("Reentrant publish on the same event bus is not supported.");
            if (!channels.TryGetValue(typeof(TEvent), out var entry)) return;

            var snapshot = ((Channel<TEvent>)entry).Snapshot;
            List<Exception> failures = null;
            dispatching = true;
            try
            {
                for (var i = 0; i < snapshot.Length; i++)
                {
                    try { snapshot[i].Handler(payload); }
                    catch (Exception failure)
                    {
                        if (failures == null) failures = new List<Exception>();
                        failures.Add(failure);
                    }
                }
            }
            finally
            {
                dispatching = false;
                if (disposed) ClearChannels();
            }
            if (failures != null) throw new AggregateException("Event handlers failed.", failures);
        }

        /// <summary>Closes immediately; current dispatch finishes before all channel references are released.</summary>
        /// <remarks>Idempotent on the owning thread. Does not dispose payloads or handler targets.</remarks>
        public void Dispose()
        {
            thread.Check();
            if (disposed) return;
            disposed = true;
            if (!dispatching) ClearChannels();
        }

        private void CheckAlive()
        {
            thread.Check();
            if (disposed) throw new ObjectDisposedException(nameof(EventBus));
        }

        private void ClearChannels()
        {
            foreach (var channel in channels.Values) channel.Clear();
            channels.Clear();
        }

        private interface IChannel { void Clear(); }

        private readonly struct Entry<TEvent>
        {
            public readonly Subscription<TEvent> Token;
            public readonly Action<TEvent> Handler;
            public Entry(Subscription<TEvent> token, Action<TEvent> handler) { Token = token; Handler = handler; }
        }

        private sealed class Channel<TEvent> : IChannel
        {
            public Entry<TEvent>[] Snapshot { get; private set; } = Array.Empty<Entry<TEvent>>();

            public IDisposable Add(Action<TEvent> handler, ThreadGuard thread)
            {
                var token = new Subscription<TEvent>(this, thread);
                var next = new Entry<TEvent>[Snapshot.Length + 1];
                Array.Copy(Snapshot, next, Snapshot.Length);
                next[next.Length - 1] = new Entry<TEvent>(token, handler);
                Snapshot = next;
                return token;
            }

            public void Remove(Subscription<TEvent> token)
            {
                for (var i = 0; i < Snapshot.Length; i++)
                {
                    if (!ReferenceEquals(Snapshot[i].Token, token)) continue;
                    if (Snapshot.Length == 1) Snapshot = Array.Empty<Entry<TEvent>>();
                    else
                    {
                        var next = new Entry<TEvent>[Snapshot.Length - 1];
                        Array.Copy(Snapshot, 0, next, 0, i);
                        Array.Copy(Snapshot, i + 1, next, i, next.Length - i);
                        Snapshot = next;
                    }
                    return;
                }
            }

            // Tokens retain no delegate. Clearing even a token-retained channel releases all handlers.
            public void Clear() { Snapshot = Array.Empty<Entry<TEvent>>(); }
        }

        private sealed class Subscription<TEvent> : IDisposable
        {
            private readonly ThreadGuard thread;
            private Channel<TEvent> channel;
            public Subscription(Channel<TEvent> channel, ThreadGuard thread)
            { this.channel = channel; this.thread = thread; }

            public void Dispose()
            {
                thread.Check();
                if (channel == null) return;
                channel.Remove(this);
                channel = null;
            }
        }
    }
}
