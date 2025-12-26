using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using Pigeon.Movement;

[HarmonyPatch]
public static class DropPodPatches
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(DropPod), "StartCountdown_ClientRpc")]
    public static void StartCountdown_ClientRpc_Postfix()
    {
        if (!SparrohPlugin.skinRandomizer.Value)
            return;

        RandomizeFavoriteSkins();

        /*
        if (Menu.Instance != null)
        {
            SparrohPlugin.Instance.StartCoroutine(OpenAndCloseMenu());
        }
        */
    }

    /*
     private static System.Collections.IEnumerator OpenAndCloseMenu()

    {
        if (Menu.Instance != null && !Menu.Instance.IsOpen && !Menu.Instance.IsClosing)
        {
            Menu.Instance.Open(true);
            while (!Menu.Instance.IsOpen)
            {
                yield return null;
            }
            Menu.Instance.Close();
        }
    }
    */

    private static void RandomizeFavoriteSkins()
    {
        if (Player.LocalPlayer == null) return;

        var gears = new List<IUpgradable>
        {
            Player.LocalPlayer.Character,
            /*
            Player.LocalPlayer.Gear[0],
            Player.LocalPlayer.Gear[1],
            */
        };

        if (DropPod.Instance != null)
        {
            gears.Add((IUpgradable)DropPod.Instance);
        }

        foreach (var gear in gears)
        {
            RandomizeSkinsForGear(gear);
        }
    }

    private static void RandomizeSkinsForGear(IUpgradable gear)
    {
        var gearData = PlayerData.GetGearData(gear);
        if (gearData == null) return;

        var equippedSkinsField = typeof(PlayerData.GearData).GetField("equippedSkins", BindingFlags.NonPublic | BindingFlags.Instance);
        var equippedSkins = equippedSkinsField?.GetValue(gearData) as List<PlayerData.UpgradeEquipData>;
        if (equippedSkins != null)
        {
            equippedSkins.Clear();
        }

        var mainSkins = new List<UpgradeInstance>();
        var gunCrabs = new List<UpgradeInstance>();
        var constellations = new List<UpgradeInstance>();

        var skinEnumerator = new PlayerData.SkinEnumerator(gear);
        while (skinEnumerator.MoveNext())
        {
            var upgrade = skinEnumerator.Upgrade;
            if (!upgrade.Favorite) continue;

            var skinUpgrade = upgrade.Upgrade as SkinUpgrade;
            if (skinUpgrade == null) continue;

            SkinUpgradeProperty_VFXCrab vfxCrabProp;
            bool hasVFXCrab = skinUpgrade.HasProperty(upgrade.Seed, out vfxCrabProp, out _);

            SkinUpgradeProperty_GunCrab gunCrabProp;
            bool hasGunCrab = skinUpgrade.HasProperty(upgrade.Seed, out gunCrabProp, out _);

            if (hasVFXCrab)
            {
                constellations.Add(upgrade);
            }
            else if (hasGunCrab)
            {
                gunCrabs.Add(upgrade);
            }
            else
            {
                mainSkins.Add(upgrade);
            }
        }

        SelectAndEquipRandom(mainSkins, gearData);
        SelectAndEquipRandom(gunCrabs, gearData);
        SelectAndEquipRandom(constellations, gearData);
    }

    private static void SelectAndEquipRandom(List<UpgradeInstance> skins, PlayerData.GearData gearData)
    {
        if (skins.Count == 0) return;

        var randomIndex = UnityEngine.Random.Range(0, skins.Count);
        var selectedSkin = skins[randomIndex];

        gearData.EquipUpgrade(selectedSkin, 0, 0, 0);

        if (gearData.Gear == Player.LocalPlayer.Character)
        {
            Player.LocalPlayer.ApplySkins();
        }
        else if (gearData.Gear is DropPod dropPod)
        {
            dropPod.ApplyGearSkins();
        }
        else
        {
            var skinUpgrade = selectedSkin.Upgrade as SkinUpgrade;
            if (skinUpgrade != null)
            {
                skinUpgrade.Apply(gearData.Gear, selectedSkin.Seed, Player.LocalPlayer);
            }

            gearData.Gear.OnUpgradesChanged(null, null);
        }

        Player.LocalPlayer.ApplySkins();
}



}
