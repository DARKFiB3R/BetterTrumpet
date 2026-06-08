# BetterTrumpet Release Playbook

Use this file as the canonical release flow for an AI agent.
It is written to keep releases consistent, fast, and low-risk.

## Goal

Ship a tagged BetterTrumpet release with:
- `BetterTrumpet-X.Y.Z-setup.exe`
- `BetterTrumpet-X.Y.Z-portable.zip`
- matching GitHub release notes
- matching Chocolatey package
- matching Winget manifests

## Required Inputs

- Target version `X.Y.Z`
- Final release notes file under `.claude/`
- GitHub token already available through `gh auth`
- Chocolatey API key when publishing to Chocolatey

## Hard Rules

1. Edit version files before building.
2. Commit first, then tag, then build.
3. Build Release x86 only.
4. Rebuild the portable zip from the Release output.
5. Use the installer SHA256 for Chocolatey and Winget.
6. Publish both installer and portable assets on GitHub Releases.
7. If the tag moves, republish the release with `--draft=false`.

## Version Files

Update these before the release build:
- `GitVersion.yml`
- `installer.iss` (all version fields)
- `build-portable.ps1`
- `chocolatey/bettertrumpet.nuspec`
- `chocolatey/tools/chocolateyInstall.ps1`
- `manifests/x/xmn/BetterTrumpet/X.Y.Z/`

## Release Flow

### 1. Clean the workspace

```powershell
Remove-Item '.git\gitversion_cache' -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item 'EarTrumpet\obj' -Recurse -Force -ErrorAction SilentlyContinue
```

### 2. Build Release x86

```powershell
& 'C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe' `
  'EarTrumpet\EarTrumpet.csproj' /p:Configuration=Release /p:Platform=x86 `
  /p:OutputPath=..\Build\Release /t:Rebuild /v:minimal
```

### 3. Create the portable zip

```powershell
& '.\build-portable.ps1'
```

### 4. Create the installer

```powershell
& 'C:\Users\xammen\AppData\Local\Programs\Inno Setup 6\ISCC.exe' installer.iss
```

### 5. Compute checksum

```powershell
$hash = (Get-FileHash 'dist\BetterTrumpet-X.Y.Z-setup.exe' -Algorithm SHA256).Hash
```

### 6. Update package metadata

- Replace the Chocolatey checksum.
- Replace the Winget installer checksum.
- Update Winget version, installer URL, and release date.

### 7. Commit and tag

```powershell
git add -A
git commit -m "release: bump version to X.Y.Z"
git tag -a vX.Y.Z -m "BetterTrumpet X.Y.Z"
git push origin master
git push origin vX.Y.Z
```

### 8. Publish GitHub Release

```powershell
gh release create vX.Y.Z `
  'dist\BetterTrumpet-X.Y.Z-setup.exe' `
  'dist\BetterTrumpet-X.Y.Z-portable.zip' `
  --title "BetterTrumpet X.Y.Z" `
  --notes-file '.claude\release-X.Y.Z-notes.md'
```

If the release already exists and the tag was moved:

```powershell
gh release edit vX.Y.Z --repo xammen/BetterTrumpet --draft=false --latest
```

### 9. Publish Chocolatey

```powershell
Push-Location chocolatey
choco pack
choco push bettertrumpet.X.Y.Z.nupkg --source https://push.chocolatey.org/ --api-key <API_KEY>
Pop-Location
```

### 10. Submit Winget

```powershell
$token = gh auth token
wingetcreate submit --prtitle "Add xmn.BetterTrumpet X.Y.Z" --token $token `
  "C:\Users\xammen\Documents\CLAUDE\ear\manifests\x\xmn\BetterTrumpet\X.Y.Z"
```

## Validation

Verify these before declaring the release done:
- GitHub release is live.
- Both assets are present.
- Installer hash matches Chocolatey and Winget.
- Chocolatey push succeeded.
- Winget PR was created.
- Auto-update can see the new version.

## Release Notes Format

Keep release notes short, professional, and structured:
- Title line: `## BetterTrumpet vX.Y.Z`
- Short intro paragraph
- `### Downloads`
- `### Bug Fixes`
- `### CLI Improvements`
- `### Performance and UI`
- `### Under the Hood`
- `### Thanks`
- `### Full Changelog`

## Release Voice

Use the same vibe as `v3.0.11`:
- lowercase, direct, and human
- short sentences
- light personality, but not goofy
- clean section names and a simple flow
- mention the real user-facing wins first
- keep the notes readable in GitHub release view

Good pattern:
- `summary`
- `startup cleanup` or `performance`
- `bug fixes`
- `ui polish`
- `cli bits` if relevant
- `under the hood`
- `thanks`

If a release has a standout interaction, call it out plainly, for example:
- `right-click a device and choose hide this device`
- `the tray appears sooner`
- `the flyout no longer blanks out on startup`

Avoid:
- overly formal changelog language
- long feature lists with too much nesting
- emoji-heavy headings
- marketing fluff
- vague claims without a concrete user-facing effect

## Common Failure Modes

- Building before tagging gives the wrong version.
- Forgetting the portable zip leaves the release incomplete.
- Reusing an old checksum breaks Chocolatey or Winget.
- Moving a tag makes the GitHub release draft again.
- Missing Winget locale manifests makes `wingetcreate` fail.

## Recommended Output For The Agent

When the release is complete, report:
- GitHub release URL
- asset names uploaded
- Chocolatey status
- Winget PR URL
- any warnings or follow-up work
