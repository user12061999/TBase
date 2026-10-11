# Unity Foundation Requirements

Status: proposed for review, 2026-10-09. Architecture: [ARCHITECTURE.md](ARCHITECTURE.md). Delivery gates: [ROADMAP.md](ROADMAP.md).

## Context and constraints

TBase is the main Unity project and integration host, per user clarification. There is no requirement to create a separate unity-foundation-test repository. Framework code must be a portable Unity package, independent of game assets, project settings, scenes and application assemblies. The portable package can be extracted for Git submodule consumption later; the user explicitly deferred that integration by choosing option 2.

Phase 1.0 authorizes the embedded package skeleton and structural validation tests in TBase. The user selected option 2 and explicitly deferred Git submodule integration. Do not implement other runtime systems, modify unrelated Unity assets/settings, install dependencies, create remote repositories or publish releases. Existing Unity Test Framework 1.6.0 is available; adding a third-party dependency requires approval. Phase 1.5 Unity adapters and console sink are now authorized following completed Core/Event Bus increments.

## Phase 1 functional acceptance criteria

| ID | Requirement | Evidence required before acceptance |
| --- | --- | --- |
| PKG-01 | Installable com.studio.foundation package with metadata, explicit asmdefs, usage docs and stable asset GUIDs | Clean package import in Unity 6000.3.26f1 |
| PKG-02 | No application, renderer, input-system, MCP or optional-module dependency in base runtime | Reference/dependency audit and clean fixture import |
| PKG-03 | Portable package root supports a pinned Git submodule without copying source into Assets | Fresh consumer checkout, recursive submodule initialization, successful import |
| REG-01 | Exact-contract, explicit instance registration; reject null and duplicate contracts | Edit Mode success and failure tests |
| REG-02 | Freeze before resolution; required missing lookup fails; optional lookup returns false | Registry boundary tests; no mutable public runtime registry |
| REG-03 | Registry does not dispose services; host owns only explicitly enrolled unique instances | Aliased-service, borrowed-resource and double-disposal tests |
| BOOT-01 | Explicit composition, validate dependency graph before side effects, freeze then initialize | Cycle, missing-module, duplicate-ID and deterministic-order tests |
| BOOT-02 | One application host, no automatic initialization on package installation | Import-only smoke and duplicate-host Play Mode tests |
| LIFE-01 | Deterministic initialize, tick and reverse stop; separate lifecycle interfaces | Order assertions across update/fixed/late callbacks |
| LIFE-02 | Startup failure rolls back successful participants, disposes owned resources, preserves original error | Failure injection at every startup stage |
| LIFE-03 | Cleanup continues after errors; stop/dispose idempotent; invalid transitions explicit | State-machine, reentrant transition and teardown tests |
| LIFE-04 | Tick failure faults host and prevents future dispatch; stop during callback is deferred safely | Deterministic tick failure and callback teardown tests |
| LIFE-05 | No stale host/services/subscriptions across scene changes or repeated Play Mode sessions | Reload configuration matrix and scene transition integration tests |
| EVT-01 | Exact-type synchronous subscription-order delivery with independent disposable tokens | Ordering, duplicate delegate, type-isolation and disposal tests |
| EVT-02 | Mutation uses current-publish snapshot; reentrant publish rejected | Subscribe/unsubscribe/dispose-during-publish tests |
| EVT-03 | Continue handlers on errors then aggregate failures; bus disposal releases subscriptions | Exception, disposal and reference-retention tests |
| LOG-01 | Injectable logger, no-op logger, thresholds/categories/exception context and Unity console sink | Recording-sink tests and expected Unity console output |
| LOG-02 | Sink failures isolated, bounded failure accounting, no recursive logging | Throwing-sink and multiple-sink tests |
| MOD-01 | Explicit module contracts and acyclic dependencies; base works without future modules | Core-only composition and representative custom-module fixture |

## Quality requirements

1. Compatibility: validate 6000.3.26f1 on Windows first. Installation alone does not establish compatibility. Other Editors/platforms/backends remain unverified until tested and recorded.
2. Portability: Abstractions and Core compile with no Unity engine references; no game-specific classes, serialized assets, scene names or hard-coded machine paths.
3. Predictability: public APIs document thread affinity, ownership, ordering, failure and post-disposal semantics. All mutable runtime operations are confined to one owning thread.
4. AOT readiness: no reflection scanning, dynamic code generation or runtime assembly loading. A successful IL2CPP player smoke test is still required before advertising IL2CPP support.
5. Resource safety: shutdown attempts every owned cleanup operation once; repeated lifecycle sessions must not retain listeners, services or Unity host objects.
6. Performance: no LINQ, reflection or rebuilding tick lists in steady-state host dispatch. Measure warmed-up empty host ticks for zero managed allocation attributable to the framework. Record event subscription/publish allocations and scaling at 0, 1, 10 and 100 listeners; no unmeasured zero-allocation promise or invented latency budget.
7. Maintainability: small public surface, explicit assembly references, acyclic dependency graph, stable metadata, XML API docs and focused behavior tests. No mandatory DI/logging/event library.
8. Release quality: SemVer, immutable tags, changelog, migration notes, owner-selected license and a tested consumption guide. Preview status must be clear; production readiness is an exit gate, not an initial claim.

## Testing strategy

Package Edit Mode tests own deterministic registry, lifecycle, module ordering, event and logging behavior. Use plain C# fakes and NUnit supplied by the existing Unity Test Framework. Do not install an additional mocking or performance library. Separate runtime test assemblies exercise the Unity host in Play Mode. TBase integration tests verify application wiring; reusable package tests must not reference those fixtures.

For non-embedded package consumption, the host manifest explicitly lists com.studio.foundation under testables so packaged tests are discoverable. Configure Editor tests as Editor-only and runtime tests as test assemblies; verify an ordinary player excludes all test code and NUnit. Sample code is imported only on demand. Do not place runtime sample source into an automatically compiled game folder during Phase 1.0.

| Layer | Required cases / gate |
| --- | --- |
| Static/package audit | Valid manifest, unique asmdefs, permitted references only, stable metas, runtime free of UnityEditor/tests/game references |
| Edit Mode | Every functional requirement above, negative paths and failure cleanup; deterministic repeated execution |
| Play Mode | Explicit startup, duplicate host, ticks, persistent host across scenes, destruction/quit cleanup, startup failure |
| Editor reload matrix | Domain reload on/off crossed with scene reload on/off; at least three enter/exit cycles per configuration; restore original settings |
| Player | Windows standalone compile and boot/stop smoke; Mono and IL2CPP when toolchains are available; record unavailable backends as blockers to that support claim |
| Portability | Temporary clean Unity fixture with only foundation plus needed test tooling; import sample, compile, run tests, no URP/Input/MCP assumptions |
| Distribution | Local/embedded import plus pinned submodule checkout; direct UPM Git install once a real package remote exists |
| Performance | Warm-up before measuring, separate harness allocations, record Editor/player and backend; inspect retained objects after repeated cleanup |

Future CI uses the exact supported Editor and isolated working directories, preserves XML test reports and Editor/build logs, checks actual failing-test counts and build status, and fails on new compiler errors or framework test failures. Do not run batchmode against the project directory already open in the Editor. CI licensing/runner configuration is still to be arranged; no CI execution has happened in this task.

## Explicitly out of Phase 1

Async startup, cancellation orchestration, dependency injection containers, reflection auto-registration, scene scopes, hot swapping, event queues/replay/threaded dispatch, save systems, networking, asset loading, pooling, UI/audio/input frameworks, analytics, file/remote log sinks, ECS/DOTS, custom Editor dashboards and third-party integrations. Optional packages are an architectural extension point, not a request to implement these features.

## Review decisions

The user selected com.studio.foundation with Studio.Foundation namespaces, embedded in TBase/Packages/com.studio.foundation. TBase remains the source repository; submodules are deferred. Phase 1.0 has two runtime assemblies (Abstractions and Core); the Unity adapter is deferred until it has code. Preserve synchronous main-thread Phase 1, strict duplicate registration, event failure aggregation and lifecycle fault-and-cleanup policy. The package license is MIT, Copyright (c) 2026 Towis. Before public distribution, validate the chosen remote consumption route, confirm target platforms/backends and approve the public API.

## Phase 1.0 gate

Validate package discovery/version, source-backed compilation of the two metadata-only runtime assemblies, acyclic asmdef references, no UnityEngine/UnityEditor/game references in Abstractions/Core, exclusion of tests from ordinary player compilation, and execution of package Edit/Play Mode structural tests. Preserve the runtime test folder without adding speculative behavior tests. Git submodule integration is excluded from the current acceptance gate by the user-selected option 2; PKG-03 remains a future distribution requirement. The larger Phase 1 runtime and player-behavior criteria above remain pending.

## Phase 1.1 gate

Add only interfaces and a severity enum to Abstractions, with XML ownership/thread/failure documentation and a reviewable API guide. Verify compilation, capability segregation, reference-type service constraints, value/reference event signatures and package dependency boundaries. No implementation behavior is claimed by signature tests. Preserve all existing asset GUIDs, project settings and dependency versions. The complete runtime criteria above still require later implementations and behavioral tests.

## Phase 1.2 gate

Implement exact-contract frozen registry and pure logging against the existing interfaces. Validate null/duplicate/missing inputs, retained capability invalidation, borrowed ownership, independent scopes and owning-thread checks. Validate all log thresholds, constructor snapshotting, invalid filtered messages, sink exception isolation, reentrancy suppression and disposal during delivery. REG-01/REG-02 and registry-side REG-03 are in scope; host ownership and lifecycle cleanup remain pending. LOG-01 is partial until Unity console sink exists; LOG-02 is covered by Core tests. No third-party dependency or Unity adapter is introduced.

## Phase 1.3 gate

Validate graph errors before configuration, ordinal dependency ordering, frozen registry before initialization, stop-only participant eligibility, explicit identity-based ownership, reverse teardown, original-error preservation and complete cleanup despite failures. Exercise tick phases, invalid deltas, reentrant transitions, deferred Stop/Dispose and thread affinity. Measure warmed empty/nonempty tick dispatch allocations in Editor; this is not a player performance claim. BOOT-01 and pure-host LIFE-01 through LIFE-04 are in scope; Unity host uniqueness, scene/reload behavior and LIFE-05 remain deferred.

## Phase 1.4 gate

Implement EVT-01 through EVT-03 using the existing interfaces: exact-type ordered delivery, snapshot mutation, independent idempotent tokens, aggregate failures, rejected reentrancy and thread confinement. Verify handler release after token/bus disposal, including retained tokens, and closure during dispatch. Record warmed publish allocations at 0, 1, 10 and 100 listeners on the detected Editor. These measurements do not establish player or IL2CPP performance. Unity adapters and reload validation remain deferred.

## Phase 1.5 gate

Add Studio.Foundation.Unity with inward references only. Verify explicit startup, duplicate rejection before configuration, scaled/fixed tick forwarding, startup/tick failure cleanup, disable/destroy shutdown, persistence through scene unload, deferred callback shutdown and ownership-guard reentrancy. Verify severity/category/exception console mapping. Run the domain/scene reload combinations for three enter/exit cycles each in an isolated fixture; record the exact composition scenario and unsupported scenarios. Preserve TBase assets/settings and existing metadata. Player builds, mobile suspension/termination, distribution and release remain separate gates.

## Phase 1.6 gate

Expose an opt-in UPM sample without installing it into TBase. In a clean fixture, compile the imported sample, verify three sessions per domain/scene reload combination using a saved scene marker, confirm fresh lifecycle/event state and stable component identity when scene reload is disabled. Build a Windows standalone Mono player and execute explicit startup/tick/event/cleanup assertions; require both a successful build and player exit/result evidence. Verify ordinary player output excludes test assemblies/NUnit. Record IL2CPP toolchain limitations without installing dependencies. No production-readiness, distribution or release claim follows from this smoke test.
