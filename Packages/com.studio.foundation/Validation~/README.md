# Isolated validation fixtures

This folder is ignored by Unity package import. Do not copy these tools into a production project. PlayerSmoke deliberately quits its executable after assertions. PlayerBuild and reload tests require batchmode and a `.foundation-validation-fixture` marker in the working project root.

## Prepare a disposable project

1. Create a fresh directory with Assets, Packages and ProjectSettings. Copy the package into Packages/com.studio.foundation, preserving metadata. Copy the host's verified ProjectVersion.txt into ProjectSettings.
2. Use a minimal manifest with com.unity.test-framework 1.6.0 and built-in audio, imgui and jsonserialize modules at 1.0.0. These are fixture tooling/built-ins, not base-package dependencies. Use the supported Editor and existing installed tooling; do not upgrade the application's dependencies.
3. Copy Samples~/Minimal Bootstrap into Assets/Sample (including its metadata) to simulate the imported sample. Copy Validation~/Runtime and Validation~/Editor into Assets/Validation. Create the marker file in the fixture root. Do not copy README files into Editor source assemblies unnecessarily.
4. Run one Unity process at a time against that fixture. Keep its logs, XML reports, build result and player result. Omit `-quit` when using `-runTests`; the Test Framework exits on completion.

## Scene-authored reload matrix

Invoke Unity with:

```text
-batchmode -nographics -projectPath <fixture>
-runTests -testPlatform EditMode
-testFilter Studio.Foundation.Validation.SceneHostReloadTests.SceneAuthoredHostRestartsAcrossAllReloadSettings
-testResults <fixture>/scene-reload.xml -logFile <fixture>/scene-reload.log
```

The test creates and saves its own scene containing the sample marker, camera and light. It enters/exits Play Mode three times per domain/scene reload combination. The sample entry point must start the pre-existing component each time. Tests assert fresh counters, successful ticks/event delivery, stop logs, and stable Unity component identity when scene reload is disabled. SessionState carries only test progress across domain reload. Teardown restores the fixture's original play options and removes the temporary log subscription.

## Windows player smoke

Build in the marked fixture:

```text
-batchmode -nographics -quit -projectPath <fixture>
-executeMethod Studio.Foundation.Validation.PlayerBuild.Mono
-logFile <fixture>/build-mono.log
```

The builder creates a separate sample/assertion scene, builds StandaloneWindows64 using Mono, writes Builds/Mono/build-result.txt and restores the previous scripting backend. For an installed, configured IL2CPP toolchain, use PlayerBuild.Il2Cpp instead; this command does not install missing tools.

Run the resulting executable with:

```text
-batchmode -nographics -logFile <fixture>/player-mono.log
-foundationResult <fixture>/player-mono-result.txt
```

Require all three: successful build report, player process exit code 0, and a PASS result file produced by the player. A build alone is not a smoke-test pass. The player checks startup, ticks, registry identity, events, duplicate-host rejection, idempotent cleanup, disposed event-bus capability and replacement startup. Use a bounded external timeout and report a timeout as failure; never infer success from missing logs. Inspect the ordinary player's Managed output to verify NUnit and package test assemblies are absent.

This is a narrow Windows headless functional smoke test, not rendering, performance, mobile or complete backend compatibility validation. Fresh consumer install via a real Git remote/submodule remains a separate distribution gate.
