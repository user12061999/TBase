# Initial commit proposal — 2026-10-11

Status: prepared for owner review; nothing staged or committed. The repository has no local commits/tracked files. This proposal preserves the embedded package path and does not extract a repository, create a submodule, publish a tag or change remote visibility.

## Proposed commit

Suggested subject: `chore: establish MIT-licensed Unity foundation preview`

Include only:

- Root .gitignore, README.md, AGENTS.md, ARCHITECTURE.md, REQUIREMENTS.md, ROADMAP.md, CONTRIBUTING.md and LICENSING.md.
- All current files under Packages/com.studio.foundation, including runtime, tests, sample source/metas, ignored-by-Unity documentation/validation tooling, manifest, MIT license and contributor guidance.

Retain the package's existing location so a future UPM Git subdirectory URL can address it. The proposed commit is a framework source snapshot, not a complete runnable TBase Unity project. Consumers use their own project or the isolated fixture instructions. Including the application host can be considered separately after its assets/settings/rights are reviewed.

## Explicitly outside the proposal

| Paths | Reason |
| --- | --- |
| Assets/** | Application/template content is not needed for package installation; license scope and historical Mobile_RPAsset discrepancy remain separate |
| ProjectSettings/** | Host-specific Unity configuration; retain locally unchanged |
| Packages/manifest.json and packages-lock.json | Host tooling/render/input dependencies are not foundation dependencies |
| Library, Temp, Logs, UserSettings, generated project/solution files | Generated/local state covered by root ignore rules |
| External fixture projects, build outputs and logs | Evidence artifacts, not source distribution |

Exclusion from this proposed commit does not delete, move or newly ignore the host's source files. They will remain visible as untracked local work. Do not use `git add .` or treat the expected remaining untracked host files as disposable.

## Pre-commit checks and limits

The candidate inventory records each included path, byte size and SHA-256 in a local task artifact outside the repository. It must be regenerated or compared before staging if files change. It is an allowlist for review, not a saved Git index or commit.

Static checks cover generated/binary output exclusion, basic credential/key patterns in candidate text (findings report path/category/line only), existing Unity source/meta pairs, duplicate GUIDs, package JSON and local documentation links. Pattern scanning is not a guarantee that no confidential content or ownership concern exists. No unrelated ignored file or credential store is inspected.

Historical runtime evidence remains in PHASE_1_6_REPORT.md. Documentation/source-selection work does not justify claiming a new test run; the final immutable candidate still needs the applicable verification. The source snapshot remains an unreleased preview even after a local commit.

## Next action requiring authorization

Owner approval is required to create the proposed local commit under the current AGENTS.md rule. Once authorized, recheck the inventory, stage only the allowlisted paths, inspect the staged diff, and create the local commit. Report its SHA and the deliberately untracked host files. A local commit does not authorize push, tag or publication.
