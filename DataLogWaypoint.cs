using System;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Reflection;
using Object = UnityEngine.Object;

public static class DataLogWaypointPatches
{
    private static Dictionary<string, Transform> datalogPings = new Dictionary<string, Transform>();
    private static FieldInfo logIDField;

    public static void Initialize()
    {
        try
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            logIDField = typeof(TextLogInteractable).GetField("logID", BindingFlags.NonPublic | BindingFlags.Instance);
            var playerDataOnDataLogOpened = typeof(PlayerData).GetMethod("OnDataLogOpened", BindingFlags.Public | BindingFlags.Instance);
            var postfix = typeof(DataLogWaypointPatches).GetMethod("PlayerDataOnDataLogOpened_Postfix", BindingFlags.Public | BindingFlags.Static);
            if (playerDataOnDataLogOpened != null && postfix != null)
            {
                var harmony = new Harmony("sparroh.enhancedsettings");
                harmony.Patch(playerDataOnDataLogOpened, postfix: new HarmonyMethod(postfix));
            }
            else
            {
            }
        }
        catch (Exception ex)
        {
            SparrohPlugin.Logger.LogError($"Error initializing DataLogWaypointPatches: {ex.Message}");
        }
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        try
        {
            var harmony = new Harmony("sparroh.enhancedsettings");
            var textSetup = typeof(TextLogWindow).GetMethod("Setup", new System.Type[] { typeof(string) });
            var imageSetup = typeof(ImageLogWindow).GetMethod("Setup", new System.Type[] { typeof(string) });
            var textPostfix = typeof(DataLogWaypointPatches).GetMethod("TextLogWindowSetup_Postfix", BindingFlags.Public | BindingFlags.Static);
            var imagePostfix = typeof(DataLogWaypointPatches).GetMethod("ImageLogWindowSetup_Postfix", BindingFlags.Public | BindingFlags.Static);
            if (textSetup != null && imageSetup != null && textPostfix != null && imagePostfix != null)
            {
                harmony.Patch(textSetup, postfix: new HarmonyMethod(textPostfix));
                harmony.Patch(imageSetup, postfix: new HarmonyMethod(imagePostfix));
            }
            else
            {
            }

            if (SparrohPlugin.dataLogWaypoints.Value)
            {
                var interactables = Object.FindObjectsOfType<TextLogInteractable>();
                int added = 0;
                foreach (var tli in interactables)
                {
                    string logID = logIDField.GetValue(tli) as string;
                    if (logID != null && PlayerData.Instance != null && PlayerData.Instance.discoveredDataLogs != null && !PlayerData.Instance.discoveredDataLogs.Contains(logID))
                    {
                        Highlighter.Instance?.AddWaypointPing(tli.transform);
                        datalogPings[logID] = tli.transform;
                        added++;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            SparrohPlugin.Logger.LogError($"Error in OnSceneLoaded: {ex.Message}");
        }
    }

    public static void TextLogWindowSetup_Postfix(string id)
    {
        try
        {
            if (!SparrohPlugin.dataLogWaypoints.Value) return;
            if (datalogPings.TryGetValue(id, out var target))
            {
                Highlighter.Instance?.RemovePing(target);
                datalogPings.Remove(id);
            }
        }
        catch (Exception ex)
        {
            SparrohPlugin.Logger.LogError($"Error in TextLogWindowSetup_Postfix: {ex.Message}");
        }
    }

    public static void ImageLogWindowSetup_Postfix(string id)
    {
        try
        {
            if (!SparrohPlugin.dataLogWaypoints.Value) return;
            if (datalogPings.TryGetValue(id, out var target))
            {
                Highlighter.Instance?.RemovePing(target);
                datalogPings.Remove(id);
            }
        }
        catch (Exception ex)
        {
            SparrohPlugin.Logger.LogError($"Error in ImageLogWindowSetup_Postfix: {ex.Message}");
        }
    }

    public static void PlayerDataOnDataLogOpened_Postfix(string id)
    {
        try
        {
            if (!SparrohPlugin.dataLogWaypoints.Value) return;
            if (datalogPings.TryGetValue(id, out var target))
            {
                Highlighter.Instance?.RemovePing(target);
                datalogPings.Remove(id);
            }
        }
        catch (Exception ex)
        {
            SparrohPlugin.Logger.LogError($"Error in PlayerDataOnDataLogOpened_Postfix: {ex.Message}");
        }
    }
}
