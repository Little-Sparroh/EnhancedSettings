using System;
using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Unity.Netcode;


internal static class AllBounceIndicators
{
    internal static Dictionary<Gun, List<LineRenderer>> BounceLines = new Dictionary<Gun, List<LineRenderer>>();
    internal static Dictionary<Gun, int> CachedBounces = new Dictionary<Gun, int>();
    internal static LineRenderer BounceLinePrefab;



    private static Color GetSelectedColor()
    {
        if (SparrohPlugin.enableOrange.Value) return new Color(1f, 0.5f, 0f);
        if (SparrohPlugin.enableWhite.Value) return new Color(1f, 1f, 1f);
        if (SparrohPlugin.enableGreen.Value) return new Color(0f, 1f, 0f);
        if (SparrohPlugin.enableBlue.Value) return new Color(0f, 0f, 1f);
        if (SparrohPlugin.enableRed.Value) return new Color(1f, 0f, 0f);
        if (SparrohPlugin.enableYellow.Value) return new Color(1f, 1f, 0f);
        if (SparrohPlugin.enablePurple.Value) return new Color(1f, 0f, 1f);
        if (SparrohPlugin.enableCyan.Value) return new Color(0f, 1f, 1f);

        return new Color(1f, 0f, 0f);
    }

    public static LineRenderer CreateBounceLinePrefab()
    {
        var lineObj = new GameObject("BounceIndicatorPrefab");
        var lineRenderer = lineObj.AddComponent<LineRenderer>();

        Color indicatorColor = GetSelectedColor();

        var material = new Material(Shader.Find("Sprites/Default"));
        material.color = indicatorColor;

        Color emissionColor = indicatorColor * 0.5f;
        emissionColor.a = 1f;
        material.SetColor("_EmissionColor", emissionColor);

        lineRenderer.material = material;

        lineRenderer.startWidth = 0.01f;
        lineRenderer.endWidth = 0.01f;
        lineRenderer.startColor = indicatorColor;
        lineRenderer.endColor = indicatorColor;
        lineRenderer.positionCount = 2;
        lineRenderer.useWorldSpace = true;

        lineObj.SetActive(false);

        UnityEngine.Object.DontDestroyOnLoad(lineObj);

        return lineRenderer;
    }
}




[HarmonyPatch]
public static class GunBouncePatches
{
    private static readonly PropertyInfo IsOwnerProperty = typeof(NetworkBehaviour).GetProperty("IsOwner");
    private static readonly PropertyInfo ActiveProperty = typeof(Gun).GetProperty("Active");

    private static int GetBounces(Gun gun)
    {
        string gunType = gun.GetType().Name;

        try
        {
            var gunDataType = gun.GunData.GetType();

            var field = gunDataType.GetField("bounces", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (field != null)
            {
                var value = field.GetValue(gun.GunData);
                return (int)value;
            }

            var prop = gunDataType.GetProperty("bounces", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (prop != null)
            {
                var value = prop.GetValue(gun.GunData);
                return (int)value;
            }

            var possibleFields = gunDataType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            foreach (var f in possibleFields)
            {
                if (f.Name.ToLower().Contains("bounce") || f.Name.ToLower().Contains("ricochet"))
                {
                    var value = f.GetValue(gun.GunData);
                    if (value is int intVal) return intVal;
                    if (value is float floatVal) return (int)floatVal;
                }
            }

            IWeapon weapon = (IWeapon)gun;
            UpgradeStatChanges statChanges = new UpgradeStatChanges();
            var secondaryEnum = weapon.EnumerateSecondaryStats(statChanges);
            int statIndex = 0;
            while (secondaryEnum.MoveNext())
            {
                if (secondaryEnum.Current.name.ToLower().Contains("bounce"))
                {
                    float value = float.TryParse(secondaryEnum.Current.value, out float v) ? v : 0;
                    return (int)value;
                }
                statIndex++;
            }

            var primaryEnum = weapon.EnumeratePrimaryStats(statChanges);
            statIndex = 0;
            while (primaryEnum.MoveNext())
            {
                if (primaryEnum.Current.name.ToLower().Contains("bounce"))
                {
                    float value = float.TryParse(primaryEnum.Current.value, out float v) ? v : 0;
                    return (int)value;
                }
                statIndex++;
            }
        }
        catch (Exception ex)
        {
            SparrohPlugin.Logger.LogInfo($"  ERROR: GetBounces exception: {ex.Message}");
        }

        return 0;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(Gun), "Enable")]
    public static void Enable_Postfix(Gun __instance)
    {
        int bounces = GetBounces(__instance);
        AllBounceIndicators.CachedBounces[__instance] = bounces;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(Gun), "Update")]
    public static void Update_Postfix(Gun __instance)
    {
        try {
            if (!((bool)IsOwnerProperty.GetValue(__instance)) || !((bool)ActiveProperty.GetValue(__instance)))
                return;

            HandleBounceIndicators(__instance);
        } catch (Exception ex) {
            SparrohPlugin.Logger.LogInfo($"ERROR: Update postfix failed: {ex.Message}");
        }
    }

    private static void HandleBounceIndicators(Gun gun)
    {
        if (!AllBounceIndicators.CachedBounces.TryGetValue(gun, out int bounces))
        {
            bounces = 0;
        }
        bool configEnabled = SparrohPlugin.enableAllBounceIndicators.Value;
        string gunType = gun.GetType().Name;

        if (!configEnabled || bounces < 1) {
            return;
        }

        if (!AllBounceIndicators.BounceLines.ContainsKey(gun))
        {
            AllBounceIndicators.BounceLines[gun] = new List<LineRenderer>();
        }
        var bounceLines = AllBounceIndicators.BounceLines[gun];

        if (AllBounceIndicators.BounceLinePrefab == null) {
            AllBounceIndicators.BounceLinePrefab = AllBounceIndicators.CreateBounceLinePrefab();
        }

        int bulletsPerShot = gun.GunData.bulletsPerShot;
        int totalSegmentsNeeded = bulletsPerShot * bounces;

        while (bounceLines.Count < totalSegmentsNeeded)
        {
            var line = UnityEngine.Object.Instantiate(AllBounceIndicators.BounceLinePrefab);
            bounceLines.Add(line);
        }

        var traverse = Traverse.Create(gun);
        traverse.Method("PrepareFireData").GetValue();

        var playerObj = traverse.Field("player").GetValue();
        bool isSprinting = (bool)playerObj?.GetType().GetProperty("IsSprinting")?.GetValue(playerObj);
        bool isFireLocked = (bool)playerObj?.GetType().GetProperty("IsFireLocked")?.GetValue(playerObj);

        if (isSprinting || isFireLocked)
        {
            for (int i = 0; i < bounceLines.Count; i++)
            {
                if (bounceLines[i].gameObject.activeSelf)
                {
                    bounceLines[i].gameObject.SetActive(false);
                }
            }
        }
        else
        {
            UpdateBounceLines(gun);
        }
    }



    [HarmonyPostfix]
    [HarmonyPatch(typeof(Gun), "Disable")]
    public static void Disable_Postfix(Gun __instance)
    {
        if (AllBounceIndicators.BounceLines.TryGetValue(__instance, out var bounceLines))
        {
            for (int i = 0; i < bounceLines.Count; i++)
            {
                bounceLines[i].gameObject.SetActive(false);
            }
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(Gun), "OnDestroy")]
    public static void OnDestroy_Postfix(Gun __instance)
    {
        if (AllBounceIndicators.BounceLines.TryGetValue(__instance, out var bounceLines))
        {
            for (int i = 0; i < bounceLines.Count; i++)
            {
                if (bounceLines[i] != null)
                {
                    UnityEngine.Object.Destroy(bounceLines[i].gameObject);
                }
            }
            AllBounceIndicators.BounceLines.Remove(__instance);
        }
        AllBounceIndicators.CachedBounces.Remove(__instance);
    }

    private static Vector3 CalculateSpreadDirection(Vector3 baseDirection, int bulletIndex, int totalBullets, Gun gun)
    {
        if (totalBullets <= 1) return baseDirection;

        try {
            var gunData = gun.GunData;
            var spreadData = gunData.GetType().GetField("spreadData")?.GetValue(gunData);

            if (spreadData != null) {
                var spreadDataType = spreadData.GetType();

                var allMethods = spreadDataType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                var methodNames = new List<string>();
                foreach (var method in allMethods) methodNames.Add(method.Name);

                var allFields = spreadDataType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                var fieldNames = new List<string>();
                foreach (var field in allFields) fieldNames.Add(field.Name);

                Vector3? attemptedDirection = null;

                var getSpreadMethods = new List<MethodInfo>();
                foreach (var method in allMethods) {
                    if (method.Name.Contains("Spread")) getSpreadMethods.Add(method);
                }
                foreach (var method in getSpreadMethods) {
                    try {
                        var parameters = method.GetParameters();
                        object[] args = null;

                        if (parameters.Length == 2 && parameters[0].ParameterType == typeof(int) && parameters[1].ParameterType == typeof(int)) {
                            args = new object[] { bulletIndex, totalBullets };
                        } else if (parameters.Length == 3 && parameters[0].ParameterType == typeof(Vector3) && parameters[1].ParameterType == typeof(int) && parameters[2].ParameterType == typeof(int)) {
                            args = new object[] { baseDirection, bulletIndex, totalBullets };
                        }

                        if (args != null) {
                            var result = method.Invoke(spreadData, args);
                            if (result is Vector3 spreadDir) {
                                return spreadDir.normalized;
                            }
                        }
                    } catch (Exception e) {
                        SparrohPlugin.Logger.LogInfo($"Method {method.Name} failed: {e.Message}");
                    }
                }

                var spreadAngleProp = spreadDataType.GetProperty("spreadAngle");
                var spreadAngleField = spreadDataType.GetField("spreadAngle");
                float spreadAngle = 0;

                if (spreadAngleProp != null) {
                    spreadAngle = (float)spreadAngleProp.GetValue(spreadData);
                } else if (spreadAngleField != null) {
                    spreadAngle = (float)spreadAngleField.GetValue(spreadData);
                }

                if (spreadAngle > 0) {
                    float maxRadius = spreadAngle * Mathf.Deg2Rad;

                    float theta = bulletIndex * (2 * Mathf.PI) / totalBullets;
                    float phi = (bulletIndex * maxRadius) / totalBullets;

                    float r = phi;
                    float y = r * Mathf.Sin(theta);
                    float x = r * Mathf.Cos(theta);

                    Vector3 localSpread = new Vector3(x, y, 1f).normalized;
                    Vector3 worldSpread = Quaternion.LookRotation(baseDirection) * localSpread;

                    return worldSpread;
                }

                var calculateSpreadMethod = spreadDataType.GetMethod("CalculateSpread", bindingAttr: BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (calculateSpreadMethod != null) {
                    var result = calculateSpreadMethod.Invoke(spreadData, new object[] { baseDirection, bulletIndex, totalBullets, gun });
                    if (result is Vector3 calculatedDir) {
                        return calculatedDir.normalized;
                    }
                }

            }
        } catch (Exception ex) {
            SparrohPlugin.Logger.LogInfo($"Error examining spread data: {ex.Message}");
        }

        float fallbackSpreadAngle = 15f;
        float fallbackMaxRadius = fallbackSpreadAngle * Mathf.Deg2Rad;

        float fallbackTheta = bulletIndex * (2 * Mathf.PI) / totalBullets;
        float fallbackPhi = (bulletIndex + 1) * fallbackMaxRadius / totalBullets;

        float sinFallbackPhi = Mathf.Sin(fallbackPhi);
        float cosFallbackPhi = Mathf.Cos(fallbackPhi);

        Vector3 fallbackLocalSpread = new Vector3(sinFallbackPhi * Mathf.Cos(fallbackTheta), sinFallbackPhi * Mathf.Sin(fallbackTheta), cosFallbackPhi).normalized;
        Vector3 fallbackWorldSpread = Quaternion.LookRotation(baseDirection) * fallbackLocalSpread;

        return fallbackWorldSpread;
    }

    private static void UpdateBounceLines(Gun gun)
    {
        if (!AllBounceIndicators.BounceLines.TryGetValue(gun, out var bounceLines)) return;


        try {
            var traverse = Traverse.Create(gun);
            var fireData = traverse.Field("fireData").GetValue();
            var firePosition = (Vector3)fireData.GetType().GetField("firePosition").GetValue(fireData);
            var bulletRotation = (Quaternion)fireData.GetType().GetField("bulletRotation").GetValue(fireData);
            var bulletsPerShot = gun.GunData.bulletsPerShot;

            int maxBounces = 0;
            if (AllBounceIndicators.CachedBounces.TryGetValue(gun, out int bouncesValue))
            {
                maxBounces = bouncesValue;
            }

            int lineIndex = 0;

            for (int bulletIndex = 0; bulletIndex < bulletsPerShot && lineIndex < bounceLines.Count; bulletIndex++)
            {
                Vector3 currentPosition = firePosition;
                Vector3 baseDirection = bulletRotation * Vector3.forward;
                Vector3 currentDirection = CalculateSpreadDirection(baseDirection, bulletIndex, bulletsPerShot, gun);

                Vector3 lastDrawPosition = firePosition;

                if (Physics.Raycast(currentPosition, currentDirection, out RaycastHit firstHit, 50f, LayerMask.GetMask("Default", "Terrain", "Environment")))
                {
                    Vector3 firstHitPoint = firstHit.point;
                    currentPosition = firstHitPoint;
                    currentDirection = Vector3.Reflect(currentDirection, firstHit.normal);

                    for (int bounceIndex = 0; bounceIndex < maxBounces && lineIndex < bounceLines.Count; bounceIndex++)
                    {
                        lastDrawPosition = currentPosition;

                        if (Physics.Raycast(currentPosition, currentDirection, out RaycastHit bounceHit, 50f, LayerMask.GetMask("Default", "Terrain", "Environment")))
                        {
                            Vector3 bounceHitPoint = bounceHit.point;
                            Vector3[] segmentPositions = { lastDrawPosition, bounceHitPoint };

                            bounceLines[lineIndex].SetPositions(segmentPositions);
                            bounceLines[lineIndex].gameObject.SetActive(true);

                            currentPosition = bounceHitPoint;
                            currentDirection = Vector3.Reflect(currentDirection, bounceHit.normal);
                            lineIndex++;
                        }
                        else
                        {
                            Vector3 endPoint = currentPosition + currentDirection * 15f;
                            Vector3[] segmentPositions = { lastDrawPosition, endPoint };

                            bounceLines[lineIndex].SetPositions(segmentPositions);
                            bounceLines[lineIndex].gameObject.SetActive(true);
                            lineIndex++;

                            break;
                        }
                    }
                }
            }

            while (lineIndex < bounceLines.Count)
            {
                bounceLines[lineIndex].gameObject.SetActive(false);
                lineIndex++;
            }

        } catch (Exception ex) {
            SparrohPlugin.Logger.LogInfo($"ERROR: UpdateBounceLines failed: {ex.Message}");
        }
    }
}
