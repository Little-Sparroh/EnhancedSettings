using HarmonyLib;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MycopunkSkipIntro
{
    internal static class IntroSkip
    {
    


    internal class IntroPatches
    {
        [HarmonyPatch(typeof(StartMenu), "Awake")]
        [HarmonyPrefix]
        private static bool SkipIntroPrefix(StartMenu __instance)
        {
            if (!SparrohPlugin.skipIntro.Value)
                return true;

            var staticTraverse = Traverse.Create(typeof(StartMenu));
            bool hasInitialized = staticTraverse.Field("hasInitialized").GetValue<bool>();

            if (!hasInitialized)
            {
                try
                {
                    var instanceTraverse = Traverse.Create(__instance);

                    staticTraverse.Field("hasInitialized").SetValue(true);

                    Type gameManagerType = AccessTools.TypeByName("GameManager");
                    if (gameManagerType != null)
                    {
                        object settingsWin = instanceTraverse.Field("settingsWindow").GetValue();
                        Window window = settingsWin as Window;
                        if (window != null)
                        {
                            Traverse.Create(gameManagerType).Method("InitializeSettingRecursive", new Type[] { typeof(Transform) }).GetValue(window.transform);
                        }
                    }

                    object initScreen = instanceTraverse.Field("initializeScreen").GetValue();
                    GameObject gameObj = initScreen as GameObject;
                    if (gameObj != null) gameObj.SetActive(false);

                    object startScr = instanceTraverse.Field("startScreen").GetValue();
                    RectTransform rect = startScr as RectTransform;
                    if (rect != null) rect.gameObject.SetActive(true);

                    object bootScreen = instanceTraverse.Field("bootingScreen").GetValue();
                    GameObject bs = bootScreen as GameObject;
                    if (bs != null) bs.SetActive(false);

                    object splashScr = instanceTraverse.Field("splashScreen").GetValue();
                    GameObject ss = splashScr as GameObject;
                    if (ss != null) ss.SetActive(false);

                    object initBar = instanceTraverse.Field("initializeBar").GetValue();
                    GameObject ib = initBar as GameObject;
                    if (ib != null) ib.SetActive(false);

                    object logTxt = instanceTraverse.Field("logText").GetValue();
                    TextMeshProUGUI tm = logTxt as TextMeshProUGUI;
                    if (tm != null) tm.gameObject.SetActive(false);

                    object initStuff = instanceTraverse.Field("initializeStuff").GetValue();
                    GameObject isf = initStuff as GameObject;
                    if (isf != null) isf.SetActive(false);

                    object verTxt = instanceTraverse.Field("verifiedText").GetValue();
                    TextMeshProUGUI vt = verTxt as TextMeshProUGUI;
                    if (vt != null) vt.gameObject.SetActive(false);

                    object loadBar = instanceTraverse.Field("loadingBar").GetValue();
                    Image lb = loadBar as Image;
                    if (lb != null) lb.gameObject.SetActive(false);

                    object initWipeObj = instanceTraverse.Field("initializeWipe").GetValue();
                    RectTransform initializeWipe = initWipeObj as RectTransform;
                    if (initializeWipe != null)
                    {
                        initializeWipe.anchoredPosition = new Vector2(0f, -2160f);
                    }

                    object wipeChildObj = instanceTraverse.Field("wipeChild").GetValue();
                    RectTransform wipeChild = wipeChildObj as RectTransform;
                    if (wipeChild != null)
                    {
                        wipeChild.anchoredPosition = new Vector2(0f, 2160f);
                    }

                    Type playerDataType = AccessTools.TypeByName("PlayerData");
                    if (playerDataType != null)
                    {
                        object playerData = Traverse.Create(playerDataType).Field("Instance").GetValue();
                        if (playerData != null)
                        {
                            object profileConfig = Traverse.Create(playerData).Property("ProfileConfig").GetValue();
                            if (profileConfig != null)
                            {
                                object currentProfile = Traverse.Create(profileConfig).Property("CurrentProfile").GetValue();
                                if (currentProfile != null)
                                {
                                    bool isValid = (bool)Traverse.Create(currentProfile).Method("IsValid").GetValue();
                                    int autohubFlag = (int)Traverse.Create(playerData).Method("GetFlag", new Type[] { typeof(string) }).GetValue();

                                    if (!isValid && autohubFlag == 0)
                                    {
                                        Traverse.Create(playerData).Method("SetFlag", new Type[] { typeof(string), typeof(int) }).GetValue("autohub", 1);
                                        instanceTraverse.Method("Host").GetValue();
                                    }
                                }
                            }
                        }
                    }
                    if (initializeWipe != null)
                    {
                        initializeWipe.gameObject.SetActive(false);
                    }

                    Type playerInputType = AccessTools.TypeByName("PlayerInput");
                    if (playerInputType != null)
                    {
                        Traverse.Create(playerInputType).Method("UnlockCursor").GetValue();
                    }
                }
                catch (Exception ex)
                {
                    SparrohPlugin.Logger.LogError($"Error in SkipIntroPrefix: {ex}");
                }
            }
            return true;
        }

        [HarmonyPatch(typeof(StartMenu), "Awake")]
        [HarmonyPostfix]
        private static void SkipIntroPostfix(StartMenu __instance)
        {
            try
            {
                if (!SparrohPlugin.skipIntro.Value)
                    return;

                Traverse staticTraverse = Traverse.Create(typeof(StartMenu));
                bool hasInitialized = staticTraverse.Field("hasInitialized").GetValue<bool>();

                if (hasInitialized)
                {
                    Traverse instanceTraverse = Traverse.Create(__instance);
                    bool isMusicPlaying = instanceTraverse.Field("isMusicPlaying").GetValue<bool>();

                    if (!isMusicPlaying)
                    {
                        Type globalType = AccessTools.TypeByName("Global");
                        if (globalType != null)
                        {
                            var prop = globalType.GetProperty("AreAllBanksLoaded");
                            if (prop != null)
                            {
                                bool banksLoaded = (bool)prop.GetValue(null, null);
                                if (banksLoaded)
                                {
                                    object musicObj = instanceTraverse.Field("music").GetValue();
                                    if (musicObj != null && musicObj.GetType().ToString().Contains("AK.Wwise.Event"))
                                    {
                                        Traverse.Create(musicObj).Method("Post", new Type[] { typeof(GameObject) }).GetValue(__instance.gameObject);
                                        instanceTraverse.Field("isMusicPlaying").SetValue(true);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                SparrohPlugin.Logger.LogError($"Error in SkipIntroPostfix: {ex}");
            }
        }
    }
    }
}
