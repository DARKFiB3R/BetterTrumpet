## BetterTrumpet v3.2.0

### Media popup

- **Native acrylic surface** - The media popup now uses the same translucent Windows acrylic styling as the main flyout.
- **Smoother playback timeline** - The timecode interpolates locally between Windows media updates and supports reliable click and drag seeking.
- **Per-app volume** - The popup volume control now adjusts only the app that owns the active media session.
- **Polished transitions** - Track, artwork, accent color, and expand or collapse transitions are smoother and avoid first-use stalls.
- **Paused-session access** - An optional setting allows opening the popup while media is paused so playback can be resumed directly.
- **Refined controls** - Compact Phosphor Bold icons and action-oriented expand and collapse chevrons improve clarity.

### Audio controls

- **Volume sound feedback** - The volume tick can be disabled from settings, is now fully localized, and is preserved by settings export and import.
- **Persistent hard mute** - Apps can be marked to stay muted whenever their audio sessions appear, including after relaunch or reboot.
- **Independent monitored inputs** - Distinct recording devices using Windows Listen to this device are no longer merged into one system-sounds row when Windows exposes separate session groups.
- **Smoother app feedback** - Mute, solo-mute, hide, and app entrance animations have been refined.

### Interface and reliability

- **Tray menu polish** - The tray context menu has clearer organization, localized labels, acrylic styling, and refined icons.
- **Startup hardening** - Tray icon initialization and launch-at-startup handling are more reliable on .NET 8.
- **Diagnostics bundles** - Manual troubleshooting and crash reporting now produce more useful support bundles with recent logs.
