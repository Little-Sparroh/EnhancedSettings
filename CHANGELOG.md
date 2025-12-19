# Changelog

## 1.2.0 (2025-12-18)

* Major overhaul of Singleplayer Pause functionality:
  * Now comprehensively pauses AI navigation and behaviors
  * Freezes all projectiles, enemy cores, and brains (including overclocked enemies)
  * Pauses weapon systems (guns, autocannons, blades)
  * Stops hornets and other flying enemies
  * Preserves explosive enemy part fuse timing
  * Disables enemy spawning during pause
* Added Skip Mission Countdown: Skips the countdown timer before mission start
* Added Resize Item Popups: Reduces the size of item upgrade popups and repositions them
* All features remain independently configurable via BepInEx config file

## 1.1.0 (2025-11-29)

* Added multiple new client-side gameplay enhancements:
  * Skip Intro: Skips the startup intro sequence for faster loading
  * Singleplayer Pause: Pauses gameplay when opening menu in singleplayer mode to freeze AI, projectiles, and enemies
  * Bounce Indicators: Shows prediction lines for weapon bounce trajectories (Jackrabbit-specific and universal options)
  * Multiplayer Region Bypass: Allows setting lobby distance to "Worldwide" to bypass regional restrictions
* Added customizable color options for bounce indicators with glow effects (Orange, White, Green, Blue, Red, Yellow, Purple, Cyan)
* Renamed source code files to PascalCase convention for consistency
* All features are independently configurable via BepInEx config file
* Client-side only, no multiplayer impact
* Uses HarmonyLib for safe patching

## 1.0.0 (2025-08-19)

* Initial release combining multiple client-side gameplay enhancements:
  * Disable Aim FOV: Prevents FOV changes when aiming down sights
  * Disable Sprint FOV: Blocks FOV zoom effects while sprinting
  * Toggle Aim: Changes aim from hold to toggle mode with automatic sprint resume
  * Toggle Crouch: Allows holding crouch/slide by pressing slide button
* All features are independently configurable via BepInEx config file
* Client-side only, no multiplayer impact
* Uses HarmonyLib for safe patching

### Tech (Template Setup)
* Initial mod template setup with BepInEx framework
* Add MinVer for version management
* Add thunderstore.toml configuration for mod publishing
* Add LICENSE.md and CHANGELOG.md template files
* Basic plugin structure with HarmonyLib integration
* Placeholder for mod-specific functionality
