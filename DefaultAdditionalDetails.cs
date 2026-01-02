using HarmonyLib;
using Pigeon;

internal static class DefaultAdditionalDetailsPatches
{
    [HarmonyPatch(typeof(HoverInfo), nameof(HoverInfo.ShowExtraInfo))]
    [HarmonyPrefix]
    public static bool ShowExtraInfoPrefix(ref bool __result)
    {
        if (SparrohPlugin.showDefaultAdditionalDetails.Value)
        {
            __result = true;
            return false;
        }
        return true;
    }
}
