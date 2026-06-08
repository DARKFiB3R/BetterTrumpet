# BetterTrumpet 3.0.13 Release Notes

## Summary
small release, big cleanup. this one smooths out first-launch weirdness, speeds up startup, and adds a couple of nicer quality-of-life touches.

## startup cleanup
- the app now gets past the first-launch backdrop issue without needing a trip through settings.
- theme init timing is cleaner, so the flyout stops flashing weird on startup.
- device loading happens in the background, so the tray appears sooner and the app feels lighter.

## bug fixes
- the backdrop / acrylic render issue from #13 is fixed.
- hidden device handling is more stable now.
- context menus around devices behave more consistently.

## ui polish
- you can now right-click a device and choose **hide this device**.
- device header actions are easier to reach, including the quick default-device switch.
- menus are cleaner and less noisy overall.

## cli bits
- **`doctor`** gives you a quick audio checkup.
- **`batch`** lets you chain commands in one go.
- shorthand aliases make common mute / volume actions faster to type.

```bash
bt doctor
bt batch --set-volume 67 --app discord --set-volume 30 --app vivaldi
bt volume discord 67
bt mute spotify
bt unmute chrome
```

## under the hood
- code cleanup and structure improvements.
- better error handling and logging.
- smarter resource handling during startup.

## credits
- big thanks to @Meteony for reporting the backdrop issue.
- and thanks to everyone sending feedback and bug reports.

---

**Full Changelog**: https://github.com/xammen/BetterTrumpet/compare/v3.0.12...v3.0.13
