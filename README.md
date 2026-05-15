# gr8scale

A tiny Windows app with one slider that smoothly desaturates everything on your screen, from 0% (full color) to 100% (fully gray) — in real time.

Windows' built-in grayscale filter (Win+Ctrl+C) is on/off only. gr8scale gives you the dial in between.

## How it works

Uses the Windows Magnification API (`MagSetFullscreenColorEffect`) to apply a 5×5 color matrix to the desktop compositor. At 0% the matrix is identity; at 100% it's a BT.601 luminance matrix; in between it's linearly interpolated. The effect is alive while gr8scale is running and reverts on quit.

OS-level limits (not workarounds available): DRM-protected video (e.g. Netflix) is exempted, and the mouse cursor isn't affected.

## Run

Just double-click `gr8scale.exe` from the published folder:

```
bin\Release\net8.0-windows\win-x64\publish\gr8scale.exe
```

- Drag the slider to set the level
- Close window (X or —) → hides to tray, **effect stays applied**
- Right-click tray icon → Quit → effect reverts
- Tick "Start with Windows" to auto-launch at login
- Slider position is remembered between runs (`%LOCALAPPDATA%\gr8scale\settings.json`)

## Build from source

Requires .NET 8 SDK.

```pwsh
# Run during development
dotnet run

# Publish a stand-alone single-file .exe (~68 MB)
dotnet publish -c Release -r win-x64 --self-contained true `
  -p:PublishSingleFile=true `
  -p:IncludeNativeLibrariesForSelfExtract=true `
  -p:EnableCompressionInSingleFile=true
```
