using HarmonyLib;
using UnityEngine;

internal static class RegionBypassPatches
{

    [HarmonyPatch(typeof(Option), "OnEnable")]
    [HarmonyPrefix]
    private static void Option_OnEnable_Prefix(Option __instance)
    {
        if (__instance.ID == "LobbyDistance")
        {
            Traverse.Create(__instance).Field<float>("maxValue").Value = 3f;

            Transform scrollBarTransform = __instance.gameObject.transform.Find("ScrollRect/ScrollBar");
            if (scrollBarTransform != null)
            {
                ScrollBar scrollBar = scrollBarTransform.GetComponent<ScrollBar>();
                if (scrollBar != null)
                {
                    scrollBar.SetSnapPoints(4, 0);
                }
            }
        }
    }

    [HarmonyPatch(typeof(Option), "SaveValue", typeof(float))]
    [HarmonyPostfix]
    private static void Option_SaveValue_Postfix(Option __instance, float value)
    {
        if (__instance.ID == "LobbyDistance")
        {
            int rounded = Mathf.RoundToInt(value);
            string label = rounded switch
            {
                0 => "Close",
                1 => "Medium",
                2 => "Far",
                3 => "Worldwide",
                _ => "Unknown"
            };
            Transform numTransform = __instance.transform.Find("Num");
            if (numTransform != null)
            {
                TMPro.TextMeshProUGUI numText = numTransform.GetComponent<TMPro.TextMeshProUGUI>();
                if (numText != null)
                {
                    numText.text = label;
                }
            }
        }
    }
}
