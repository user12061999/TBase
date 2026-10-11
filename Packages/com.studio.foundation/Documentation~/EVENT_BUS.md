# Typed Event Bus — Phase 1.4

`Studio.Foundation.Events.EventBus` implements `IEventBus` in Core. All operations run on the creating thread; no Unity engine dependency or implicit thread marshaling exists. Give consumers `IEventPublisher` or `IEventSubscriber`; keep disposal with the composition owner.

```csharp
using Studio.Foundation.Events;

using (var bus = new EventBus())
{
    using (var subscription = bus.Subscribe<int>(value => System.Console.WriteLine(value)))
    {
        bus.Publish(42);
    }
}
```

In an ApplicationHost composition, explicitly `Own(bus)` and register borrowed event capabilities during module configuration. Registering a service does not transfer ownership. Participants should release their tokens in Stop; host-owned bus disposal subsequently releases remaining handlers. Do not dispose the bus merely because one subscriber stops.

## Delivery and lifetime

- The generic type is the dispatch key. Publishing a derived object as its base type only invokes base-type subscriptions. Null reference/nullable payloads are rejected, even with no listeners.
- Each Subscribe call creates an independent token, including duplicate delegates. Handlers run synchronously in subscription order.
- A publish captures its channel's array. Adding or removing subscriptions inside a handler affects the next publish; a removed handler still runs if present in the current snapshot.
- All subscribed delegates are attempted. Failures are reported afterward in an AggregateException, in invocation order, even for one failure. Multicast delegates remain a single subscription with ordinary .NET invocation-list behavior; subscribe handlers separately to isolate their failures.
- Reentrant Publish on the same bus is rejected across all event types. Independent buses may publish from callbacks. Cycles between buses can still cause failures; avoid cyclic forwarding.
- Dispose closes the bus immediately. An in-flight snapshot finishes, including error aggregation, before channels release handlers. The bus never disposes handler targets or payloads.
- Bus and token disposal are idempotent on the owning thread. Tokens remain safe after bus disposal. Wrong-thread calls still fail after disposal.

## Implementation trade-offs

Copy-on-write arrays move snapshot allocation to subscription changes. Subscribe/unsubscribe are O(n); successful stable dispatch is O(n) with a dictionary lookup. This favors stable subscriber sets; it is not intended as a high-churn or concurrent event system. Exceptions allocate an error list and aggregate. Empty channel buckets retain their generic type until bus disposal, but retain no handlers. Tokens do not retain delegates directly, and disposed channels are cleared even when a token remains referenced.

There is no event retention, replay, queue, automatic discovery or static bus. Existing public contracts are unchanged; no API migration is required.

## Validation — 2026-10-09

Unity 6000.3.26f1, TBase, MCP Edit Mode job `f1155821292c4b8db6417e2d6dc0a3d8`: **96 passed, 0 failed, 0 skipped**, including 23 new Event Bus cases. Tests exercise payload dispatch, ordering, duplicates, callback mutation, error recovery, reentrancy, shutdown, thread affinity and weak-reference handler release with retained tokens.

After 100 warm-up publishes, 1,000 publishes of an int payload measured 0 managed bytes for each of 0, 1, 10 and 100 listeners via GC.GetAllocatedBytesForCurrentThread. This measures this Editor/test path only; it does not establish allocations for every payload/backend or player performance. Subscription mutation and exception paths allocate by design.

Unity completed compilation before the test run. The console included one MCP transport warning: `Unexpected receive error: WebSocket is not initialised`; routing recovered and the test job completed. No C# compilation errors were observed. No Play Mode or player build was run for this pure C# increment. Bootstrap/adapters, scene/reload behavior and distribution validation remain future work.
