# Unity adapters — Phase 1.5

Reference `Studio.Foundation.Unity`, `Studio.Foundation.Core` and the Abstractions assembly explicitly from the application's composition asmdef. Unity adapters depend inward; Core and Abstractions remain engine-independent. Merely installing the package creates no object and starts no application.

## Explicit composition

Call the following from application-owned startup code on the main thread. The application's entry point must run once every Play session. A RuntimeInitializeOnLoadMethod using AfterSceneLoad is suitable for an explicit application entry point even when domain/scene reload are disabled; Awake/Start alone should not be assumed to repeat with scene reload disabled.

```csharp
var root = new UnityEngine.GameObject("Application");
var adapter = root.AddComponent<Studio.Foundation.Unity.FoundationBehaviour>();
adapter.Initialize(host =>
{
    // host.AddModule(applicationModule, context => { ... });
    // Construct/inject services explicitly. Register capabilities and enroll lifecycle
    // participants in the module callback; Own disposable resources before fallible work.
});
```

The callback receives a borrowed ApplicationHost solely for pre-start composition. Do not retain it to drive ticks or call Start yourself; the adapter owns startup, dispatch and shutdown. Services become available after successful initialization. Consumers receive narrow interfaces by injection rather than searching the scene or accessing a static host.

`Initialize(configure, persistAcrossScenes: true)` defaults to persistence. It requires an active, enabled component on a root GameObject. Passing false permits scene-local roots or child objects. Duplicate application hosts fail before configuration. Each component gets one startup attempt per session, including failures; create a replacement after shutdown to restart in the same session. A duplicate rejection does not consume that component's attempt.

`Update` and `LateUpdate` receive Time.deltaTime; `FixedUpdate` receives Time.fixedDeltaTime. These are scaled Unity time values. Disable, destroy and application quit call shutdown. Disable ends ownership rather than pausing; re-enable does not restart. During a callback, shutdown waits until it returns and skips subsequent Core callbacks. Reentrant cleanup is rejected while retaining the duplicate guard. Scene-local hosts stop when destroyed on unload; persistent roots survive source-scene unload.

Explicit Initialize/Shutdown failures propagate to the caller. Core startup/tick failures preserve Faulted and clean their resources. Failure in the outer composition callback occurs before Core startup and closes the still-Created host to Stopped. Unity callback errors are logged with Debug.LogException after cleanup; later ticks stop. A failure in cleanup still attempts all owned cleanup and releases the application slot. The adapter never destroys unrelated GameObjects or owns borrowed services implicitly.

The only session-global mutable state is a private duplicate guard, main-thread identity and session token. SubsystemRegistration clears stale ownership for no-domain-reload sessions. There is no global service resolver or event bus. Runtime script hot reload, mobile suspension and forced process termination are not lifecycle guarantees.

## Console output

```csharp
var logger = new Studio.Foundation.Logging.SinkLogger(
    Studio.Foundation.Logging.LogLevel.Information,
    new Studio.Foundation.Unity.UnityConsoleSink());
// Enroll logger as an owned resource in composition if the host owns its lifetime.
```

Construct the sink on the Unity main thread. Calls are checked against that thread. Trace/Debug/Information map to Log, Warning to Warning, Error/Critical to Error. Each entry has `[category] message`, followed by the optional exception's full text. This keeps one entry per call, but does not use Unity's separate exception entry type. SinkLogger supplies thresholds and isolates sink failures; the sink alone validates arguments and forwards output. No file, remote or third-party logging dependency is introduced.

## Validation and limits

Play Mode tests cover inert installation, explicit startup, duplicate ownership, all tick phases, composition/startup/tick faults, disable/destroy, persistence, callback shutdown and cleanup reentrancy. The explicit ReloadMatrixTests test is batch-only and must run in a disposable consumer; it changes that fixture's Editor play options and restores them in teardown. Normal interactive test runs skip this test. It exercises three sessions per domain/scene reload combination with an explicitly created runtime host each session; Phase 1.6 additionally verifies retained scene-authored composition using the opt-in sample; see PHASE_1_6_REPORT.md.

No existing TBase scene is wired to the framework. Phase 1.6 adds the sample and Windows Mono smoke validation; see PHASE_1_6_REPORT.md. IL2CPP, mobile lifecycle and external Git distribution remain later validation work. This increment is not a production-readiness or release claim. See ROADMAP.md for executed results.
