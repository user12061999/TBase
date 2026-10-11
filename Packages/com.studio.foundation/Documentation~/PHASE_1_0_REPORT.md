# Phase 1.0 — Validation Report

Date: 2026-10-09 (Asia/Bangkok). Result: **complete for the user-selected option 2 scope**. Git submodule integration was explicitly deferred; the original submodule-inclusive definition of done has not been satisfied or claimed.

## 1. Architecture review

The baseline dependency graph is acyclic. Preserve pure Abstractions/Core boundaries, explicit composition and ownership, and future inward-facing Unity adapters. Rename Contracts to the requested Abstractions folder. Avoid a mandatory umbrella Modules assembly or empty Editor/Unity adapter assemblies. No runtime systems or public runtime contracts were introduced.

The review clarified future lifecycle semantics: stop before start prevents later start and disposes owned resources; registration failure disposes resources already enrolled; ordinary cleanup errors finish Stopped and are reported; startup/tick failures finish Faulted; empty module IDs and self-dependencies are invalid.

## 2. Documentation changes

ARCHITECTURE.md, REQUIREMENTS.md, ROADMAP.md and AGENTS.md now use com.studio.foundation / Studio.Foundation, authorize only the skeleton and structural tests, and record the user's embedded-package selection. Package-root copies accompany the portable package; TBase root copies remain synchronized review snapshots. README and CHANGELOG describe actual skeleton functionality and deferred distribution work.

## 3. Created structure

```text
Packages/com.studio.foundation/
  package.json
  README.md
  CHANGELOG.md
  AGENTS.md
  ARCHITECTURE.md
  REQUIREMENTS.md
  ROADMAP.md
  Runtime/
    Abstractions/  (asmdef + AssemblyInfo.cs)
    Core/          (asmdef + AssemblyInfo.cs)
    Modules/       (README only)
  Editor/          (README only)
  Tests/
    Editor/        (asmdef + PackageStructureTests.cs)
    Runtime/       (asmdef + AssemblyBoundaryTests.cs)
  Samples~/        (README only; no advertised sample)
  Documentation~/ (README + this report)
```

Importable files/folders have 25 unique valid .meta GUIDs. Existing GUIDs were not replaced. Runtime AssemblyInfo.cs files contain only assembly-level package provenance attributes, providing real compilation units without placeholder classes. No source was copied to Assets. The temporary sibling draft directory was moved into the selected package location; it was never a Git repository.

## 4. Assembly definitions

| Assembly | Direct references | Boundary |
| --- | --- | --- |
| Studio.Foundation.Abstractions | None | noEngineReferences=true |
| Studio.Foundation.Core | Studio.Foundation.Abstractions | noEngineReferences=true |
| Studio.Foundation.Tests.Editor | Both runtime assemblies, UnityEngine.TestRunner, UnityEditor.TestRunner | Editor only; UNITY_INCLUDE_TESTS |
| Studio.Foundation.Tests.Runtime | Both runtime assemblies, UnityEngine.TestRunner | UNITY_INCLUDE_TESTS; no UnityEditor reference |

All four have autoReferenced=false. Runtime asmdefs exclude automatic precompiled third-party references. Test asmdefs explicitly reference the existing nunit.framework.dll. Ordinary player compilation assembly discovery excludes both test assemblies; this was tested, but no player build was run.

## 5. Package metadata

- Name: com.studio.foundation
- Display name: Studio Foundation
- Version: 0.1.0-preview.1
- Minimum declared Unity: 6000.3, release 26f1
- Validated Editor: 6000.3.26f1 (de8167e7440c)
- Mandatory package dependencies: none
- Status: development skeleton, not a published or production-ready release

## 6. Git integration

TBase is both source repository and current Unity integration host. The package is embedded source, not a submodule. No .gitmodules, gitlink, separate repository, commit, push or tag was created. The source repository was initially unborn main with existing untracked project files; these remain user-owned work. Submodule distribution and independent-consumer validation are deferred by explicit user choice.

Unity automatically added only com.studio.foundation to Packages/packages-lock.json with source=embedded, version=file:com.studio.foundation, dependencies={}. Packages/manifest.json was unchanged. No third-party dependency was installed.

## 7. Unity MCP validation

Routing was explicitly selected to TBase@f0e8eacf80b80320; live project root/version matched the workspace. Custom-tool discovery, project info, refresh/compile, Editor readiness and console queries succeeded. Compile start/finish was observed and all four DLLs were emitted in Library/ScriptAssemblies.

| Executed suite | Passed | Failed | Skipped | MCP job ID |
| --- | --- | --- | --- | --- |
| EditMode: Studio.Foundation.Tests.Editor | 4 | 0 | 0 | f765e17addba456d8f5d434228b87e45 |
| PlayMode: Studio.Foundation.Tests.Runtime | 2 | 0 | 0 | 6857c3e1b6e74131bfb9b89a2c4b35a4 |

Edit Mode checked UPM identity/version/dependencies, exact acyclic runtime asmdef references, actual compiled outputs and absence of engine compiler references, and test exclusion from ordinary player compilation. Play Mode loaded each runtime assembly and checked package provenance plus absence of engine/editor/game assembly references. MCP's runner temporarily disabled domain reload for that run. Its persisted EditorSettings change was detected by the final hash audit; enterPlayModeOptions was restored through the live Editor API to None (enabled=true), then the persisted file was restored only after its reconstructed content matched the exact pre-task SHA-256 hash. No reload-configuration matrix is claimed. These are structural tests, not lifecycle or application behavior tests.

## 8. Errors and warnings

No C# compiler errors or warnings were observed for the skeleton. During domain reload, one MCP transport warning reported: "[WebSocket] Unexpected receive error: WebSocket is not initialised". MCP recovered and both suites completed. The final console query returned zero errors/warnings; no console-clear operation was issued by the agent. The transport warning is recorded rather than hidden.

## 9. Remaining risks and validation limits

No standalone player, IL2CPP, mobile, clean external consumer, submodule or direct Git UPM installation was tested. Only the installed 6000.3.26f1 Editor is validated. Runtime behavior does not exist yet. License selection, reproducible shared distribution, CI and repository tracking hygiene remain future work. No production-readiness claim is made. Package-root portability is a design property until tested in a separate consumer.

Hash comparison against the pre-task baseline found no modifications to existing Assets or ProjectSettings files; among existing Assets/ProjectSettings/Packages files, only packages-lock.json changed. Generated Library/Temp/log/IDE artifacts are expected from Unity compilation and test execution, not source edits.

## 10. Next task

Phase 1.1: review and introduce minimal public contracts in Abstractions for registry access, lifecycle, events and logging, with focused API tests. Keep implementations separate and defer the Unity adapter assembly until real adapter code is authorized. Stop after Phase 1.0; no Bootstrap, Service Container, Event Bus or logging implementation was added.
