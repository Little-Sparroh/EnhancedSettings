# EnhancedSettings

A BepInEx mod for MycoPunk that combines multiple gameplay enhancement features into a single, configurable package.

## Description

This mod provides various client-side enhancements for MycoPunk gameplay, all configurable via BepInEx configuration file. Features are independent and can be enabled/disabled individually.

## Features

- **FOV Controls**: Disable FOV changes when aiming or sprinting
- **Toggle Mechanics**: Toggle aim and crouch modes
- **Bounce Indicators**: Prediction lines for weapon bounces (Jackrabbit-specific or all weapons), with customizable colors
- **Singleplayer Pause**: Pause gameplay when opening menu in singleplayer
- **Skip Intro**: Skip startup intro sequence
- **Skip Mission Countdown**: Skip countdown before mission start
- **Resize Item Popups**: Reduce size of item upgrade popups
- **Data Log Waypoints**: Show waypoints for undiscovered data logs on minimap
- **Skin Randomizer**: Randomly equip favorite skins at mission start
- **Show Default Additional Details**: Forces hover info to always display additional details by default
- **Multiplayer Region Bypass**: Bypass regional restrictions in lobby distance

## Installation

### Via Thunderstore (Recommended)
1. Install Thunderstore Mod Manager
2. Search for "EnhancedSettings" by Sparroh
3. Download and install

### Manual Installation
1. Download the mod package
2. Extract the .dll file to `<MycoPunk Directory>/BepInEx/plugins/`
3. Ensure BepInEx is installed

## Configuration

All features are configured via the BepInEx config file located at `<MycoPunk Directory>/BepInEx/config/sparroh.enhancedsettings.cfg`

The config includes sections for General settings and Bounce Indicators.

## Requirements

- MycoPunk
- BepInEx 5.4.2403 or compatible
- .NET Framework 4.8

## Changelog

See CHANGELOG.md for version history.

## Authors

- Sparroh
- funlennysub (BepInEx template)
- [@DomPizzie](https://twitter.com/dompizzie) (README template)

## License

This project is licensed under the MIT License - see the LICENSE file for details.
