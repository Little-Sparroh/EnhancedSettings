# Changelog

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
