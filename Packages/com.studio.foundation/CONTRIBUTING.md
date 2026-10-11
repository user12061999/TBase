# Contributing to Studio Foundation

Public distribution is planned. The package is unreleased and licensed under [MIT](LICENSE.md), Copyright (c) 2026 Towis. Preserve applicable notices and identify third-party material when proposing a contribution. This guide does not assert a separate contribution agreement.

## Development

Use Unity 6000.3.26f1 for the current validation target. In TBase, edit this embedded package in place. In an independent consumer, use a local package checkout. Do not modify PackageCache as a source workflow.

Keep Abstractions/Core engine-independent. Unity adapters may depend inward, never on game/test/sample code. Avoid global service locators, automatic reflection registration and new mandatory dependencies. Preserve .meta GUIDs and review ownership, ordering, threading and failure behavior with any API change. See [architecture](ARCHITECTURE.md) and [API review](Documentation~/API_REVIEW.md).

## Changes and validation

- Describe the concrete problem, proposed behavior and compatibility impact. Discuss new dependencies or significant public APIs before implementing them.
- Keep changes focused; include relevant docs and an unreleased changelog entry. Do not reformat unrelated files or commit generated caches/builds.
- Run Edit Mode tests for pure behavior and Play Mode tests for Unity lifecycle changes. Report actual counts, skipped cases and logs; an empty console is not proof of passing tests.
- Run reload/build checks only in a disposable fixture using [validation instructions](Validation~/README.md). The smoke driver exits its player and must never be imported into an application project.
- For bugs, provide Editor version, OS/backend, package revision, minimal reproduction and expected/actual results. Remove credentials and personal data from shared logs.

No automatic CI execution, review response time or platform beyond recorded evidence is promised. A preview release requires the [release checklist](Documentation~/RELEASE_CHECKLIST.md); submitting a change does not authorize publication.
