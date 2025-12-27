## SDK Pinning
This folder uses a local `global.json` to pin the .NET SDK to 10.x, matching the supported .NET MAUI 10 TFMs.

Build targets:
- net10.0-android
- net10.0-ios
- net10.0-maccatalyst

CI notes:
- The `maui-macos` job runs with `working-directory: DiagnosticsToolkit.Maui.Sample` so the local `global.json` (10.x) is honored during restore/build.

## Install
dotnet workload install ios maccatalyst android

## Battery usage:

Configurable SetSamplingInterval() to tune refresh cadence
Android adaptive backoff: exponential throttle while backgrounded (up to ~5s), resets on foreground
Both platforms pause sampling when backgrounded

## GC pressure:

Allocation-free caching: snapshots cached between intervals; no allocations per API call
Span-based /proc parsing with stackalloc buffers (Android/Linux providers)
No intermediate string arrays or dictionaries

## Background execution:

Foreground/background lifecycle hooks (OnAppForegrounded()/OnAppBackgrounded())
Automatic MAUI wiring via lifecycle events
Sample demonstrates pause/resume and state simulation

## Sample app shows it all working:

Auto-refresh with configurable interval (50–2000 ms)
Toolbar pause/resume toggle
"Simulate Background" switch to trigger adaptive backoff
Live display of Android's adaptive interval