using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using UnityEngine.AI;
using System.Reflection;

public class SingleplayerPause : MonoBehaviour
{
    private static bool menuWasOpen = false;
    private static bool isPaused = false;

    private static readonly List<AI> pausedAI = new List<AI>();
    private static readonly Dictionary<AI, bool> aiOriginalEnabled = new Dictionary<AI, bool>();
    private static readonly Dictionary<NavMeshAgent, bool> navAgentOriginalEnabled = new Dictionary<NavMeshAgent, bool>();

    private static readonly List<SimpleProjectileBullet> pausedProjectiles = new List<SimpleProjectileBullet>();
    private static readonly Dictionary<SimpleProjectileBullet, bool> projectileOriginalEnabled = new Dictionary<SimpleProjectileBullet, bool>();

    private static readonly List<EnemyCore> pausedEnemyCores = new List<EnemyCore>();
    private static readonly Dictionary<EnemyCore, bool> enemyCoreOriginalEnabled = new Dictionary<EnemyCore, bool>();
    private static readonly List<EnemyBrain> overclockedEnemies = new List<EnemyBrain>();
    private static readonly List<EnemyBrain> pausedEnemyBrains = new List<EnemyBrain>();
    private static readonly Dictionary<EnemyBrain, bool> enemyBrainOriginalEnabled = new Dictionary<EnemyBrain, bool>();
    private static readonly List<EnemyBrain> deactivatedOverclockedBrains = new List<EnemyBrain>();

    private static readonly List<MonoBehaviour> pausedWeaponArms = new List<MonoBehaviour>();
    private static readonly Dictionary<MonoBehaviour, bool> weaponArmOriginalEnabled = new Dictionary<MonoBehaviour, bool>();

    private static readonly List<Hornet> pausedHornets = new List<Hornet>();
    private static readonly Dictionary<Hornet, bool> hornetOriginalEnabled = new Dictionary<Hornet, bool>();

    private static readonly List<ExplosiveEnemyPart> pausedExplosives = new List<ExplosiveEnemyPart>();
    private static readonly List<float> explosiveFuseTimes = new List<float>();

    private static bool wasSpawningEnabled;
    private static bool isPausedMod = false;
    public static bool IsPaused = false;

    void Update()
    {
        if (Menu.Instance == null || !SparrohPlugin.enableSingleplayerPause.Value) return;

        bool isOpen = Menu.Instance.IsOpen;
        bool isSingleplayer = IsSingleplayer();

        if (isPaused && !isSingleplayer)
        {
            ResumePause();
        }

        if (isSingleplayer)
        {
            bool isMissionSelect = false;
            if (GameManager.Instance != null && GameManager.Instance.WindowSystem != null)
            {
                for (int i = 0; i < GameManager.Instance.WindowSystem.Count; i++)
                {
                    if (GameManager.Instance.WindowSystem[i] is MissionSelectWindow)
                    {
                        isMissionSelect = true;
                        break;
                    }
                }
            }
            if (isOpen && !menuWasOpen && !isPaused && !isMissionSelect)
            {
                StartPause();
            }
            else if (!isOpen && menuWasOpen && isPaused)
            {
                ResumePause();
            }
            else if (isOpen && isPaused && isMissionSelect)
            {
                ResumePause();
            }
        }

        menuWasOpen = isOpen;
    }

    void LateUpdate()
    {
        if (Menu.Instance == null || !SparrohPlugin.enableSingleplayerPause.Value) return;

        bool isMissionSelectLate = false;
        if (GameManager.Instance != null && GameManager.Instance.WindowSystem != null)
        {
            for (int i = 0; i < GameManager.Instance.WindowSystem.Count; i++)
            {
                if (GameManager.Instance.WindowSystem[i] is MissionSelectWindow)
                {
                    isMissionSelectLate = true;
                    break;
                }
            }
        }

        if (isMissionSelectLate || Menu.Instance.IsOpen || isPaused)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    private void StartPause()
    {
        try
        {
            isPaused = true;
            isPausedMod = true;
            IsPaused = true;

            AI[] ais = FindObjectsOfType<AI>(true);

            foreach (AI ai in ais)
            {
                try
                {
                    if (ai != null && ai.gameObject != null && ai.gameObject.activeInHierarchy)
                    {
                        pausedAI.Add(ai);
                        aiOriginalEnabled[ai] = ai.enabled;
                        ai.enabled = false;
                        if (ai.NavAgent != null)
                        {
                            navAgentOriginalEnabled[ai.NavAgent] = ai.NavAgent.enabled;
                            ai.NavAgent.enabled = false;
                        }
                        ai.StopAllCoroutines();
                    }
                }
                catch (System.Exception e)
                {
                    SparrohPlugin.Logger.LogError($"Failed to pause AI {ai?.name ?? "null"}: {e.Message}");
                }
            }

            SimpleProjectileBullet[] projectiles = FindObjectsOfType<SimpleProjectileBullet>(true);
            foreach (SimpleProjectileBullet proj in projectiles)
            {
                if (proj != null && proj.gameObject != null && proj.gameObject.activeInHierarchy && proj.isActiveAndEnabled)
                {
                    pausedProjectiles.Add(proj);
                    projectileOriginalEnabled[proj] = proj.enabled;
                    proj.enabled = false;
                }
            }

            EnemyCore[] cores = FindObjectsOfType<EnemyCore>(true);
            foreach (EnemyCore core in cores)
            {
                if (core != null && core.gameObject != null && core.gameObject.activeInHierarchy)
                {
                    pausedEnemyCores.Add(core);
                    enemyCoreOriginalEnabled[core] = core.enabled;
                    core.enabled = false;
                    core.StopAllCoroutines();
                    if (core.GetComponentInParent<EnemyBrain>() is EnemyBrain brain && brain.Overclocked > 0)
                    {
                        overclockedEnemies.Add(brain);
                    }
                }
            }

            EnemyBrain[] brains = FindObjectsOfType<EnemyBrain>(true);
            foreach (EnemyBrain brain in brains)
            {
                if (brain != null && brain.gameObject != null && brain.gameObject.activeInHierarchy)
                {
                    pausedEnemyBrains.Add(brain);
                    enemyBrainOriginalEnabled[brain] = brain.enabled;
                    if (brain.Overclocked > 0)
                    {
                        deactivatedOverclockedBrains.Add(brain);
                    }
                    brain.Overclocked = 0;
                    brain.enabled = false;
                    brain.StopAllCoroutines();
                }
            }

            var weaponArmTypes = new[] { typeof(GunArmTip), typeof(AutocannonArmTip), typeof(BladeArmTip) };
            foreach (var weaponType in weaponArmTypes)
            {
                MonoBehaviour[] weapons = FindObjectsOfType(weaponType, true) as MonoBehaviour[];
                foreach (MonoBehaviour weapon in weapons)
                {
                    if (weapon != null && weapon.gameObject != null && weapon.gameObject.activeInHierarchy)
                    {
                        pausedWeaponArms.Add(weapon);
                        weaponArmOriginalEnabled[weapon] = weapon.enabled;
                        weapon.enabled = false;
                    }
                }
            }

            Hornet[] hornets = FindObjectsOfType<Hornet>(true);
            foreach (Hornet hornet in hornets)
            {
                if (hornet != null && hornet.gameObject != null && hornet.gameObject.activeInHierarchy && hornet.isActiveAndEnabled)
                {
                    pausedHornets.Add(hornet);
                    hornetOriginalEnabled[hornet] = hornet.enabled;
                    hornet.enabled = false;
                }
            }

            ExplosiveEnemyPart[] explosives = FindObjectsOfType<ExplosiveEnemyPart>(true);
            FieldInfo startFuseTimeField = typeof(ExplosiveEnemyPart).GetField("startFuseTime", BindingFlags.NonPublic | BindingFlags.Instance);
            foreach (ExplosiveEnemyPart explosive in explosives)
            {
                if (explosive != null && explosive.gameObject != null && explosive.gameObject.activeInHierarchy)
                {
                    float startFuseTime = (float)startFuseTimeField.GetValue(explosive);
                    if (startFuseTime > 0f)
                    {
                        pausedExplosives.Add(explosive);
                        explosiveFuseTimes.Add(Time.time - startFuseTime);
                    }
                }
            }

            if (EnemyManager.Instance != null)
            {
                wasSpawningEnabled = true;
                EnemyManager.Instance.DisableSpawning();
            }
        }
        catch (System.Exception e)
        {
            SparrohPlugin.Logger.LogError($"Failed at start pause: {e.Message}");
            isPaused = false;
        }
    }

    private void ResumePause()
    {
            isPaused = false;
            isPausedMod = false;
            IsPaused = false;

        foreach (AI ai in pausedAI)
        {
            if (ai != null)
            {
                if (aiOriginalEnabled.TryGetValue(ai, out bool enabled))
                    ai.enabled = enabled;
                if (ai.NavAgent != null && navAgentOriginalEnabled.TryGetValue(ai.NavAgent, out enabled))
                    ai.NavAgent.enabled = enabled;
            }
        }

        foreach (SimpleProjectileBullet proj in pausedProjectiles)
        {
            if (proj != null && projectileOriginalEnabled.TryGetValue(proj, out bool enabled))
                proj.enabled = enabled;
        }

        foreach (EnemyCore core in pausedEnemyCores)
        {
            if (core != null)
            {
                if (enemyCoreOriginalEnabled.TryGetValue(core, out bool enabled))
                    core.enabled = enabled;
            }
        }
        foreach (EnemyBrain brain in overclockedEnemies)
        {
        }

        foreach (EnemyBrain brain in pausedEnemyBrains)
        {
            if (brain != null)
            {
                if (enemyBrainOriginalEnabled.TryGetValue(brain, out bool enabled))
                    brain.enabled = enabled;
            }
        }

        foreach (MonoBehaviour weapon in pausedWeaponArms)
        {
            if (weapon != null && weaponArmOriginalEnabled.TryGetValue(weapon, out bool enabled))
                weapon.enabled = enabled;
        }

        foreach (EnemyBrain brain in deactivatedOverclockedBrains)
        {
            if (brain != null)
            {
                brain.Overclocked = 1f;
            }
        }

        foreach (Hornet hornet in pausedHornets)
        {
            if (hornet != null && hornetOriginalEnabled.TryGetValue(hornet, out bool enabled))
                hornet.enabled = enabled;
        }

        FieldInfo startFuseTimeField = typeof(ExplosiveEnemyPart).GetField("startFuseTime", BindingFlags.NonPublic | BindingFlags.Instance);
        for (int i = 0; i < pausedExplosives.Count; i++)
        {
            ExplosiveEnemyPart explosive = pausedExplosives[i];
            if (explosive != null)
            {
                float remainingFuseTime = explosiveFuseTimes[i];
                startFuseTimeField.SetValue(explosive, Time.time - remainingFuseTime);
            }
        }

        pausedAI.Clear();
        aiOriginalEnabled.Clear();
        navAgentOriginalEnabled.Clear();
        pausedProjectiles.Clear();
        projectileOriginalEnabled.Clear();
        pausedEnemyCores.Clear();
        enemyCoreOriginalEnabled.Clear();
        overclockedEnemies.Clear();
        pausedEnemyBrains.Clear();
        enemyBrainOriginalEnabled.Clear();
        deactivatedOverclockedBrains.Clear();
        pausedWeaponArms.Clear();
        weaponArmOriginalEnabled.Clear();
        pausedHornets.Clear();
        hornetOriginalEnabled.Clear();
        pausedExplosives.Clear();
        explosiveFuseTimes.Clear();

        if (wasSpawningEnabled && EnemyManager.Instance != null)
        {
            EnemyManager.Instance.EnableSpawning();
        }
    }



    private static bool IsSingleplayer()
    {
        if (NetworkManager.Singleton != null)
        {
            return NetworkManager.Singleton.ConnectedClients.Count <= 1;
        }

        if (GameManager.Instance != null)
        {
            return GameManager.players.Count <= 1;
        }
        return true;
    }
}
