using UnityEngine;
using System.Collections;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class Gun : MonoBehaviour
{
    [Header("Gun Stats")]
    public string gunName = "M4A1";
    public float damage = 35.0f;
    public float fireRate = 0.11f;
    public float maxRange = 150.0f;
    public bool isAutomatic = true;

    [Header("Ammo & Reload")]
    public int clipSize = 30;
    public int currentAmmo = 30;
    public int reserveAmmo = 120;
    public float reloadTime = 1.8f;
    public bool isReloading;
    public float reloadProgress;

    [Header("Viewmodel Positions")]
    public Vector3 hipfirePosition = new Vector3(0.24f, -0.22f, 0.48f);
    public Vector3 hipfireRotation = new Vector3(0f, 0f, 0f);
    public Vector3 adsPosition = new Vector3(0.0f, -0.162f, 0.36f);
    public Vector3 adsRotation = new Vector3(0f, 0f, 0f);
    public float adsSpeed = 12.0f;
    public float hipFov = 60.0f;
    public float adsFov = 46.0f;

    [Header("Procedural Recoil Spring")]
    public float recoilKickZ = 0.06f;
    public float recoilMuzzleClimb = 3.2f;
    public float recoilMuzzleRoll = 0.8f;
    public float recoilSnappiness = 20.0f;
    public float recoilReturnSpeed = 10.0f;

    [Header("Sway & Movement")]
    public float swayAmount = 0.003f;
    public float maxSwayAmount = 0.05f;
    public float swaySmoothness = 6.0f;

    [Header("Shell Ejection & Parts")]
    public GameObject shellPrefab;
    public Transform ejectionPort;
    public Transform boltTransform;
    public Transform muzzlePoint;
    public float shellEjectForce = 3.2f;

    [Header("Audio Clips")]
    public AudioClip[] fireSounds;
    public AudioClip reloadDropMag;
    public AudioClip reloadInsertMag;
    public AudioClip reloadBolt;
    public AudioClip emptyClickSound;
    public AudioClip headshotSound;

    [Header("Visual Effects")]
    public Light muzzleFlashLight;
    public ParticleSystem muzzleFlashParticles;

    private Camera m_PlayerCam;
    private AudioSource m_AudioSource;
    private PlayerController m_PlayerController;
    private Animator m_Animator;
    private Transform m_ClipTransform;
    private Vector3 m_ClipDefaultPos;
    private float m_NextFireTime;
    private bool m_IsAiming;

    private Vector3 m_RecoilPosOffset;
    private Vector3 m_RecoilRotOffset;
    private Vector3 m_TargetRecoilPos;
    private Vector3 m_TargetRecoilRot;
    private Vector3 m_BoltDefaultPos;

    private Vector3 m_ReloadPosOffset;
    private Vector3 m_ReloadRotOffset;

    private float m_ShakeTimer;
    private float m_ShakeIntensity;

    private Renderer[] m_GunRenderers;
    private MaterialPropertyBlock m_PropBlock;
    private Light m_BerserkAuraLight;
    private bool m_BerserkWasActive = false;

    public bool IsAiming => m_IsAiming;
    public Camera PlayerCamera => m_PlayerCam;

    private void Awake()
    {
        m_AudioSource = GetComponent<AudioSource>();
        if (m_AudioSource == null) m_AudioSource = gameObject.AddComponent<AudioSource>();
        m_AudioSource.spatialBlend = 0.0f;
        m_AudioSource.playOnAwake = false;

        m_PlayerCam = GetComponentInParent<Camera>();
        if (m_PlayerCam == null) m_PlayerCam = Camera.main;

        m_PlayerController = GetComponentInParent<PlayerController>() ?? FindAnyObjectByType<PlayerController>();
        m_Animator = GetComponent<Animator>() ?? GetComponentInChildren<Animator>();

        m_GunRenderers = GetComponentsInChildren<Renderer>();
        m_PropBlock = new MaterialPropertyBlock();

        var bLightObj = new GameObject("Gun_Berserk_Aura_Light");
        bLightObj.transform.SetParent(transform, false);
        bLightObj.transform.localPosition = new Vector3(0f, 0.05f, 0.4f);
        m_BerserkAuraLight = bLightObj.AddComponent<Light>();
        m_BerserkAuraLight.type = LightType.Point;
        m_BerserkAuraLight.range = 1.8f;
        m_BerserkAuraLight.intensity = 0f;
        m_BerserkAuraLight.color = new Color(1.0f, 0.15f, 0.15f);
        m_BerserkAuraLight.enabled = false;

        currentAmmo = clipSize;

        if (muzzleFlashLight != null)
        {
            muzzleFlashLight.enabled = false;
        }

        if (ejectionPort == null)
        {
            var shutter = transform.Find("CasingExitShutter");
            if (shutter != null) ejectionPort = shutter;
        }

        if (boltTransform == null)
        {
            var bolt = transform.Find("Reload_Pull");
            if (bolt != null) boltTransform = bolt;
        }
        if (boltTransform != null)
        {
            m_BoltDefaultPos = boltTransform.localPosition;
        }

        if (m_ClipTransform == null)
        {
            m_ClipTransform = transform.Find("Clip") ?? transform.Find("M4A1/Clip");
        }
        if (m_ClipTransform != null)
        {
            m_ClipDefaultPos = m_ClipTransform.localPosition;
        }

        if (muzzlePoint == null)
        {
            var mObj = transform.Find("MuzzlePoint");
            if (mObj == null)
            {
                var go = new GameObject("MuzzlePoint");
                go.transform.SetParent(transform);
                go.transform.localPosition = new Vector3(0.00f, 0.08f, 0.78f);
                go.transform.localRotation = Quaternion.identity;
                muzzlePoint = go.transform;
            }
            else
            {
                muzzlePoint = mObj;
                muzzlePoint.localPosition = new Vector3(0.00f, 0.08f, 0.78f);
            }
        }

        if (muzzleFlashLight != null)
        {
            muzzleFlashLight.transform.localPosition = new Vector3(0.00f, 0.08f, 0.78f);
        }

        if (shellPrefab == null)
        {
            shellPrefab = Resources.Load<GameObject>("Bullet_Shell_Physics");
        }

        if (reloadDropMag == null)
        {
            reloadDropMag = Resources.Load<AudioClip>("01_assault_rifle_reload_1_drop_the_mag");
        }
        if (reloadInsertMag == null)
        {
            reloadInsertMag = Resources.Load<AudioClip>("02_assault_rifle_reload_1_insert_the_mag");
        }
        if (reloadBolt == null)
        {
            reloadBolt = Resources.Load<AudioClip>("03_assault_rifle_reload_1_bolt");
        }
    }

    private void Update()
    {
        if (Time.timeScale == 0f) return;

        if (m_PlayerController == null)
        {
            m_PlayerController = GetComponentInParent<PlayerController>() ?? FindAnyObjectByType<PlayerController>();
        }

        if (m_PlayerController != null && m_PlayerController.isDead)
        {
            m_IsAiming = false;
            return;
        }

        HandleAimInput();
        HandleFireInput();
        HandleReloadInput();
        UpdateViewmodelStance();
        UpdateProceduralRecoil();
        UpdateScreenShake();
        UpdateBerserkVisualAura();
    }

    private void UpdateBerserkVisualAura()
    {
        if (m_PlayerController == null)
        {
            m_PlayerController = GetComponentInParent<PlayerController>() ?? FindAnyObjectByType<PlayerController>();
        }

        bool isBerserk = m_PlayerController != null && m_PlayerController.IsBerserk;

        if (m_BerserkAuraLight != null)
        {
            m_BerserkAuraLight.enabled = isBerserk;
            if (isBerserk)
            {
                m_BerserkAuraLight.intensity = 1.8f + Mathf.Sin(Time.time * 8.0f) * 0.6f;
            }
        }

        if (m_GunRenderers != null && m_GunRenderers.Length > 0)
        {
            if (isBerserk)
            {
                Color emissiveRed = new Color(1.0f, 0.08f, 0.08f) * (1.6f + Mathf.Sin(Time.time * 8.0f) * 0.6f);
                for (int i = 0; i < m_GunRenderers.Length; i++)
                {
                    if (m_GunRenderers[i] == null) continue;
                    m_GunRenderers[i].GetPropertyBlock(m_PropBlock);
                    m_PropBlock.SetColor("_EmissionColor", emissiveRed);
                    m_GunRenderers[i].SetPropertyBlock(m_PropBlock);
                }
            }
            else if (m_BerserkWasActive)
            {
                for (int i = 0; i < m_GunRenderers.Length; i++)
                {
                    if (m_GunRenderers[i] == null) continue;
                    m_GunRenderers[i].GetPropertyBlock(m_PropBlock);
                    m_PropBlock.SetColor("_EmissionColor", Color.black);
                    m_GunRenderers[i].SetPropertyBlock(m_PropBlock);
                }
            }
        }
        m_BerserkWasActive = isBerserk;
    }

    private void HandleAimInput()
    {
        if (m_PlayerController != null && m_PlayerController.isDead)
        {
            m_IsAiming = false;
            return;
        }

        bool aimHeld = false;
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null)
        {
            aimHeld = Mouse.current.rightButton.isPressed;
        }
#else
        aimHeld = Input.GetMouseButton(1);
#endif
        m_IsAiming = aimHeld && !isReloading;
    }

    private void HandleFireInput()
    {
        if (m_PlayerController != null && m_PlayerController.isDead) return;

        bool firePressed = false;
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null)
        {
            firePressed = isAutomatic ? Mouse.current.leftButton.isPressed : Mouse.current.leftButton.wasPressedThisFrame;
        }
#else
        firePressed = isAutomatic ? Input.GetMouseButton(0) : Input.GetMouseButtonDown(0);
#endif

        if (firePressed && Time.time >= m_NextFireTime && !isReloading)
        {
            if (currentAmmo > 0)
            {
                Shoot();
            }
            else
            {
                PlayEmptyClick();
                m_NextFireTime = Time.time + 0.25f;
            }
        }
    }

    public void Shoot()
    {
        if (m_PlayerController != null && m_PlayerController.isDead) return;

        currentAmmo--;
        m_NextFireTime = Time.time + fireRate;

        PlayRandomSound(fireSounds, 1.0f);

        ZombieAI.BroadcastGunshotAlert(transform.position, 85.0f);

        float kickMult = m_IsAiming ? 0.45f : 1.0f;
        m_TargetRecoilPos += new Vector3(
            Random.Range(-0.005f, 0.005f) * kickMult,
            Random.Range(0.005f, 0.012f) * kickMult,
            -recoilKickZ * kickMult
        );
        m_TargetRecoilRot += new Vector3(
            -recoilMuzzleClimb * kickMult,
            Random.Range(-recoilMuzzleRoll, recoilMuzzleRoll) * kickMult,
            Random.Range(-0.5f, 0.5f) * kickMult
        );

        TriggerScreenShake(0.08f, m_IsAiming ? 0.6f : 1.2f);

        StartCoroutine(CycleBoltRoutine());

        StartCoroutine(MuzzleFlashRoutine());

        EjectShell();

        PerformRaycast();
    }

    private void PerformRaycast()
    {
        if (m_PlayerCam == null) return;

        Ray ray = new Ray(m_PlayerCam.transform.position, m_PlayerCam.transform.forward);

        RaycastHit[] hits = Physics.RaycastAll(ray, maxRange, ~LayerMask.GetMask("Ignore Raycast"), QueryTriggerInteraction.Ignore);
        if (hits == null || hits.Length == 0) return;

        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        for (int i = 0; i < hits.Length; i++)
        {
            var hit = hits[i];
            if (hit.collider == null || hit.collider.isTrigger) continue;

            if (m_PlayerController != null && hit.collider.transform.root == m_PlayerController.transform.root)
                continue;

            var zombie = hit.collider.GetComponentInParent<ZombieAI>();
            if (zombie != null && !zombie.IsDead)
            {
                bool isHeadshot = CheckHeadshot(hit, zombie);
                float finalDamage = damage;
                if (m_PlayerController != null && m_PlayerController.IsBerserk)
                {
                    finalDamage *= 2.0f;
                }

                zombie.TakeDamage(finalDamage, isHeadshot, hit.point, ray.direction);

                if (isHeadshot && headshotSound != null)
                {
                    m_AudioSource.PlayOneShot(headshotSound, 0.95f);
                }

                var hud = FindAnyObjectByType<GunHUD>();
                if (hud != null) hud.ShowHitmarker(isHeadshot);

                CreateImpactVFX(hit.point, hit.normal, true);
                return;
            }
            else
            {

                CreateImpactVFX(hit.point, hit.normal, false);
                return;
            }
        }
    }

    private bool CheckHeadshot(RaycastHit hit, ZombieAI zombie)
    {
        string colName = hit.collider.name.ToLower();
        if (colName.Contains("head") || colName.Contains("skull") || colName.Contains("neck")) return true;

        Transform headBone = null;
        var anim = zombie.GetComponent<Animator>();
        if (anim != null && anim.isHuman)
        {
            headBone = anim.GetBoneTransform(HumanBodyBones.Head);
        }

        if (headBone == null)
        {
            var transforms = zombie.GetComponentsInChildren<Transform>();
            for (int i = 0; i < transforms.Length; i++)
            {
                string tName = transforms[i].name.ToLower();
                if (tName.Contains("head") && !tName.Contains("top") && !tName.Contains("ik"))
                {
                    headBone = transforms[i];
                    break;
                }
            }
        }

        if (headBone != null)
        {
            float maxHeadDist = (zombie.zombieType == ZombieType.Boss) ? 0.85f : 0.42f;
            if (Vector3.Distance(hit.point, headBone.position) <= maxHeadDist)
                return true;
        }

        if (zombie.zombieType == ZombieType.Crawler)
        {

            Vector3 localHit = zombie.transform.InverseTransformPoint(hit.point);
            if (localHit.z > 0.35f) return true;
        }
        else if (zombie.zombieType == ZombieType.Boss)
        {

            float scaleY = zombie.transform.lossyScale.y;
            float bossHeadThreshold = 1.40f * scaleY;
            float hitRelY = hit.point.y - zombie.transform.position.y;
            if (hitRelY >= bossHeadThreshold) return true;
        }
        else
        {
            float hitRelY = hit.point.y - zombie.transform.position.y;
            float threshold = 1.35f * zombie.transform.lossyScale.y;
            if (hitRelY >= threshold) return true;
        }

        return false;
    }

    private void EjectShell()
    {
        if (shellPrefab == null) return;

        Vector3 spawnPos = ejectionPort != null ? ejectionPort.position : transform.position + transform.right * 0.18f + transform.up * 0.08f;
        var shell = Instantiate(shellPrefab, spawnPos, transform.rotation);

        var shellComp = shell.GetComponent<BulletShell>();
        if (shellComp == null) shellComp = shell.AddComponent<BulletShell>();

        Vector3 ejectVelocity = (transform.right * 2.2f + transform.up * 1.8f + transform.forward * 0.4f);
        Vector3 angularVel = new Vector3(Random.Range(-15f, 15f), Random.Range(-15f, 15f), Random.Range(-15f, 15f));
        shellComp.Eject(ejectVelocity, angularVel);
    }

    private IEnumerator CycleBoltRoutine()
    {
        if (boltTransform == null) yield break;

        Vector3 backPos = m_BoltDefaultPos + Vector3.back * 0.045f;
        boltTransform.localPosition = backPos;
        yield return new WaitForSeconds(0.04f);
        boltTransform.localPosition = m_BoltDefaultPos;
    }

    private IEnumerator MuzzleFlashRoutine()
    {
        if (muzzleFlashLight != null) muzzleFlashLight.enabled = true;
        if (muzzleFlashParticles != null) muzzleFlashParticles.Play();

        Vector3 tipPos = muzzlePoint != null ? muzzlePoint.position : transform.position + transform.forward * 0.78f + transform.up * 0.08f;
        var spark = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        spark.name = "MuzzleSparkVFX";
        spark.transform.position = tipPos;
        spark.transform.localScale = Vector3.one * 0.065f;
        var r = spark.GetComponent<Renderer>();
        if (r != null) r.material.color = new Color(1.0f, 0.85f, 0.2f, 1.0f);
        var col = spark.GetComponent<Collider>();
        if (col != null) Destroy(col);
        Destroy(spark, 0.04f);

        yield return new WaitForSeconds(0.04f);

        if (muzzleFlashLight != null) muzzleFlashLight.enabled = false;
    }

    private void CreateImpactVFX(Vector3 pos, Vector3 normal, bool isFlesh)
    {
        var vfx = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        vfx.name = isFlesh ? "BloodImpactVFX" : "SandImpactVFX";
        vfx.transform.position = pos + normal * 0.02f;
        vfx.transform.localScale = Vector3.one * (isFlesh ? 0.14f : 0.09f);

        var r = vfx.GetComponent<Renderer>();
        if (r != null)
        {
            r.material.color = isFlesh ? new Color(0.85f, 0.05f, 0.05f, 0.95f) : new Color(1.0f, 0.85f, 0.4f, 0.9f);
        }

        var col = vfx.GetComponent<Collider>();
        if (col != null) Destroy(col);
        Destroy(vfx, isFlesh ? 0.45f : 0.25f);
    }

    public void TriggerScreenShake(float duration, float intensity)
    {
        m_ShakeTimer = duration;
        m_ShakeIntensity = intensity;
    }

    private void UpdateScreenShake()
    {
        if (m_PlayerCam == null) return;

        if (m_ShakeTimer > 0f)
        {
            m_ShakeTimer -= Time.deltaTime;
            Vector3 shake = Random.insideUnitSphere * (m_ShakeIntensity * 0.025f);
            m_PlayerCam.transform.localPosition += shake;
        }
    }

    private void HandleReloadInput()
    {
        if (m_PlayerController != null && m_PlayerController.isDead) return;

        bool reloadPressed = false;

#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null)
        {
            reloadPressed = Keyboard.current.rKey.wasPressedThisFrame;
        }
#else
        reloadPressed = Input.GetKeyDown(KeyCode.R);
#endif

        if (reloadPressed && currentAmmo < clipSize && reserveAmmo > 0 && !isReloading)
        {
            StartCoroutine(ReloadRoutine());
        }
    }

    private void PlayGunAnimation(string stateName)
    {
        if (m_Animator != null && m_Animator.runtimeAnimatorController != null)
        {
            m_Animator.Play(stateName, 0, 0f);
        }
    }

    private IEnumerator ReloadRoutine()
    {
        isReloading = true;
        reloadProgress = 0f;

        PlayGunAnimation("ClipOut");

        if (reloadDropMag != null) m_AudioSource.PlayOneShot(reloadDropMag, 0.9f);

        float elapsed = 0f;
        bool inserted = false;
        bool bolted = false;

        while (elapsed < reloadTime)
        {
            elapsed += Time.deltaTime;
            reloadProgress = elapsed / reloadTime;

            float t = reloadProgress;
            float dip = Mathf.Sin(t * Mathf.PI) * -0.06f;
            float roll = Mathf.Sin(t * Mathf.PI) * 14.0f;
            m_ReloadPosOffset = new Vector3(0.01f, dip, -0.02f);
            m_ReloadRotOffset = new Vector3(-8f * Mathf.Sin(t * Mathf.PI), 0f, roll);

            if (m_ClipTransform != null)
            {
                if (t < 0.45f)
                {

                    float dropT = t / 0.45f;
                    m_ClipTransform.localPosition = m_ClipDefaultPos + Vector3.down * (dropT * 0.18f);
                }
                else if (t < 0.70f)
                {

                    float insertT = (t - 0.45f) / 0.25f;
                    m_ClipTransform.localPosition = Vector3.Lerp(m_ClipDefaultPos + Vector3.down * 0.18f, m_ClipDefaultPos, insertT);
                }
                else
                {
                    m_ClipTransform.localPosition = m_ClipDefaultPos;
                }
            }

            if (elapsed >= 0.85f && !inserted)
            {
                inserted = true;
                PlayGunAnimation("ClipIn");
                if (reloadInsertMag != null) m_AudioSource.PlayOneShot(reloadInsertMag, 0.95f);
            }

            if (elapsed >= 1.35f && !bolted)
            {
                bolted = true;
                PlayGunAnimation("PinOut");
                if (reloadBolt != null) m_AudioSource.PlayOneShot(reloadBolt, 0.95f);
                if (boltTransform != null)
                {
                    boltTransform.localPosition = m_BoltDefaultPos + Vector3.back * 0.05f;
                }
            }
            else if (bolted && boltTransform != null && elapsed >= 1.55f)
            {
                boltTransform.localPosition = m_BoltDefaultPos;
            }

            yield return null;
        }

        if (m_ClipTransform != null) m_ClipTransform.localPosition = m_ClipDefaultPos;
        if (boltTransform != null) boltTransform.localPosition = m_BoltDefaultPos;
        PlayGunAnimation("Idle");

        m_ReloadPosOffset = Vector3.zero;
        m_ReloadRotOffset = Vector3.zero;

        int needed = clipSize - currentAmmo;
        int taken = Mathf.Min(needed, reserveAmmo);
        currentAmmo += taken;
        reserveAmmo -= taken;

        isReloading = false;
        reloadProgress = 1f;
    }

    private void UpdateViewmodelStance()
    {
        Vector3 targetPos = m_IsAiming ? adsPosition : hipfirePosition;
        Vector3 targetRot = m_IsAiming ? adsRotation : hipfireRotation;

        transform.localPosition = Vector3.Lerp(transform.localPosition, targetPos + m_RecoilPosOffset + m_ReloadPosOffset, Time.deltaTime * adsSpeed);
        transform.localRotation = Quaternion.Slerp(transform.localRotation, Quaternion.Euler(targetRot + m_RecoilRotOffset + m_ReloadRotOffset), Time.deltaTime * adsSpeed);

        if (m_PlayerCam != null)
        {
            float targetFov = m_IsAiming ? adsFov : hipFov;
            m_PlayerCam.fieldOfView = Mathf.Lerp(m_PlayerCam.fieldOfView, targetFov, Time.deltaTime * adsSpeed);
        }
    }

    private void UpdateProceduralRecoil()
    {
        m_TargetRecoilPos = Vector3.Lerp(m_TargetRecoilPos, Vector3.zero, Time.deltaTime * recoilReturnSpeed);
        m_TargetRecoilRot = Vector3.Lerp(m_TargetRecoilRot, Vector3.zero, Time.deltaTime * recoilReturnSpeed);

        m_RecoilPosOffset = Vector3.Lerp(m_RecoilPosOffset, m_TargetRecoilPos, Time.deltaTime * recoilSnappiness);
        m_RecoilRotOffset = Vector3.Lerp(m_RecoilRotOffset, m_TargetRecoilRot, Time.deltaTime * recoilSnappiness);
    }

    private void PlayEmptyClick()
    {
        if (emptyClickSound != null && m_AudioSource != null)
        {
            m_AudioSource.PlayOneShot(emptyClickSound, 0.8f);
        }
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
}

