# Phase 1.2 — Registry and logging

Assembly: Studio.Foundation.Core. References Abstractions only; no Unity/game/third-party runtime dependencies. Consumer code uses the unchanged Phase 1.1 interfaces. Composition code explicitly references Core to create these implementations.

## ServiceRegistryBuilder

```csharp
using Studio.Foundation.Services;
using Studio.Foundation.Logging;

// Keep the builder alive for the entire application scope, not just startup.
using (var scope = new ServiceRegistryBuilder())
{
    scope.Registry.Register<ILogger>(new NullLogger());
    IServiceResolver services = scope.Build();
    ILogger logger = services.GetRequired<ILogger>();
    logger.Log(LogLevel.Information, "bootstrap", "Composition complete");
}
```

The builder owns only the registration dictionary and its capability lifetime. Its Registry property supplies a separate private registration-only view. Build freezes once and returns a separate private resolver; neither view can be cast to the builder, IDisposable or the opposite capability. No resolver is available before Build. Repeated Build and post-freeze registration throw InvalidOperationException. Registry access after Build is allowed but every write through it fails.

Lookup uses typeof(TService) exactly. Duplicate contracts (even the same instance) are rejected without replacement; aliasing one instance under different contracts is allowed. Required absence throws KeyNotFoundException naming the type, optional absence returns false/null. Registration nulls throw ArgumentNullException. A failed registration leaves existing entries intact.

Dispose is idempotent on the creating thread. It clears registrations and invalidates retained registry/resolver views; it never invokes Stop or Dispose on services. Ownership enrollment and actual service cleanup belong to the future host or current application composition owner. Explicitly stop/dispose owned services before disposing the scope if their cleanup needs resolution. Do not wrap startup alone in a using statement and then retain the resolver.

Each builder and logger captures its creating thread. Wrong-thread access throws InvalidOperationException before touching state; post-disposal operations on the owning thread throw ObjectDisposedException. No locks, global registry, service scanning, reflection construction, factories, transient scopes or async behavior are introduced.

## NullLogger

An instance-based no-op implementation of ILogger. All defined severities, including None, return false from IsEnabled. It still validates thread and arguments (including filtered log messages). It holds no sink/resource references and has no IDisposable lifecycle. There is no static singleton.

## SinkLogger

```csharp
// Supply application-owned implementations of ILogSink.
using (var logger = new Studio.Foundation.Logging.SinkLogger(
    Studio.Foundation.Logging.LogLevel.Warning, applicationSink))
{
    if (logger.IsEnabled(Studio.Foundation.Logging.LogLevel.Error))
        logger.Log(Studio.Foundation.Logging.LogLevel.Error, "loading", "Load failed", exception);
}
```

Constructor parameters are an immutable minimum threshold and params ILogSink[] slots. The array is defensively copied; null arrays/elements and undefined thresholds are rejected. Empty arrays are valid. Repeating a sink creates separate output slots and thus multiple invocations. Sinks are borrowed, never disposed by the logger.

IsEnabled compares severity to the configured threshold; it does not probe sink health or count. None always returns false. Log accepts only Trace through Critical, validates category/message before filtering (null invalid, empty allowed), and preserves the original exception reference. No message wrapper allocation, formatting or history retention is added. Callers use IsEnabled before building expensive text.

Enabled entries visit sink slots in order. Each ordinary Exception from a sink is caught; later slots still run. SinkFailureCount saturates at Int32.MaxValue, retaining no exception objects or messages. A failure does not unregister a sink: the next entry attempts it again. The logger does not attempt to make fatal runtime failures recoverable.

Valid enabled Log calls made reentrantly during sink delivery are dropped; SuppressedReentrantLogCount increments up to Int32.MaxValue. This stops forwarding cycles, including cycles through other loggers. Arguments/thread/lifetime are still validated before suppression. Filtered nested logs do not increment the suppression counter. Neither failure path tries to log its own error.

Dispose closes the logger immediately and is idempotent on the creating thread. Outside dispatch it releases sink references immediately. During dispatch it finishes the current snapshot then releases references in finally. Subsequent Log/IsEnabled calls throw ObjectDisposedException. Counters remain readable after disposal on the creating thread. Dispose never disposes sinks; composition owns their cleanup.

## Scope and validation

Phase 1.2 tests exercise exact-type lookup, missing/duplicate/null registration, freeze, separated capabilities, aliases, independent scopes, disposal and wrong-thread rejection; logging tests cover severity thresholds, input validation, sink order/context, error isolation, snapshot copying, ownership, recursive forwarding, disposal during delivery and wrong-thread rejection.

There is no Unity console sink yet; that belongs with the future Unity adapter assembly. No Bootstrap, lifecycle host, module graph execution or Event Bus implementation was added. No player/IL2CPP, allocation benchmark, external consumer or Git-submodule support claim follows from Editor unit tests. Test results are recorded after execution below.

## Validation record — 2026-10-09

- Unity 6000.3.26f1 completed compilation through MCP; post-compile console returned zero errors/warnings.
- MCP Edit Mode job bb908524ef8d4402a395b967673009ca: **37 passed, 0 failed, 0 skipped** (26 new behavioral cases plus 11 existing API/package tests).
- The behavior suite includes all seven thresholds, invalid filtered input, sink failure recovery, retained capabilities, duplicate aliases, thread violations, recursive logging and disposal during delivery.
- Hash comparison confirmed no changes to existing Assets, ProjectSettings, host manifest/lock, asmdefs or prior .meta files. New assets have unique metadata; no third-party dependencies were added.
- Play Mode was not rerun: this increment changes pure C# services, not Unity lifecycle/adapters. No player/IL2CPP, allocation benchmark, GC-retention stress or counter-saturation stress run was performed.
- Package remains 0.1.0-preview.1 with changes recorded under Unreleased. No commit, push, tag or release was created.

Next increment: lifecycle host and module dependency orchestration, using explicit owned-resource enrollment and failure rollback. It is not part of this implementation.
