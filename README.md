# EnhancedSettings

A BepInEx mod for MycoPunk that combines multiple gameplay enhancement features into a single, configurable package.

## Features

### Disable Aim FOV
- Prevents FOV changes when aiming down sights
- Keeps a consistent field of view during aiming
- Configurable via `DisableAimFOV` setting

### Disable Sprint FOV
- Blocks FOV zoom effects while sprinting
- Maintains stable vision during movement
- Configurable via `DisableSprintFOV` setting

### Toggle Aim
- Changes aim behavior from hold to toggle mode
- Press aim button once to enter/exit aim
- Automatically resumes sprinting after aiming/firing
- Configurable via `EnableToggleAim` setting

### Toggle Crouch
- Allows holding crouch/slide by pressing the slide button
- No need to keep slide button held
- Press slide again to release
- Configurable via `EnableToggleCrouch` setting

## Installation

### Prerequisites
- MycoPunk (base game)
- [BepInEx](https://github.com/BepInEx/BepInEx) - Version 5.4.2403 or compatible
- .NET Framework 4.8

### Installing via Thunderstore (Recommended)
1. Open the Thunderstore Mod Manager
2. Search for "EnhancedSettings"
3. Download and install
4. Launch the game - the mod will automatically load

### Manual Installation
1. Download the mod files
2. Place `sparroh.enhancedsettings.dll` in `<MycoPunk Directory>/BepInEx/plugins/`
3. Launch the game

## Configuration

The mod creates a config file at `MycoPunk/BepInEx/config/sparroh.enhancedsettings.cfg` with the following settings:

```
[General]
DisableAimFOV = true      # Disable FOV zoom when aiming
DisableSprintFOV = true   # Disable FOV changes while sprinting
EnableToggleAim = true    # Enable toggle aim mode
EnableToggleCrouch = true # Enable toggle crouch mode
```

- Settings can be changed in-game and take effect immediately
- Config changes are automatically reloaded without restarting
- Set any feature to `false` to disable it

## Building from Source

1. Clone this repository
2. Open in Visual Studio or Rider
3. Build in Release mode
4. The compiled DLL will be in `bin/Release/net48/`

Or use dotnet CLI:
```bash
dotnet build --configuration Release
```

## Compatibility

- Client-side mod only (does not affect multiplayer sync)
- Compatible with other BepInEx mods
- Uses HarmonyLib for safe patching

## Troubleshooting

- **Mod not loading?** Check BepInEx console for errors
- **Features not working?** Verify config file settings
- **Conflicts with other mods?** Disable individual features in config
- **Game crashes?** Try disabling all features and re-enable one by one

## Changes

### Version 1.0.0
- Initial release combining DisableAimFOV, DisableSprintFOV, ToggleAim, and ToggleCrouch
- All features are configurable and can be enabled/disabled independently

## Authors

* Sparroh - Merged mod functionality

## License

This project is licensed under the MIT License - see LICENSE.md for details
