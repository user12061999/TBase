# Studio Foundation 0.1.0-preview.1 — draft

**Unreleased.** Commit, distribution URL and publication date are pending. The package uses [MIT](../LICENSE.md), Copyright (c) 2026 Towis. This document is not an installation announcement.

## Included

- Engine-independent contracts and Core for an exact-contract service registry, lifecycle/module ordering, typed event bus and logging.
- Explicit Unity host adapter with duplicate-host protection, tick forwarding, scene persistence and cleanup; Unity console sink.
- Optional Minimal Bootstrap sample demonstrating composition, service capabilities, lifecycle ownership and events.
- Package tests and isolated fixture tooling for reload and Windows player validation.

## Compatibility evidence

Local validation on Unity 6000.3.26f1 includes Edit/Play Mode tests, all four reload combinations, and an executed Windows x64 Mono headless smoke test. Exact results and limits are in [the validation report](PHASE_1_6_REPORT.md). These are development results; the final candidate must be checked separately before publication.

## Adoption notes

Consumers explicitly reference the relevant asmdefs. Installing the package does not create a host. Configure ownership deliberately: service registration alone never transfers disposal responsibility. APIs are synchronous and thread-confined. Each host is single-use; disabling the Unity adapter ends its scope rather than pausing it. Importing the sample requires explicitly placing its marker in a scene.

This is the first proposed preview; there is no released version to migrate from. Public interfaces and behavior may change during preview, with each change documented and reviewed. No stable 1.0 compatibility guarantee is claimed.

## Known limitations

Current development uses an embedded package in TBase. Remote UPM Git and submodule consumption are unvalidated. IL2CPP/mobile/other Unity versions are unverified. CI remains unconfigured; the MIT selection covers the foundation package only. No async lifecycle, threaded event dispatch, reflection-based DI or automatic application discovery is supplied by Core.

Publish only after completing the applicable [release checklist](RELEASE_CHECKLIST.md), replacing all pending release identity fields and receiving explicit authorization.
