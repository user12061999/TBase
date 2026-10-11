# Phase 1.3 — ApplicationHost and module ordering

Pure C# implementation in Studio.Foundation.Core, namespace Studio.Foundation.Lifecycle. This host has no Unity callback attributes, MonoBehaviour, scene access, static active instance, automatic discovery or asynchronous operations. Existing Abstractions interfaces remain unchanged.

## Composition API

ApplicationHost exposes State, Services, AddModule(IModule, Action<ModuleContext> = null), Own(IDisposable), Start(), Update(float), FixedUpdate(float), LateUpdate(float), Stop() and Dispose(). All operations use the constructing thread; State is readable after disposal. Create one host per application session. Multiple independent hosts are allowed for tests and isolated application scopes; a future Unity adapter will enforce a single scene/application host where required.

1. Construct the host and add module metadata plus optional configuration callbacks while Created.
2. Pre-existing resources can transfer disposal responsibility with host.Own before startup. The host rejects owning itself or boxed value types. Caller-created resources not yet accepted by Own remain caller-owned.
3. Start snapshots all module IDs/dependencies and validates the full graph before invoking any configuration callback. Metadata getters are external code and must be side-effect-free; getter exceptions fault the host.
4. Configure modules in topological order, with ordinal smallest currently-ready ID chosen first. Missing/blank/duplicate IDs, null/blank/duplicate dependency entries, self-dependencies and cycles are invalid. IDs are case-sensitive.
5. Each callback receives a short-lived ModuleContext: Registry registers borrowed service instances; Enroll enrolls reference-type lifecycle participants; Own enrolls owned disposable resources. A participant must implement at least one supported lifecycle interface. Duplicate participant enrollment fails by identity, while repeat ownership enrollment is a no-op by identity. Overridden Equals/GetHashCode never controls enrollment identity.
6. Context access expires when its callback returns, including exceptional exit. Retained registry views still enforce the registry's freeze/disposal rules. Configuration cannot resolve services, start/stop the host or enroll through host.Own; use context.Own while configuring.
7. After all callbacks finish, freeze the registry. Initialize enrolled participants in module/enrollment order. Constructor-inject dependencies when composing; the host resolver is available after freeze, including during initialization for application composition diagnostics.
8. Only successful participants become eligible for Stop. A participant without Initialize becomes successful when its enrollment is reached. Tick lists are then built once; state becomes Running.

IModule is metadata only. Neither registration, lifecycle enrollment nor implementing IDisposable implicitly transfers ownership. Module objects themselves are borrowed unless explicitly passed to Own. Caller-created resources intended for cleanup even if graph validation fails should be enrolled with host.Own before Start; resources constructed during a callback should be owned immediately after acquisition.

## Example (composition outline)

```csharp
using Studio.Foundation.Lifecycle;
using Studio.Foundation.Logging;

// moduleMetadata implements IModule; service implements lifecycle interfaces as needed.
using (var host = new ApplicationHost())
{
    host.AddModule(moduleMetadata, context =>
    {
        context.Registry.Register<ILogger>(new NullLogger());
        context.Enroll(service); // borrows it; does not acquire disposal ownership
        // context.Own(disposableResource); // explicit, when this host should dispose it
    });
    host.Start();
    host.Update(0.016f); // a future Unity adapter supplies actual frame time
    host.Stop();
}
```

The names moduleMetadata/service above are application-supplied instances, not new framework classes. Do not wrap startup alone in using if the host must tick across future frames.

## State and transitions

Created -> Starting -> Running -> Stopping -> Stopped. Startup/tick failure instead ends Faulted after cleanup. Start is single-use even after failure. AddModule/host.Own require Created. Ticks require Running and finite non-negative seconds; invalid caller input is rejected without faulting an otherwise healthy host. Update and LateUpdate consume scaled frame delta; FixedUpdate consumes scaled fixed-step delta from the caller. Zero is valid.

Stop before Start skips all configuration/initialization and disposes resources already accepted by host.Own. Repeated Stop/Dispose after terminal cleanup is harmless on the owning thread. Start/tick after Dispose throws ObjectDisposedException; invalid state calls otherwise throw InvalidOperationException. Accessing Services before freeze throws InvalidOperationException; after terminal cleanup it throws ObjectDisposedException. A previously retained resolver is invalidated when cleanup ends.

Nested tick dispatch is rejected, including cross-phase calls. Stop/Dispose requested inside a tick does not interrupt that callback: the state stays Running until it returns, then remaining callbacks in that dispatch are skipped and cleanup begins. Multiple requests coalesce. If the callback then throws, Faulted takes precedence over the normal stop request. Stop/Dispose during startup or cleanup is rejected as a reentrant transition; if that exception escapes user code it participates in normal failure handling. This is not async startup cancellation.

## Cleanup and failures

Stop successfully started participants in reverse startup order, then dispose explicitly owned resources in reverse first-enrollment order. Each operation is attempted once. A throwing initializer must unwind its own partial active state; the host does not Stop it but still disposes it if explicitly owned. Unreached participants are not stopped; resources owned during configuration are disposed even if initialization never reaches them.

Every ordinary cleanup Exception is collected while other cleanup continues. Registry resolution remains available through Stop and resource Dispose callbacks if registration reached freeze, then the host invalidates resolver/registry views and clears module, participant, resource and tick references. Normal shutdown ends Stopped even if cleanup fails, and throws AggregateException containing failures in attempt order. Subsequent Stop does not retry or rethrow old failures.

Startup/tick failures end Faulted. With clean teardown, rethrow the same original exception while preserving its stack. With teardown failures, throw AggregateException with the original failure first, followed by cleanup errors in order; do not flatten nested user exceptions or retain them indefinitely on the host. The caller can log/capture the exception. The host does not depend on a working logger to report failures. Fatal runtime failure recovery is not guaranteed.

Tick arrays are fixed after successful startup; normal dispatch uses no LINQ, reflection scanning, delegate construction or rebuilding lists. Startup and cleanup can allocate. Host cleanup does not guarantee destruction of service instances still referenced by application code.

## Scope and validation limits

This increment implements the pure host, module graph and explicit resource lifetime. Unity host uniqueness, scene persistence, Enter Play Mode/domain reload matrix, application quit hooks, Unity console sink and Event Bus remain future work. No Unity scene/settings modification or third-party package is needed. Edit Mode unit tests exercise this code directly; they are not evidence for a player/IL2CPP build or Unity scene lifecycle correctness.

## Validation record — 2026-10-09

- Unity 6000.3.26f1 completed compilation through MCP with no reported compiler errors/warnings.
- Edit Mode job 078ea6135bc9478085d0ca67c0324a46 passed **73/73**, zero failures/skips: 36 new host/module cases plus 37 existing tests.
- Covered graph validation before configuration, deterministic ready-node ordering, metadata snapshotting, identity-based enrollment, registry freeze/cleanup access, original-exception preservation, reverse and failure-tolerant cleanup, all three tick phases, invalid time, deferred Stop/Dispose, reentrancy and thread affinity.
- Both warmed allocation checks passed with zero bytes measured by GC.GetAllocatedBytesForCurrentThread across 3,000 tick calls, after 3,000 warm-up calls, for an empty host and one no-op participant. This excludes startup, cleanup, test assertions, application callback allocations and other threads; no player performance claim is made.
- Baseline hash audit confirmed existing assets/settings, dependency manifests, asmdefs and prior GUIDs unchanged. New files have unique .meta GUIDs. No dependency, commit, push or release was added.
- No Play Mode rerun, player/IL2CPP build, Unity scene/reload integration or leak stress test was performed in this pure C# increment.

Next increment: synchronous typed Event Bus following the approved contracts, with ordering, snapshot mutation, exception aggregation and disposal tests. Unity Bootstrap/adapters follow once the core services are complete.
