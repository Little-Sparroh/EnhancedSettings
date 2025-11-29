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
        // Priority-ordered color selection
        if (SparrohPlugin.enableOrange.Value) return new Color(1f, 0.5f, 0f); // Orange
        if (SparrohPlugin.enableWhite.Value) return new Color(1f, 1f, 1f); // White
        if (SparrohPlugin.enableGreen.Value) return new Color(0f, 1f, 0f); // Green
        if (SparrohPlugin.enableBlue.Value) return new Color(0f, 0f, 1f); // Blue
        if (SparrohPlugin.enableRed.Value) return new Color(1f, 0f, 0f); // Red
        if (SparrohPlugin.enableYellow.Value) return new Color(1f, 1f, 0f); // Yellow
        if (SparrohPlugin.enablePurple.Value) return new Color(1f, 0f, 1f); // Magenta
        if (SparrohPlugin.enableCyan.Value) return new Color(0f, 1f, 1f); // Cyan

        // Fallback to orange
        return new Color(1f, 0.5f, 0f);
    }

    public static LineRenderer CreateBounceLinePrefab()
    {
        var lineObj = new GameObject("BounceIndicatorPrefab");
        var lineRenderer = lineObj.AddComponent<LineRenderer>();

        // Get selected color using priority booleans
        Color indicatorColor = GetSelectedColor();

        // Create material with configured color
        var material = new Material(Shader.Find("Sprites/Default"));
        material.color = indicatorColor;

        // Set emission color based on the indicator color for glow effect
        Color emissionColor = indicatorColor * 0.5f; // 50% intensity glow
        emissionColor.a = 1f; // Full alpha for emission
        material.SetColor("_EmissionColor", emissionColor);

        lineRenderer.material = material;

        // Set line properties similar to typical bounce indicators
        lineRenderer.startWidth = 0.01f;
        lineRenderer.endWidth = 0.01f;
        lineRenderer.startColor = indicatorColor;
        lineRenderer.endColor = indicatorColor;
        lineRenderer.positionCount = 2; // Start and end position
        lineRenderer.useWorldSpace = true;

        // Set to inactive by default - will be activated when used
        lineObj.SetActive(false);

        // Prevent prefab from being destroyed
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
            // Try to access as field
            var gunDataType = gun.GunData.GetType();

            var field = gunDataType.GetField("bounces", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (field != null)
            {
                var value = field.GetValue(gun.GunData);
                return (int)value;
            }

            // Try as property
            var prop = gunDataType.GetProperty("bounces", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (prop != null)
            {
                var value = prop.GetValue(gun.GunData);
                return (int)value;
            }

            // Try field names with different cases - prioritize maxBounces since it was found
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

            // Otherwise, try enumerating stats like ExpandedHUD
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
        // Cache bounces value when gun is enabled to avoid calling expensive GetBounces every frame
        int bounces = GetBounces(__instance);
        AllBounceIndicators.CachedBounces[__instance] = bounces;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(Gun), "Update")]
    public static void Update_Postfix(Gun __instance)
    {
        try {
            // Only process if owned and active (same conditions as OnActiveUpdate)
            if (!((bool)IsOwnerProperty.GetValue(__instance)) || !((bool)ActiveProperty.GetValue(__instance)))
                return;

            HandleBounceIndicators(__instance);
        } catch (Exception ex) {
            SparrohPlugin.Logger.LogInfo($"ERROR: Update postfix failed: {ex.Message}");
        }
    }

    private static void HandleBounceIndicators(Gun gun)
    {
        // Use cached bounces value instead of expensive GetBounces call
        if (!AllBounceIndicators.CachedBounces.TryGetValue(gun, out int bounces))
        {
            bounces = 0; // Fallback if not cached
        }
        bool configEnabled = SparrohPlugin.enableAllBounceIndicators.Value;
        string gunType = gun.GetType().Name;

        if (!configEnabled || bounces < 1) {
            return; // Silent for performance - only log when processing
        }

        // Get or create bounce lines for this gun
        if (!AllBounceIndicators.BounceLines.ContainsKey(gun))
        {
            AllBounceIndicators.BounceLines[gun] = new List<LineRenderer>();
        }
        var bounceLines = AllBounceIndicators.BounceLines[gun];

        // Lazy initialization: create prefab when first needed
        if (AllBounceIndicators.BounceLinePrefab == null) {
            AllBounceIndicators.BounceLinePrefab = AllBounceIndicators.CreateBounceLinePrefab();
        }

        int bulletsPerShot = gun.GunData.bulletsPerShot;
        int totalSegmentsNeeded = bulletsPerShot * bounces; // Each bullet needs maxBounces segments (only bounce lines)

        // Create bounce line segments for all bullets and bounces
        while (bounceLines.Count < totalSegmentsNeeded)
        {
            var line = UnityEngine.Object.Instantiate(AllBounceIndicators.BounceLinePrefab);
            bounceLines.Add(line);
        }

        // Prepare fire data
        var traverse = Traverse.Create(gun);
        traverse.Method("PrepareFireData").GetValue();

        // Check if sprinting or fire locked
        var playerObj = traverse.Field("player").GetValue();
        bool isSprinting = (bool)playerObj?.GetType().GetProperty("IsSprinting")?.GetValue(playerObj);
        bool isFireLocked = (bool)playerObj?.GetType().GetProperty("IsFireLocked")?.GetValue(playerObj);

        if (isSprinting || isFireLocked)
        {
            // Deactivate bounce lines
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
            // Update bounce lines
            UpdateBounceLines(gun);
        }
    }



    [HarmonyPostfix]
    [HarmonyPatch(typeof(Gun), "Disable")]
    public static void Disable_Postfix(Gun __instance)
    {
        // Deactivate bounce lines when gun is disabled
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
        // Clean up bounce lines when gun is destroyed
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
        // Also clear cached bounces
        AllBounceIndicators.CachedBounces.Remove(__instance);
    }

    private static Vector3 CalculateSpreadDirection(Vector3 baseDirection, int bulletIndex, int totalBullets, Gun gun)
    {
        if (totalBullets <= 1) return baseDirection;

        try {
            // Try to use weapon's actual spread data for authentic patterns
            var gunData = gun.GunData;
            var spreadData = gunData.GetType().GetField("spreadData")?.GetValue(gunData);

            if (spreadData != null) {
                var spreadDataType = spreadData.GetType();

                // Log all available methods for debugging
                var allMethods = spreadDataType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                var methodNames = new List<string>();
                foreach (var method in allMethods) methodNames.Add(method.Name);

                // Log all fields
                var allFields = spreadDataType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                var fieldNames = new List<string>();
                foreach (var field in allFields) fieldNames.Add(field.Name);

                // Try various spread calculation method signatures
                Vector3? attemptedDirection = null;

                // Try GetSpread with different signatures
                var getSpreadMethods = new List<MethodInfo>();
                foreach (var method in allMethods) {
                    if (method.Name.Contains("Spread")) getSpreadMethods.Add(method);
                }
                foreach (var method in getSpreadMethods) {
                    try {
                        var parameters = method.GetParameters();
                        object[] args = null;

                        // Try different parameter combinations
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

                // Try direct property/field access for spread angle
                var spreadAngleProp = spreadDataType.GetProperty("spreadAngle");
                var spreadAngleField = spreadDataType.GetField("spreadAngle");
                float spreadAngle = 0;

                if (spreadAngleProp != null) {
                    spreadAngle = (float)spreadAngleProp.GetValue(spreadData);
                } else if (spreadAngleField != null) {
                    spreadAngle = (float)spreadAngleField.GetValue(spreadData);
                }

                // If we found spread angle, try to use it with spherical spread (concentric circles)
                if (spreadAngle > 0) {
                    // Create spherical distribution like concentric circles
                    // Distribute bullets in a cone around base direction
                    float maxRadius = spreadAngle * Mathf.Deg2Rad; // Convert to radians

                    // Calculate angular position for this bullet
                    float theta = bulletIndex * (2 * Mathf.PI) / totalBullets; // Around circle
                    float phi = (bulletIndex * maxRadius) / totalBullets; // From center outward

                    // Convert to 3D direction relative to base direction
                    // Use spherical to cartesian conversion
                    float r = phi; // Distance from center
                    float y = r * Mathf.Sin(theta); // Vertical spread
                    float x = r * Mathf.Cos(theta); // Horizontal spread

                    // Apply rotation to align with firing direction
                    Vector3 localSpread = new Vector3(x, y, 1f).normalized; // Forward with spread
                    Vector3 worldSpread = Quaternion.LookRotation(baseDirection) * localSpread;

                    return worldSpread;
                }

                // Try other common spread method signatures
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

        // Final fallback: spherical spread with default angle
        float fallbackSpreadAngle = 15f; // Degrees cone
        float fallbackMaxRadius = fallbackSpreadAngle * Mathf.Deg2Rad;

        // Simple conical distribution
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
            // Get gun data
            var traverse = Traverse.Create(gun);
            var fireData = traverse.Field("fireData").GetValue();
            var firePosition = (Vector3)fireData.GetType().GetField("firePosition").GetValue(fireData);
            var bulletRotation = (Quaternion)fireData.GetType().GetField("bulletRotation").GetValue(fireData);
            var bulletsPerShot = gun.GunData.bulletsPerShot;

            // Get bounces for this weapon (should be cached)
            int maxBounces = 0;
            if (AllBounceIndicators.CachedBounces.TryGetValue(gun, out int bouncesValue))
            {
                maxBounces = bouncesValue;
            }

            int lineIndex = 0;

            // Create bounce lines for each bullet
            for (int bulletIndex = 0; bulletIndex < bulletsPerShot && lineIndex < bounceLines.Count; bulletIndex++)
            {
                Vector3 currentPosition = firePosition;
                // Calculate spread-adjusted direction for this bullet
                Vector3 baseDirection = bulletRotation * Vector3.forward;
                Vector3 currentDirection = CalculateSpreadDirection(baseDirection, bulletIndex, bulletsPerShot, gun);

                // Do initial raycast to find first bounce point (but don't draw initial line)
                Vector3 lastDrawPosition = firePosition; // Start of first bounce segment

                if (Physics.Raycast(currentPosition, currentDirection, out RaycastHit firstHit, 50f, LayerMask.GetMask("Default", "Terrain", "Environment")))
                {
                    // First surface found - calculate reflection and set up for bounce drawing
                    Vector3 firstHitPoint = firstHit.point;
                    currentPosition = firstHitPoint;
                    currentDirection = Vector3.Reflect(currentDirection, firstHit.normal);

                    // Now draw bounce segments starting from first hit point
                    for (int bounceIndex = 0; bounceIndex < maxBounces && lineIndex < bounceLines.Count; bounceIndex++)
                    {
                        lastDrawPosition = currentPosition; // Start of this bounce segment

                        if (Physics.Raycast(currentPosition, currentDirection, out RaycastHit bounceHit, 50f, LayerMask.GetMask("Default", "Terrain", "Environment")))
                        {
                            // Found next surface - draw bounce line
                            Vector3 bounceHitPoint = bounceHit.point;
                            Vector3[] segmentPositions = { lastDrawPosition, bounceHitPoint };

                            bounceLines[lineIndex].SetPositions(segmentPositions);
                            bounceLines[lineIndex].gameObject.SetActive(true);

                            // Prepare for next bounce
                            currentPosition = bounceHitPoint;
                            currentDirection = Vector3.Reflect(currentDirection, bounceHit.normal);
                            lineIndex++;
                        }
                        else
                        {
                            // No more surfaces - create fallback end segment
                            Vector3 endPoint = currentPosition + currentDirection * 15f;
                            Vector3[] segmentPositions = { lastDrawPosition, endPoint };

                            bounceLines[lineIndex].SetPositions(segmentPositions);
                            bounceLines[lineIndex].gameObject.SetActive(true);
                            lineIndex++;

                            // Finished with bounces for this bullet
                            break;
                        }
                    }
                }
            }

            // Deactivate unused lines
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
