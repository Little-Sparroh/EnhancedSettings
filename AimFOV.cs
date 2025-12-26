using System;
using HarmonyLib;
using Pigeon.Movement;
using Pigeon.Math;
using UnityEngine;

[HarmonyPatch]
public static class AimFOVPatches
{
    [HarmonyPatch(typeof(PlayerLook), "UpdateAiming")]
    [HarmonyPrefix]
    public static bool UpdateAimingPrefix(PlayerLook __instance)
    {
        try
        {
            if (!SparrohPlugin.aimFOVChange.Value && SparrohPlugin.isAimingPLField != null && SparrohPlugin.aimStateChangeTimeField != null && SparrohPlugin.aimDurationPLField != null && SparrohPlugin.aimFOVPLField != null && SparrohPlugin.defaultFOVGetter != null && SparrohPlugin.fovField != null)
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
        catch (Exception ex)
        {
            SparrohPlugin.Logger.LogError($"Error in UpdateAimingPrefix: {ex.Message}");
            return true;
        }
    }

    [HarmonyPatch(typeof(PlayerLook), "UpdateCameraFOV")]
    [HarmonyPostfix]
    public static void UpdateCameraFOVPostfix(PlayerLook __instance)
    {
        try
        {
            if (!SparrohPlugin.aimFOVChange.Value && SparrohPlugin.isAimingPLField != null && SparrohPlugin.fovField != null && SparrohPlugin.defaultFOVGetter != null)
            {
                object isAimingObj = SparrohPlugin.isAimingPLField.GetValue(__instance);
                if (isAimingObj is bool isAiming && isAiming)
                {
                    SparrohPlugin.fovField.SetValue(__instance, SparrohPlugin.defaultFOVGetter.Invoke(__instance, null));
                }
            }
        }
        catch (Exception ex)
        {
            SparrohPlugin.Logger.LogError($"Error in UpdateCameraFOVPostfix: {ex.Message}");
        }
    }
}
