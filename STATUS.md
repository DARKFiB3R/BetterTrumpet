# STATUS (checkpoint — not auto-updated after this point)

- Branch: `fork-v3.2.0` (built via `merge-tree` off `upstream/master` at v3.2.0; not the `wip-checkpoint`/`pr-*` branches)
- Last commit: `dd582c23` "Add toggle to disable global text-color theming, keep sliders-only"
- Uncommitted changes (not yet committed as of this checkpoint):
  - `EarTrumpet/App.xaml`
  - `EarTrumpet/UI/Controls/BalanceSlider.cs`
  - `EarTrumpet/UI/Controls/VolumeSlider.cs`
  - `EarTrumpet/UI/ViewModels/ColorTheme.cs`
  - `EarTrumpet/UI/ViewModels/EarTrumpetColorsSettingsPageViewModel.cs`
  - `EarTrumpet/UI/Views/DeviceView.xaml`
  - `EarTrumpet/UI/Views/SettingsWindow.xaml`
- `dist/*` and `nuget.exe` diffs in `git status` are pre-existing autocrlf noise, unrelated, safe to ignore

## Three fixes just made (balance slider peak meter)

- **Peak meter respects Peak Meter Style setting** (Classic/Dotted/Blocks/Bars/Wave)
  - `EarTrumpet/UI/Controls/BalanceSlider.cs:147` — new `ApplyPeakMeterStyle()`, mirrors `VolumeSlider`'s per-style Height/CornerRadius/OpacityMask, applied uniformly to both `_peakLeft`/`_peakRight` (unlike VolumeSlider, both bars always stay visible — they're two different channels, not a stereo-depth pair)
  - `EarTrumpet/UI/Controls/VolumeSlider.cs` — `CreateDottedBrush` changed from `private` to `internal static` so `BalanceSlider` can reuse it instead of duplicating
  - Wired to `App.Settings.PeakMeterStyleChanged` (subscribe/unsubscribe in `OnLoaded`/`OnUnloaded`), same pattern as `VolumeSlider`

- **Vertical centering fixed**
  - `EarTrumpet/App.xaml` — removed static `Margin="0,-6,0,0"` from `PeakLeft`/`PeakRight` Border definitions (search `Name="PeakLeft"` / `Name="PeakRight"`)
  - `EarTrumpet/UI/Controls/BalanceSlider.cs:334` and `:341` — code-set `Margin` Y-component changed from `-6` to `0`
  - Meter is now centered on the track like `VolumeSlider`'s own meter; whatever segment sits directly under the Thumb is simply covered by it (expected, matches how a physical fader cap covers part of a VU strip)

- **"L/R" label bolded to match the value text**
  - `EarTrumpet/UI/Views/DeviceView.xaml:149` — added `FontWeight="Bold"` to the "L/R" TextBlock

## Open theory — not yet confirmed

- User reports the peak meter shows roughly equal left/right levels even with Balance set to +60 (should show right louder than left)
- `PeakValue1`/`PeakValue2` come from `IAudioMeterInformation` activated directly on the audio endpoint device (`EarTrumpet/DataModel/WindowsAudio/Internal/AudioDevice.cs:52`, read via `Helpers.ReadPeakValues` at `:189`)
- Hypothesis: on some drivers, this endpoint-level meter taps the signal *before* the per-channel volume scalar that Balance actually adjusts (`IAudioEndpointVolume.SetChannelVolumeLevelScalar`, via `AudioDeviceChannel.Level`) — i.e., a driver-level APO chain ordering issue outside this app's control, not a bug in this code
- Not confirmed. Temporary diagnostic logging being added next to watch live `PeakValue1`/`PeakValue2` while Balance is adjusted during playback, to get a definitive answer instead of a theory
