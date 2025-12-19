using HarmonyLib;
using DropPodFields = DisableCountdown.DropPodFields;

internal static class DisableCountdown
{
    internal static class DropPodFields
    {
        public static readonly System.Reflection.FieldInfo LaunchCountdownTime = typeof(DropPod).GetField("launchCountdownTime", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
    }

    [HarmonyPatch(typeof(DropPod), "StartCountdown_ClientRpc")]
    [HarmonyPrefix]
    private static void SkipDropPodLaunchCountdown(DropPod __instance)
    {
        if (SparrohPlugin.skipMissionCountdown.Value)
        {
            if (DropPodFields.LaunchCountdownTime != null)
            {
                DropPodFields.LaunchCountdownTime.SetValue(__instance, 0f);
            }
        }
    }
}
