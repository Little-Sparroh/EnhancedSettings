using HarmonyLib;
using Pigeon.Movement;
using UnityEngine;

[HarmonyPatch]
public static class ToggleAimPatches
{
    [HarmonyPatch(typeof(PlayerInput), "Initialize")]
    [HarmonyPostfix]
    public static void PlayerInputInitializePostfix()
    {
        SparrohPlugin.aimAction = PlayerInput.Controls?.Player.Aim;
        SparrohPlugin.ConfigureAimSubscription();
    }

    [HarmonyPatch(typeof(Gun), "OnAimInputPerformed")]
    [HarmonyPrefix]
    public static bool SkipPrefix()
    {
        return !SparrohPlugin.toggleAim.Value;
    }

    [HarmonyPatch(typeof(Gun), "OnAimInputCancelled")]
    [HarmonyPrefix]
    public static bool SkipPrefixCancelled()
    {
        return !SparrohPlugin.toggleAim.Value;
    }

    [HarmonyPatch(typeof(Gun), "HandleAim")]
    [HarmonyPrefix]
    public static void HandleAimPrefix(Gun __instance)
    {
        if (SparrohPlugin.toggleAim.Value)
        {
            bool prevHeld = (bool)SparrohPlugin.isAimInputHeldField.GetValue(__instance);
            SparrohPlugin.isAimInputHeldField.SetValue(__instance, SparrohPlugin.isAimToggled);
            if (SparrohPlugin.isAimToggled)
            {
                SparrohPlugin.lastPressedAimTimeField.SetValue(__instance, Time.time);
            }
        }
    }

    [HarmonyPatch(typeof(Gun), "Update")]
    [HarmonyPostfix]
    public static void UpdatePostfix(Gun __instance)
    {
        if (SparrohPlugin.toggleAim.Value)
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

    [HarmonyPatch(typeof(Player), "Resurrect_ClientRpc")]
    [HarmonyPostfix]
    public static void ResetTogglePostfix()
    {
        SparrohPlugin.isAimToggled = false;
    }
}
