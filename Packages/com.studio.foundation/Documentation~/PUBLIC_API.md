# Public contracts

Status: Abstractions contracts introduced in Phase 1.1, retained through Phase 1.6. Package remains 0.1.0-preview.1 in development; no release/tag exists. See API_REVIEW.md for the full 23-type runtime surface and RELEASE_CHECKLIST.md for publication gates.

## Capabilities

| Namespace suffix (under Studio.Foundation) | Contract | Public members |
| --- | --- | --- |
| Services | IServiceRegistry | Register<TService>(TService), where TService : class |
| Services | IServiceResolver | GetRequired<TService>(), TryGet<TService>(out TService), where TService : class |
| Lifecycle | IInitializable / IStoppable | Initialize() / Stop() |
| Lifecycle | IUpdatable / IFixedUpdatable / ILateUpdatable | Update(float) / FixedUpdate(float) / LateUpdate(float) |
| Events | IEventPublisher | Publish<TEvent>(TEvent) |
| Events | IEventSubscriber | IDisposable Subscribe<TEvent>(Action<TEvent>) |
| Events | IEventBus | Inherits publisher, subscriber and IDisposable; owner-facing only |
| Logging | ILogger | IsEnabled(LogLevel), Log(LogLevel, string category, string message, Exception = null) |
| Logging | ILogSink | Write(LogLevel, string category, string message, Exception) |
| Logging | LogLevel | Trace=0, Debug=1, Information=2, Warning=3, Error=4, Critical=5, None=6 |
| Modules | IModule | string Id { get; }, IReadOnlyList<string> Dependencies { get; } |

Use only the capability a consumer needs. Consumer asmdefs explicitly reference Studio.Foundation.Abstractions. No UnityEngine, UnityEditor, game assembly, Core type or third-party API occurs in the contracts. Interfaces have no default implementations. Service resolution is restricted to composition; ordinary systems receive dependencies through constructors.

## Service registration and lookup

Register exact reference-type contracts with non-null instances. There is no implicit replacement or assignable-type search. Null registration throws ArgumentNullException; duplicate registration throws InvalidOperationException. Registration borrows instances and never determines lifecycle/resource ownership. Registering one instance under multiple contracts does not imply multiple disposal calls.

Core's ServiceRegistryBuilder freezes composition and exposes IServiceResolver only afterwards. Freeze/build is deliberately not a registration capability, so an installer cannot prematurely close composition. No builder interface or factory API is added to Abstractions. Holding a registry after freeze does not allow further registration. Required missing lookup throws KeyNotFoundException naming the contract; optional missing lookup returns false and a null out value. Optional lookup must not swallow other errors.

Registry/resolver operations use the owning thread. Wrong-thread or invalid-phase access throws InvalidOperationException. After owner disposal, operations throw ObjectDisposedException. A caller must use the correct thread even when testing post-disposal behavior; no precedence is promised when multiple preconditions are violated. Resolved instances are borrowed, not caller-owned.

## Lifecycle and time

Each capability is independent. The host invokes enrolled participants synchronously on the owning thread in stable startup order, after registry freeze. An initializer must clean up its own partial initialization on failure. Stop happens in reverse successful startup order. A participant with no Initialize callback becomes started when its enrollment is reached; if it implements IStoppable it is then eligible for cleanup. This closes the stop-only lifecycle case without forcing every service to implement initialization.

Callbacks do not enforce or own the host state machine themselves. Host state, ownership enrollment, failure aggregation and rollback are specified in ARCHITECTURE.md and implemented by ApplicationHost; see LIFECYCLE_HOST.md. Stop ceases activity; IDisposable releases explicitly owned resources in a separate pass. An exception during stop must not prevent the host from attempting later cleanup. Hosts call stop/dispose at most once; implementations should tolerate repeated direct stop calls. Do not dispatch callbacks after host shutdown/disposal. Interfaces without IDisposable do not transfer ownership.

Tick parameters are finite, non-negative seconds; zero is valid. Update/LateUpdate receive scaled frame time and FixedUpdate receives scaled fixed-step time from FoundationBehaviour. The host validates time before invoking participants and rejects negative/NaN/infinite values with ArgumentOutOfRangeException. Only Running hosts tick. A tick failure faults the host and aborts subsequent dispatch. Stop/dispose requests during callbacks are deferred until return. No Task, CancellationToken, Unity Time reference or asynchronous lifecycle is introduced.

## Events and disposal

Dispatch uses the exact generic TEvent, not the payload's runtime type. Value and reference payloads are accepted; null reference/nullable payloads throw ArgumentNullException. Subscribe rejects null handlers. Each subscription is independent, including duplicate delegates; its owner disposes its token on shutdown. No weak subscription or hidden target destruction detection exists.

Publish synchronously visits a subscription-order snapshot. Subscribe/token disposal during a callback affects later publishes only. Reentrant publish throws InvalidOperationException. All current handlers are attempted; failures emerge as one AggregateException after delivery. Events are not queued or retained. Exception recovery belongs to the caller/host.

IEventBus is reserved for the owner; consumers normally receive IEventPublisher or IEventSubscriber, neither of which exposes disposal. All bus/token operations use the owning thread. Dispose is idempotent. Token disposal after bus disposal is harmless. Bus disposal during dispatch requests closure immediately (no new subscriptions), but releases handlers only after finishing the snapshot; new operations on a closed bus throw ObjectDisposedException. Existing handlers still finish. Disposal never disposes payloads or handler targets. If multiple preconditions fail, exception precedence is unspecified.

## Logging

Use IsEnabled before building an expensive message; arguments are already evaluated when Log is called. None is a threshold sentinel: IsEnabled(None) is false and Log(None, ...) throws ArgumentOutOfRangeException. Undefined enum values also throw. Category and message must be non-null, even for filtered messages; empty strings are allowed. Exception is optional. Configuration and minimum threshold are composition concerns and are not mutable consumer properties.

ILogger filters before invoking sinks. Valid sink failures must not escape ILogger.Log: attempt other sinks, record bounded failure accounting and never recursively log into the failing sink. Invalid caller arguments, thread/lifecycle violations still throw. ILogSink is a lower-level output boundary that may throw; isolation belongs to SinkLogger, not to the sink interface. No global logger, logging extension helpers, format abstraction or separate LogEntry allocation is introduced.

Logger and sink access is confined to the owning thread. Consumers borrow loggers/sinks. If implementations need disposal, composition enrolls IDisposable separately; these consumer interfaces do not expose it. Operations after disposal by the owner throw ObjectDisposedException. Stateless no-op implementations need not acquire an artificial disposal lifecycle.

## Module identity

IModule describes identity and required module IDs only. It is not an auto-installer or host. IDs are non-whitespace ordinal/case-sensitive strings and unique within the host. Dependencies are non-null with non-whitespace unique entries. Self-dependencies, missing IDs and cycles fail host validation before registration/initialization side effects. Topological ordering breaks ties by ordinal ID, not dependency-list enumeration order.

IReadOnlyList is a read-only view, not an immutability guarantee. Module authors keep metadata stable; the host takes a snapshot before graph validation. Metadata remains readable through shutdown. Phase 1.3 provides scoped ModuleContext for explicit registration and enrollment; see LIFECYCLE_HOST.md.

## Validation scope and implementation guides

API regression tests check capability segregation, generic constraints, event payload signatures, explicit time, read-only module metadata, logging parameters and public dependency boundaries. Existing package/compiler tests remain. Tests do not claim ordering, exception behavior, leak freedom or rollback correctness without implementations. No fake container/event bus is implemented merely to make behavioral tests pass.

Phase 1.2 implements the frozen registry and logging router in Core with failure-path tests; see CORE_SERVICES.md. Phase 1.3 adds the pure lifecycle/module host; see LIFECYCLE_HOST.md. Phase 1.4 implements EventBus; see EVENT_BUS.md. Phase 1.5 adds Unity bootstrap/adapters and console output; see UNITY_ADAPTERS.md. Phase 1.6 adds sample and Windows Mono player validation; see PHASE_1_6_REPORT.md. Optional integrations, external Git distribution and IL2CPP validation remain later work.

## Phase 1.1 validation record — 2026-10-09

Unity 6000.3.26f1 completed compilation through MCP; all four package DLLs were emitted. Edit Mode job 375d687bb1e94a3c9557c897b0c2797b passed 11/11 tests (7 API tests plus 4 existing package tests), with zero failures/skips. Console inspection after compilation returned no errors/warnings. Existing assets, project settings, host manifest/lock, asmdefs and all pre-existing .meta files matched their pre-task hashes. No dependency, commit, push or release was added.

Play Mode tests were not rerun for this interface-only increment; there are no new Unity adapters or lifecycle implementations. Previous Phase 1.0 results are historical evidence only. Player builds, IL2CPP and runtime behavior remain unvalidated. Package version stays 0.1.0-preview.1 and new changes are recorded under Unreleased.
