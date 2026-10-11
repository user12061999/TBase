# Public distribution preparation — 2026-10-10

The owner selected public distribution. The owner explicitly selected MIT with Copyright (c) 2026 Towis; see [LICENSE.md](../LICENSE.md). The selection covers the foundation package, including its samples, documentation and validation tools. No source was committed and no tag or release created by this preparation.

## Current repository and proposed route

Read-only checks found the existing origin at https://github.com/user12061999/TBase.git. GitHub's unauthenticated repository metadata reported public visibility; git ls-remote returned no refs/heads/main. The local repository also has no tracked files or candidate commit. The framework is not yet installable from a published revision.

Keep the approved embedded layout at Packages/com.studio.foundation. The proposed first public consumption route is UPM Git using that subdirectory and an immutable revision. This avoids introducing a second repository or changing the deferred submodule decision.

Unity supports a subdirectory query before the revision fragment. After a real reviewed revision is available, the install URL will have this form:

```text
https://github.com/user12061999/TBase.git?path=/Packages/com.studio.foundation#<published-commit-or-tag>
```

This is a template, not a working release URL. Replace the placeholder only with a published, validated revision. See [Unity's Git path/revision documentation](https://docs.unity3d.com/6000.3/Documentation/Manual/upm-git.html#paths-and-revisions). Remote installation remains untested.

UPM selects the package folder, but publishing the TBase repository would still expose all committed repository content to repository readers. Review the entire proposed commit, including application assets and third-party content. Do not assume the package's MIT license automatically covers every file in the Unity host.

Direct Git submodule placement at Packages/com.studio.foundation requires the package at the source repository root. The whole TBase repository cannot be used that way. A later package-root extraction remains a separate, explicitly authorized migration.

## Prepared locally

- Root Unity .gitignore excludes generated caches, local settings and build outputs while retaining source assets and metadata.
- Contributor guides describe technical scope and validation without inventing a license, CLA, maintainer identity or support promise.
- Public intent is recorded in the release checklist and roadmap. No package version, runtime signature or dependency changed.

## Still required

1. Completed: package MIT license and metadata use Copyright (c) 2026 Towis. Review rights/notices of unrelated host content before including it in a public commit.
2. Review all source intended for the public repository and approve an initial candidate commit. Resolve the historical renderer-asset discrepancy if including that host asset.
3. Validate the committed candidate locally, then authorize the concrete remote publication step.
4. After the revision is remotely accessible, test clean UPM Git installation/sample consumption and associate test evidence with that revision before announcing a preview release.

No repository visibility was changed, no new repository was created, and nothing was staged, committed, pushed or published.
