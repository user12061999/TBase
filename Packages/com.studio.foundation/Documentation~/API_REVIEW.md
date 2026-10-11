# Public API review — 2026-10-10

Scope: source review of the current embedded package through Phase 1.6. This is a proposed preview baseline, not owner approval of an API freeze or proof of production readiness. No runtime signatures were changed in this review.

## Declared public surface

There are 23 top-level public types: 14 in Abstractions, 7 in Core and 2 in Unity. The table lists framework-declared members; inherited Object/MonoBehaviour members are not a new foundation API. Sample and Validation~ types are examples/tooling, excluded from this baseline.

| Assembly / namespace suffix | Type | Declared API |
| --- | --- | --- |
| Abstractions / Services | IServiceRegistry | void Register<TService>(TService), where TService : class |
| Abstractions / Services | IServiceResolver | TService GetRequired<TService>(); bool TryGet<TService>(out TService); both constrained to class |
| Abstractions / Lifecycle | IInitializable | void Initialize() |
| Abstractions / Lifecycle | IStoppable | void Stop() |
| Abstractions / Lifecycle | IUpdatable | void Update(float deltaTime) |
| Abstractions / Lifecycle | IFixedUpdatable | void FixedUpdate(float fixedDeltaTime) |
| Abstractions / Lifecycle | ILateUpdatable | void LateUpdate(float deltaTime) |
| Abstractions / Events | IEventPublisher | void Publish<TEvent>(TEvent) |
| Abstractions / Events | IEventSubscriber | IDisposable Subscribe<TEvent>(Action<TEvent>) |
| Abstractions / Events | IEventBus | IEventPublisher, IEventSubscriber, IDisposable |
| Abstractions / Logging | ILogger | bool IsEnabled(LogLevel); void Log(LogLevel, string category, string message, Exception exception = null) |
| Abstractions / Logging | ILogSink | void Write(LogLevel, string category, string message, Exception exception) |
| Abstractions / Logging | LogLevel | Trace=0, Debug=1, Information=2, Warning=3, Error=4, Critical=5, None=6 |
| Abstractions / Modules | IModule | string Id { get; }; IReadOnlyList<string> Dependencies { get; } |
| Core / Services | ServiceRegistryBuilder | public parameterless constructor; Registry { get; }; Build(): IServiceResolver; Dispose() |
| Core / Logging | NullLogger | implicit public parameterless constructor; ILogger methods |
| Core / Logging | SinkLogger | constructor(LogLevel minimumLevel, params ILogSink[] sinks); ILogger methods; SinkFailureCount and SuppressedReentrantLogCount getters; Dispose() |
| Core / Events | EventBus | implicit public parameterless constructor; IEventBus methods |
| Core / Lifecycle | ApplicationHost | implicit public parameterless constructor; State and Services getters; AddModule(IModule, Action<ModuleContext> configure = null); Own(IDisposable); Start(); Update(float); FixedUpdate(float); LateUpdate(float); Stop(); Dispose() |
| Core / Lifecycle | ApplicationState | Created=0, Starting=1, Running=2, Stopping=3, Stopped=4, Faulted=5 |
| Core / Lifecycle | ModuleContext | Registry getter; Enroll(object); Own(IDisposable); no public constructor |
| Unity / Unity | FoundationBehaviour | State: ApplicationState? and Services getters; Initialize(Action<ApplicationHost>, bool persistAcrossScenes = true); Shutdown(); created via Unity component API |
| Unity / Unity | UnityConsoleSink | implicit public parameterless constructor; ILogSink.Write |

All concrete runtime classes are sealed. Internal graph, identity, validation and thread helpers remain internal/private. Core references Abstractions only; Unity references Core/Abstractions; no reverse sample/game/test/optional-module dependency was found in the declared runtime asmdefs. Compiled/player boundary evidence is recorded in [Phase 1.6](PHASE_1_6_REPORT.md), not newly rerun here.

## Contract review and decisions

| Area | Assessment and proposed decision |
| --- | --- |
| Registry | Keep exact generic contract keys and separate registration/resolution views. No implicit replacement, assignability lookup or ownership transfer. |
| Lifecycle | Keep synchronous, single-use host and explicit enrollment. Graph validation precedes configuration. Stop reverses successfully started participants, then owned-resource disposal reverses enrollment. |
| Failure policy | Preserve original startup/tick fault and collect cleanup failures. Normal cleanup errors leave Stopped; startup/tick errors leave Faulted. No blanket swallowing of application faults. |
| Events | Keep exact generic type, independent tokens, ordered copy-on-write snapshots and aggregated handler errors. Same-bus reentrancy remains rejected. |
| Logging | Keep immutable thresholds, borrowed sinks, bounded counters and failure isolation. No global logger or implicit sink ownership. |
| Unity lifecycle | Keep explicit Initialize, one active application slot and one startup attempt per component/session. Disable ends ownership; enable does not resume. Session reset exposes no global resolver. |
| Optionality | Logging/events are opt-in at composition time within Core, not separately installable packages. Significant future integrations must remain separate packages. |

No public API redesign is proposed from this bounded review. This conclusion is not a guarantee that no defects remain.

## Trade-offs consumers must understand

- ModuleContext.Enroll(object) provides runtime validation of lifecycle capabilities rather than a marker interface. It supports independent interfaces without forcing one common base contract.
- FoundationBehaviour.Initialize exposes ApplicationHost to composition code, which could retain it or call Start/ticks incorrectly. The documented composition-only usage is a convention, not a capability-enforced sandbox. Keep it for this preview; a narrower builder would be an intentional future API change requiring migration review.
- UnityConsoleSink checks the creating thread on calls; it does not prove that construction occurred on Unity's main thread. Construct it in main-thread composition. It also renders exception text in one severity-mapped entry rather than a separate LogType.Exception entry.
- EventBus attempts each subscribed delegate. A multicast Action is one delegate with normal .NET exception behavior. Subscription changes allocate, and empty type buckets live until bus disposal.
- UnityEngine.Object destroyed-object semantics are not managed by the pure registry/event bus. Register plain service objects or manage Unity object lifetime explicitly; a CLR reference is not a liveness guarantee.
- Public signatures are only part of compatibility. Enum numeric values, defaults, exact-type lookup, callback ordering, owning-thread requirements and disposal/exception behavior must be reviewed alongside signatures.

## Documentation corrections

Updated PUBLIC_API.md to describe implemented systems and distinguish historical validation records from current status. Renamed the skeleton changelog heading as unreleased: the manifest's preview version was assigned during development but no release/tag exists. Updated the documentation index. These are documentation changes only; no migration is needed.

Before publication, review this baseline against the exact candidate commit. Existing interface-only API tests do not constitute a complete exported-member compatibility snapshot. Add a compiled API comparison when maintaining a released baseline; do not claim it already exists.
