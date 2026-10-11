# Studio Foundation in TBase

A reusable Unity foundation package under [Packages/com.studio.foundation](Packages/com.studio.foundation/README.md).

The package provides explicit service registration, lifecycle/module orchestration, typed events, logging and Unity adapters. An opt-in Minimal Bootstrap sample demonstrates composition. Initial validation targets Unity 6000.3.26f1 and Windows x64 Mono; see the [validation report](Packages/com.studio.foundation/Documentation~/PHASE_1_6_REPORT.md) for evidence and limits.

## Status and installation

Unreleased preview in development. No published revision is available yet. Follow the [public distribution plan](Packages/com.studio.foundation/Documentation~/PUBLIC_DISTRIBUTION.md) once a reviewed revision is published; the URL template there is not a live release.

For local evaluation, add the package directory to an existing compatible Unity project as a local package. The proposed first public commit contains framework source and documentation, not the TBase application's Assets, ProjectSettings or host manifest. It is not a complete Unity project checkout. Use your own consumer or the documented [isolated validation fixture](Packages/com.studio.foundation/Validation~/README.md).

## Documentation

- [Usage and sample](Packages/com.studio.foundation/README.md)
- [Architecture](Packages/com.studio.foundation/ARCHITECTURE.md)
- [Public API review](Packages/com.studio.foundation/Documentation~/API_REVIEW.md)
- [Contributing](CONTRIBUTING.md)
- [Release checklist](Packages/com.studio.foundation/Documentation~/RELEASE_CHECKLIST.md)

The foundation package is [MIT licensed](Packages/com.studio.foundation/LICENSE.md), Copyright (c) 2026 Towis. See [licensing scope](LICENSING.md) for the distinction from unrelated host and third-party content.
