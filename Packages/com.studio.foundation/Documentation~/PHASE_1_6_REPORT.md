# Phase 1.6 — sample and player validation

Date: 2026-10-10. Target: Unity 6000.3.26f1, Windows x64, Mono. No package release was created; version remains 0.1.0-preview.1.

## Delivered

- Opt-in UPM Minimal Bootstrap sample, its own inward-facing asmdef and stable source metadata. Import alone does not create a host; attach the sample marker to one scene root.
- Explicit sample composition connects the frozen registry, lifecycle participant, typed events and console logger. Logger/bus ownership is explicit; Stop releases the subscription before resource disposal.
- Reusable isolated validation sources under Validation~: scene-host reload test, Windows builder and executable smoke assertions. Unity ignores this folder during ordinary package import. See [validation instructions](../Validation~/README.md).
- Updated architecture, requirements, roadmap, agent scope, README and changelog. Existing runtime APIs/assemblies were not modified.

## Executed evidence

| Check | Result |
| --- | --- |
| Sample import simulation in fresh consumer | Copied sample source/metas to fixture Assets; compiled on 6000.3.26f1 |
| Scene-authored host reload matrix | 1 CLI integration test passed; 12/12 enter/exit cycles, three per domain/scene reload combination |
| Retained scene object | Component instance identity checked when scene reload is disabled; fresh session counters and cleanup checked |
| Windows standalone build | BuildReport Succeeded; 0 build errors, 0 build warnings; Unity process exit 0 |
| Mono executable smoke | Player process exit 0 and explicit PASS result file |
| Player behaviors | Startup, ticks, event delivery, exact service identity, duplicate rejection before composition, idempotent cleanup, event-bus invalidation, replacement host |
| Ordinary player assembly output | Foundation runtime, sample and fixture driver present; no NUnit, TestRunner, Foundation.Tests or validation Editor DLLs |
| TBase UPM recognition | MCP job 378bc108d66f40f5aec9f14aad4e6d43: 1/1 focused package-recognition test passed |
| Workspace preservation | Existing Assets, ProjectSettings, host manifest/lock, asmdefs and .meta hashes match the start of this increment; no deleted baseline files |

The fresh fixture used foundation plus the existing Test Framework version 1.6.0 and Unity built-in audio/imgui/jsonserialize modules. It did not use TBase application code, URP, Input System or MCP. No dependency was added to the foundation manifest or installed into TBase. Sample import was simulated by copying the sample into the isolated consumer; the Package Manager UI import button was not exercised.

The Mono build log included a Unity TLS/certificate network diagnostic, but the build report recorded no errors/warnings and the player passed. No certificate/security settings were changed. TBase still had historical MCP reconnect and expected test-log entries; logs were not cleared.

## Evidence artifacts

The local task artifact directory contains phase16-fixture with scene-reload.xml/log, build-mono.log, Builds/Mono/build-result.txt, player-mono.log and player-mono-result.txt. The latter records:

```text
PASS: startup, ticks, exact service identity, events, duplicate guard, cleanup, bus invalidation, replacement
```

Generated fixture projects, logs and executables are not package source and were not added to TBase.

## Limits and next gate

Only Windows x64 Mono headless functional behavior was built and executed. Installed Windows player variations contain Mono only; IL2CPP was not built and remains blocked on installation/configuration of the appropriate backend/toolchain, requiring separate authorization. No rendering, performance, mobile, crash/forced-termination or other-platform guarantee is implied.

Earlier Phase 1.5 detected a Mobile_RPAsset hash discrepancy of unknown origin. This increment preserved that file exactly as found; it does not resolve or attribute the historical discrepancy.

Git submodule/UPM Git distribution, owner-selected license, API/release review and CI remain outstanding. No commit, push, tag, publication or production-ready claim. Next: review public API and release checklist, then address authorized distribution/toolchain gaps before publishing a preview.
