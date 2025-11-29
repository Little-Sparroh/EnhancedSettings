using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;

[HarmonyPatch]
public static class BounceShotgunPatches
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(BounceShotgun), "OnActiveUpdate")]
    public static void OnActiveUpdate_Postfix(BounceShotgun __instance)
    {
        if (!SparrohPlugin.showJackrabbitBounceIndicators.Value)
        {
            var bounceLines = Traverse.Create(__instance).Field<List<LineRenderer>>("bounceLines").Value;
            if (bounceLines != null)
            {
                for (int i = 0; i < bounceLines.Count; i++)
                {
                    if (bounceLines[i].gameObject.activeSelf)
                    {
                        bounceLines[i].gameObject.SetActive(false);
                    }
                }
            }
        }
    }
}
