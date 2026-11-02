# UndisturbedBossFights

Source for the UndisturbedBossFights Valheim mod: while a boss fight is going on, raids wait until it's over and no new creatures spawn near it. The fight itself is unchanged.

The player-facing description lives in [package/README.md](package/README.md).

## Layout

- `src/` — plugin code (BepInEx + Harmony)
  - `FightTracker.cs` — finds boss fights; clients report theirs to the server
  - `Patches.cs` — holds the raid timers (`RandEventSystem.UpdateRandomEvent`) and blocks natural spawns (`SpawnSystem.Spawn`)
  - `ConfigSync.cs` — server-to-client settings sync and version check
  - `Commands.cs` — `ubf_status`, `ubf_raidsoon`
- `package/` — Thunderstore files: `manifest.json`, `README.md`, `CHANGELOG.md`, `icon.png`
- `build.ps1` — builds the DLL and the Thunderstore zip

## Building

Needs Valheim and a BepInEx install (for `BepInEx.dll` and `0Harmony.dll`). No .NET SDK is required; the script uses the C# compiler that ships with Windows.

```powershell
powershell -ExecutionPolicy Bypass -File build.ps1
```

Pass `-Managed` and `-BepInExCore` if your game or BepInEx folders are elsewhere. The output is `bin/UndisturbedBossFights.dll` and `dist/UndisturbedBossFights-<version>.zip`, ready to upload to Thunderstore.

## Releasing

Bump the version in both `src/Plugin.cs` (`Version`) and `package/manifest.json` (`version_number`), add a `package/CHANGELOG.md` entry, build, and upload the zip from `dist/`.
