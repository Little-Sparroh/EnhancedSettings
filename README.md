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

### Skip Intro
- Skips the intro sequence when launching the game
- Reduces loading time and goes directly to menu
- Configurable via `SkipIntro` setting

### Singleplayer Pause
- Comprehensively pauses gameplay in singleplayer when opening the menu
- Freezes AI navigation and behaviors, projectiles, enemy cores and brains (including overclocked enemies)
- Pauses weapon systems (guns, autocannons, blades), hornets, and other flying enemies
- Preserves explosive enemy part fuse timing and disables enemy spawning
- Useful for checking settings without losing progress
- Configurable via `SingleplayerPause` setting

### Bounce Indicators
- Shows prediction lines for weapon bounce trajectories
- Available for Jackrabbit specifically or all bouncing weapons
- Customizable color selection with glow effects
- Configurable via `JackrabbitBounceIndicator` and `AllBounceIndicators` settings
- Color options: Orange, White, Green, Blue, Red, Yellow, Purple, Cyan

### Skip Mission Countdown
- Skips the countdown timer before mission start
- Configurable via `SkipMissionCountdown` setting

### Resize Item Popups
- Reduces the size of item upgrade popups and repositions them
- Configurable via `ResizeItemPopups` setting

### Multiplayer Region Bypass
- Allows setting lobby distance to "Worldwide"
- Bypasses regional restrictions for joining lobbies
- No setting required (always enabled)

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
# FOV and Aim settings
DisableAimFOV = true      # Disable FOV zoom when aiming
DisableSprintFOV = true   # Disable FOV changes while sprinting
EnableToggleAim = true    # Enable toggle aim mode
EnableToggleCrouch = true # Enable toggle crouch mode

# General enhancements
SkipIntro = true          # Skip the intro sequence on startup
SingleplayerPause = true  # Enable singleplayer pause functionality
SkipMissionCountdown = true # Skip the mission countdown timer
ResizeItemPopups = true    # Resize and reposition item upgrade popups

[Bounce Indicators]
# Enable/disable bounce indicators
JackrabbitBounceIndicator = true # Show jackrabbit bounce indicators
AllBounceIndicators = true       # Show bounce indicators for all weapons

[All Bounce Indicators]
# Color selection for bounce indicators (priority ordered, highest checked first)
Orange = true   # Standard orange color (highest priority, default)
White = false
Green = false
Blue = false
Red = false
Yellow = false
Purple = false
Cyan = false
```

- Settings can be changed in-game and take effect immediately
- Config changes are automatically reloaded without restarting
- Set any feature to `false` to disable it
- For bounce indicator colors, multiple can be enabled, but the highest priority color will be used

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

### Version 1.2.0
- Major overhaul of Singleplayer Pause with comprehensive pausing of AI, projectiles, enemies, weapons, spawning, and more
- Added Skip Mission Countdown feature
- Added Resize Item Popups feature
- All features remain independently configurable

### Version 1.1.0
- Added Skip Intro feature to skip startup sequence
- Added Singleplayer Pause functionality when opening menu
- Added Bounce Indicators for Jackrabbit and all bouncing weapons
- Added Multiplayer Region Bypass for unlimited lobby distance
- Renamed source files to PascalCase convention for consistency
- All features are configurable and can be enabled/disabled independently
- Added customizable colors for bounce indicators with glow effects

### Version 1.0.0
- Initial release combining DisableAimFOV, DisableSprintFOV, ToggleAim, and ToggleCrouch
- All features are configurable and can be enabled/disabled independently

## Authors

* Sparroh
* funlennysub (original mod template)
* [@DomPizzie](https://twitter.com/dompizzie) (README template)

## License

This project is licensed under the MIT License - see LICENSE.md for details
