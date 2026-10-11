# Preview release checklist

Reviewed 2026-10-10. Decision: **not ready to publish**. The local implementation has useful validation evidence, but there is no committed release candidate or validated remote consumption route. MIT licensing is now selected for the package, Copyright (c) 2026 Towis. Package version 0.1.0-preview.1 is development metadata, not a published release.

## Evidence already available

| Gate | Current evidence | Limit |
| --- | --- | --- |
| Runtime boundaries | Three runtime asmdefs; Abstractions/Core engine-independent; Unity depends inward | Source review plus prior compilation/player checks |
| Functional tests | Phase 1.5: 105 Edit Mode tests passed, 1 batch-only skip; 15 Play Mode tests passed | Historical runs, not one final committed candidate suite |
| Runtime-created reload | 12 cycles across four domain/scene reload combinations | Phase 1.5 isolated fixture |
| Scene-authored reload | 12 cycles with opt-in sample; retained component identity checked | Phase 1.6 isolated fixture |
| Independent consumer | Fresh project with package/sample and existing test tooling/built-ins | Source copy, not remote Git installation |
| Windows x64 Mono | Build succeeded; player exit 0 and explicit smoke PASS; test DLLs absent | Headless functional smoke only |
| UPM/sample metadata | Package recognized; sample path/source/metas validated | Package Manager UI sample import not exercised |
| Dependencies | Foundation manifest dependencies empty | Host's URP/Input/MCP are not foundation dependencies |

Detailed evidence and warning context: [Phase 1.6 report](PHASE_1_6_REPORT.md). Do not add historical test counts together or present them as one latest run.

## Required before any preview publication

- [ ] Owner reviews the [API baseline](API_REVIEW.md), usage constraints and intended support claim.
- [x] Owner selected public distribution on 2026-10-10.
- [x] Owner selected MIT, Copyright (c) 2026 Towis; package LICENSE.md and license metadata added. Unrelated host/third-party content is not relicensed.
- [ ] Choose a consumption route. Current choice remains embedded in TBase; submodule migration is deferred. A package-only repository requires separately authorized extraction. Keeping TBase requires validating installation from the package subdirectory at a real immutable revision. No route is currently verified remotely.
- [ ] Establish a reviewed source commit. The local repository is currently unborn and files are untracked, including generated Library/Temp/Logs/csproj files. Root ignore rules and a framework-only [initial commit proposal](INITIAL_COMMIT_REVIEW.md) are prepared; review them and stage only intended source; never broadly stage this workspace.
- [ ] Resolve or explicitly document the historical Mobile_RPAsset discrepancy before any whole-project commit. Do not overwrite it based on a hash-only backup. It is outside the package's runtime API.
- [ ] Select the exact package payload; exclude fixture outputs, credentials, generated build/cache files and unrelated project content. Preserve existing GUIDs and include sample metadata.
- [ ] Re-run applicable tests/build/smoke against the immutable candidate, retain XML/logs and link results to its commit. Include the independent fixture and reload tests; do not infer passing tests from compilation.
- [ ] Validate clean installation and sample consumption through the chosen route. If submodule support is advertised, demonstrate fresh recursive initialization against the real extracted repository.
- [ ] Consolidate unreleased changelog entries into one accurate preview release entry and align version/release notes with the chosen revision. Dates of development steps are not publication dates.
- [ ] Receive explicit authorization for tagging/publishing/pushing the concrete reviewed candidate. Create no tag or remote change before that authorization.

## Support claims and conditional gates

The proposed initial claim is Unity 6000.3.26f1 on Windows x64 Mono, limited to the recorded functional validation. Do not broaden it to other Unity versions, IL2CPP, Android/iOS, rendering correctness or production performance without evidence.

IL2CPP is an open conditional gate: current installed Windows player variations contain Mono only. Installing/configuring an additional backend/toolchain needs authorization, followed by its own build and executed smoke result. IL2CPP is not a reason to invent an all-platform support claim or a successful test.

Licensed CI is still unconfigured. Before stable 1.0, arrange reproducible CI, broader failure/compatibility coverage and sustained application use. A preview without CI must state that tests were run manually and identify the candidate and artifacts; it must not claim CI passed.

## Candidate verification order

1. Preserve the selected package MIT license and review source selection for public distribution.
2. Create the authorized candidate commit and record its identity.
3. Install that candidate in a fresh consumer; validate package discovery and sample import.
4. Run Edit/Play tests and isolated reload matrix with the exact Editor.
5. Build/run the supported player smoke; inspect ordinary player assembly output.
6. Review errors/warnings, metadata, changelog and API differences against the prior released baseline, if any.
7. Present candidate commit, payload, evidence, open limitations and draft release notes for the publication decision.

This checklist is a plan, not authorization to commit, migrate, install toolchains or publish. No mandatory third-party dependency is proposed.
