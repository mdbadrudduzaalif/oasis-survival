using UnityEngine;

public enum PickupType
{
    Ammo,
    Health,
    Shield,
    Berserk
}

public class SupplyPickup : MonoBehaviour
{
    [Header("Pickup Settings")]
    public PickupType pickupType = PickupType.Ammo;
    public int ammoAmount = 20;
    public float healAmount = 50.0f;
    public float shieldAmount = 50.0f;
    public float berserkDuration = 12.0f;
    public AudioClip pickupSound;

    [Header("Collection Range & Magnetism")]
    public float pickupRadius = 3.2f;
    public float magnetSpeed = 14.0f;
    public float collectDistance = 1.3f;

    [Header("Hover & Animation")]
    public float bobSpeed = 2.4f;
    public float bobHeight = 0.12f;
    public float rotateSpeed = 45.0f;

    private Vector3 m_StartPos;
    private Light m_BeaconLight;
    private Transform m_PlayerTransform;
    private PlayerController m_PlayerController;
    private bool m_IsCollected = false;

    private static AudioClip s_CalmHealthChimeClip;

    private void Start()
    {
        m_StartPos = transform.position;

        if (pickupSound == null)
        {
            if (pickupType == PickupType.Health || pickupType == PickupType.Shield)
            {
                pickupSound = GetOrCreateCalmHealthChime();
            }
#if UNITY_EDITOR
            else
            {
                pickupSound = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/FreeWeaponSounds/AssaultRifle/Foley/assault_rifle_mag_draw.wav");
            }
#endif
        }

        var col = GetComponent<SphereCollider>();
        if (col == null)
        {
            var box = GetComponent<BoxCollider>();
            if (box != null) box.isTrigger = true;
            else
            {
                col = gameObject.AddComponent<SphereCollider>();
                col.isTrigger = true;
                col.radius = pickupRadius;
            }
        }
        else
        {
            col.isTrigger = true;
            col.radius = Mathf.Max(col.radius, pickupRadius);
        }

        m_BeaconLight = GetComponentInChildren<Light>();
        if (m_BeaconLight == null)
        {
            var lightObj = new GameObject("Pickup_Beacon_Light");
            lightObj.transform.SetParent(transform, false);
            lightObj.transform.localPosition = Vector3.up * 0.4f;
            m_BeaconLight = lightObj.AddComponent<Light>();
            m_BeaconLight.type = LightType.Point;
            m_BeaconLight.range = 4.0f;
            m_BeaconLight.intensity = 1.8f;
            if (pickupType == PickupType.Ammo) m_BeaconLight.color = new Color(1.0f, 0.85f, 0.2f);
            else if (pickupType == PickupType.Health) m_BeaconLight.color = new Color(0.2f, 1.0f, 0.4f);
            else if (pickupType == PickupType.Shield) m_BeaconLight.color = new Color(0.0f, 0.85f, 1.0f);
            else if (pickupType == PickupType.Berserk) m_BeaconLight.color = new Color(1.0f, 0.15f, 0.15f);
        }

        FindPlayer();
    }

    public static AudioClip GetOrCreateCalmHealthChime()
    {
        if (s_CalmHealthChimeClip != null) return s_CalmHealthChimeClip;

        int sampleRate = 44100;
        float duration = 0.85f;
        int sampleCount = Mathf.CeilToInt(sampleRate * duration);
        float[] samples = new float[sampleCount];

        float[] freqs = { 523.25f, 659.25f, 783.99f, 1046.50f };
        float[] noteOffsets = { 0.0f, 0.09f, 0.18f, 0.27f };

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleRate;
            float val = 0f;

            for (int n = 0; n < freqs.Length; n++)
            {
                float noteT = t - noteOffsets[n];
                if (noteT > 0f)
                {

                    float env = Mathf.Exp(-noteT * 4.2f);
                    float attack = Mathf.Clamp01(noteT / 0.025f);

                    float fundamental = Mathf.Sin(2f * Mathf.PI * freqs[n] * noteT);
                    float harmonic = Mathf.Sin(4f * Mathf.PI * freqs[n] * noteT) * 0.18f;

                    val += (fundamental + harmonic) * env * attack * 0.22f;
                }
            }

            samples[i] = Mathf.Clamp(val, -1f, 1f);
        }

        s_CalmHealthChimeClip = AudioClip.Create("CalmHealthChime", sampleCount, 1, sampleRate, false);
        s_CalmHealthChimeClip.SetData(samples, 0);
        return s_CalmHealthChimeClip;
    }

    private void FindPlayer()
    {
        var p = GameObject.FindWithTag("Player") ?? GameObject.Find("PlayerCapsule");
        if (p != null)
        {
            m_PlayerTransform = p.transform;
            m_PlayerController = p.GetComponent<PlayerController>() ?? p.GetComponentInParent<PlayerController>();
        }
    }

    private bool CanPlayerCollect()
    {
        if (m_PlayerController == null) FindPlayer();
        if (m_PlayerController == null || m_PlayerController.isDead) return false;

        if (pickupType == PickupType.Health)
        {

            return m_PlayerController.storedPotions < m_PlayerController.maxPotions;
        }
        if (pickupType == PickupType.Shield)
        {

            return m_PlayerController.currentShield < m_PlayerController.maxShield;
        }
        if (pickupType == PickupType.Berserk)
        {
            return m_PlayerController.storedBerserkJars < m_PlayerController.maxBerserkJars;
        }

        return true;
    }

    private void Update()
    {
        if (m_IsCollected) return;

        if (m_PlayerTransform == null)
        {
            FindPlayer();
        }

        bool isMagnetizing = false;

        if (m_PlayerTransform != null && CanPlayerCollect())
        {
            Vector3 playerTargetPos = m_PlayerTransform.position + Vector3.up * 0.8f;
            float dist = Vector3.Distance(transform.position, playerTargetPos);

            if (dist <= pickupRadius)
            {
                isMagnetizing = true;

                transform.position = Vector3.MoveTowards(transform.position, playerTargetPos, magnetSpeed * Time.deltaTime);

                if (dist <= collectDistance)
                {
                    TryCollectPickup();
                    return;
                }
            }
        }

        if (!isMagnetizing)
        {
            float newY = m_StartPos.y + Mathf.Sin(Time.time * bobSpeed) * bobHeight;
            transform.position = new Vector3(transform.position.x, newY, transform.position.z);
        }

        transform.Rotate(Vector3.up, rotateSpeed * (isMagnetizing ? 3.0f : 1.0f) * Time.deltaTime, Space.World);

        if (m_BeaconLight != null)
        {
            m_BeaconLight.intensity = 1.6f + Mathf.Sin(Time.time * 4.0f) * 0.9f;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (m_IsCollected) return;

        if (other.CompareTag("Player") || other.GetComponent<PlayerController>() != null || other.GetComponentInParent<PlayerController>() != null)
        {
            if (CanPlayerCollect())
            {
                TryCollectPickup();
            }
        }
    }

    private void TryCollectPickup()
    {
        if (m_IsCollected) return;

        if (m_PlayerController == null) FindPlayer();
        if (m_PlayerController != null && m_PlayerController.isDead) return;

        var player = m_PlayerController ?? FindAnyObjectByType<PlayerController>();
        var gun = player != null ? (player.GetComponentInChildren<Gun>() ?? FindAnyObjectByType<Gun>()) : FindAnyObjectByType<Gun>();
        var hud = FindAnyObjectByType<GunHUD>();

        bool pickedUp = false;

        if (pickupType == PickupType.Ammo && gun != null)
        {
            gun.reserveAmmo += ammoAmount;
            if (hud != null) hud.ShowAmmoPickupToast(ammoAmount);
            pickedUp = true;
        }
        else if (pickupType == PickupType.Health && player != null)
        {
            if (player.AddHealthPotion(1))
            {
                pickedUp = true;
            }
            else
            {
                if (hud != null) hud.ShowWaveStatus("Health Potions Full (3/3)!", new Color(1f, 0.85f, 0.3f, 1f), 1.0f);
            }
        }
        else if (pickupType == PickupType.Shield && player != null)
        {
            if (player.currentShield < player.maxShield)
            {
                player.AddShield(shieldAmount);
                pickedUp = true;
            }
            else
            {
                if (hud != null) hud.ShowWaveStatus("Shield Full (150/150)!", new Color(0f, 0.85f, 1f), 1.0f);
            }
        }
        else if (pickupType == PickupType.Berserk && player != null)
        {
            if (player.AddBerserkJar(1))
            {
                pickedUp = true;
            }
            else
            {
                if (hud != null) hud.ShowWaveStatus("Berserk Jars Full (2/2)!", new Color(1f, 0.3f, 0.3f), 1.0f);
            }
        }

        if (pickedUp)
        {
            m_IsCollected = true;

            AudioClip soundToPlay = pickupSound;
            if (soundToPlay == null && (pickupType == PickupType.Health || pickupType == PickupType.Shield))
            {
                soundToPlay = GetOrCreateCalmHealthChime();
            }

            if (soundToPlay != null)
            {
                AudioSource.PlayClipAtPoint(soundToPlay, transform.position, 0.9f);
            }
            Destroy(gameObject);
        }
    }
}

