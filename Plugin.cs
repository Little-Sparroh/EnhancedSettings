using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using Pigeon.Movement;
using System;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;

[BepInPlugin(PluginGUID, PluginName, PluginVersion)]
[MycoMod(null, ModFlags.IsClientSide)]
public class SparrohPlugin : BaseUnityPlugin
{
    public const string PluginGUID = "sparroh.enhancedsettings";
    public const string PluginName = "EnhancedSettings";
    public const string PluginVersion = "1.1.0";

    internal static new ManualLogSource Logger;

    internal static ConfigEntry<bool> aimFOVChange;
    internal static ConfigEntry<bool> sprintFOVChange;
    internal static ConfigEntry<bool> toggleAim;
    internal static ConfigEntry<bool> toggleCrouch;

    internal static ConfigEntry<bool> showJackrabbitBounceIndicators;
    internal static ConfigEntry<bool> enableAllBounceIndicators;
    internal static ConfigEntry<bool> enableOrange;
    internal static ConfigEntry<bool> enableWhite;
    internal static ConfigEntry<bool> enableGreen;
    internal static ConfigEntry<bool> enableBlue;
    internal static ConfigEntry<bool> enableRed;
    internal static ConfigEntry<bool> enableYellow;
    internal static ConfigEntry<bool> enablePurple;
    internal static ConfigEntry<bool> enableCyan;
    internal static ConfigEntry<bool> enableSingleplayerPause;
    internal static ConfigEntry<bool> skipIntro;

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

    private void Awake()
    {
        Logger = base.Logger;

        aimFOVChange = Config.Bind("General", "Aim FOV Change", true, "If true, enables FOV zoom changes when aiming.");
        sprintFOVChange = Config.Bind("General", "Sprint FOV Change", true, "If true, enables FOV changes while sprinting.");
        toggleAim = Config.Bind("General", "Toggle Aim", false, "If true, aim becomes a toggle (press to enter/exit) instead of hold.");
        toggleCrouch = Config.Bind("General", "Toggle Crouch", false, "If true, enables toggle crouch functionality (hold crouch by pressing slide button).");

        showJackrabbitBounceIndicators = Config.Bind("Bounce Indicators", "Jackrabbit Bounce Indicator", true, "Show jackrabbit bounce indicators");
        enableAllBounceIndicators = Config.Bind("Bounce Indicators", "All Bounce Indicators", false, "Show bounce/ricochet prediction lines for all weapons with bounces >= 1.");
        enableOrange = Config.Bind("Bounce Indicators", "Orange", false, "Use standard orange color (highest priority, default)");
        enableWhite = Config.Bind("Bounce Indicators", "White", false, "Use white color");
        enableGreen = Config.Bind("Bounce Indicators", "Green", false, "Use green color");
        enableBlue = Config.Bind("Bounce Indicators", "Blue", false, "Use blue color");
        enableRed = Config.Bind("Bounce Indicators", "Red", false, "Use red color");
        enableYellow = Config.Bind("Bounce Indicators", "Yellow", false, "Use yellow color");
        enablePurple = Config.Bind("Bounce Indicators", "Purple", false, "Use purple color");
        enableCyan = Config.Bind("Bounce Indicators", "Cyan", false, "Use cyan color");
        enableSingleplayerPause = Config.Bind("General", "Singleplayer Pause", false, "Enable singleplayer pause functionality");
        skipIntro = Config.Bind("General", "Skip Intro", false, "Skip the intro sequence on startup");

        aimFOVChange.SettingChanged += OnAimFOVChanged;
        enableAllBounceIndicators.SettingChanged += OnEnableAllBounceIndicatorsChanged;
        enableSingleplayerPause.SettingChanged += OnEnableSingleplayerPauseChanged;
        sprintFOVChange.SettingChanged += OnSprintFOVChanged;
        toggleAim.SettingChanged += OnToggleAimChanged;
        toggleCrouch.SettingChanged += OnToggleCrouchChanged;

        enableOrange.SettingChanged += OnColorConfigChanged;
        enableWhite.SettingChanged += OnColorConfigChanged;
        enableGreen.SettingChanged += OnColorConfigChanged;
        enableBlue.SettingChanged += OnColorConfigChanged;
        enableRed.SettingChanged += OnColorConfigChanged;
        enableYellow.SettingChanged += OnColorConfigChanged;
        enablePurple.SettingChanged += OnColorConfigChanged;
        enableCyan.SettingChanged += OnColorConfigChanged;

        SetupFileWatchers();

        SetupAccessTools();

        var harmony = new Harmony(PluginGUID);

        ApplyAimFOVPatches(harmony);
        ApplySprintFOVPatches(harmony);
        ApplyToggleAimPatches(harmony);
        ApplyToggleCrouchPatches(harmony);

        ApplyDisableBounceIndicatorPatches(harmony);
        ApplyRegionBypassPatches(harmony);
        ApplyAllBounceIndicatorsPatches(harmony);
        ApplySingleplayerPausePatches();
        ApplySkipIntroPatches(harmony);

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

    private void ApplySingleplayerPausePatches()
    {
        gameObject.AddComponent<SingleplayerPause>();
    }

    private void ApplySkipIntroPatches(Harmony harmony)
    {
        harmony.PatchAll(typeof(MycopunkSkipIntro.IntroSkip.IntroPatches));
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

    private void OnEnableSingleplayerPauseChanged(object sender, EventArgs e)
    {
    }

    private void OnColorConfigChanged(object sender, EventArgs e)
    {
        UpdateBounceIndicatorColors();
    }

    private Color GetSelectedColor()
    {
        if (enableOrange.Value) return new Color(1f, 0.5f, 0f);
        if (enableWhite.Value) return new Color(1f, 1f, 1f);
        if (enableGreen.Value) return new Color(0f, 1f, 0f);
        if (enableBlue.Value) return new Color(0f, 0f, 1f);
        if (enableRed.Value) return new Color(1f, 0f, 0f);
        if (enableYellow.Value) return new Color(1f, 1f, 0f);
        if (enablePurple.Value) return new Color(1f, 0f, 1f);
        if (enableCyan.Value) return new Color(0f, 1f, 1f);
        return new Color(1f, 0.5f, 0f);
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
        // Invalidate cached prefab so newly equipped weapons get the updated color
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
