## BetterTrumpet v3.1.1

### Startup Hotfix

hey, sorry about the startup issue on this one - that was pretty annoying. here's what happened and what's fixed:

- **Windows startup fix** - when BetterTrumpet saved the "launch at startup" setting from the app, it was writing `BetterTrumpet.dll` into the Windows startup registry entry instead of `BetterTrumpet.exe`. that came from the .NET 8 migration: the old assembly path API now points at the DLL, not the executable apphost. Windows then tried to launch the DLL at boot, which obviously is not the normal app.
- **Correct launch target** - the app now writes the real process path, so startup points at `BetterTrumpet.exe` again.
- **Installer path still good** - the installer was already writing the correct executable path; this hotfix covers the in-app setting and onboarding path that could overwrite it later.

If you already hit the issue, installing 3.1.1 and toggling "launch at startup" off/on will repair the startup entry.
