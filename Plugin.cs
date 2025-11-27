using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using Pigeon.Movement;
using Pigeon.Math;
using System;
using System.Diagnostics;
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
    public const string PluginVersion = "1.0.0";

    internal static new ManualLogSource Logger;

    internal static ConfigEntry<bool> disableFOVChange;
    internal static ConfigEntry<bool> disableSprintFOV;
    internal static ConfigEntry<bool> enableToggleAim;
    internal static ConfigEntry<bool> enableToggleCrouch;

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

        disableFOVChange = Config.Bind("General", "DisableAimFOV", true, "If true, disables FOV zoom changes when aiming.");
        disableSprintFOV = Config.Bind("General", "DisableSprintFOV", true, "If true, disables FOV changes while sprinting.");
        enableToggleAim = Config.Bind("General", "EnableToggleAim", true, "If true, aim becomes a toggle (press to enter/exit) instead of hold.");
        enableToggleCrouch = Config.Bind("General", "EnableToggleCrouch", true, "If true, enables toggle crouch functionality (hold crouch by pressing slide button).");

        disableFOVChange.SettingChanged += OnDisableFOVChanged;
        disableSprintFOV.SettingChanged += OnDisableSprintFOVChanged;
        enableToggleAim.SettingChanged += OnEnableToggleAimChanged;
        enableToggleCrouch.SettingChanged += OnEnableToggleCrouchChanged;

        SetupFileWatchers();

        SetupAccessTools();

        var harmony = new Harmony(PluginGUID);

        ApplyAimFOVPatches(harmony);
        ApplySprintFOVPatches(harmony);
        ApplyToggleAimPatches(harmony);
        ApplyToggleCrouchPatches(harmony);

        Logger.LogInfo($"{PluginName} loaded successfully.");
    }

    private void SetupFileWatchers()
    {
        var configPath = Paths.ConfigPath;

        disableAimFOVWatcher = new FileSystemWatcher(configPath, $"{PluginGUID}.cfg");
        disableAimFOVWatcher.Changed += (s, e) => { Logger.LogInfo("Config file changed, reloading"); disableFOVChange.ConfigFile.Reload(); };
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
        var updateAimingMethod = AccessTools.Method(typeof(PlayerLook), "UpdateAiming");
        if (updateAimingMethod != null)
        {
            harmony.Patch(updateAimingMethod, prefix: new HarmonyMethod(typeof(AimFOVPatches), nameof(AimFOVPatches.UpdateAimingPrefix)));
        }

        var updateCameraFOVMethod = AccessTools.Method(typeof(PlayerLook), "UpdateCameraFOV");
        if (updateCameraFOVMethod != null)
        {
            harmony.Patch(updateCameraFOVMethod, postfix: new HarmonyMethod(typeof(AimFOVPatches), nameof(AimFOVPatches.UpdateCameraFOVPostfix)));
        }
    }

    private void ApplySprintFOVPatches(Harmony harmony)
    {
        if (disableSprintFOV.Value)
        {
            harmony.PatchAll(typeof(SprintFOVPatches));
        }
    }

    private void ApplyToggleAimPatches(Harmony harmony)
    {
        var playerInputInit = AccessTools.Method(typeof(PlayerInput), "Initialize");
        harmony.Patch(playerInputInit, postfix: new HarmonyMethod(typeof(ToggleAimPatches), nameof(ToggleAimPatches.PlayerInputInitializePostfix)));

        var onAimPerformedMethod = AccessTools.Method(typeof(Gun), "OnAimInputPerformed");
        harmony.Patch(onAimPerformedMethod, prefix: new HarmonyMethod(typeof(ToggleAimPatches), nameof(ToggleAimPatches.SkipPrefix)));

        var onAimCancelledMethod = AccessTools.Method(typeof(Gun), "OnAimInputCancelled");
        harmony.Patch(onAimCancelledMethod, prefix: new HarmonyMethod(typeof(ToggleAimPatches), nameof(ToggleAimPatches.SkipPrefix)));

        var handleAimMethod = AccessTools.Method(typeof(Gun), "HandleAim");
        harmony.Patch(handleAimMethod, prefix: new HarmonyMethod(typeof(ToggleAimPatches), nameof(ToggleAimPatches.HandleAimPrefix)));

        var updateMethod = AccessTools.Method(typeof(Gun), "Update");
        harmony.Patch(updateMethod, postfix: new HarmonyMethod(typeof(ToggleAimPatches), nameof(ToggleAimPatches.UpdatePostfix)));

        var resurrectMethod = AccessTools.Method(typeof(Player), "Resurrect_ClientRpc");
        harmony.Patch(resurrectMethod, postfix: new HarmonyMethod(typeof(ToggleAimPatches), nameof(ToggleAimPatches.ResetTogglePostfix)));
    }

    private void ApplyToggleCrouchPatches(Harmony harmony)
    {
        harmony.PatchAll();
    }

    private void OnDisableFOVChanged(object sender, EventArgs e)
    {
        Logger.LogInfo($"DisableFOVChange changed to {disableFOVChange.Value}");
    }

    private void OnDisableSprintFOVChanged(object sender, EventArgs e)
    {
        Logger.LogInfo($"DisableSprintFOV changed to {disableSprintFOV.Value}");
    }

    private void OnEnableToggleAimChanged(object sender, EventArgs e)
    {
        Logger.LogInfo($"EnableToggleAim changed to {enableToggleAim.Value}");
        ConfigureAimSubscription();
    }

    private void OnEnableToggleCrouchChanged(object sender, EventArgs e)
    {
        Logger.LogInfo($"EnableToggleCrouch changed to {enableToggleCrouch.Value}");
        if (!enableToggleCrouch.Value)
        {
            ToggleCrouchPatches.isToggleOn = false;
        }
    }

    internal static void ConfigureAimSubscription()
    {
        if (aimAction != null)
        {
            aimAction.started -= OnAimStarted;
            if (enableToggleAim.Value)
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
        if (enableToggleAim.Value)
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

internal class AimFOVPatches
{
    public static bool UpdateAimingPrefix(PlayerLook __instance)
    {
        if (SparrohPlugin.disableFOVChange.Value && SparrohPlugin.isAimingPLField != null && SparrohPlugin.aimStateChangeTimeField != null && SparrohPlugin.aimDurationPLField != null && SparrohPlugin.aimFOVPLField != null && SparrohPlugin.defaultFOVGetter != null && SparrohPlugin.fovField != null)
        {
            bool isAiming = (bool)SparrohPlugin.isAimingPLField.GetValue(__instance);
            if (isAiming)
            {
                float aimStateChangeTime = (float)SparrohPlugin.aimStateChangeTimeField.GetValue(__instance);
                aimStateChangeTime = Mathf.Min(aimStateChangeTime + Time.deltaTime / (float)SparrohPlugin.aimDurationPLField.GetValue(__instance), 1f);
                SparrohPlugin.aimStateChangeTimeField.SetValue(__instance, aimStateChangeTime);
                float defaultFOV = (float)SparrohPlugin.defaultFOVGetter.Invoke(__instance, null);
                SparrohPlugin.aimFOVPLField.SetValue(__instance, defaultFOV);
                SparrohPlugin.fovField.SetValue(__instance, Mathf.LerpUnclamped(defaultFOV, defaultFOV, EaseFunctions.EaseInOutCubic(aimStateChangeTime)));
            }
            else if ((float)SparrohPlugin.aimStateChangeTimeField.GetValue(__instance) > 0f)
            {
                float aimStateChangeTime = (float)SparrohPlugin.aimStateChangeTimeField.GetValue(__instance);
                aimStateChangeTime = Mathf.Max(aimStateChangeTime - Time.deltaTime / (float)SparrohPlugin.aimDurationPLField.GetValue(__instance), 0f);
                SparrohPlugin.aimStateChangeTimeField.SetValue(__instance, aimStateChangeTime);
                float defaultFOV = (float)SparrohPlugin.defaultFOVGetter.Invoke(__instance, null);
                SparrohPlugin.aimFOVPLField.SetValue(__instance, defaultFOV);
                SparrohPlugin.fovField.SetValue(__instance, Mathf.LerpUnclamped(defaultFOV, defaultFOV, EaseFunctions.EaseInOutCubic(aimStateChangeTime)));
            }
            return false;
        }
        return true;
    }

    public static void UpdateCameraFOVPostfix(PlayerLook __instance)
    {
        if (SparrohPlugin.disableFOVChange.Value && SparrohPlugin.isAimingPLField != null && SparrohPlugin.fovField != null && SparrohPlugin.defaultFOVGetter != null)
        {
            object isAimingObj = SparrohPlugin.isAimingPLField.GetValue(__instance);
            if (isAimingObj is bool isAiming && isAiming)
            {
                SparrohPlugin.fovField.SetValue(__instance, SparrohPlugin.defaultFOVGetter.Invoke(__instance, null));
            }
        }
    }
}

[HarmonyPatch(typeof(PlayerLook), "AddFOV")]
internal class SprintFOVPatches
{
    public static bool Prefix(ref float value)
    {
        if (SparrohPlugin.disableSprintFOV != null && !SparrohPlugin.disableSprintFOV.Value)
            return true;

        StackTrace stackTrace = new StackTrace();

        foreach (var frame in stackTrace.GetFrames())
        {
            var method = frame.GetMethod();
            if (method?.ReflectedType?.Name == "Player" && method.Name.Contains("Update"))
            {
                return false;
            }
        }
        return true;
    }
}

internal class ToggleAimPatches
{
    public static void PlayerInputInitializePostfix()
    {
        SparrohPlugin.aimAction = PlayerInput.Controls?.Player.Aim;
        SparrohPlugin.ConfigureAimSubscription();
    }

    public static bool SkipPrefix()
    {
        return !SparrohPlugin.enableToggleAim.Value;
    }

    public static void HandleAimPrefix(Gun __instance)
    {
        if (SparrohPlugin.enableToggleAim.Value)
        {
            bool prevHeld = (bool)SparrohPlugin.isAimInputHeldField.GetValue(__instance);
            SparrohPlugin.isAimInputHeldField.SetValue(__instance, SparrohPlugin.isAimToggled);
            if (SparrohPlugin.isAimToggled)
            {
                SparrohPlugin.lastPressedAimTimeField.SetValue(__instance, Time.time);
            }
        }
    }

    public static void UpdatePostfix(Gun __instance)
    {
        if (SparrohPlugin.enableToggleAim.Value)
        {
            bool isAiming = (bool)SparrohPlugin.isAimingGetter.Invoke(__instance, null);
            bool wantsToFire = (bool)SparrohPlugin.wantsToFireGetter.Invoke(__instance, null);
            float lastFireTime = (float)SparrohPlugin.lastFireTimeGetter.Invoke(__instance, null);
            float lastPressedFireTime = (float)SparrohPlugin.lastPressedFireTimeField.GetValue(__instance);
            Player player = (Player)SparrohPlugin.playerField.GetValue(__instance);
            if (player != null && !isAiming && !wantsToFire && Time.time - Mathf.Max(lastFireTime, lastPressedFireTime) > 0.5f)
            {
                player.ResumeSprint();
            }
        }
    }

    public static void ResetTogglePostfix()
    {
        SparrohPlugin.isAimToggled = false;
    }
}

[HarmonyPatch(typeof(Player), "EndCrouch")]
internal class EndCrouchPatch
{
    static bool Prefix()
    {
        return !(SparrohPlugin.enableToggleCrouch.Value && ToggleCrouchPatches.isToggleOn);
    }
}

[HarmonyPatch(typeof(Player), "EndSlide")]
internal class EndSlidePatch
{
    static bool Prefix()
    {
        return !(SparrohPlugin.enableToggleCrouch.Value && ToggleCrouchPatches.isToggleOn);
    }
}

[HarmonyPatch(typeof(Player), "Update")]
internal class ToggleCrouchPatches
{
    internal static bool isToggleOn = false;

    static void Prefix(Player __instance)
    {
        if (SparrohPlugin.enableToggleCrouch.Value && PlayerInput.Controls.Player.Slide.WasPressedThisFrame())
        {
            var crouchingProp = typeof(Player).GetProperty("Crouching");
            var slidingProp = typeof(Player).GetProperty("Sliding");
            bool isCrouching = (bool)crouchingProp.GetValue(__instance);
            bool isSliding = (bool)slidingProp.GetValue(__instance);

            if (isCrouching || isSliding || isToggleOn)
            {
                isToggleOn = false;
            }
            else
            {
                isToggleOn = true;
            }
        }
    }
}
