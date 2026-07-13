# BetterTrumpet - Project Context for Codex

## Mirror Policy

On Windows, `AGENTS.md` and `agents.md` are the same path. Use `AGENTS.md` as the canonical filename.

## Maintenance Rule

Update this file whenever a task adds a meaningful feature, fixes an important bug, changes user-visible behavior, changes release/build/package workflow, adds a new CLI command, modifies diagnostics/logging, or introduces a recurring pitfall. Keep entries concise and practical so the next AI session can understand the current app state without rereading the whole history.

## Current Branch State

- Branch: `master`
- `master`, `origin/master`, `migration/net8`, and `origin/migration/net8` contain the released 3.2.0 source. A small post-release manifest/status commit may leave `master` ahead of tag `v3.2.0` without changing the shipped binary.
- Public tag: `v3.2.0` points at the final release-artifact commit `3ab038c1`.
- Current version line: `3.2.0` (released). The x86 Release binary and public release artifacts report `3.2.0`.
- Target framework: `net8.0-windows10.0.19041.0`
- Language: C# / WPF
- Assembly name: `BetterTrumpet`
- Namespace: `EarTrumpet`
- The tree contains unrelated user work. Known unrelated local state includes `bettertrumpet-site`, `.planning/*`, `docs/FEATURES-3.0.13.md`, `docs/RECENT-CHANGES.md`, and untracked Chocolatey `.nupkg` files. Never revert changes you did not make.

## What This Is

BetterTrumpet is a fork of [EarTrumpet](https://github.com/File-New-Project/EarTrumpet), the Windows per-app volume mixer. This fork adds themes, onboarding, auto-updates, CLI, media popup, crash reporting, QuickTrumpet presets, and release tooling.

- Owner: `xammen`
- Repo: `https://github.com/xammen/BetterTrumpet`
- Build system: MSBuild + GitVersion + Inno Setup
- Current distribution: GitHub Releases, Chocolatey, Winget, Microsoft Store submission path
- Possible future distribution: Scoop bucket, web-hosted MSIX `.appinstaller`, npm wrapper for devs, Intune/enterprise package

## Build And Verify

```bash
nuget.exe restore EarTrumpet.vs15.sln
"C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe" EarTrumpet\EarTrumpet.csproj /p:Configuration=Release /p:Platform=x86 /p:OutputPath=..\Build\Release /t:Rebuild /v:minimal
powershell -ExecutionPolicy Bypass -File build-portable.ps1
& 'C:\Users\xammen\AppData\Local\Programs\Inno Setup 6\ISCC.exe' installer.iss
[System.Diagnostics.FileVersionInfo]::GetVersionInfo('Build\Release\BetterTrumpet.exe').FileVersion
```

- Build x86 Release only for real validation.
- `Release|x86` is self-contained for GitHub/Chocolatey/Winget distribution so users do not need to install the x86 .NET desktop runtime separately.
- If `SelfContained` is removed or not applied for `Release|x86`, users can see a ".NET 8.0.0 (x86) required" launch prompt from `BetterTrumpet.exe`.
- During dev validation, if the running `Build\Release\BetterTrumpet.exe` locks build outputs, it is acceptable to close the running BetterTrumpet process before rebuilding; after a successful rebuild, relaunch `Build\Release\BetterTrumpet.exe` when runtime verification is needed.
- Before version bumps, commit first, then tag, then build.
- Clear `.git\gitversion_cache` and `EarTrumpet\obj` if the version looks stale.
- `GitVersion.yml` intentionally sets `assembly-file-versioning-format: '{Major}.{Minor}.{Patch}'` so `FileVersionInfo.FileVersion` displays `3.1.0`, not `3.1.0.0`.
- Sign binaries before calculating SHA256 checksums if code signing is added later.
- After rebuilding public assets, recalculate and update `release-checksums-*.txt`, Chocolatey checksum, and Winget `InstallerSha256` together.
- Never touch `dist/` unless the task explicitly requires release packaging.
- Microsoft Store packaging is now for the new BetterTrumpet Partner Center app, not the inherited EarTrumpet listing. Partner Center identity: `Package/Identity/Name=xammen.Bettertrumpet`, `Package/Identity/Publisher=CN=7EDFC72A-8780-4841-8F34-30B45D719EAF`, `Package/Properties/PublisherDisplayName=xammen`.

## Workbench

Use `python tools/bettertrumpet_workbench.py` for repo-aware routing and validation.

- `analyze` classifies the current diff.
- `check --scope auto` runs the fast validations.
- `check --scope auto --full` adds heavier checks.
- `build`, `web`, and `package` run explicit heavy actions.
- `learn --area ... --symptom ... --rule ...` records recurring traps.

## Repo Map

```
EarTrumpet/
├── App.xaml.cs              # Startup, onboarding, changelog, tray menu
├── AppSettings.cs           # Registry / portable settings
├── CLI/
│   ├── CliHandler.cs        # CLI command parsing and pipe IPC
│   └── CliEntryPoint.cs     # bt entry point / help text
├── DataModel/
│   ├── UpdateService.cs     # GitHub release checks and installer flow
│   ├── StorageFactory.cs    # Registry vs portable JSON detection
│   └── SettingsExportService.cs
├── Diagnosis/
│   └── ErrorReporter.cs     # Crash reporting and logs
├── UI/
│   ├── Views/
│   │   ├── OnboardingWindow.xaml(.cs)
│   │   ├── ChangelogWindow.xaml(.cs)
│   │   ├── SettingsWindow.xaml(.cs)
│   │   └── FlyoutWindow.xaml(.cs)
│   └── ViewModels/
│       ├── OnboardingViewModel.cs
│       ├── FlyoutViewModel.cs
│       ├── EarTrumpetColorsSettingsPageViewModel.cs
│       └── EarTrumpetAboutPageViewModel.cs
├── Properties/
│   ├── Resources.resx
│   ├── Resources.fr-FR.resx
│   └── Resources.Designer.cs
└── Interop/Helpers/
    ├── PackageHelper.cs
    └── PipeClient.cs
```

## Localization Rules

- XAML uses `Text="{x:Static resx:Resources.KeyName}"`.
- C# uses `EarTrumpet.Properties.Resources.KeyName`.
- In `App.xaml.cs`, use the full `EarTrumpet.Properties.Resources` namespace because `Properties` is ambiguous.
- When adding UI text, update `Resources.resx`, `Resources.fr-FR.resx`, and `Resources.Designer.cs` together.

## Settings Storage

- Installed mode: `HKCU\Software\EarTrumpet` via `RegistrySettingsBag`
- Portable mode: `settings.json` next to the exe when `portable.marker` exists
- Store/MSIX packaging is only for the Microsoft Store release path. Installed mode still uses registry settings; portable mode still uses `settings.json` next to the exe.

## Key Design Decisions

- Use pack URIs without assembly qualifiers: `pack://application:,,,/Assets/file.ext`
- For animatable WPF visuals, use inline `<SolidColorBrush>` elements instead of raw color attributes
- `#if DEBUG` should not gate features that must exist in Release
- `PrepareToInstall` in `installer.iss` kills `BetterTrumpet.exe`
- Moving a tag makes the GitHub release draft again; republish with `--draft=false`
- Public EXE/setup signing is not configured yet. For public Authenticode signing, use a trusted code-signing certificate or Microsoft Trusted Signing; self-signed certificates are only useful for dev/test. Sign before hashing, Chocolatey/Winget updates, and GitHub upload.

## Startup And First Run

- `Left Ctrl` at startup forces onboarding
- `Left Shift` at startup forces changelog
- `HasShownFirstRun` is presence-based. Deleting `HKCU\Software\EarTrumpet\hasShownFirstRun` forces onboarding; writing `false` is not the same thing.
- The tray icon can become active before all startup work is finished. Keep tray icon code null-safe.

## Onboarding

Current flow is 5 pages:

1. Audio output
2. Appearance
3. Privacy
4. Ready
5. Tray pin

Notes:

- `TrayPin.gif` is animated via `XamlAnimatedGif`.
- Telemetry is staged in the onboarding ViewModel and only applied on the privacy step.
- The appearance step can either keep system colors or apply the custom BetterTrumpet palette.
- Disabling telemetry during onboarding requires an explicit confirmation because telemetry is used for crash, bug, and memory-leak diagnostics and no data is sold.
- The final tray pin page is the last step, not a decorative extra.
- The onboarding text is localized in EN and FR.

## Current Branch Notes

Recent work in `master` includes:

- Public 3.1.0 release on GitHub with setup exe, portable zip, and checksum file
- 3.1.0 hotfix: when custom slider colors are disabled, volume bars and peak meters now reapply theme brushes instead of falling back to white WPF defaults
- 3.1.1 hotfix: app-managed launch-at-startup now writes `BetterTrumpet.exe` via `Environment.ProcessPath` instead of `BetterTrumpet.dll` from `Assembly.Location`
- Release packaging hardening: `Release|x86` is self-contained, and file/product versions display `3.1.1`
- Onboarding refactor to a calmer 5-step flow with localized text, working option cards, and telemetry opt-out confirmation
- CLI app mute support via `--toggle-mute --app` and friendly `toggle-mute APP`
- QuickTrumpet / preset support expansion: `resolve-apps`, `rule-preview`, `rule-apply`, `preset-create`, plus aliases like `save`, `apply`, `mode`, `presets`
- Theme and slider color fix so custom colors no longer fall back to white bars
- App item entrance animation cleanup via `AnimateOnLoad`
- App mute/unmute visual polish: app rows fade smoothly when mute state changes
- Ctrl+click solo-mute feedback: subtle micro-scale animation on the clicked app, without accent glow
- Hidden app polish: hide/unhide uses fade, slide, and micro-scale only; avoid delayed or heavy layout-height animation
- Tray context menu polish: clearer order, shorter labels, localized EN/FR resources, no hard-coded English labels
- Tray context menu acrylic redesign: right-click tray menu now uses section headers, roomier rows, left glyphs, right-side checks/chevrons, blue translucent fallback styling, and `AccentPolicyLibrary.EnableAcrylic` blur on the popup when available.
- Changelog window hardening: fixed the missing `PrimaryButton` StaticResource by using the onboarding button style and localized the window strings
- Diagnostics hardening: manual diagnostics now export a `.zip` support bundle with logs and snapshot data; crash dialogs create an exception bundle and copy its path to the clipboard
- Tray icon startup hardening: null-safe icon handling and first-frame readiness
- Startup registry fix: app-managed launch-at-startup now writes `BetterTrumpet.exe` via `Environment.ProcessPath` instead of `Assembly.Location`, which points at `BetterTrumpet.dll` on .NET 8.
- Docs updates in `docs/CLI.md`
- After the experimental redesign was fully reverted, a new minimal media-popup pass removed only the blurred album-art background, dark scrim, glow, and shimmer. The popup surface now reuses the flyout's `FlyoutBackground` content brush and `AcrylicColor_Flyout` Windows Acrylic tint; media/session behavior remains at the `HEAD` baseline.
- Hard mute (persistent per-app mute): apps can be flagged "keep muted" from the flyout app focus menu. A hard-muted app is force-muted every time one of its audio sessions appears, including after relaunch or reboot. Keyed by `ExeName` (stable across restarts, unlike AppId/session ids). Stored in `AppSettings` as `HardMutedAppEntriesJson`, applied in `DeviceViewModel.AddSession` and re-applied via the `HardMutedAppsChanged` event (`DeviceViewModel.ApplyHardMuteState`). Toggle lives in `FocusedAppItemViewModel` as a checkable menu item; localized keys `HardMuteAppButtonText`/`HardMuteAppMenuText` (EN/FR). Included in settings export/import via the `HardMutedAppsJson` passthrough. WASAPI note: an app with no open audio session cannot be pre-muted because Windows exposes no per-app volume object until first playback; hard mute takes effect the moment the session is created. Disabling hard mute leaves the current mute state untouched so the user stays in control.
- 3.1.2 (in development): monitored recording-device sessions from Windows "Listen to this device" are no longer collapsed into one system-sounds app row when WASAPI exposes distinct grouping parameters. `AudioDeviceSessionCollection.AddSystemSoundsSession()` separates system-sounds session groups by `GroupingParam`, and `AppItemViewModel.DoesGroupWith()` keeps system-sounds rows distinct by session id so each listened-to device can be adjusted independently from the main flyout.
- 3.2.0 adds a hidden monkey volume-sound easter egg: four clicks on the BetterTrumpet logo in About unlock and enable three cleaned PCM/WAV clips selected by volume (`monkeylow.wav` at 0-20, `monkeymid.wav` above 20 and below 85, `monkeyhigh.wav` at 85-100), then reveals a persistent toggle on the About page. Low is sourced from `monkeylow.mp3`, mid from the shorter `monkeymid2.mp3`, and high keeps its existing source. The normal tick remains independently disableable in mouse/volume settings. `MonkeyTickSoundUnlocked`, `UseMonkeyTickSound`, and `UseVolumeTickSound` participate in settings export/import. `MonkeySoundPlayer` alternates two channels, overlaps repetitions by 75 ms, and crossfades range changes over 40 ms. Audio cleanup uses a conservative `-50 dB` threshold and only compresses silences longer than 40 ms, preserving quiet monkey details while removing MP3 padding and long gaps.
- The post-update changelog is now a compact confirmation window showing the installed version, with `OK` and a localized link to `https://bettertrumpet.com/changelog`. It no longer downloads or renders full release notes inside the app.
- The tray context menu is anchored to the monitor work area rather than the click's Y coordinate. After opening, its popup HWND is clamped with an 8-DIP gap from the taskbar/work-area edges, preventing the menu from overlapping the taskbar across bottom, top, left, and right taskbar layouts and DPI scales.

## Release State

3.1.1 has been released as a startup hotfix:

- GitHub Release: `https://github.com/xammen/BetterTrumpet/releases/tag/v3.1.1`
- Tag `v3.1.1`: annotated tag on `13b8b884`
- `master` and `migration/net8`: both pushed to `13b8b884`
- GitHub assets:
  - `BetterTrumpet-3.1.1-setup.exe` SHA256 `347F9ED0AC304A0A5FFC16D1968055960047620ECD67D96214E23A89318A7CEE`
  - `BetterTrumpet-3.1.1-portable.zip` SHA256 `209594B31E6B10D251DBF52EFB013BDC5B5689123FC73C9D60E2EAC630424DCD`
- Chocolatey `bettertrumpet.3.1.1.nupkg` was pushed successfully and is pending moderation.
- Winget PR is open: `https://github.com/microsoft/winget-pkgs/pull/390442`.
- Microsoft Store package/submission is a separate Partner Center path; do not mix Store artifact versioning with GitHub/Choco/Winget without checking the Store manifest.

3.2.0 was released on 2026-07-13:

- GitHub Release: `https://github.com/xammen/BetterTrumpet/releases/tag/v3.2.0`
- Tag `v3.2.0`: annotated tag on `3ab038c1`
- `master` and `migration/net8` were pushed with the complete release source.
- GitHub assets:
  - `BetterTrumpet-3.2.0-setup.exe` SHA256 `A1040A2E8C3988DABED29E9050BBC76079537446B6228C9F51650D321DC75011`
  - `BetterTrumpet-3.2.0-portable.zip` SHA256 `5BEA8FEAA70437286B7CA93E12F44236236B851699174443B93566BB46B6C9EE`
- Chocolatey `bettertrumpet.3.2.0.nupkg` was pushed successfully and is awaiting automated checks/moderation.
- Winget PR: `https://github.com/microsoft/winget-pkgs/pull/401693` (open, CLA passed, WinGetSvc checks running at submission time).

If replacing same-version GitHub assets again, update hashes everywhere before or immediately after upload. Winget and Chocolatey verify the setup hash and will fail if the GitHub asset changes without their metadata changing.

## CLI

`bt.cmd` maps to `BetterTrumpet.exe`.

The CLI surface now includes:

- `list-devices`, `list-apps`, `get-volume`, `set-volume`, `set-device`
- `mute`, `unmute`, `toggle-mute`
- app-friendly aliases that accept `--app NAME`
- QuickTrumpet preset commands and rule/preset helpers
- update, settings export/import, and health commands

Device matching is partial (`IndexOf`). App matching is exact on `ExeName` or `DisplayName`.
Treat `docs/CLI.md` as the user-facing syntax reference when in doubt.

## Theme And Volume UI

`EarTrumpetColorsSettingsPageViewModel.cs` owns the theme engine:

- 7 color channels: thumb, fill, track background, peak meter, window background, text, accent glow
- built-in presets plus custom theme save/load/import/export
- dynamic album art theme mode

`UI/Controls/VolumeSlider.cs` is sensitive:

- custom slider colors now fall back to theme defaults instead of white
- when custom slider colors are disabled, reset code must reapply the resolved theme brushes; `ClearValue` alone can expose white WPF fallback bars and hide peak meters
- the peak meter default should stay accent-colored
- tick sound playback is in this control

`ThemeRegistry.cs` defines the default palette.

## Media Popup (minimal Acrylic background pass)

The experimental 2026-07-10 popup redesign was reverted completely. A new isolated visual pass now changes only the popup surface while keeping `MediaSessionService.cs` and all session behavior at the `HEAD` baseline.

- `MediaPopupWindow` no longer paints album art across the full popup or applies the old dark gradient, dominant-color glow, or shimmer animation.
- The root content surface uses `Theme:Brush.Background="FlyoutBackground"` under the `Flyout` theme scope.
- Window Acrylic uses the same `AcrylicColor_Flyout` reference as the main flyout and refreshes on theme changes; the effect is disabled again when the popup hides.
- Popup Acrylic enforces a minimum tint alpha of `0xA8`, making the Windows backdrop clearly visible while retaining enough dark tint for control legibility.
- Media controls now use local Phosphor Bold `Path` geometries (play/pause, skip, shuffle, repeat, volume, caret) from `PhosphorIconData` instead of mixed thin Segoe MDL2 glyphs.
- Media-control glyphs stay deliberately compact inside unchanged hit targets. The expand/collapse caret crossfades between separate up/down paths with a 2-DIP directional slide instead of rotating 180 degrees.
- The collapsed caret points up (expand action) and the expanded caret points down (collapse action); its crossfade starts immediately with no delayed incoming phase.
- Expand/collapse animates `Window.Height`/`Top` only with short `FillBehavior=Stop` clocks over final base values, while the timecode uses local interpolation and performs no COM refresh during the transition. Artwork adds a 6-DIP fade/slide/micro-scale; stale cover-fade completions are state-guarded.
- The popup entrance storyboard is controllable and finalized into base opacity/transform values on completion or before the first caret action, so its `HoldEnd` clocks cannot conflict with the first expansion.
- Track changes no longer scale/flash the entire popup. Only the title and expanded artwork use a short fade/6-DIP slide; the new title returns independently of slower artwork loading, and late artwork gets a 140 ms fade.
- Each decoded track thumbnail animates the shared media accent and volume gradient to its dominant color over 320 ms. New transitions start from the currently rendered color to avoid flashes during rapid track changes.
- The timecode is now a real WPF `Slider`: click-to-seek and dragging update the time optimistically and send one seek on release.
- SMTC often advances its reported timeline only every 3-5 seconds. The popup stores each SMTC position as an anchor and interpolates locally at 100 ms while playing; SMTC events, pause/play, track changes, and seeks resynchronize the anchor without polling COM at render frequency.
- Mouse seeking is owned explicitly by the popup instead of relying on WPF `Slider`/`RepeatButton` command ordering: pointer down captures the mouse and maps X to duration, move previews continuously, and pointer up commits exactly one SMTC seek. Lost capture also commits the last preview safely.
- After a seek, stale SMTC positions are ignored until Windows reports a position within 2 seconds of the optimistic anchor or a 5-second safety timeout expires; direct clicks therefore cannot snap back to the old timecode.
- Seek requests await the SMTC result and retry after 180 ms; a final delayed retry is used only when the provider still rejects the command.
- Popup storyboards and active/inactive icon brushes are cached; hidden track changes only preload artwork instead of refreshing the whole view.
- The expanded foreground artwork remains unchanged, including its low-resolution fallback handling.
- The expanded artwork uses a cheap flat 2-DIP translucent shadow layer instead of WPF `DropShadowEffect`, avoiding first-use effect preparation during the caret interaction.
- The obsolete blur preview/slider was removed from the media-popup settings page. `AppSettings.MediaPopupBlurRadius` remains tolerated for stored-settings compatibility but is no longer consumed by the popup.
- The media setting is presented as `ShowWhenPaused` while retaining the inverse `MediaPopupShowOnlyWhenPlaying` storage key for compatibility. When enabled, tray hover opens only if Windows exposes a controllable paused/playing session, allowing Play to resume it without showing an empty popup.
- Media-popup volume is app-only. It resolves the current SMTC `SourceAppUserModelId` against flyout `AppId`/`ExeName`, locks that app for the duration of a drag, and never falls back to the default-device/master volume. If no reliable app session is available, the volume row is disabled.
- The known SMTC limitation remains: Windows may choose a browser session/thumbnail instead of Spotify in some multi-session situations.
- Any future session-selection fix should be developed and verified separately from a visual redesign so behavior can be validated before changing the UI.
- Do not restore the discarded planning files or assume source navigation, snapshot coordination, color modes, or the redesigned sliders are currently implemented.

## Distribution Notes

- GitHub Releases are the canonical source for setup exe and portable zip.
- Winget and Chocolatey consume the GitHub setup exe and validate SHA256.
- Scoop is a good next channel because the portable zip already exists; start with a personal bucket before attempting Scoop Extras.
- A web-hosted MSIX `.appinstaller` can add App Installer-based installs outside Store, but it is separate from the current Inno setup path.
- npm is possible only as a dev-oriented wrapper package that downloads the GitHub installer or portable zip; do not treat npm as the primary Windows distribution channel.
- Intune/enterprise deployment can reuse Inno silent switches: `/VERYSILENT /SUPPRESSMSGBOXES /NORESTART /SP-`.

## Tray And Startup Hardening

Important recent fixes:

- `TaskbarIconSource` must survive the window asking for the tray icon before animation has populated the first frame
- `ShellNotifyIcon` now tolerates a missing current icon
- `IconExtensions.AsDisposableIcon()` must handle null safely

Do not undo these changes unless you are actively replacing the tray pipeline.

## Tray Context Menu

`GetTrayContextMenuItems()` in `App.xaml.cs` owns the tray right-click menu.

Current intended order:

1. `SORTIE AUDIO` / audio output header, then playback devices
2. Hidden app/device restore menus, only when needed
3. Add-on items, when present
4. `ACTIONS` header, then BetterTrumpet primary actions: open mixer, open settings
5. Update/install action, only when an update is available
6. Support/info actions: check updates, what's new, onboarding, GitHub
7. `AUTRES OUTILS` / other tools header, then Windows audio tools submenu
8. Exit, isolated at the bottom

Menu labels must be localized through `Resources.resx`, `Resources.fr-FR.resx`, and `Resources.Designer.cs`. The recent tray-only keys are `TrayOpenVolumeMixer`, `TrayOpenSettings`, `TrayShowOnboarding`, `TrayWhatsNew`, `TrayStarProject`, `TrayWindowsAudioTools`, `TraySectionAudioOutput`, `TraySectionActions`, and `TraySectionOtherTools`. Hard-coded glyph values like `"\xE713"` are acceptable because they are Segoe MDL2 icon codes, not user-visible text.

The visual template lives in `App.xaml` under the global `ContextMenu`/`MenuItem` styles. `ShellNotifyIcon.ShowContextMenu()` applies acrylic blur to the popup HWND via `AccentPolicyLibrary.EnableAcrylic`; the XAML gradient is the fallback when DWM/acrylic is unavailable.

Tray menu primary icons can use local Phosphor Bold geometries from `UI/Helpers/PhosphorIconData.cs` via `ContextMenuItem.IconData`/`IconScale`. Keep the legacy `Glyph` populated as a Segoe MDL2 fallback and avoid webfont/CDN dependencies.

## Diagnostics And Logs

`ErrorReporter` wires Trace to both an in-memory circular listener and `FileTraceListener`.

- Installed logs: `%APPDATA%\BetterTrumpet\logs`
- Portable logs: `config\logs` next to the executable
- Log rotation: `bettertrumpet-*.log`, max 5 files of 5 MB
- Manual export: Settings -> About -> `TroubleshootEarTrumpetText`
- Manual export creates `BetterTrumpet-diagnostics-*.zip`, opens Explorer on it, and copies the path to the clipboard
- Crash handling creates a diagnostic bundle with the exception and recent logs, without taking a live audio snapshot to avoid cascading failures

The diagnostic zip can contain app names, device names, process IDs, endpoint IDs, settings state, and recent logs. Keep this clear in user-facing copy when asking users to attach it.

## Common Pitfalls

1. Build before tag gives the wrong version in the binary.
2. `git add -A` can accidentally scoop up `dist/`.
3. `Resources.Designer.cs` is not auto-generated.
4. `Properties.Resources` in `App.xaml.cs` is ambiguous.
5. Frozen WPF brushes crash when animated.
6. Custom theme colors that are stored as `Transparent` are meant to mean "use the current default", not "render white".
7. The onboarding first-run flag is presence-based, not bool-based.
8. Replacing GitHub release assets changes their SHA256; Chocolatey and Winget must be updated or installs will fail checksum validation.
9. `VolumeSlider.ResetVisualElementColors()` must not rely on `ClearValue` alone after `Theme:Brush` has written local values.
10. For startup/run entries on .NET 8, do not use `Assembly.GetExecutingAssembly().Location`; it points to `BetterTrumpet.dll`. Use `Environment.ProcessPath` for `BetterTrumpet.exe`.
11. SMTC's manager-level `GetCurrentSession()` can switch between Spotify and browser tabs between calls. This remains a known baseline limitation after the experimental fix was reverted; isolate any future behavioral fix from visual redesign work.

## Validation Status

- `x86 Release` build passes as of `2026-06-19` after the 3.1.1 startup hotfix
- `Build\Release\BetterTrumpet.exe` reports `FileVersion=3.1.1` and `ProductVersion=3.1.1`
- `build-portable.ps1` and `installer.iss` both succeeded for the 3.1.1 hotfix assets
- Onboarding first-run launch was exercised successfully
- Latest onboarding log showed no onboarding crash
- The previous `ChangelogWindow` StaticResource crash has been fixed
- `x86 Release` rebuild passes as of `2026-07-10` with the minimal media-popup Acrylic background pass. Startup is clean (`MediaPopup initialized`, no popup-related exception), and the binary remains on the 3.1.1 version line. Visual confirmation at real tray hover is pending creator review.
- `x86 Release` rebuild also passes after the media accent transition, action-oriented caret, and paused-session popup option. Runtime startup remains clean and a 300x300 SMTC thumbnail decodes successfully.
- `x86 Release` rebuild passes for 3.2.0. `BetterTrumpet.exe` reports `FileVersion=3.2.0` and `ProductVersion=3.2.0`; the portable ZIP and Inno Setup installer were generated, Chocolatey packaging succeeded, and the isolated three-file Winget manifest set validates successfully.

## Release Notes Convention

GitHub release notes must be:

- In English
- Professional tone
- Format: `## BetterTrumpet vX.Y.Z`, then `### Section`, then `- **Feature** - description`
