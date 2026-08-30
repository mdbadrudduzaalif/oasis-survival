using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public enum ZombieType
{
    Walker,
    Runner,
    Tank,
    Crawler,
    Boss
}

public class ZombieAI : MonoBehaviour
{
    private static readonly List<ZombieAI> s_AllActiveZombies = new List<ZombieAI>();

    [Header("Zombie Profile")]
    public ZombieType zombieType = ZombieType.Walker;
    public float maxHealth = 100f;
    public float currentHealth;
    public float damage = 25f;
    public float attackRange = 1.8f;
    public float detectionRange = 45.0f;
    public float chaseSpeed = 2.8f;
    public float attackCooldown = 1.0f;

    [Header("Random Kill Pickup Drops")]
    [Range(0f, 1f)] public float dropChance = 0.35f;
    public GameObject ammoBoxPrefab;
    public GameObject healthJarPrefab;
    public GameObject shieldDropPrefab;
    public GameObject berserkDropPrefab;

    [Header("Audio")]
    public AudioClip[] idleGroans;
    public AudioClip[] attackRoars;
    public AudioClip[] hurtSounds;
    public AudioClip[] deathSounds;
    public AudioClip[] biteSounds;
    public float minGroanInterval = 3.0f;
    public float maxGroanInterval = 8.0f;

    [Header("Boss Audio Suite")]
    public AudioClip bossPunchImpact;
    public AudioClip bossPunchGrunt;
    public AudioClip bossClawSlash;
    public AudioClip bossClawSnarl;
    public AudioClip bossFastWhoosh;
    public AudioClip bossLeapLaunch;
    public AudioClip bossLeapAirWhoosh;
    public AudioClip bossLeapCraterSlam;
    public AudioClip bossStandingRoar;
    public AudioClip bossFootstepStomp;
    public AudioClip bossDeathBodySlam;
    public AudioClip bossDeathWhisper;

    [Header("Ground Alignment")]
    public float groundHeightOffset = 0.0f;

    private Animator m_Animator;
    private AudioSource m_AudioSource;
    private Transform m_Player;
    private PlayerController m_PlayerController;
    private bool m_IsDead;
    private float m_LastAttackTime;
    private float m_NextGroanTime;
    private bool m_HasAlerted;
    private int m_AttackVariantCount = 3;
    private Vector3 m_KineticImpulse;
    private float m_LastHitReactionTime;
    private float m_NextLeapTime;
    private bool m_IsLeaping;
    private float m_RoarEndTime;
    private float m_LastRoarAudioTime = -999f;
    private float m_NextBossCombatRoarTime;

    [Header("Boss Roar Rage Buff (Minions Only)")]
    public float rageDuration = 5.0f;
    public float rageSpeedMultiplier = 1.5f;
    public float rageDamageMultiplier = 1.45f;
    private float m_RageEndTime = -1f;

    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int AttackTriggerHash = Animator.StringToHash("Attack");
    private static readonly int AttackTypeHash = Animator.StringToHash("AttackType");
    private static readonly int IsDeadHash = Animator.StringToHash("IsDead");
    private static readonly int HitReactionHash = Animator.StringToHash("HitReaction");

    public bool IsDead => m_IsDead;
    public bool IsLeaping => m_IsLeaping;
    public bool IsRoaring => Time.time < m_RoarEndTime;
    public bool IsEnraged => Time.time < m_RageEndTime && zombieType != ZombieType.Boss;

    private void SetAnimFloat(int id, float val, float dampTime, float deltaTime)
    {
        if (m_Animator != null && m_Animator.runtimeAnimatorController != null)
        {
            m_Animator.SetFloat(id, val, dampTime, deltaTime);
        }
    }

    private void SetAnimFloat(int id, float val)
    {
        if (m_Animator != null && m_Animator.runtimeAnimatorController != null)
        {
            m_Animator.SetFloat(id, val);
        }
    }

    private void SetAnimTrigger(int id)
    {
        if (m_Animator != null && m_Animator.runtimeAnimatorController != null)
        {
            m_Animator.SetTrigger(id);
        }
    }

    private void SetAnimBool(int id, bool val)
    {
        if (m_Animator != null && m_Animator.runtimeAnimatorController != null)
        {
            m_Animator.SetBool(id, val);
        }
    }

    private void SetAnimInteger(int id, int val)
    {
        if (m_Animator != null && m_Animator.runtimeAnimatorController != null)
        {
            m_Animator.SetInteger(id, val);
        }
    }

    private void ResetAnimTrigger(int id)
    {
        if (m_Animator != null && m_Animator.runtimeAnimatorController != null)
        {
            m_Animator.ResetTrigger(id);
        }
    }

    private void Awake()
    {
        m_Animator = GetComponent<Animator>() ?? GetComponentInChildren<Animator>();
        m_AudioSource = GetComponent<AudioSource>();
        if (m_AudioSource == null) m_AudioSource = gameObject.AddComponent<AudioSource>();
        if (zombieType == ZombieType.Boss)
        {
            m_AudioSource.spatialBlend = 0.35f;
            m_AudioSource.rolloffMode = AudioRolloffMode.Linear;
            m_AudioSource.minDistance = 15.0f;
            m_AudioSource.maxDistance = 80.0f;
        }
        else
        {
            m_AudioSource.spatialBlend = 1.0f;
            m_AudioSource.rolloffMode = AudioRolloffMode.Logarithmic;
            m_AudioSource.minDistance = 2.0f;
            m_AudioSource.maxDistance = 30.0f;
        }
        m_AudioSource.playOnAwake = false;

        ApplyArchetypeStats();
        SetupHitbox();
        currentHealth = maxHealth;
        m_NextGroanTime = Time.time + Random.Range(1.5f, 5.0f);
        m_NextLeapTime = Time.time + Random.Range(3.5f, 6.5f);

        if (zombieType == ZombieType.Boss)
        {
            LoadBossAudioClips();
        }

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

        if (ammoBoxPrefab == null)
            ammoBoxPrefab = Resources.Load<GameObject>("AmmoBox_Pickup");
        if (healthJarPrefab == null)
            healthJarPrefab = Resources.Load<GameObject>("HealthJar_Pickup");
    }

    private void SetupHitbox()
    {
        var col = GetComponent<CapsuleCollider>();
        if (col == null) col = gameObject.AddComponent<CapsuleCollider>();

        col.isTrigger = false;

        switch (zombieType)
        {
            case ZombieType.Crawler:

                col.center = new Vector3(0f, 0.35f, 0.15f);
                col.radius = 0.55f;
                col.height = 1.45f;
                col.direction = 2;
                break;

            case ZombieType.Boss:

                col.center = new Vector3(0f, 1.05f, 0.05f);
                col.radius = 0.75f;
                col.height = 2.15f;
                col.direction = 1;
                break;

            default:

                col.center = new Vector3(0f, 0.95f, 0f);
                col.radius = 0.45f;
                col.height = 1.90f;
                col.direction = 1;
                break;
        }
    }

    private void OnEnable()
    {
        if (!s_AllActiveZombies.Contains(this))
            s_AllActiveZombies.Add(this);
    }

    private void OnDisable()
    {
        s_AllActiveZombies.Remove(this);
    }

    public void ApplyArchetypeStats()
    {
        SetupHitbox();

        switch (zombieType)
        {
            case ZombieType.Runner:
                maxHealth = 45f;
                damage = 15f;
                chaseSpeed = 4.8f;
                attackRange = 1.8f;
                detectionRange = 40f;
                attackCooldown = 1.0f;
                groundHeightOffset = 0.05f;
                m_AttackVariantCount = 3;
                break;

            case ZombieType.Crawler:
                maxHealth = 60f;
                damage = 20f;
                chaseSpeed = 2.2f;
                attackRange = 1.5f;
                detectionRange = 30f;
                attackCooldown = 1.4f;
                groundHeightOffset = -0.05f;
                m_AttackVariantCount = 2;
                break;

            case ZombieType.Tank:
                maxHealth = 220f;
                damage = 35f;
                chaseSpeed = 2.4f;
                attackRange = 2.2f;
                detectionRange = 35f;
                attackCooldown = 1.8f;
                groundHeightOffset = 0.05f;
                m_AttackVariantCount = 3;
                break;

            case ZombieType.Boss:
                maxHealth = 3000f;
                damage = 40f;
                chaseSpeed = 3.5f;
                attackRange = 4.0f;
                detectionRange = 140f;
                attackCooldown = 2.0f;
                groundHeightOffset = 0.08f;
                m_AttackVariantCount = 3;
                break;

            default:
                maxHealth = 80f;
                damage = 22f;
                chaseSpeed = 3.0f;
                attackRange = 1.9f;
                detectionRange = 35f;
                attackCooldown = 1.3f;
                groundHeightOffset = 0.05f;
                m_AttackVariantCount = 3;
                break;
        }

        currentHealth = maxHealth;
    }

    public static ZombieAI FindClosestActiveZombie(Vector3 fromPosition, float maxRadius = 50.0f)
    {
        ZombieAI closest = null;
        float minDistSq = maxRadius * maxRadius;

        for (int i = 0; i < s_AllActiveZombies.Count; i++)
        {
            var z = s_AllActiveZombies[i];
            if (z == null || z.m_IsDead) continue;

            float distSq = (z.transform.position - fromPosition).sqrMagnitude;
            if (distSq < minDistSq)
            {
                minDistSq = distSq;
                closest = z;
            }
        }

        return closest;
    }

    private GameObject m_RageEyesRoot;
    private static Material s_RageEyeMaterial;

    private void SetupRageEyeGlow()
    {
        if (zombieType == ZombieType.Boss || m_RageEyesRoot != null) return;

        if (s_RageEyeMaterial == null)
        {
            Shader unlitShader = Shader.Find("Universal Render Pipeline/Particles/Unlit") ??
                                 Shader.Find("Universal Render Pipeline/Unlit") ??
                                 Shader.Find("Particles/Standard Unlit") ??
                                 Shader.Find("Unlit/Color");
            s_RageEyeMaterial = new Material(unlitShader);
            s_RageEyeMaterial.SetColor("_BaseColor", new Color(1.0f, 0.08f, 0.08f, 0.95f));
            s_RageEyeMaterial.SetColor("_Color", new Color(1.0f, 0.08f, 0.08f, 0.95f));
        }

        Transform eyeBoneL = null;
        Transform eyeBoneR = null;
        foreach (var t in GetComponentsInChildren<Transform>())
        {
            string n = t.name.ToLower();
            if (n == "unrealeye_l" || n == "eye_l" || n == "eyel" || n == "eye.l" || n.EndsWith("eye_l"))
                eyeBoneL = t;
            else if (n == "unrealeye_r" || n == "eye_r" || n == "eyer" || n == "eye.r" || n.EndsWith("eye_r"))
                eyeBoneR = t;
        }

        m_RageEyesRoot = new GameObject("Rage_GlowingEyes");

        if (eyeBoneL != null && eyeBoneR != null)
        {
            m_RageEyesRoot.transform.SetParent(eyeBoneL.parent, false);

            CreateSingleEyeGlow("LeftEyeGlow", eyeBoneL.position, eyeBoneL);
            CreateSingleEyeGlow("RightEyeGlow", eyeBoneR.position, eyeBoneR);

            var lightObj = new GameObject("RageEyeLight");
            lightObj.transform.SetParent(m_RageEyesRoot.transform, false);
            lightObj.transform.position = (eyeBoneL.position + eyeBoneR.position) * 0.5f;
            var pLight = lightObj.AddComponent<Light>();
            pLight.type = LightType.Point;
            pLight.color = new Color(1.0f, 0.15f, 0.12f);
            pLight.intensity = 0.75f;
            pLight.range = 0.85f;
            pLight.shadows = LightShadows.None;
        }
        else
        {

            Transform head = null;
            if (m_Animator != null && m_Animator.isHuman)
            {
                head = m_Animator.GetBoneTransform(HumanBodyBones.Head);
            }
            if (head == null)
            {
                foreach (var t in GetComponentsInChildren<Transform>())
                {
                    string n = t.name.ToLower();
                    if (n == "face" || n == "head" || n.Contains("head") || n == "spine.005")
                    {
                        head = t;
                        break;
                    }
                }
            }
            if (head == null) head = transform;

            m_RageEyesRoot.transform.SetParent(head, false);

            Vector3 leftOffset = new Vector3(-0.038f, 0.065f, 0.09f);
            Vector3 rightOffset = new Vector3(0.038f, 0.065f, 0.09f);

            if (zombieType == ZombieType.Crawler)
            {
                leftOffset = new Vector3(-0.035f, 0.05f, 0.12f);
                rightOffset = new Vector3(0.035f, 0.05f, 0.12f);
            }

            CreateSingleEyeGlowLocal("LeftEye", leftOffset, m_RageEyesRoot.transform);
            CreateSingleEyeGlowLocal("RightEye", rightOffset, m_RageEyesRoot.transform);

            var lightObj = new GameObject("RageEyeLight");
            lightObj.transform.SetParent(m_RageEyesRoot.transform, false);
            lightObj.transform.localPosition = (leftOffset + rightOffset) * 0.5f;
            var pLight = lightObj.AddComponent<Light>();
            pLight.type = LightType.Point;
            pLight.color = new Color(1.0f, 0.15f, 0.12f);
            pLight.intensity = 0.75f;
            pLight.range = 0.85f;
            pLight.shadows = LightShadows.None;
        }

        m_RageEyesRoot.SetActive(false);
    }

    private void CreateSingleEyeGlow(string name, Vector3 worldPos, Transform parent)
    {
        var eye = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        eye.name = name;
        eye.transform.SetParent(parent, false);
        eye.transform.position = worldPos;
        eye.transform.localScale = Vector3.one * 0.022f;

        var col = eye.GetComponent<Collider>();
        if (col != null) Destroy(col);

        var r = eye.GetComponent<MeshRenderer>();
        if (r != null)
        {
            r.sharedMaterial = s_RageEyeMaterial;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
        }
    }

    private void CreateSingleEyeGlowLocal(string name, Vector3 localPos, Transform parent)
    {
        var eye = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        eye.name = name;
        eye.transform.SetParent(parent, false);
        eye.transform.localPosition = localPos;
        eye.transform.localScale = Vector3.one * 0.022f;

        var col = eye.GetComponent<Collider>();
        if (col != null) Destroy(col);

        var r = eye.GetComponent<MeshRenderer>();
        if (r != null)
        {
            r.sharedMaterial = s_RageEyeMaterial;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
        }
    }

    private void Start()
    {
        FindPlayer();
        SetupRageEyeGlow();
        if (TryGetGroundHeight(transform.position, out float groundY))
        {
            Vector3 pos = transform.position;
            pos.y = groundY + groundHeightOffset;
            transform.position = pos;
        }
    }

    private void FindPlayer()
    {
        var p = GameObject.FindWithTag("Player") ?? GameObject.Find("PlayerCapsule");
        if (p != null)
        {
            m_Player = p.transform;
            m_PlayerController = p.GetComponent<PlayerController>() ?? p.GetComponentInParent<PlayerController>();
        }
    }

    public static void BroadcastGunshotAlert(Vector3 soundPos, float alertRadius = 80.0f)
    {
        for (int i = 0; i < s_AllActiveZombies.Count; i++)
        {
            var z = s_AllActiveZombies[i];
            if (z != null && !z.m_IsDead)
            {
                float d = Vector3.Distance(z.transform.position, soundPos);
                if (d <= alertRadius)
                {
                    z.AlertToPlayer();
                }
            }
        }
    }

    public void PlayBossRoar()
    {
        if (m_IsDead || Time.time < m_LastRoarAudioTime + 4.5f) return;
        m_LastRoarAudioTime = Time.time;
        m_RoarEndTime = Time.time + 4.0f;
        m_NextBossCombatRoarTime = Time.time + Random.Range(14.0f, 20.0f);
        SetAnimFloat(SpeedHash, 0f);
        SetAnimTrigger(HitReactionHash);
        StartCoroutine(PlayDelayedRoarRoutine(0.65f));

        BroadcastBossRoarBuff(5.0f, 1.5f, 1.45f);
    }

    public void BroadcastBossRoarBuff(float duration = 5.0f, float speedMult = 1.5f, float dmgMult = 1.45f)
    {
        if (zombieType != ZombieType.Boss) return;

        for (int i = 0; i < s_AllActiveZombies.Count; i++)
        {
            var z = s_AllActiveZombies[i];
            if (z != null && z != this && z.zombieType != ZombieType.Boss && !z.m_IsDead)
            {
                z.ApplyBossRageBuff(duration, speedMult, dmgMult);
            }
        }
    }

    public void ApplyBossRageBuff(float duration = 5.0f, float speedMult = 1.5f, float dmgMult = 1.45f)
    {
        if (m_IsDead || zombieType == ZombieType.Boss) return;

        m_RageEndTime = Time.time + duration;
        rageSpeedMultiplier = speedMult;
        rageDamageMultiplier = dmgMult;

        m_HasAlerted = true;
        detectionRange = Mathf.Max(detectionRange, 150.0f);
        if (m_Player == null) FindPlayer();
        if (m_Player != null) FaceTarget(m_Player.position);

        if (Random.value < 0.65f)
        {
            PlayRandomSound(attackRoars, 1.0f);
        }
    }

    private IEnumerator PlayDelayedRoarRoutine(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (!m_IsDead)
        {
            PlayClip(bossStandingRoar, 1.0f);
        }
    }

    public void AlertToPlayer()
    {
        if (m_IsDead) return;
        if (!m_HasAlerted)
        {
            m_HasAlerted = true;

            if (zombieType == ZombieType.Boss)
            {
                PlayBossRoar();
            }
            else
            {
                PlayRandomSound(attackRoars, 1.0f);
            }
        }
        detectionRange = Mathf.Max(detectionRange, 90.0f);
    }

    private void Update()
    {
        if (m_RageEyesRoot != null)
        {
            bool shouldGlow = IsEnraged && !m_IsDead;
            if (m_RageEyesRoot.activeSelf != shouldGlow)
            {
                m_RageEyesRoot.SetActive(shouldGlow);
            }
        }

        if (m_IsDead || m_IsLeaping) return;

        if (m_Player == null)
        {
            FindPlayer();
            if (m_Player == null) return;
        }

        if (Time.time < m_RoarEndTime)
        {
            SetAnimFloat(SpeedHash, 0f, 0.05f, Time.deltaTime);
            FaceTarget(m_Player.position);

            if (TryGetGroundHeight(transform.position, out float groundedY))
            {
                Vector3 grounded = transform.position;
                grounded.y = Mathf.MoveTowards(transform.position.y, groundedY + groundHeightOffset, Time.deltaTime * 10.0f);
                transform.position = grounded;
            }
            return;
        }

        if (m_KineticImpulse.sqrMagnitude > 0.001f)
        {
            transform.position += m_KineticImpulse * Time.deltaTime;
            m_KineticImpulse = Vector3.Lerp(m_KineticImpulse, Vector3.zero, Time.deltaTime * 8.0f);
        }

        float distanceToPlayer = Vector3.Distance(transform.position, m_Player.position);

        if (Time.time >= m_NextGroanTime)
        {
            if (zombieType != ZombieType.Boss)
            {
                PlayRandomSound(idleGroans, 0.7f);
            }
            m_NextGroanTime = Time.time + Random.Range(minGroanInterval, maxGroanInterval);
        }

        if (distanceToPlayer <= attackRange)
        {
            SetAnimFloat(SpeedHash, 0f, 0.10f, Time.deltaTime);
            FaceTarget(m_Player.position);

            if (Time.time >= m_LastAttackTime + attackCooldown)
            {
                PerformAttack();
            }

            if (TryGetGroundHeight(transform.position, out float targetY))
            {
                Vector3 grounded = transform.position;
                grounded.y = Mathf.MoveTowards(transform.position.y, targetY + groundHeightOffset, Time.deltaTime * 10.0f);
                transform.position = grounded;
            }
        }
        else if (zombieType == ZombieType.Boss && distanceToPlayer >= 14.0f && distanceToPlayer <= 32.0f && Time.time >= m_NextLeapTime)
        {
            StartCoroutine(BossLeapRoutine());
        }
        else if (zombieType == ZombieType.Boss && Time.time >= m_NextBossCombatRoarTime && Time.time >= m_RoarEndTime)
        {
            PlayBossRoar();
        }
        else if (distanceToPlayer <= detectionRange || m_HasAlerted || IsEnraged)
        {
            if (!m_HasAlerted)
            {
                AlertToPlayer();
            }

            FaceTarget(m_Player.position);

            Vector3 targetDir = (m_Player.position - transform.position);
            targetDir.y = 0;
            Vector3 direction = targetDir.normalized;

            Vector3 separationForce = Vector3.zero;
            int neighborCount = 0;
            Collider[] nearbyColliders = Physics.OverlapSphere(transform.position + Vector3.up * 0.5f, 1.4f);
            for (int i = 0; i < nearbyColliders.Length; i++)
            {
                var nCol = nearbyColliders[i];
                if (nCol == null || nCol.transform == transform || nCol.transform.IsChildOf(transform)) continue;
                var otherAI = nCol.GetComponentInParent<ZombieAI>();
                if (otherAI != null && !otherAI.m_IsDead)
                {
                    Vector3 diff = transform.position - otherAI.transform.position;
                    diff.y = 0;
                    float dist = diff.magnitude;
                    if (dist < 1.2f && dist > 0.001f)
                    {
                        separationForce += (diff.normalized / dist);
                        neighborCount++;
                    }
                }
            }
            if (neighborCount > 0)
            {
                direction = (direction + separationForce.normalized * 1.6f).normalized;
            }

            if (Physics.Raycast(transform.position + Vector3.up * 0.6f, direction, out RaycastHit wallHit, 0.8f, ~LayerMask.GetMask("Ignore Raycast")))
            {
                if (!wallHit.collider.isTrigger && !wallHit.transform.IsChildOf(transform) && !wallHit.collider.CompareTag("Player"))
                {
                    Vector3 slideDir = Vector3.Cross(wallHit.normal, Vector3.up);
                    if (Vector3.Dot(slideDir, targetDir) < 0) slideDir = -slideDir;
                    direction = (direction * 0.4f + slideDir * 0.6f).normalized;
                }
            }

            float effectiveSpeed = chaseSpeed * (IsEnraged ? rageSpeedMultiplier : 1.0f);
            Vector3 intendedPos = transform.position + direction * effectiveSpeed * Time.deltaTime;
            Vector3 nextPos = intendedPos;

            if (TryGetGroundHeight(nextPos, out float targetY))
            {
                float speed = Mathf.Abs(transform.position.y - (targetY + groundHeightOffset)) > 1.0f ? 25.0f : 12.0f;
                nextPos.y = Mathf.MoveTowards(transform.position.y, targetY + groundHeightOffset, Time.deltaTime * speed);
            }
            else
            {
                nextPos.y -= 9.8f * Time.deltaTime;
            }
            transform.position = nextPos;

            float animSpeedParam = 1.5f;
            if (zombieType == ZombieType.Runner) animSpeedParam = 3.2f;
            else if (zombieType == ZombieType.Crawler) animSpeedParam = 2.8f;
            else if (zombieType == ZombieType.Walker) animSpeedParam = 2.0f;

            if (IsEnraged) animSpeedParam *= 1.35f;

            SetAnimFloat(SpeedHash, direction != Vector3.zero ? animSpeedParam : 0f, 0.12f, Time.deltaTime);
        }
        else
        {
            SetAnimFloat(SpeedHash, 0f, 0.15f, Time.deltaTime);

            if (TryGetGroundHeight(transform.position, out float idleY))
            {
                Vector3 grounded = transform.position;
                grounded.y = Mathf.MoveTowards(transform.position.y, idleY + groundHeightOffset, Time.deltaTime * 10.0f);
                transform.position = grounded;
            }
        }
    }

    public bool TryGetGroundHeight(Vector3 samplePos, out float groundHeight)
    {
        groundHeight = samplePos.y;
        RaycastHit[] hits = Physics.RaycastAll(new Vector3(samplePos.x, samplePos.y + 8.0f, samplePos.z), Vector3.down, 25.0f);
        float bestGroundY = float.MinValue;
        bool found = false;

        for (int i = 0; i < hits.Length; i++)
        {
            var h = hits[i];
            if (h.collider == null || h.collider.isTrigger) continue;
            if (h.transform == transform || h.transform.IsChildOf(transform)) continue;
            if (h.collider.CompareTag("Player") || (m_Player != null && h.transform.IsChildOf(m_Player))) continue;
            if (h.collider.GetComponentInParent<ZombieAI>() != null) continue;

            string colName = h.collider.gameObject.name.ToLower();

            if (colName.Contains("tree") || colName.Contains("palm") || colName.Contains("leaf") || colName.Contains("leaves") || colName.Contains("branch"))
                continue;
            if (colName.Contains("tent") || colName.Contains("canopy") || colName.Contains("cloth") || colName.Contains("prop") || colName.Contains("barrel"))
                continue;
            if (colName.Contains("boundary") || colName.Contains("bottom_catch") || colName.Contains("trigger"))
                continue;

            if (h.normal.y < 0.40f) continue;

            if (h.point.y > bestGroundY)
            {
                bestGroundY = h.point.y;
                found = true;
            }
        }

        if (found)
        {
            groundHeight = bestGroundY;
        }

        return found;
    }

    private void FaceTarget(Vector3 targetPos)
    {
        Vector3 lookDir = (targetPos - transform.position);
        lookDir.y = 0;
        if (lookDir.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(lookDir);
            float turnSpeed = (zombieType == ZombieType.Runner) ? 10f : 6f;
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * turnSpeed);
        }
    }

    private void PerformAttack()
    {
        m_LastAttackTime = Time.time;

        int chosenAttack = Random.Range(0, m_AttackVariantCount);
        SetAnimInteger(AttackTypeHash, chosenAttack);
        SetAnimTrigger(AttackTriggerHash);

        if (zombieType == ZombieType.Boss)
        {
            if (chosenAttack == 0)
            {

                StartCoroutine(BossPunchRoutine(damage));
            }
            else
            {

                StartCoroutine(BossClawRoutine(damage));
            }
        }
        else
        {
            if (chosenAttack == 3 || zombieType == ZombieType.Crawler)
            {
                PlayRandomSound(biteSounds, 1.0f);
            }
            else
            {
                PlayRandomSound(attackRoars, 1.0f);
            }

            float effectiveDamage = damage * (IsEnraged ? rageDamageMultiplier : 1.0f);
            StartCoroutine(DealDamageAfterDelay(0.35f, effectiveDamage));
        }
    }

    private IEnumerator BossPunchRoutine(float dmgAmount)
    {

        PlayClip(bossPunchGrunt, 1.0f);

        yield return new WaitForSeconds(0.45f);

        if (m_IsDead) yield break;

        PlayClip(bossPunchImpact, 1.0f);

        if (m_Player != null)
        {
            float dist = Vector3.Distance(transform.position, m_Player.position);
            if (dist <= attackRange * 1.35f)
            {
                if (m_PlayerController == null) FindPlayer();
                if (m_PlayerController != null)
                {
                    m_PlayerController.TakeDamage(dmgAmount);
                    var gun = FindAnyObjectByType<Gun>();
                    if (gun != null) gun.TriggerScreenShake(0.35f, 4.5f);
                }
            }
        }
    }

    private IEnumerator BossClawRoutine(float dmgAmount)
    {

        PlayClip(bossClawSnarl, 1.0f);

        yield return new WaitForSeconds(0.45f);
        if (m_IsDead) yield break;
        PlayClip(bossFastWhoosh != null ? bossFastWhoosh : bossPunchGrunt, 0.85f);

        yield return new WaitForSeconds(0.90f);
        if (m_IsDead) yield break;
        PlayClip(bossClawSlash, 1.0f);

        if (m_Player != null)
        {
            float dist = Vector3.Distance(transform.position, m_Player.position);
            if (dist <= attackRange * 1.35f)
            {
                if (m_PlayerController == null) FindPlayer();
                if (m_PlayerController != null)
                {
                    m_PlayerController.TakeDamage(dmgAmount);
                    var gun = FindAnyObjectByType<Gun>();
                    if (gun != null) gun.TriggerScreenShake(0.40f, 5.5f);
                }
            }
        }
    }

    private IEnumerator DealDamageAfterDelay(float delay, float dmgAmount)
    {
        yield return new WaitForSeconds(delay);

        if (!m_IsDead && m_Player != null)
        {
            float dist = Vector3.Distance(transform.position, m_Player.position);
            if (dist <= attackRange * 1.25f)
            {
                if (m_PlayerController == null) FindPlayer();
                if (m_PlayerController != null)
                {
                    m_PlayerController.TakeDamage(dmgAmount);
                }
            }
        }
    }

    public void TakeDamage(float incomingDamage, bool isHeadshot, Vector3 hitPoint, Vector3 hitDirection)
    {
        if (m_IsDead) return;

        AlertToPlayer();
        AlertNearbyPack(30.0f);

        float finalDamage = incomingDamage;

        if (isHeadshot)
        {
            switch (zombieType)
            {
                case ZombieType.Runner:
                    finalDamage = incomingDamage * 3.5f;
                    break;
                case ZombieType.Walker:
                    finalDamage = incomingDamage * 1.25f;
                    break;
                case ZombieType.Tank:
                    finalDamage = incomingDamage * 2.0f;
                    break;
                case ZombieType.Crawler:
                    finalDamage = incomingDamage * 2.2f;
                    break;
                case ZombieType.Boss:
                    finalDamage = incomingDamage * 1.8f;
                    break;
            }
        }
        else
        {
            switch (zombieType)
            {
                case ZombieType.Runner:
                    finalDamage = incomingDamage * 0.60f;
                    break;
                default:
                    finalDamage = incomingDamage * 1.0f;
                    break;
            }
        }

        currentHealth -= finalDamage;
        PlayRandomSound(hurtSounds, 1.0f);

        if (zombieType == ZombieType.Boss)
        {
            var hud = FindAnyObjectByType<GunHUD>();
            if (hud != null) hud.UpdateBossHealth(Mathf.Max(0f, currentHealth), maxHealth);
        }

        if (currentHealth <= 0)
        {
            currentHealth = 0f;
            Die(isHeadshot, hitPoint);
        }
        else
        {
            m_KineticImpulse = hitDirection * (isHeadshot ? 2.5f : 1.5f);

            if (zombieType == ZombieType.Boss)
            {
                if (Time.time >= m_LastHitReactionTime + 12.0f && (isHeadshot ? Random.value < 0.40f : Random.value < 0.20f))
                {
                    m_LastHitReactionTime = Time.time;
                    PlayBossRoar();
                }
            }
            else
            {
                if (isHeadshot || Random.value < 0.35f)
                {
                    SetAnimTrigger(HitReactionHash);
                    PlayRandomSound(hurtSounds, 1.0f);
                }
            }
        }
    }

    private IEnumerator BossLeapRoutine()
    {
        m_IsLeaping = true;
        m_NextLeapTime = Time.time + Random.Range(7.0f, 11.0f);

        SetAnimFloat(SpeedHash, 0f, 0.05f, Time.deltaTime);
        if (m_Player != null) FaceTarget(m_Player.position);

        SetAnimInteger(AttackTypeHash, 2);
        SetAnimTrigger(AttackTriggerHash);
        PlayClip(bossPunchGrunt, 1.0f);

        yield return new WaitForSeconds(0.28f);

        if (m_IsDead || m_Player == null)
        {
            m_IsLeaping = false;
            yield break;
        }

        PlayClip(bossLeapLaunch, 1.0f);

        Vector3 startPos = transform.position;
        Vector3 targetPos = m_Player.position;

        Vector3 approachDir = (m_Player.position - startPos);
        approachDir.y = 0f;
        approachDir = approachDir.normalized;
        Vector3 landPos = targetPos - approachDir * 2.0f;

        if (TryGetGroundHeight(landPos, out float landGroundY))
        {
            landPos.y = landGroundY + groundHeightOffset;
        }

        float jumpDistance = Vector3.Distance(startPos, landPos);
        float leapDuration = Mathf.Clamp(jumpDistance / 18.0f, 0.75f, 1.15f);
        float leapApex = Mathf.Clamp(jumpDistance * 0.22f, 3.5f, 6.0f);

        float elapsed = 0f;
        bool playedWhoosh = false;
        while (elapsed < leapDuration)
        {
            if (m_IsDead) yield break;

            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / leapDuration);

            if (!playedWhoosh && t >= 0.25f)
            {
                playedWhoosh = true;
                PlayClip(bossLeapAirWhoosh, 0.9f);
            }

            float arcHeight = Mathf.Sin(t * Mathf.PI) * leapApex;
            Vector3 currentPos = Vector3.Lerp(startPos, landPos, t);
            currentPos.y += arcHeight;

            transform.position = currentPos;
            if (m_Player != null) FaceTarget(m_Player.position);
            yield return null;
        }

        transform.position = landPos;

        PlayClip(bossLeapCraterSlam != null ? bossLeapCraterSlam : (attackRoars != null && attackRoars.Length > 0 ? attackRoars[0] : null), 1.0f);

        var gun = FindAnyObjectByType<Gun>();
        if (gun != null) gun.TriggerScreenShake(0.45f, 7.0f);

        if (m_Player != null && !m_IsDead)
        {
            float hitDist = Vector3.Distance(transform.position, m_Player.position);
            if (hitDist <= 6.0f)
            {
                if (m_PlayerController == null) FindPlayer();
                if (m_PlayerController != null)
                {
                    m_PlayerController.TakeDamage(25.0f);
                }
            }
        }

        yield return new WaitForSeconds(0.35f);
        m_IsLeaping = false;
    }

    private void AlertNearbyPack(float radius)
    {
        for (int i = 0; i < s_AllActiveZombies.Count; i++)
        {
            var other = s_AllActiveZombies[i];
            if (other != null && other != this && !other.m_IsDead)
            {
                if (Vector3.Distance(transform.position, other.transform.position) <= radius)
                {
                    other.AlertToPlayer();
                }
            }
        }
    }

    private void Die(bool isHeadshot, Vector3 hitPoint)
    {
        if (m_IsDead) return;
        m_IsDead = true;

        s_AllActiveZombies.Remove(this);
        m_KineticImpulse = Vector3.zero;
        if (m_RageEyesRoot != null) m_RageEyesRoot.SetActive(false);

        ResetAnimTrigger(HitReactionHash);
        ResetAnimTrigger(AttackTriggerHash);
        SetAnimFloat(SpeedHash, 0f);
        SetAnimTrigger(IsDeadHash);

        if (zombieType == ZombieType.Boss)
        {
            PlayClip(bossDeathWhisper, 1.0f);
        }
        else
        {
            PlayRandomSound(deathSounds, 1.0f);
        }

        if (isHeadshot)
        {
            SpawnHeadSplatter(hitPoint);
        }

        var col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        var hud = FindAnyObjectByType<GunHUD>();
        if (hud != null) hud.RegisterKill(isHeadshot);

        var scoreMgr = Score.Instance ?? FindAnyObjectByType<Score>();
        if (scoreMgr != null) scoreMgr.AddKillScore(isHeadshot, zombieType == ZombieType.Boss);

        var waveMgr = FindAnyObjectByType<WaveManager>();
        if (waveMgr != null) waveMgr.RegisterZombieDeath(this);

        TryDropRandomPickup();

        StartCoroutine(DeathAndSinkRoutine());
    }

    private void TryDropRandomPickup()
    {
        if (Random.value <= dropChance)
        {
            Vector3 dropPos = transform.position + Vector3.up * 0.45f;
            if (m_PlayerController == null) FindPlayer();

            GameObject prefabToSpawn = null;
            string dropName = "Dropped_AmmoBox";

            if ((zombieType == ZombieType.Boss || Random.value < 0.20f) && berserkDropPrefab != null)
            {
                prefabToSpawn = berserkDropPrefab;
                dropName = "Dropped_Berserk";
            }

            if (prefabToSpawn == null && m_PlayerController != null && m_PlayerController.currentShield < m_PlayerController.maxShield && shieldDropPrefab != null)
            {
                if (Random.value < 0.45f)
                {
                    prefabToSpawn = shieldDropPrefab;
                    dropName = "Dropped_Shield";
                }
            }

            if (prefabToSpawn == null)
            {
                bool spawnHealth = false;
                if (m_PlayerController != null && m_PlayerController.currentHealth < m_PlayerController.maxHealth * 0.7f)
                {
                    spawnHealth = Random.value < 0.65f;
                }
                else
                {
                    spawnHealth = Random.value < 0.35f;
                }

                prefabToSpawn = spawnHealth ? (healthJarPrefab ?? ammoBoxPrefab) : (ammoBoxPrefab ?? healthJarPrefab);
                dropName = spawnHealth ? "Dropped_HealthJar" : "Dropped_AmmoBox";
            }

            if (prefabToSpawn != null)
            {
                var drop = Instantiate(prefabToSpawn, dropPos, Quaternion.identity);
                drop.name = dropName;
            }
        }
    }

    private void SpawnHeadSplatter(Vector3 pos)
    {
        var splatter = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        splatter.name = "HeadSplatterVFX";
        splatter.transform.position = pos;
        splatter.transform.localScale = Vector3.one * 0.35f;
        var r = splatter.GetComponent<Renderer>();
        if (r != null) r.material.color = new Color(0.85f, 0.05f, 0.05f, 0.95f);
        var col = splatter.GetComponent<Collider>();
        if (col != null) Destroy(col);
        Destroy(splatter, 0.5f);
    }

    private IEnumerator DeathAndSinkRoutine()
    {

        yield return new WaitForSeconds(2.35f);
        if (zombieType == ZombieType.Boss)
        {
            PlayClip(bossDeathBodySlam, 1.0f);
        }

        yield return new WaitForSeconds(2.95f);

        float sinkDuration = 2.5f;
        float elapsed = 0f;
        Vector3 startPos = transform.position;
        Vector3 endPos = startPos + Vector3.down * 1.8f;

        while (elapsed < sinkDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / sinkDuration;
            transform.position = Vector3.Lerp(startPos, endPos, t);
            yield return null;
        }

        Destroy(gameObject);
    }

    private void PlayRandomSound(AudioClip[] clips, float volume)
    {
        if (clips == null || clips.Length == 0 || m_AudioSource == null) return;
        AudioClip clip = clips[Random.Range(0, clips.Length)];
        if (clip != null)
        {
            m_AudioSource.PlayOneShot(clip, volume);
        }
    }

    private void PlayClip(AudioClip clip, float volume)
    {
        if (clip != null && m_AudioSource != null)
        {
            m_AudioSource.PlayOneShot(clip, volume);
        }
    }

    private void LoadBossAudioClips()
    {
#if UNITY_EDITOR
        if (bossPunchImpact == null) bossPunchImpact = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Boss/Boss_Punch_Impact.wav") ?? UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Raw/universfield-punch-140236.mp3");
        if (bossPunchGrunt == null) bossPunchGrunt = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Boss/Boss_Punch_Grunt.wav") ?? UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Raw/dragon-studio-animalistic-grunt-463204.mp3");
        if (bossClawSlash == null) bossClawSlash = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Boss/Boss_Claw_Slash.wav") ?? UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Raw/dragon-studio-violent-sword-slice-2-393841.mp3");
        if (bossClawSnarl == null) bossClawSnarl = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Boss/Boss_Claw_Snarl.wav") ?? UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Raw/capaholiczsfx-creature-snarl-very-close-403154.mp3");
        if (bossFastWhoosh == null) bossFastWhoosh = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Boss/Boss_Fast_Whoosh.wav") ?? UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Raw/studiokolomna-fast-whoosh-118248.mp3");
        if (bossLeapLaunch == null) bossLeapLaunch = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Boss/Boss_Leap_Launch.wav") ?? UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Raw/Explosive_ground_pus_#4-1787769558987.mp3");
        if (bossLeapAirWhoosh == null) bossLeapAirWhoosh = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Boss/Boss_Leap_AirWhoosh.wav") ?? UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Raw/Heavy_rushing_wind_a_#4-1787769602349.mp3");
        if (bossLeapCraterSlam == null) bossLeapCraterSlam = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Boss/Boss_Leap_CraterSlam.wav") ?? UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Raw/Seismic_earth-shatte_#3-1787769635697.mp3");
        if (bossStandingRoar == null) bossStandingRoar = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Boss/Boss_Standing_Roar.wav") ?? UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Raw/Deep,_bass-heavy,_re_#3-1787769917870.mp3");
        if (bossFootstepStomp == null) bossFootstepStomp = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Boss/Boss_Footstep_Stomp.wav") ?? UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Raw/Heavy_sand_thud_deep_#3-1787769968403.mp3");
        if (bossDeathBodySlam == null) bossDeathBodySlam = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Boss/Boss_Death_BodySlam.wav") ?? UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Raw/Massive_heavy_sand_b_#1-1787770212041.mp3");
        if (bossDeathWhisper == null) bossDeathWhisper = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Boss/Boss_Death_Whisper.wav") ?? UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Raw/Dying_breath_whisper_#1-1787770251222.mp3");
#endif
    }
}

