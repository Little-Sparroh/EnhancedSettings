using HarmonyLib;
using Pigeon.Movement;

[HarmonyPatch]
public static class ToggleCrouchPatches
{
    public static bool isToggleOn = false;

    [HarmonyPatch(typeof(Player), "Update")]
    [HarmonyPrefix]
    public static void Prefix(Player __instance)
    {
        if (SparrohPlugin.toggleCrouch.Value && PlayerInput.Controls.Player.Slide.WasPressedThisFrame())
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

[HarmonyPatch]
public static class EndCrouchPatches
{
    [HarmonyPatch(typeof(Player), "EndCrouch")]
    [HarmonyPrefix]
    static bool Prefix()
    {
        return !(SparrohPlugin.toggleCrouch.Value && ToggleCrouchPatches.isToggleOn);
    }
}

[HarmonyPatch]
public static class EndSlidePatches
{
    [HarmonyPatch(typeof(Player), "EndSlide")]
    [HarmonyPrefix]
    static bool Prefix()
    {
        return !(SparrohPlugin.toggleCrouch.Value && ToggleCrouchPatches.isToggleOn);
    }
}
