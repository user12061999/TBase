# Changelog

## Unreleased — Licensing

- Apply the owner-selected MIT license to the foundation package, Copyright (c) 2026 Towis.
- Add license metadata and clarify scope; no runtime or version change.


All entries below describe unreleased development work. No version has been tagged or published.

## Unreleased — API and release review

- Document the 23-type runtime API baseline, compatibility trade-offs and release gates.
- Add draft preview notes; correct stale implementation status and the misleading skeleton release heading.
- No runtime signature, behavior, version or dependency change.


## Unreleased — Phase 1.6

- Add the opt-in Minimal Bootstrap sample with explicit module composition, lifecycle, typed events and console logging.
- Add isolated scene-authored reload matrix and Windows player smoke validation sources under Validation~.
- Preserve all Core/Unity APIs, package version and dependency declarations; no migration is required for existing consumers.


## Unreleased — Phase 1.5

- Add Studio.Foundation.Unity with explicit FoundationBehaviour initialization, duplicate guard, tick forwarding, persistence and teardown.
- Add UnityConsoleSink with severity/category/exception mapping.
- Add Play Mode lifecycle tests and an explicit batch-only reload matrix fixture.
- Consumers must explicitly reference the new Unity assembly. Existing Core contracts are unchanged.
- No package version bump, dependency installation, release or application scene edits.

## Unreleased — Phase 1.4

- Add thread-confined EventBus with exact-type ordered delivery and copy-on-write subscription snapshots.
- Aggregate handler errors; reject reentrant publish; safely finish in-flight dispatch during disposal.
- Add mutation, lifetime, thread, retention and allocation tests. No public contract changes or migration required.
- Existing package version is unchanged; Unity adapters remain deferred.

## Unreleased — Phase 1.3

- Add pure ApplicationHost, ApplicationState and scoped ModuleContext to Core.
- Validate/snapshot module dependencies before configuration; order startup deterministically.
- Add explicit participant/resource enrollment, frozen service access, reverse cleanup and fault rollback.
- Defer Stop/Dispose requests inside tick callbacks and reject reentrant transitions.
- Add failure-path, thread-affinity and warmed dispatch allocation tests; no Unity adapters or Event Bus implemented.

## Unreleased — Phase 1.2

- Implement ServiceRegistryBuilder with exact-contract registrations, single-use Build and separately scoped capabilities.
- Implement NullLogger and SinkLogger with argument/thread checks, immutable thresholds, borrowed sinks and bounded failure counters.
- Prevent recursive sink forwarding and finish the current sink snapshot on disposal.
- Add behavioral tests and Core usage documentation; no third-party dependency or Unity adapter added.

## Unreleased — Phase 1.1

- Add public service, lifecycle, event, logging and module metadata contracts in Abstractions.
- Separate consumer capabilities from registration and ownership; document thread, lifetime and failure contracts.
- Add API boundary tests and PUBLIC_API.md; clarify stop-only participants and typed-event null handling.
- Existing preview package version is unchanged; no release/tag or concrete runtime system is introduced.

## Unreleased — Phase 1.0 skeleton (2026-10-09)

- Establish independent com.studio.foundation UPM package skeleton.
- Add pure Abstractions/Core assembly boundaries and package provenance metadata.
- Add structural Edit Mode and Play Mode tests; reserve documented module, Editor and sample directories.
- Carry forward architecture with the explicitly requested package naming and submodule workflow.
- No runtime systems or public runtime API implemented.
