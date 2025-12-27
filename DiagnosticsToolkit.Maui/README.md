# MAUI SDK Pinning

This folder contains a `global.json` pinning the .NET SDK to 10.x to align with .NET MAUI 10 supported TFMs:

- Target frameworks: `net10.0-android; net10.0-ios; net10.0-maccatalyst`
- SDK pin: `10.0.100` (roll-forward to latest feature)

CI:
- The `maui-macos` job installs .NET SDK 10.x and 8.x.
- MAUI builds run from this folder (via step `working-directory`) so the local pin is honored.
