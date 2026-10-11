# Unity Foundation Roadmap

Status: proposed for review, 2026-10-09. No duration estimates are asserted before API review and implementation. TBase is the application and integration host; no separate test repository is planned.

## Phase 0 — Architecture review (completed; baseline retained)

Deliver the architecture, requirements, roadmap and agent instructions. Inspect the current repository and installed Editor, verify MCP through read-only queries, and document evidence and limits. Propose package boundaries, lifecycle contracts, optional modules and validation gates.

Exit satisfied by the explicit Phase 1.0 request. TBase remains the integration host. The final user selection is an embedded package in TBase, with package ID com.studio.foundation; Git submodule integration is deferred.

## Phase 1 — Minimal reliable foundation

Implement in reviewable increments, each with tests for its observable contracts:

1. **Phase 1.0 skeleton; Phase 1.1 contracts.** Establish package manifest, Abstractions/Core asmdefs, test asmdefs, stable metadata and documentation. Defer the Unity asmdef until adapters exist. Verify embedded-package import, real compilation and structural tests in TBase. Submodule integration is deferred by user choice. Phase 1.1 reviews and introduces minimal public contracts separately. Preserve existing project content; do not broadly stage the currently untracked Unity project.
2. **Logging and registry.** Implement the small injectable contracts, no-op/recording sinks, threshold handling, explicit instances and frozen resolver. Validate failure semantics and ownership before introducing bootstrap orchestration.
3. **Lifecycle and module graph.** Implement state machine, graph validation, deterministic order, tick lists, owned resources and rollback. Test partial startup, cleanup failures and reentrancy with no Unity scene dependency.
4. **Typed event bus.** Implement subscription tokens, snapshots, exact-type dispatch, handler error aggregation and disposal. Test all mutation and failure cases before use in a sample.
5. **Unity bootstrap adapter.** Add explicit host wiring and Unity console sink, then an opt-in minimal sample. Verify scene transitions, duplicate hosts and reload matrix on 6000.3.26f1. No automatic edits to the application's existing scenes.
6. **Hardening and preview.** Validate in TBase and a clean temporary fixture, perform supported player smoke tests, measure steady-state behavior, review the public API and publish consumption instructions. Prepare 0.1.0-preview.1 only after gates pass and release is authorized.

Phase 1 acceptance requires all applicable REQUIREMENTS.md criteria, no unresolved lifecycle/resource leaks, passing Edit/Play Mode tests, demonstrated package independence, a supported Windows player build and documented unsupported matrix entries. If a release advertises Git submodule consumption, it must be tested against the real extracted package repository before that release. Do not call a preview production-ready because it compiles.

## Phase 2 — Distribution and operational validation

Phase 1.0 establishes an embedded package in TBase. Extraction to a dedicated package repository and Git submodule integration are deferred to this phase. Phase 2 selects license and shared hosting, preserves GUIDs, validates portable submodule and UPM Git installation in fresh consumers, and arranges licensed CI. Add API-change review and release automation only after the manual release process is proven. Keep TBase as the integration consumer and record the package revision it uses.

Phase 2 may be pulled forward when external consumption is needed for the first preview. There is no requirement to create another Unity test repository; temporary validation fixtures can be generated in isolated directories.

## Phase 3 — Demand-driven optional modules

Select modules from actual application needs. Each proposal states its package/asmdef boundaries, external dependencies, compatibility range, lifecycle/resource ownership, tests and removal behavior. The base package must remain buildable without the module. Third-party dependencies require approval. Async lifecycle, scene scopes and threaded events require separate behavioral designs before expanding the Phase 1 contracts.

## Phase 4 — Stable 1.0 release

Require sustained integration use, complete failure-path tests, documented platform/backend support, migration guidance, reproducible distribution and CI validation, license and API review. Publish an immutable version with matching package metadata and changelog. Future releases follow SemVer and explicit deprecation policy.

## Historical evidence from Phase 0 (before skeleton implementation)

- Inspected TBase files, Git origin/status, project version, manifest/lock and installed Editor modules.
- Verified MCP routing, live project version/state and warning/error console read without Editor mutations.
- Only the four Markdown documents are intended changes. No runtime, package scaffold, new dependency, scene/settings edit, test run, build or release.
- Current Git repository has no commits and its pre-existing files are untracked; no baseline build/test result exists. Later verification must distinguish pre-existing issues from framework regressions.

## Risks to resolve during review and delivery

| Risk | Planned response |
| --- | --- |
| Framework becomes coupled to TBase | No reverse assembly references; validate in clean temporary consumer |
| Overengineering before a real use case | Five small capabilities; no generic DI container or speculative modules |
| Hidden global state survives Play Mode | Explicit ownership, limited static guard, reload matrix |
| Cleanup failure masks startup fault | Reverse cleanup, continue after errors, retain original failure |
| Optional modules become mandatory | Independent package dependencies and absence tests |
| Unverified IL2CPP/mobile claims | Platform-specific smoke tests before advertising support |
| Embedded folder/submodule migration loses files | Separate reviewed migration; preserve package paths, history and GUIDs |

Phase 1.1 is authorized: introduce public contracts and focused API tests. After this increment, implement logging and the frozen registry in Core with behavioral tests as a separately authorized task.

## Phase 1.0 result — option 2

Embedded package created and compiled on Unity 6000.3.26f1. MCP structural tests passed: 4 Edit Mode and 2 Play Mode. Git submodules explicitly deferred. No runtime systems implemented. See Packages/com.studio.foundation/Documentation~/PHASE_1_0_REPORT.md in TBase, or Documentation~/PHASE_1_0_REPORT.md from the package root, for evidence and limits.

## Phase 1.1 result — public contracts

Added 13 interfaces and LogLevel across Services, Lifecycle, Events, Logging and Modules in Abstractions. XML documentation and Documentation~/PUBLIC_API.md specify thread affinity, ownership, exceptions, null handling and disposal. Unity 6000.3.26f1 compilation passed; MCP Edit Mode tests passed 11/11 (7 API tests plus 4 package tests). No concrete runtime systems were added; Core still contains assembly metadata only. Play Mode was not rerun for this interface-only change. Next increment: frozen registry and logging implementation with behavioral tests, after user authorization.

## Phase 1.2 result — registry and logging

Implemented ServiceRegistryBuilder with separated borrowed capabilities and single-use Build; disposal releases registrations without owning services. Implemented NullLogger and SinkLogger with threshold filtering, borrowed sink snapshots, bounded failure counters, reentrancy suppression and safe disposal during dispatch. Unity 6000.3.26f1 compile and all 37 Edit Mode tests passed (26 new behavioral cases, 11 existing). See Documentation~/CORE_SERVICES.md from the package root for usage and evidence. Unity console adapter, Bootstrap, lifecycle host, module execution and Event Bus remain pending. Next: lifecycle host/module ordering and explicit owned-resource cleanup, only after authorization.

## Phase 1.3 result — pure lifecycle/module host

Added ApplicationHost, ApplicationState and scoped ModuleContext in Core. Graph validation precedes configuration; registry freezes before initialization; cleanup reverses successful participant startup then owned-resource enrollment. Faults preserve the original error and collect cleanup errors. Tick dispatch rejects invalid time/reentrancy and defers Stop/Dispose until callback return. Compilation on 6000.3.26f1 passed; all 73 Edit Mode tests passed (36 new cases), including zero measured managed allocations for warmed empty/no-op host dispatch. See Documentation~/LIFECYCLE_HOST.md from the package root for limitations. Next: typed Event Bus implementation and behavior tests; Unity Bootstrap/adapters remain deferred.

## Phase 1.4 result — typed Event Bus

Implemented EventBus in Core using the existing contracts, ordered copy-on-write snapshots, independent tokens, aggregate handler failures and deferred handler release on in-flight disposal. Unity 6000.3.26f1 compilation completed; MCP Edit Mode job f1155821292c4b8db6417e2d6dc0a3d8 passed 96/96 tests (23 new cases). Warmed int publishes measured 0 managed bytes across 1,000 publishes at 0/1/10/100 listeners in Editor. One MCP WebSocket transport warning was observed; no C# compilation errors. See Documentation~/EVENT_BUS.md for semantics and validation limits. Next authorized increment should add Unity Bootstrap/adapters and Unity console sink with Play Mode lifecycle/reload tests. No adapter, player build, release or Git distribution is included in Phase 1.4.

## Phase 1.5 result — Unity adapters (2026-10-10)

Added Studio.Foundation.Unity with FoundationBehaviour and UnityConsoleSink. Explicit startup, duplicate guard, tick dispatch, persistent roots, teardown and callback/reentrant shutdown are implemented. Final MCP Edit Mode job ab48db2c6780459b9c4f60cb61c3b841: 105 passed, 0 failed, 1 intentionally skipped batch-only reload test. Final MCP Play Mode job 6e0f395813cc4464a570df79d3c631d8: 15/15 passed. The isolated 6000.3.26f1 CLI fixture passed the separate reload matrix test with 12 enter/exit cycles (three per domain/scene reload combination) and no URP/Input/MCP dependency. This matrix uses explicit runtime-created hosts each session; retained scene-authored composition is not yet verified. See Documentation~/UNITY_ADAPTERS.md for use and limitations.

The fixture emitted AudioListener warnings because its minimal manifest omitted the audio built-in module; test assertions passed. TBase emitted an MCP WebSocket reconnect warning, with no observed compiler errors. Console sink and tick-failure tests intentionally emit expected logs/errors verified by LogAssert. EditorSettings was restored byte-for-byte to the pre-test backup. Pre-existing metadata and host manifest/lock are unchanged. A hash discrepancy in Assets/Settings/Mobile_RPAsset.asset was detected during the increment; its source is unconfirmed and no prior content backup exists, so it was preserved instead of overwritten. All remaining existing assets/settings matched the baseline. This prevents claiming the whole host workspace is unchanged.

Next increment: an opt-in functional sample, retained scene-host reload validation, clean consumer/player smoke tests and release hardening. No player build, sample installation, submodule migration, version bump, commit, push or release has been performed in Phase 1.5.

## Phase 1.6 result — opt-in sample and Windows Mono smoke (2026-10-10)

Added Minimal Bootstrap under Samples~ with UPM sample metadata, explicit scene composition and fresh session counters. Added isolated Validation~ build/smoke/reload tools. A fresh consumer with only foundation, existing test tooling and Unity built-ins passed all 12 scene-authored reload cycles, including retained component identity with scene reload disabled. Windows x64 Mono build succeeded (0 BuildReport errors/warnings); the actual player exited 0 and wrote PASS for startup, ticks/events, registry identity, duplicate rejection, cleanup/invalidation and replacement. Ordinary player output contains no NUnit or test DLLs. TBase's focused UPM recognition test passed 1/1. All pre-existing protected files matched the increment's baseline. See Documentation~/PHASE_1_6_REPORT.md for exact evidence and limits.

IL2CPP is not validated: the installed Windows player variations contain Mono only. No additional toolchain was installed. The preview is not released or production-ready. Next: public API/release checklist review, owner-selected license and authorized distribution/CI work. A real Git remote/submodule validation and any additional backend claims must be completed before advertising that support.

## API and release-readiness review (2026-10-10)

Reviewed 23 public runtime types across Abstractions/Core/Unity. No runtime API redesign or signature change proposed; documented explicit ownership, composition capability and thread/lifecycle trade-offs. Corrected stale API-guide wording and relabeled the skeleton changelog entry as unreleased. Added Documentation~/API_REVIEW.md, RELEASE_CHECKLIST.md and PREVIEW_RELEASE_NOTES_DRAFT.md. Documentation links and runtime type inventory were checked; no tests/builds were rerun for this documentation-only change.

Publication is not ready: license/scope and consumption route require owner decisions; the local repository remains unborn with untracked files and no immutable candidate. Existing origin is the TBase project remote, not an extracted package repository. Review ignore/source selection before any explicitly authorized commit. Remote consumption, candidate-bound verification and publication authorization remain open. Existing Windows Mono evidence is retained; IL2CPP/CI are not claimed. Next action is to choose license/distribution scope and prepare a reviewed candidate, not to add speculative runtime systems.

## Public distribution decision (2026-10-10)

Owner selected public distribution. Prepared root Unity ignore rules and contributor guides; retained the embedded TBase package and deferred submodules. Proposed UPM Git consumption from the package subdirectory at a published immutable revision; no remote installation is claimed. Read-only GitHub metadata confirmed the existing TBase repository is public, but no main ref was returned and no local candidate commit exists. MIT was suggested in response to the owner's question; license and copyright-holder selection remain pending. See Documentation~/PUBLIC_DISTRIBUTION.md. Nothing was staged, committed, pushed, tagged or published. Next: license/holder decision and review of source selected for a public candidate.

## MIT license applied (2026-10-10)

Owner explicitly selected MIT and copyright holder Towis. Added package LICENSE.md with Copyright (c) 2026 Towis, stable import metadata and package.json license=MIT. Updated current release/contributor guidance and added root LICENSING.md to describe package-only scope. Earlier phase records retain their historical licensing status. No unrelated Unity assets or third-party material were relicensed; no runtime API, dependency, version, commit, push or publication changed. Next: review source selection and prepare an explicitly authorized candidate commit, then validate remote consumption.

## Initial commit preparation (2026-10-11)

Prepared a framework-only initial commit proposal containing the package at its existing path plus root docs/ignore rules. Added a root landing README that clearly states a clone is not yet a complete TBase Unity host. Application Assets, ProjectSettings and host manifest/lock are excluded from the proposal and remain untouched/untracked. The package's MIT license is retained; the historical renderer-asset discrepancy is not overwritten. See Documentation~/INITIAL_COMMIT_REVIEW.md for the exact scope and staging rules. No stage, commit, push, tag, source migration or new test/build run is performed in this preparation.
