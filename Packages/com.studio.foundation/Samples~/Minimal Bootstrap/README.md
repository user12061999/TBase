# Minimal Bootstrap

Import this sample through Package Manager, then add MinimalBootstrap to one active root GameObject in your own scene. RequireComponent adds FoundationBehaviour. Enter Play Mode: the console prints Started, each Update publishes a typed event, and shutdown prints Stopped. No rendering, input or third-party package is needed.

Only active, enabled sample markers present in the initial scene are started by the sample's AfterSceneLoad entry point. This explicit sample-only scan runs each Play session, including when scene/domain reload are disabled. Nothing starts merely because the base package is installed or because the sample is imported without a marker. For dynamically created/additively loaded markers, call StartApplication explicitly once. Multiple active markers fail the foundation duplicate-host check; use one application composition root.

This example is scene-local (persistAcrossScenes=false). Disable FoundationBehaviour or call its Shutdown to end the session. Disabling MinimalBootstrap itself only disables the sample marker; FoundationBehaviour owns the running lifetime. Re-enabling a stopped foundation does not restart it. For persistent application composition use a dedicated root and Initialize's persistence option.

The module registers borrowed capabilities, explicitly owns logger/event bus, and enrolls a plain C# lifecycle participant. Stop releases the event token before owned-resource disposal. Current exposes per-session counters for demonstration and verification. Adapt the explicit composition to the application; do not use the sample scan or counters as a general service locator.

The sample has its own asmdef. Core never references it. Remove the imported sample and scene marker when replacing it with your composition. No scene asset or project settings are changed by importing this sample.
