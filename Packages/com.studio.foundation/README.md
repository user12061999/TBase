# Studio Foundation

A reusable UPM package, independent of application code. Current version: **0.1.0-preview.1**, Phase 1.6 Core systems, Unity adapters and opt-in sample. Initial validation target: **Unity 6000.3.26f1**.

## Included

Engine-independent Abstractions and Core assembly boundaries, package provenance metadata, and structural Edit Mode/Play Mode tests. Public interfaces for services, lifecycle, events, logging and module metadata are available in Abstractions. Core now provides ServiceRegistryBuilder, NullLogger and SinkLogger. ApplicationHost now adds pure lifecycle/module orchestration and explicit cleanup. EventBus adds ordered, synchronous exact-type dispatch; see [Event Bus usage](Documentation~/EVENT_BUS.md). FoundationBehaviour and UnityConsoleSink now provide explicit Unity integration; see [Unity adapters](Documentation~/UNITY_ADAPTERS.md). See [lifecycle host](Documentation~/LIFECYCLE_HOST.md). See [Core usage](Documentation~/CORE_SERVICES.md). See [public API](Documentation~/PUBLIC_API.md).

## Consumption

The package is embedded at `TBase/Packages/com.studio.foundation`. TBase is its source repository and integration host. Unity discovers embedded packages directly; no duplicate manifest dependency or source copy in Assets is needed.

The user selected option 2 and deferred Git submodules. No separate repository, gitlink or .gitmodules was created. The package root is portable; later extraction to a dedicated package repository can support a pinned submodule in another project. That workflow and cross-project installation remain unvalidated. Do not make TBase a submodule of itself. Nothing has been committed or pushed by this task.

## Tests

Use the host's existing Unity Test Framework (validated with 1.6.0). Run assemblies Studio.Foundation.Tests.Editor in Edit Mode and Studio.Foundation.Tests.Runtime in Play Mode. For Git/local non-embedded installation, add this package ID to the host manifest's testables array. Test assemblies are constrained by UNITY_INCLUDE_TESTS and are not runtime dependencies. No third-party package is required by the package manifest.

## Development

Core explicitly references Abstractions. Both have noEngineReferences=true and autoReferenced=false; consumer asmdefs must reference what they use. AssemblyInfo.cs contains only package provenance attributes, enabling actual compiler validation without placeholder classes. Studio.Foundation.Unity explicitly references Core/Abstractions and UnityEngine. Individual optional module assemblies are added only when needed. Editor and Modules remain reserved. Samples~/Minimal Bootstrap contains an opt-in example; Validation~ contains fixture-only build and reload checks.

See [architecture](ARCHITECTURE.md), [requirements](REQUIREMENTS.md), [roadmap](ROADMAP.md) and [agent rules](AGENTS.md). The package is licensed under [MIT](LICENSE.md), Copyright (c) 2026 Towis. No production-readiness claim is implied.

## Sample and isolated validation

Import Minimal Bootstrap from the UPM Samples entry, then attach MinimalBootstrap to one active scene root. See [sample usage](Samples~/Minimal%20Bootstrap/README.md) and [isolated validation](Validation~/README.md). No sample assets are automatically added to the host project.

## Release status

This package is unreleased. See the [API review](Documentation~/API_REVIEW.md), [release checklist](Documentation~/RELEASE_CHECKLIST.md) and [draft preview notes](Documentation~/PREVIEW_RELEASE_NOTES_DRAFT.md). Current validation does not replace candidate revision or remote-consumption verification.

Public distribution is planned; see [public distribution preparation](Documentation~/PUBLIC_DISTRIBUTION.md) and [contributing](CONTRIBUTING.md). MIT licensing is selected; the release revision remains pending.
