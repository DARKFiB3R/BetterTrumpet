# BetterTrumpet — Tray Toggle Bug & Grey Slider Bug: Root Cause & Fix

**Status: RESOLVED.** Tray icon toggle now closes reliably regardless of timing. App-row
sliders (Chrome/Telegram/etc.) show correct custom colors immediately at launch. White
border has not reappeared since the slider fix.

---

## 1. Tray icon toggle bug

### Symptom

Clicking the tray icon to close an already-open flyout would sometimes reopen it instead
of closing it — intermittent, reproducible with any gap between clicks (not just fast
double-clicks), and confirmed present even on stock upstream BetterTrumpet.

### Background: two independent, racing signals

A single tray click produces **two separate events** that don't arrive together:

1. **`OnDeactivated`** — fires because clicking the tray icon (or anywhere outside the
   flyout) steals window focus, which WPF reports as deactivation. This is what actually
   closes the flyout via light-dismiss.
2. **`OpenFlyout` (via `PrimaryInvoke`)** — fires because the tray icon itself was clicked.
   This is the "toggle" logic.

Both come from the *same physical click*, but as two independently-timed Windows
messages. The bug is entirely about what happens when the app's `OpenFlyout` handler
receives event #2 without knowing event #1 already closed the flyout for the same click.

### Prior fix (upstream, `xammen`, commit `a4974fc`, June 8 2026)

Introduced a guard: if a mouse-triggered `OpenFlyout` call arrives within 300ms of the
last deactivation, **and** the flyout is still in `Closing_Stage1`/`Closing_Stage2`,
absorb it instead of reopening.

```csharp
if (inputType == InputType.Mouse
    && (State == FlyoutViewState.Closing_Stage1 || State == FlyoutViewState.Closing_Stage2)
    && (DateTime.UtcNow - _lastDeactivatedAt) < TimeSpan.FromMilliseconds(300))
{
    return;
}
```

This worked *most* of the time, which is exactly why the bug felt intermittent.

### Why it still failed sometimes

The close animation (`Closing_Stage1` → `Closing_Stage2` → `Hidden`) can finish in as
little as ~79ms. The trailing click message, however, is delivered independently and can
arrive later — observed gaps of 130–260ms in trace logs. When the click message arrives
*after* the state machine has already reached `Hidden`, the `State ==` condition in the
guard no longer matches, even though it's well within the 300ms window. The click then
falls through to `case FlyoutViewState.Hidden: BeginOpen(...)` and reopens the flyout —
the exact bug, confirmed directly from instrumented trace logs:

```
19.194  ChangeState Hidden                         <- closed in 79ms
19.250  TrayClick msg=1024 -> INVOKING PrimaryInvoke <- click lands 136ms after deactivation
19.254  OpenFlyout ENTER State=Hidden msSinceLastDeactivated=139
19.257  -> BeginOpen (Hidden)                       <- REOPENS (bug)
```

Two independent races (animation speed vs. message delivery timing) meant the guard only
protected against one of them.

### Final fix

Remove the `State ==` condition entirely — gate purely on elapsed time since the last
real deactivation, regardless of what state the flyout has already settled into:

```csharp
public void OpenFlyout(InputType inputType)
{
    // A mouse click on the tray icon while the flyout is open deactivates it first
    // (light dismiss closes it), then this click is delivered — regardless of whether
    // the close animation has already fully finished by the time the click arrives.
    // Absorb any mouse-triggered open within the window following a deactivation.
    if (inputType == InputType.Mouse
        && (DateTime.UtcNow - _lastDeactivatedAt) < TimeSpan.FromMilliseconds(300))
    {
        return;
    }

    switch (State)
    {
        case FlyoutViewState.Hidden:
            BeginOpen(inputType);
            break;
        case FlyoutViewState.Open:
            BeginClose(inputType);
            break;
        case FlyoutViewState.Closing_Stage1:
            _openAfterClose = true;
            _openAfterCloseInput = inputType;
            break;
        case FlyoutViewState.Closing_Stage2:
            _openAfterClose = true;
            _openAfterCloseInput = inputType;
            _deBounceTimer.Stop();
            ChangeState(FlyoutViewState.Hidden);
            break;
    }
}
```

This removes the race entirely — it no longer matters whether the close finished in
79ms or 400ms, or whether the click message arrives in 50ms or 200ms; any mouse-triggered
`OpenFlyout` within 300ms of a real deactivation is absorbed.

**File changed:** `EarTrumpet/UI/ViewModels/FlyoutViewModel.cs`
**Note:** applied via a GPT-assisted edit after hitting a usage limit mid-session; the
change matched the one-line fix above exactly (removed `State ==` condition, kept the
time check) — verified by diff, nothing else in the file was touched.

An earlier commit in this session (`faa672b`) also added a companion 200ms "settling
window" guard on `OnDeactivated` itself, to avoid a spurious deactivation immediately
after opening (layout/focus side effects) causing an instant false close. That guard is
unrelated to the reopen bug above but is complementary and was kept:

```csharp
if ((DateTime.UtcNow - _lastOpenedAt) < TimeSpan.FromMilliseconds(200))
{
    // Activation can still be settling immediately after opening (layout/focus
    // side effects of the device list content). A deactivation this soon after
    // opening isn't a real dismiss - ignore it rather than closing on it.
    return;
}
```

### Verification

Confirmed via instrumented `Trace.WriteLine` logging (since removed) across multiple
test sessions, including deliberately slow clicks (waited 1–10+ seconds between clicks)
and deliberately fast clicks — toggle closed reliably in every case after the fix, with
no more `_openAfterClose=true ... <-- THIS CAUSES THE REOPEN` trace lines appearing.

---

## 2. Grey slider bug (app-row sliders showing default/grey color at launch)

### Symptom

App rows (Chrome, Telegram, etc.) using `VolumeSlider` would render in grey instead of
the configured custom accent color when the flyout was first shown after app launch —
self-correcting the moment any theme change occurred.

### Root cause

Confirmed as a **pre-existing bug in upstream `VolumeSlider.cs`**, unrelated to the
balance-slider feature. `VolumeSlider.ApplyCustomColors()` is called twice, in two
different ways:

- **On theme change** (`OnThemeChangedReapplyColors`) — correctly deferred:
  ```csharp
  // After the theme system re-paints all elements, re-apply custom colors
  // Use BeginInvoke so we run AFTER the theme system finishes its updates
  Dispatcher.BeginInvoke(new Action(ApplyCustomColors), DispatcherPriority.Loaded);
  ```
- **On initial control load** (`OnLoaded`) — called **synchronously, immediately**:
  ```csharp
  // Apply custom colors if enabled
  ApplyCustomColors();
  ```

The app's own `Theme:Brush` system sets colors via a local value at a higher priority
than a plain property assignment (documented in-code: "Theme:Brush system sets local
values (priority 11) which override DataTrigger setters (priority 5)"). The theme
system's own pass runs *after* `Loaded`. On initial load, `ApplyCustomColors()` wins the
assignment first, then gets silently overwritten moments later by the theme system's
deferred pass — leaving the slider grey until something (a theme change) re-triggers the
*correctly-deferred* code path, which then wins the priority battle properly.

### Fix

Make the initial-load call defer exactly the same way the theme-change call already
does:

```csharp
// Apply custom colors if enabled — deferred, for the same reason
// OnThemeChangedReapplyColors defers: the theme system's own initial
// pass runs after Loaded and otherwise wins the priority battle,
// leaving the slider showing default (grey) colors until a later
// theme change happens to trigger the correctly-deferred re-apply.
Dispatcher.BeginInvoke(new Action(ApplyCustomColors), System.Windows.Threading.DispatcherPriority.Loaded);
```

**File changed:** `EarTrumpet/UI/Controls/VolumeSlider.cs` (line ~150, inside `OnLoaded`)

### Verification

Restarted the app multiple times post-fix; app-row sliders show correct custom colors
immediately on first flyout open, no theme change required.

---

## 3. White focus-ring border

Fixed earlier in the session (confirmed, unrelated to the above two): caused by
`BalanceSlider` explicitly calling `.Focus()` on mouse-down when no other control in that
row does. Fixed by removing the explicit `.Focus()` call and adding
`FocusVisualStyle="{x:Null}"` to `BalanceSlider`'s style — matching the existing app
convention already used elsewhere (e.g. `CaptionButtonStyle`) for suppressing the focus
ring cosmetically without disabling real focus/activation behavior.

Reappeared once, transiently, on the same day the grey-slider bug was being chased —
plausible as another instance of a startup-ordering race rather than a regression from
any code change made this session (no code touching focus/border logic was modified
during the toggle or slider color fixes). Has not reappeared since the slider color fix
was applied. Not chased further given no reliable repro; flagged here in case it recurs.

---

## Files touched, final state

| File | Change |
|---|---|
| `EarTrumpet/UI/ViewModels/FlyoutViewModel.cs` | `OpenFlyout` guard now time-based only (dropped `State ==` condition); companion 200ms settling-window guard on `OnDeactivated` retained |
| `EarTrumpet/UI/Controls/VolumeSlider.cs` | Initial `ApplyCustomColors()` call in `OnLoaded` deferred via `Dispatcher.BeginInvoke(..., DispatcherPriority.Loaded)` |

**Cleanup still pending:** diagnostic `Trace.WriteLine` calls added during investigation
(in both `FlyoutViewModel.cs` and `ShellNotifyIcon.cs`) are not meant to ship — strip
before committing for real.

## Outstanding / not part of this fix

- Balance/master-volume cross-talk on generic USB Audio Class drivers (see
  `BALANCE_SLIDER_SESSION_RECAP.md`) — separate, hardware-driver-level issue, already
  mitigated via self-correction in `DeviceViewModel`, not revisited here.
- Upstream feature-request discussion (`xammen/BetterTrumpet` Discussions #18) — worth
  updating with both fixes above if pursuing a PR path.
