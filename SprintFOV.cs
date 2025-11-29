using HarmonyLib;
using Pigeon.Movement;
using System.Diagnostics;

[HarmonyPatch]
public static class SprintFOVPatches
{
    [HarmonyPatch(typeof(PlayerLook), "AddFOV")]
    [HarmonyPrefix]
    public static bool Prefix(ref float value)
    {
        if (SparrohPlugin.sprintFOVChange.Value)
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
