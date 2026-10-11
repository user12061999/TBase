# Agent Instructions — TBase and Unity Foundation

## Current scope

TBase is the main application project. The user explicitly removed the separate unity-foundation-test repository requirement. Treat [ARCHITECTURE.md](ARCHITECTURE.md), [REQUIREMENTS.md](REQUIREMENTS.md) and [ROADMAP.md](ROADMAP.md) as proposed design documents until reviewed.

The user selected option 2 for Phase 1.0: an embedded package/assembly skeleton at Packages/com.studio.foundation and structural tests in TBase. TBase is the source repository. Git submodules and separate source/test repositories are explicitly deferred. The owner selected public distribution after Phase 1.6 and the API review. Local public-release preparation is authorized; the owner selected MIT with Copyright (c) 2026 Towis for the foundation package. Preserve its LICENSE.md and license metadata; do not extend that grant to unrelated host or third-party assets. Maintain the embedded package; do not publish, commit, migrate distribution or install toolchains without explicit authorization. A later explicit instruction authorizes subsequent phases; do not ask for repeated confirmation of already authorized work.

## Preserve the workspace

- Inspect Git status and applicable instructions before edits. At initial inspection main had no commits and all existing project files were untracked. Untracked does not mean disposable.
- Do not change unrelated assets, scenes, project settings, package versions or renderer/input configuration. Never broadly stage, clean, reset or delete user work.
- Do not install third-party dependencies without user approval. Existing Unity Test Framework can be used without adding a new test library.
- Do not create remote repositories, migrate to submodules, publish packages or tag releases unless included in the user's authorized task.
- Do not read or print full process command lines, credentials or environment secrets; query only the specific environment fields needed.

## Architectural rules for later implementation

- Keep foundation code within its UPM package and independent of TBase application classes/assets. Game composition belongs in the host project.
- Abstractions and Core have no Unity engine references. Unity adapters may depend inward; runtime never references Editor, tests, samples or optional modules.
- Use explicit asmdef references, no cycles, and platform/test constraints for Editor and test assemblies. Do not rely on folder names to exclude code from players.
- Prefer constructor injection and explicit module/service registration. No static service locator/event bus, reflection scanning, dynamic code generation or application-specific singleton framework.
- Preserve the documented ordering, thread affinity, ownership, exception and disposal contracts. If these must change, update architecture and tests in the same focused change.
- Optional integrations belong in separate packages when they add substantial capability or dependencies; the base package must work without them.
- Preserve committed .meta GUIDs. Never edit generated Library, Temp, Logs, csproj or sln files as source fixes.

## Unity environment and MCP

The verified initial Editor is 6000.3.26f1. Recheck ProjectSettings/ProjectVersion.txt and live project info rather than using another installed version automatically.

Discover MCP custom tools and active instances first. When several Editors are connected, route explicitly to the instance whose projectRoot is this workspace; instance hashes may change. Prefer read-only resources for inspection. Do not operate on IdleFishing, VFX or other open projects. Readiness and an empty console query are not proof of a clean compile or passing tests.

When implementation changes C#, wait for compilation to finish and inspect console errors. Verify unfamiliar Unity API signatures against the installed Editor or official documentation. Do not use arbitrary Editor code execution when a suitable read-only resource exists. Do not clear logs to conceal pre-existing errors.

## Validation and reporting

- Run focused Edit Mode tests for behavior changes and Play Mode tests for Unity lifecycle changes. Follow the requirements matrix for release candidates.
- Include failure/rollback/disposal and repeated-session tests, not only successful initialization.
- Run batchmode tests/builds in isolated checkouts or fixtures rather than the currently open project. Restore temporary Editor configuration changes in validation fixtures.
- Use existing tooling; adding mocking, performance, DI or logging dependencies is not implicit permission.
- For documentation-only changes, validate paths, links, consistency and diff scope; do not run unrelated builds or tests.
- Report actual checks and unresolved limits. Never claim production readiness, platform compatibility or passing builds without evidence.
- Update changelog and migration notes for public API/behavior changes; preserve release tags and follow SemVer.

Stop after local public-release preparation and report actual evidence and blockers. Do not proceed automatically into other runtime systems. In a standalone package checkout, these rules apply relative to its package root; TBase is only the initial integration host, never a required framework dependency.
