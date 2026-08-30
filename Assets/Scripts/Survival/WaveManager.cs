using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

public class WaveManager : MonoBehaviour
{
    [Header("Wave Configuration")]
    public int currentWave = 1;
    public int maxWaves = 10;
    public float timeBetweenWaves = 5.0f;
    public bool isWaveActive = false;

    [Header("Current Wave Progress")]
    public int targetKillsForWave;
    public int killsThisWave;
    public int zombiesSpawnedThisWave;

    [Header("Zombie Prefabs")]
    public GameObject skinlessBerserkerPrefab;
    public GameObject shirtlessGhoulPrefab;
    public GameObject suitCivilianPrefab;
    public GameObject shirtlessCrawlerPrefab;
    public GameObject goliathBossPrefab;

    [Header("Supply Drop Prefabs")]
    public GameObject ammoBoxPrefab;
    public GameObject healthJarPrefab;
    public GameObject shieldDropPrefab;
    public GameObject berserkDropPrefab;
    public AudioClip supplyPickupSound;

    [Header("Shield Drop Pacing Budget")]
    private int m_ShieldDropsWaves1To5 = 0;
    private int m_ShieldDropsWaves6To10 = 0;

    [Header("Open Desert Spawn Points")]
    public Vector3[] spawnPoints = new Vector3[]
    {
        new Vector3(18.0f, 0f, -185.0f),
        new Vector3(-38.0f, 0f, -210.0f),
        new Vector3(-28.0f, 0f, -85.0f),
        new Vector3(15.0f, 0f, -105.0f),
        new Vector3(-12.0f, 0f, -75.0f),
        new Vector3(30.0f, 0f, -145.0f),
        new Vector3(-32.0f, 0f, -170.0f)
    };

    [Header("Supply Drop Locations")]
    public Vector3[] supplyLocations = new Vector3[]
    {
        new Vector3(-10.0f, 0f, -132.0f),
        new Vector3(0.0f, 0f, -125.0f),
        new Vector3(-5.0f, 0f, -110.0f)
    };

    private List<ZombieAI> m_ActiveZombies = new List<ZombieAI>();
    private ZombieAI m_ActiveBoss;
    private GunHUD m_HUD;
    private GameObject m_ZombieParent;
    private float m_SandstormIntensity;
    private bool m_IsTransitioning;
    private Coroutine m_SpawnLoopCoroutine;

    [Header("Testing & Arena Debug")]
    public bool bossOnlyTestMode = false;
    public int startingWave = 1;

    public int RemainingKillsInWave => Mathf.Max(0, targetKillsForWave - killsThisWave);

    private void Awake()
    {
        m_HUD = FindAnyObjectByType<GunHUD>();
        m_ZombieParent = GameObject.Find("Zombies") ?? new GameObject("Zombies");
        for (int i = m_ZombieParent.transform.childCount - 1; i >= 0; i--)
        {
            Destroy(m_ZombieParent.transform.GetChild(i).gameObject);
        }

        if (ammoBoxPrefab == null) ammoBoxPrefab = Resources.Load<GameObject>("AmmoBox_Pickup");
        if (healthJarPrefab == null) healthJarPrefab = Resources.Load<GameObject>("Health_Drop");
        if (shieldDropPrefab == null) shieldDropPrefab = Resources.Load<GameObject>("Shield_Drop");
        if (berserkDropPrefab == null) berserkDropPrefab = Resources.Load<GameObject>("Berserk_Drop");

#if UNITY_EDITOR
        if (healthJarPrefab == null)
            healthJarPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/First aid jar/Prefabs/Health_Drop.prefab") ??
                              UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/First aid jar/Prefabs/Firstaid_2.prefab");
        if (shieldDropPrefab == null)
            shieldDropPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/First aid jar/Prefabs/Shield_Drop.prefab");
        if (berserkDropPrefab == null)
            berserkDropPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/First aid jar/Prefabs/Berserk_Drop.prefab");
        if (ammoBoxPrefab == null)
            ammoBoxPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/AmmoBox/AmmoBox_Pickup.prefab");
#endif

#if UNITY_EDITOR
        if (goliathBossPrefab == null)
            goliathBossPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/BossMutant/Prefabs/Mutant_Boss.prefab") ??
                               UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/BossMutant/Mutant.fbx") ??
                               UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Tensori/SkinlessZombie/Prefabs/skinless zombie.prefab");

        if (skinlessBerserkerPrefab == null)
            skinlessBerserkerPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Tensori/SkinlessZombie/Prefabs/skinless zombie.prefab");

        if (shirtlessGhoulPrefab == null)
            shirtlessGhoulPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/NewPunch/ShirtlessZombieFree/Prefabs/ShirtlessZombie_FREE_URP.prefab") ??
                                   UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/NewPunch/ShirtlessZombieFree/Prefabs/ShirtlessZombie_FREE.prefab");

        if (suitCivilianPrefab == null)
            suitCivilianPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ZombieMale_AAB/Prefabs/URP/ZombieMale_AAB_URP.prefab") ??
                                 UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ZombieMale_AAB/Prefabs/ZombieMale_AAB.prefab");

        if (shirtlessCrawlerPrefab == null)
            shirtlessCrawlerPrefab = shirtlessGhoulPrefab;
#endif

        if (SandstormController.Instance == null && FindAnyObjectByType<SandstormController>() == null)
        {
            var sandstormObj = new GameObject("Sandstorm_Weather_Director");
            sandstormObj.AddComponent<SandstormController>();
        }

        if (Score.Instance == null && FindAnyObjectByType<Score>() == null)
        {
            gameObject.AddComponent<Score>();
        }
    }

    private void Start()
    {

        var sceneZombies = Object.FindObjectsByType<ZombieAI>(FindObjectsInactive.Include);
        foreach (var z in sceneZombies)
        {
            if (z != null && z.zombieType != ZombieType.Boss)
            {
                Destroy(z.gameObject);
            }
        }

        if (SceneManager.GetActiveScene().name.ToLower().Contains("sand storm") || SceneManager.GetActiveScene().name.ToLower().Contains("sandstorm"))
        {

            isWaveActive = false;
            StartCoroutine(AnimateSandstorm(1.0f, 1.0f));
            if (m_HUD != null)
            {
                m_HUD.ShowWaveStatus("<b>🌪 SANDSTORM TEST ENVIRONMENT 🌪</b>", new Color(1.0f, 0.75f, 0.35f), 5.0f);
            }
            return;
        }
        else if (SceneManager.GetActiveScene().name.ToLower().Contains("boss") || bossOnlyTestMode)
        {
            bossOnlyTestMode = true;
            currentWave = 5;
        }
        else
        {
            currentWave = startingWave;
        }
        SpawnSupplies();
        StartCoroutine(StartNextWaveRoutine());
    }

    private void Update()
    {
        if (!isWaveActive) return;

        m_ActiveZombies.RemoveAll(z => z == null || z.IsDead || z.currentHealth <= 0);

        if (m_HUD != null)
        {
            m_HUD.UpdateWaveHUD(currentWave, maxWaves, RemainingKillsInWave, targetKillsForWave);

            if (m_ActiveBoss != null)
            {
                float hp = m_ActiveBoss.IsDead ? 0f : Mathf.Max(0f, m_ActiveBoss.currentHealth);
                m_HUD.UpdateBossHealth(hp, m_ActiveBoss.maxHealth);
            }
            else
            {
                m_HUD.UpdateBossHealth(0, 1);
            }
            m_HUD.SetSandstormIntensity(m_SandstormIntensity);
        }

        if (bossOnlyTestMode)
        {

            return;
        }

        if (killsThisWave >= targetKillsForWave && isWaveActive && !m_IsTransitioning)
        {
            if (currentWave >= maxWaves)
            {
                StartCoroutine(VictoryRoutine());
            }
            else
            {
                StartCoroutine(WaveClearedRoutine());
            }
        }
    }

    public void RegisterZombieDeath(ZombieAI zombie)
    {
        if (m_ActiveZombies.Contains(zombie))
        {
            m_ActiveZombies.Remove(zombie);
        }

        if (zombie == m_ActiveBoss && m_HUD != null)
        {
            m_HUD.SetBossSlayerBadge();
        }

        killsThisWave++;
        m_ActiveZombies.RemoveAll(z => z == null || z.IsDead || z.currentHealth <= 0);

        if (m_HUD != null)
        {
            m_HUD.UpdateWaveHUD(currentWave, maxWaves, RemainingKillsInWave, targetKillsForWave);
        }

        if (killsThisWave >= targetKillsForWave && isWaveActive && !m_IsTransitioning)
        {
            if (currentWave >= maxWaves)
            {
                StartCoroutine(VictoryRoutine());
            }
            else
            {
                StartCoroutine(WaveClearedRoutine());
            }
        }
    }

    private IEnumerator StartNextWaveRoutine()
    {
        isWaveActive = false;
        m_IsTransitioning = true;

        targetKillsForWave = bossOnlyTestMode ? 1 : currentWave * 5;
        killsThisWave = 0;
        zombiesSpawnedThisWave = 0;
        m_ActiveZombies.Clear();
        m_ActiveBoss = null;

        string waveTitle;
        Color bannerColor;

        if (bossOnlyTestMode)
        {
            waveTitle = "<b>⚠ BOSS ARENA TEST: GOLIATH MUTANT ⚠</b>";
            bannerColor = new Color(1f, 0.2f, 0.2f, 1f);
        }
        else if (currentWave == maxWaves)
        {
            waveTitle = "<b>★ FINAL WAVE 10: OASIS SIEGE ★</b>";
            bannerColor = new Color(1f, 0.15f, 0.15f, 1f);
        }
        else if (currentWave == 5)
        {
            waveTitle = "<b>⚠ BOSS WAVE 5: GOLIATH BERSERKER ⚠</b>";
            bannerColor = new Color(1f, 0.3f, 0.3f, 1f);
        }
        else
        {
            waveTitle = $"<b>WAVE {currentWave} / {maxWaves}</b>";
            bannerColor = new Color(1.0f, 0.85f, 0.2f, 1f);
        }

        bool hasSandstorm = (currentWave == 4 || currentWave == 7 || currentWave == 10);
        UpdateTornadoes(hasSandstorm);
        StartCoroutine(AnimateSandstorm(hasSandstorm ? 1.0f : 0.0f, 3.0f));

        int countdown = bossOnlyTestMode ? 2 : 5;
        for (int sec = countdown; sec >= 1; sec--)
        {
            if (m_HUD != null)
            {
                string targetLabel = bossOnlyTestMode ? "DEFEAT GOLIATH BOSS" : $"TARGET: {targetKillsForWave} KILLS";
                string msg = $"{waveTitle}\n<size=22><color=#FFCC00>{targetLabel} | ENGAGING IN: {sec}...</color></size>";
                m_HUD.ShowWaveStatus(msg, bannerColor, 1.1f);
            }
            yield return new WaitForSeconds(1.0f);
        }

        if (m_HUD != null)
        {
            string eliminateLabel = bossOnlyTestMode ? "DESTROY THE GOLIATH MUTANT BOSS!" : $"ELIMINATE {targetKillsForWave} ZOMBIES!";
            m_HUD.ShowWaveStatus($"<b>{waveTitle}\n<size=24><color=#FF4444>{eliminateLabel}</color></size></b>", bannerColor, 2.0f);
            m_HUD.UpdateWaveHUD(currentWave, maxWaves, RemainingKillsInWave, targetKillsForWave);
        }

        isWaveActive = true;
        m_IsTransitioning = false;

        if (m_SpawnLoopCoroutine != null) StopCoroutine(m_SpawnLoopCoroutine);
        m_SpawnLoopCoroutine = StartCoroutine(WaveSpawnLoopRoutine(currentWave));
    }

    private IEnumerator WaveSpawnLoopRoutine(int wave)
    {
        if (bossOnlyTestMode)
        {
            SpawnBoss(wave);
            zombiesSpawnedThisWave = 1;
            yield break;
        }

        if (wave == 5 || wave == 10)
        {
            SpawnBoss(wave);
            zombiesSpawnedThisWave++;
        }

        int maxConcurrent = Mathf.Clamp(4 + wave * 2, 5, 14);

        while (zombiesSpawnedThisWave < targetKillsForWave && isWaveActive)
        {
            m_ActiveZombies.RemoveAll(z => z == null || z.IsDead || z.currentHealth <= 0);

            if (m_ActiveZombies.Count < maxConcurrent)
            {
                Vector3 anchor = spawnPoints[zombiesSpawnedThisWave % spawnPoints.Length];
                Vector3 spawnPos = anchor + new Vector3(Random.Range(-3.5f, 3.5f), 0, Random.Range(-3.5f, 3.5f));
                GameObject prefab = SelectPrefabForWave(wave, zombiesSpawnedThisWave);
                SpawnSingleZombie(prefab, spawnPos, wave, false);
                zombiesSpawnedThisWave++;
            }

            yield return new WaitForSeconds(Random.Range(0.6f, 1.2f));
        }
    }

    private IEnumerator AnimateSandstorm(float target, float duration)
    {
        if (SandstormController.Instance != null)
        {
            SandstormController.Instance.SetIntensity(target);
        }

        float elapsed = 0f;
        float start = m_SandstormIntensity;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            m_SandstormIntensity = Mathf.Lerp(start, target, elapsed / duration);
            yield return null;
        }
        m_SandstormIntensity = target;
    }

    private void SpawnBoss(int wave)
    {

        if (m_ActiveBoss != null && !m_ActiveBoss.IsDead) return;

        var existingZombies = Object.FindObjectsByType<ZombieAI>(FindObjectsInactive.Include);
        foreach (var z in existingZombies)
        {
            if (z != null && z.zombieType == ZombieType.Boss && !z.IsDead)
            {
                m_ActiveBoss = z;
                if (!m_ActiveZombies.Contains(z)) m_ActiveZombies.Add(z);
                if (m_HUD != null) m_HUD.UpdateBossHealth(z.currentHealth, z.maxHealth);
                return;
            }
        }

        Vector3 playerPos = Vector3.zero;
        var player = FindAnyObjectByType<PlayerController>();
        if (player != null) playerPos = player.transform.position;

        Vector3 bossSpawnPos = (playerPos != Vector3.zero) ?
            playerPos + new Vector3(Random.Range(-8f, 8f), 0f, Random.Range(24f, 32f)) :
            new Vector3(18.0f, 0f, -145.0f);

        var bossPrefab = goliathBossPrefab ?? skinlessBerserkerPrefab ?? shirtlessGhoulPrefab ?? suitCivilianPrefab;
        var bossObj = SpawnSingleZombie(bossPrefab, bossSpawnPos, wave, true);
        if (bossObj != null)
        {
            bossObj.name = (wave == 10) ? "BOSS_Goliath_Overlord" : "BOSS_Goliath_Berserker";

            var renderers = bossObj.GetComponentsInChildren<Renderer>(true);
#if UNITY_EDITOR
            var skinMat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/BossMutant/Materials/Mutant_Mat.mat") ??
                          UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Tensori/SkinlessZombie/Art/Materials/Skin.mat") ??
                          UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/NewPunch/ShirtlessZombieFree/Materials/URP/ShirtlessZombie_URP.mat");
            var albedoTex = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/BossMutant/Textures/Mutant_Albedo.png");
            var normalTex = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/BossMutant/Textures/Mutant_Normal.png");

            if (skinMat != null)
            {
                if (albedoTex != null) skinMat.SetTexture("_BaseMap", albedoTex);
                if (normalTex != null)
                {
                    skinMat.SetTexture("_BumpMap", normalTex);
                    skinMat.EnableKeyword("_NORMALMAP");
                }
                foreach (var r in renderers)
                {
                    r.sharedMaterial = skinMat;
                }
            }
#endif

            var anim = bossObj.GetComponent<Animator>() ?? bossObj.GetComponentInChildren<Animator>();
            if (anim == null) anim = bossObj.AddComponent<Animator>();
#if UNITY_EDITOR
            if (anim.runtimeAnimatorController == null)
            {
                anim.runtimeAnimatorController = UnityEditor.AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/BossMutant/Mutant_Boss_Animator.controller") ??
                                               UnityEditor.AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/ZombieMale_AAB/Animations/Skinless_Berserker_Animator.controller");
            }
            if (anim.avatar == null)
            {
                var mutantAvatar = UnityEditor.AssetDatabase.LoadAssetAtPath<Avatar>("Assets/BossMutant/Mutant.fbx");
                if (mutantAvatar != null) anim.avatar = mutantAvatar;
                else if (skinlessBerserkerPrefab != null)
                {
                    var skinlessAnim = skinlessBerserkerPrefab.GetComponentInChildren<Animator>();
                    if (skinlessAnim != null) anim.avatar = skinlessAnim.avatar;
                }
            }
#endif
            anim.applyRootMotion = false;

            m_ActiveBoss = bossObj.GetComponent<ZombieAI>() ?? bossObj.AddComponent<ZombieAI>();
            if (m_ActiveBoss != null)
            {
                m_ActiveBoss.zombieType = ZombieType.Boss;

                float hp = (wave == 10) ? 5000f : 3000f;
                m_ActiveBoss.maxHealth = hp;
                m_ActiveBoss.currentHealth = hp;
                m_ActiveBoss.damage = (wave == 10) ? 55f : 40f;
                m_ActiveBoss.chaseSpeed = (wave == 10) ? 3.8f : 3.4f;
                m_ActiveBoss.attackRange = 4.0f;
                m_ActiveBoss.detectionRange = 140.0f;
                bossObj.transform.localScale = Vector3.one * (wave == 10 ? 2.0f : 1.75f);

                if (m_HUD != null)
                {
                    m_HUD.UpdateBossHealth(hp, hp);
                    m_HUD.ShowWaveStatus("<b><color=#FF2222>⚠ WARNING: GOLIATH BERSERKER (BOSS) HAS ENTERED THE OASIS! ⚠</color></b>", new Color(1f, 0.2f, 0.2f), 4.0f);
                }
            }
        }
    }

    private GameObject SpawnSingleZombie(GameObject prefab, Vector3 spawnPos, int wave, bool isBoss)
    {
        if (prefab == null)
        {
            prefab = shirtlessGhoulPrefab ?? suitCivilianPrefab ?? skinlessBerserkerPrefab;
        }
        if (prefab == null) return null;

        var zombieObj = Instantiate(prefab, m_ZombieParent.transform);
        zombieObj.name = $"Wave_{wave}_Zombie_{prefab.name}";

        RaycastHit[] hits = Physics.RaycastAll(new Vector3(spawnPos.x, 80.0f, spawnPos.z), Vector3.down, 140.0f);
        float bestGroundY = 0f;
        bool foundGround = false;

        for (int i = 0; i < hits.Length; i++)
        {
            var h = hits[i];
            if (h.collider == null || h.collider.isTrigger) continue;
            string n = h.collider.name.ToLower();
            if (n.Contains("tree") || n.Contains("palm") || n.Contains("leaf") || n.Contains("tent") || n.Contains("canopy") || n.Contains("prop") || n.Contains("boundary"))
                continue;
            if (h.normal.y < 0.40f) continue;

            if (h.point.y > bestGroundY || !foundGround)
            {
                bestGroundY = h.point.y;
                foundGround = true;
            }
        }

        spawnPos.y = foundGround ? bestGroundY : spawnPos.y;
        zombieObj.transform.position = spawnPos;

        var ai = zombieObj.GetComponent<ZombieAI>();
        if (ai != null)
        {
            ai.ammoBoxPrefab = ammoBoxPrefab;
            ai.healthJarPrefab = healthJarPrefab;
            ai.shieldDropPrefab = shieldDropPrefab;
            ai.berserkDropPrefab = berserkDropPrefab;

            if (!isBoss)
            {
                ai.maxHealth *= (1.0f + (wave - 1) * 0.08f);
                ai.currentHealth = ai.maxHealth;
            }
            m_ActiveZombies.Add(ai);
        }

        return zombieObj;
    }

    private GameObject SelectPrefabForWave(int wave, int index)
    {
        if (wave == 1)
        {
            return (index % 2 == 0) ? suitCivilianPrefab : shirtlessGhoulPrefab;
        }
        else if (wave == 2)
        {
            if (index % 3 == 0) return shirtlessCrawlerPrefab;
            return (index % 2 == 0) ? shirtlessGhoulPrefab : suitCivilianPrefab;
        }
        else if (wave <= 4)
        {
            int r = index % 4;
            if (r == 0) return skinlessBerserkerPrefab;
            if (r == 1) return shirtlessCrawlerPrefab;
            if (r == 2) return shirtlessGhoulPrefab;
            return suitCivilianPrefab;
        }
        else
        {
            int r = index % 5;
            if (r == 0 || r == 1) return skinlessBerserkerPrefab;
            if (r == 2) return shirtlessCrawlerPrefab;
            if (r == 3) return shirtlessGhoulPrefab;
            return suitCivilianPrefab;
        }
    }

    private IEnumerator WaveClearedRoutine()
    {
        isWaveActive = false;
        m_IsTransitioning = true;
        if (m_SpawnLoopCoroutine != null) StopCoroutine(m_SpawnLoopCoroutine);

        string clearedText = (currentWave == 5) ? "<b>★ BOSS DEFEATED! ★</b>" : $"<b>★ WAVE {currentWave} CLEARED! ★</b>";
        if (m_HUD != null)
        {
            m_HUD.ShowWaveStatus(clearedText, new Color(0.3f, 0.95f, 0.4f, 1f), 3.0f);
        }

        var scoreMgr = Score.Instance ?? FindAnyObjectByType<Score>();
        if (scoreMgr != null) scoreMgr.AddWaveClearScore(currentWave);

        SpawnSupplies();

        yield return new WaitForSeconds(3.0f);

        currentWave++;
        StartCoroutine(StartNextWaveRoutine());
    }

    private IEnumerator VictoryRoutine()
    {
        isWaveActive = false;
        m_IsTransitioning = true;
        if (m_SpawnLoopCoroutine != null) StopCoroutine(m_SpawnLoopCoroutine);

        var player = FindAnyObjectByType<PlayerController>();
        if (player != null) player.UnlockCursor();

        int totalKills = 275;
        var scoreMgr = Score.Instance ?? FindAnyObjectByType<Score>();
        int finalScore = scoreMgr != null ? scoreMgr.currentScore : 0;
        int highScore = scoreMgr != null ? scoreMgr.highScore : 0;

        var authMgr = AuthManager.Instance ?? FindAnyObjectByType<AuthManager>();
        var netMgr = NetworkManager.Instance ?? FindAnyObjectByType<NetworkManager>();
        if (netMgr != null && authMgr != null && authMgr.isLoggedIn)
        {
            var hudComp = FindAnyObjectByType<GunHUD>();
            int totKills = hudComp != null ? hudComp.TotalKills : totalKills;
            int hs = hudComp != null ? hudComp.HeadshotKills : 0;
            int dur = Mathf.RoundToInt(Time.timeSinceLevelLoad);

            StartCoroutine(netMgr.SubmitMatchResultRoutine(authMgr.playerId, finalScore, maxWaves, totKills, hs, dur, true));
        }

        var hud = FindAnyObjectByType<GunHUD>();
        if (hud != null)
        {
            hud.ShowVictoryScreen(totalKills, finalScore, highScore);
        }

        yield return null;
    }

    private void UpdateTornadoes(bool active)
    {
        var tornadoes = Object.FindObjectsByType<TornadoWanderController>(FindObjectsInactive.Include);
        foreach (var t in tornadoes)
        {
            if (t != null) t.SetTornadoActive(active);
        }
    }

    private void SpawnSupplies()
    {
        for (int i = 0; i < supplyLocations.Length; i++)
        {
            Vector3 pos = supplyLocations[i];
            RaycastHit[] hits = Physics.RaycastAll(new Vector3(pos.x, pos.y + 10f, pos.z), Vector3.down, 20.0f);
            foreach (var h in hits)
            {
                if (h.collider != null && !h.collider.isTrigger && h.normal.y > 0.5f)
                {
                    pos.y = h.point.y + 0.35f;
                    break;
                }
            }

            GameObject prefabToSpawn = null;
            string dropName = "Campsite_SupplyDrop";

            if (i == 0)
            {

                prefabToSpawn = ammoBoxPrefab;
                dropName = "Campsite_AmmoBox";
            }
            else if (i == 1)
            {

                prefabToSpawn = healthJarPrefab;
                dropName = "Campsite_HealthJar";
            }
            else
            {

                if (currentWave <= 5 && m_ShieldDropsWaves1To5 < 3 && (currentWave == 2 || currentWave == 3 || currentWave == 5 || m_ShieldDropsWaves1To5 < (currentWave - 1)))
                {
                    prefabToSpawn = shieldDropPrefab ?? healthJarPrefab;
                    dropName = "Campsite_ShieldDrop";
                    m_ShieldDropsWaves1To5++;
                }
                else if (currentWave > 5 && m_ShieldDropsWaves6To10 < 3 && (currentWave == 6 || currentWave == 8 || currentWave == 10 || m_ShieldDropsWaves6To10 < (currentWave - 6)))
                {
                    prefabToSpawn = shieldDropPrefab ?? healthJarPrefab;
                    dropName = "Campsite_ShieldDrop";
                    m_ShieldDropsWaves6To10++;
                }
                else if ((currentWave == 5 || currentWave == 10) && berserkDropPrefab != null)
                {
                    prefabToSpawn = berserkDropPrefab;
                    dropName = "Campsite_BerserkDrop";
                }
                else
                {
                    prefabToSpawn = (Random.value > 0.5f) ? ammoBoxPrefab : healthJarPrefab;
                }
            }

            if (prefabToSpawn != null)
            {
                var drop = Instantiate(prefabToSpawn, pos, Quaternion.identity);
                drop.name = dropName;
            }
        }
    }
}

