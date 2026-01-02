using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using Pigeon.Movement;
using System;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;

public enum BounceIndicatorColor
{
    [Description("Standard orange color (highest priority, default)")]
    Orange,
    [Description("Clean white color")]
    White,
    [Description("Bright green color")]
    Green,
    [Description("Deep blue color")]
    Blue,
    [Description("Vibrant red color")]
    Red,
    [Description("Sunny yellow color")]
    Yellow,
    [Description("Royal purple color")]
    Purple,
    [Description("Aqua cyan color")]
    Cyan,
    [Description("Use a custom color specified by hex code")]
    Custom
}

[BepInPlugin(PluginGUID, PluginName, PluginVersion)]
[MycoMod(null, ModFlags.IsClientSide)]
public class SparrohPlugin : BaseUnityPlugin
{
    public const string PluginGUID = "sparroh.enhancedsettings";
    public const string PluginName = "EnhancedSettings";
    public const string PluginVersion = "1.4.0";

    internal static new ManualLogSource Logger;

    internal static ConfigEntry<bool> aimFOVChange;
    internal static ConfigEntry<bool> sprintFOVChange;
    internal static ConfigEntry<bool> toggleAim;
    internal static ConfigEntry<bool> toggleCrouch;

    internal static ConfigEntry<bool> showJackrabbitBounceIndicators;
    internal static ConfigEntry<bool> enableAllBounceIndicators;
    internal static ConfigEntry<BounceIndicatorColor> bounceIndicatorColor;
    internal static ConfigEntry<string> bounceIndicatorCustomColor;
    internal static ConfigEntry<bool> skipIntro;
    internal static ConfigEntry<bool> skipMissionCountdown;
    internal static ConfigEntry<bool> resizePopups;
    internal static ConfigEntry<bool> dataLogWaypoints;
    internal static ConfigEntry<bool> skinRandomizer;
    internal static ConfigEntry<bool> showDefaultAdditionalDetails;

    private FileSystemWatcher disableAimFOVWatcher;
    private FileSystemWatcher disableSprintFOVWatcher;
    private FileSystemWatcher toggleAimWatcher;
    private FileSystemWatcher toggleCrouchWatcher;

    internal static bool isAimToggled = false;
    internal static InputAction aimAction;

    internal static MethodInfo defaultFOVGetter;
    internal static FieldInfo isAimingPLField;
    internal static FieldInfo fovField;
    internal static FieldInfo aimFOVPLField;
    internal static FieldInfo aimDurationPLField;
    internal static FieldInfo aimStateChangeTimeField;
    internal static FieldInfo isAimInputHeldField;
    internal static FieldInfo lastPressedAimTimeField;
    internal static FieldInfo lastPressedFireTimeField;
    internal static FieldInfo playerField;
    internal static MethodInfo isAimingGetter;
    internal static MethodInfo wantsToFireGetter;
    internal static MethodInfo lastFireTimeGetter;

    private GameManager gameManager;
    internal static SparrohPlugin Instance { get; set; }

    private void Awake()
    {
        Instance = this;
        Logger = base.Logger;

        aimFOVChange = Config.Bind("General", "Aim FOV Change", true, "If true, enables FOV zoom changes when aiming.");
        sprintFOVChange = Config.Bind("General", "Sprint FOV Change", true, "If true, enables FOV changes while sprinting.");
        toggleAim = Config.Bind("General", "Toggle Aim", false, "If true, aim becomes a toggle (press to enter/exit) instead of hold.");
        toggleCrouch = Config.Bind("General", "Toggle Crouch", false, "If true, enables toggle crouch functionality (hold crouch by pressing slide button).");

        showJackrabbitBounceIndicators = Config.Bind("Bounce Indicators", "Jackrabbit Bounce Indicator", true, "Show jackrabbit bounce indicators");
        enableAllBounceIndicators = Config.Bind("Bounce Indicators", "All Bounce Indicators", false, "Show bounce/ricochet prediction lines for all weapons with bounces >= 1.");
        bounceIndicatorColor = Config.Bind("Bounce Indicators", "Bounce Indicator Color", BounceIndicatorColor.Orange, "Select the color for bounce/ricochet prediction lines");
        bounceIndicatorCustomColor = Config.Bind("Bounce Indicators", "Bounce Indicator Custom Color", "#FF8000", "Hex color code when 'Custom' is selected (format: #RRGGBB or RRGGBB)");
        skipIntro = Config.Bind("General", "Skip Intro", false, "Skip the intro sequence on startup");
        skipMissionCountdown = Config.Bind("General", "Skip Mission Countdown", false, "If true, skips the countdown timer before mission start.");
        resizePopups = Config.Bind("General", "Resize Item Popups", false, "If true, reduces the size of item upgrade popups and repositions them.");
        dataLogWaypoints = Config.Bind("General", "Data Log Waypoints", true, "If true, shows waypoints for undiscovered data logs.");
        skinRandomizer = Config.Bind("General", "Skin Randomizer", false, "If true, randomly equips favorite skins on mission start.");
        showDefaultAdditionalDetails = Config.Bind("General", "Show Default Additional Details", false, "If true, shows additional details by default in hover info.");

        aimFOVChange.SettingChanged += OnAimFOVChanged;
        enableAllBounceIndicators.SettingChanged += OnEnableAllBounceIndicatorsChanged;
        sprintFOVChange.SettingChanged += OnSprintFOVChanged;
        toggleAim.SettingChanged += OnToggleAimChanged;
        toggleCrouch.SettingChanged += OnToggleCrouchChanged;
        skipMissionCountdown.SettingChanged += OnSkipMissionCountdownChanged;
        resizePopups.SettingChanged += OnResizePopupsChanged;
        bounceIndicatorColor.SettingChanged += OnBounceIndicatorColorChanged;
        bounceIndicatorCustomColor.SettingChanged += OnBounceIndicatorCustomColorChanged;

        try
        {
            SetupFileWatchers();
        }
        catch (Exception ex)
        {
            Logger.LogError($"Error setting up file watchers: {ex.Message}");
        }

        try
        {
            SetupAccessTools();
        }
        catch (Exception ex)
        {
            Logger.LogError($"Error setting up access tools: {ex.Message}");
        }



        try
        {
            var harmony = new Harmony(PluginGUID);

            ApplyAimFOVPatches(harmony);
            ApplySprintFOVPatches(harmony);
            ApplyToggleAimPatches(harmony);
            ApplyToggleCrouchPatches(harmony);

            ApplyDisableBounceIndicatorPatches(harmony);
            ApplyRegionBypassPatches(harmony);
            ApplyAllBounceIndicatorsPatches(harmony);
            ApplySkipIntroPatches(harmony);
            ApplyCountdownSkipPatches(harmony);
            ApplyDataLogWaypointPatches(harmony);
            ApplySkinRandomizerPatches(harmony);
            ApplyDefaultAdditionalDetailsPatches(harmony);
        }
        catch (Exception ex)
        {
            Logger.LogError($"Error applying patches: {ex.Message}");
        }

        Logger.LogInfo($"{PluginName} loaded successfully.");
    }

    private void SetupFileWatchers()
    {
        var configPath = Paths.ConfigPath;

        disableAimFOVWatcher = new FileSystemWatcher(configPath, $"{PluginGUID}.cfg");
        disableAimFOVWatcher.Changed += (s, e) => { aimFOVChange.ConfigFile.Reload(); };
        disableAimFOVWatcher.EnableRaisingEvents = true;

        disableSprintFOVWatcher = disableAimFOVWatcher;
        toggleAimWatcher = disableAimFOVWatcher;
        toggleCrouchWatcher = disableAimFOVWatcher;
    }

    private void SetupAccessTools()
    {
        defaultFOVGetter = AccessTools.PropertyGetter(typeof(PlayerLook), "DefaultFOV");
        isAimingPLField = AccessTools.Field(typeof(PlayerLook), "isAiming");
        fovField = AccessTools.Field(typeof(PlayerLook), "_fov");
        aimFOVPLField = AccessTools.Field(typeof(PlayerLook), "aimFOV");
        aimDurationPLField = AccessTools.Field(typeof(PlayerLook), "aimDuration");
        aimStateChangeTimeField = AccessTools.Field(typeof(PlayerLook), "aimStateChangeTime");

        isAimInputHeldField = AccessTools.Field(typeof(Gun), "isAimInputHeld");
        lastPressedAimTimeField = AccessTools.Field(typeof(Gun), "lastPressedAimTime");
        lastPressedFireTimeField = AccessTools.Field(typeof(Gun), "lastPressedFireTime");
        playerField = AccessTools.Field(typeof(Gun), "player");
        isAimingGetter = AccessTools.PropertyGetter(typeof(Gun), "IsAiming");
        wantsToFireGetter = AccessTools.PropertyGetter(typeof(Gun), "WantsToFire");
        lastFireTimeGetter = AccessTools.PropertyGetter(typeof(Gun), "LastFireTime");
    }

    private void ApplyAimFOVPatches(Harmony harmony)
    {
        harmony.PatchAll(typeof(AimFOVPatches));
    }

    private void ApplySprintFOVPatches(Harmony harmony)
    {
        harmony.PatchAll(typeof(SprintFOVPatches));
    }

    private void ApplyToggleAimPatches(Harmony harmony)
    {
        harmony.PatchAll(typeof(ToggleAimPatches));
    }

    private void ApplyToggleCrouchPatches(Harmony harmony)
    {
        harmony.PatchAll(typeof(ToggleCrouchPatches));
        harmony.PatchAll(typeof(EndCrouchPatches));
        harmony.PatchAll(typeof(EndSlidePatches));
    }

    private void ApplyDisableBounceIndicatorPatches(Harmony harmony)
    {
        harmony.PatchAll(typeof(BounceShotgunPatches));
    }

    private void ApplyRegionBypassPatches(Harmony harmony)
    {
        harmony.PatchAll(typeof(RegionBypassPatches));
    }

    private void ApplyAllBounceIndicatorsPatches(Harmony harmony)
    {
        harmony.PatchAll(typeof(GunBouncePatches));
    }

    private void ApplySkipIntroPatches(Harmony harmony)
    {
        harmony.PatchAll(typeof(IntroSkip.IntroPatches));
    }

    private void ApplyCountdownSkipPatches(Harmony harmony)
    {
        harmony.PatchAll(typeof(DisableCountdown));
    }

    private void ApplyDataLogWaypointPatches(Harmony harmony)
    {
        DataLogWaypointPatches.Initialize();
    }

    private void ApplySkinRandomizerPatches(Harmony harmony)
    {
        harmony.PatchAll(typeof(DropPodPatches));
    }

    private void ApplyDefaultAdditionalDetailsPatches(Harmony harmony)
    {
        harmony.PatchAll(typeof(DefaultAdditionalDetailsPatches));
    }

    private void OnAimFOVChanged(object sender, EventArgs e)
    {
    }

    private void OnSprintFOVChanged(object sender, EventArgs e)
    {
    }

    private void OnToggleAimChanged(object sender, EventArgs e)
    {
        ConfigureAimSubscription();
    }

    private void OnToggleCrouchChanged(object sender, EventArgs e)
    {
        if (!toggleCrouch.Value)
        {
            ToggleCrouchPatches.isToggleOn = false;
        }
    }

    private void OnEnableAllBounceIndicatorsChanged(object sender, EventArgs e)
    {
    }

    private void OnSkipMissionCountdownChanged(object sender, EventArgs e)
    {
    }

    private void OnResizePopupsChanged(object sender, EventArgs e)
    {
    }

    private void OnBounceIndicatorColorChanged(object sender, EventArgs e)
    {
        UpdateBounceIndicatorColors();
    }

    private void OnBounceIndicatorCustomColorChanged(object sender, EventArgs e)
    {
        if (bounceIndicatorColor.Value == BounceIndicatorColor.Custom)
        {
            UpdateBounceIndicatorColors();
        }
    }

    private Color GetSelectedColor()
    {
        switch (bounceIndicatorColor.Value)
        {
            case BounceIndicatorColor.Orange: return new Color(1f, 0.5f, 0f);
            case BounceIndicatorColor.White: return new Color(1f, 1f, 1f);
            case BounceIndicatorColor.Green: return new Color(0f, 1f, 0f);
            case BounceIndicatorColor.Blue: return new Color(0f, 0f, 1f);
            case BounceIndicatorColor.Red: return new Color(1f, 0f, 0f);
            case BounceIndicatorColor.Yellow: return new Color(1f, 1f, 0f);
            case BounceIndicatorColor.Purple: return new Color(1f, 0f, 1f);
            case BounceIndicatorColor.Cyan: return new Color(0f, 1f, 1f);
            case BounceIndicatorColor.Custom:
                string hexCode = bounceIndicatorCustomColor.Value;
                if (!hexCode.StartsWith("#"))
                {
                    hexCode = "#" + hexCode;
                }
                Color customColor;
                if (ColorUtility.TryParseHtmlString(hexCode, out customColor))
                {
                    return customColor;
                }
                else
                {
                    return new Color(1f, 0f, 0f);
                }
            default: return new Color(1f, 0f, 0f);
        }
    }

    private void UpdateBounceIndicatorColors()
    {
        Color newColor = GetSelectedColor();
        if (AllBounceIndicators.BounceLines != null)
        {
            foreach (var kvp in AllBounceIndicators.BounceLines)
            {
                var bounceLines = kvp.Value;
                for (int i = 0; i < bounceLines.Count; i++)
                {
                    if (bounceLines[i] != null && bounceLines[i].material != null)
                    {
                        bounceLines[i].material.color = newColor;
                        Color emissionColor = newColor * 0.5f;
                        emissionColor.a = 1f;
                        bounceLines[i].material.SetColor("_EmissionColor", emissionColor);
                        bounceLines[i].startColor = newColor;
                        bounceLines[i].endColor = newColor;
                    }
                }
            }
        }
        AllBounceIndicators.BounceLinePrefab = null;
    }

    internal static void ConfigureAimSubscription()
    {
        if (aimAction != null)
        {
            aimAction.started -= OnAimStarted;
            if (toggleAim.Value)
            {
                aimAction.started += OnAimStarted;
            }
            else
            {
                isAimToggled = false;
            }
        }
    }

    internal static void OnAimStarted(InputAction.CallbackContext context)
    {
        if (toggleAim.Value)
        {
            isAimToggled = !isAimToggled;
        }
    }



    private void Update()
    {
        if (resizePopups.Value && gameManager == null)
        {
            try
            {
                gameManager = GameManager.Instance;
                if (gameManager != null)
                {
                    ResizePopup.Initialize(gameManager);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error accessing GameManager or initializing resize popup: {ex.Message}");
            }
        }
    }

    private void OnDestroy()
    {
        if (disableAimFOVWatcher != null)
        {
            disableAimFOVWatcher.EnableRaisingEvents = false;
            disableAimFOVWatcher.Dispose();
        }
        if (aimAction != null)
        {
            aimAction.started -= OnAimStarted;
        }
    }
}
