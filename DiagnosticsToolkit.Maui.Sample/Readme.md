## Install
dotnet workload install maui-android

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