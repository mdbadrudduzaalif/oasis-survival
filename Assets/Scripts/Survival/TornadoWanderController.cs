using UnityEngine;

public class TornadoWanderController : MonoBehaviour
{
    [Header("Movement Settings")]
    [Tooltip("Base speed in meters per second.")]
    public float moveSpeed = 4.0f;

    [Tooltip("How fast the tornado turns towards its new heading.")]
    public float turnSpeed = 1.4f;

    [Tooltip("Center of the playable island arena.")]
    public Vector3 arenaCenter = new Vector3(0f, 0f, -105f);

    [Tooltip("Playable arena roaming radius across the island.")]
    public float arenaRadius = 52.0f;

    [Tooltip("How often in seconds a new wander destination is picked.")]
    public float changeDirectionInterval = 6.0f;

    [Header("Ground Clamping")]
    [Tooltip("Depth to embed the base below the hit surface to ensure zero floating gap.")]
    public float groundSinkOffset = 1.0f;

    [Tooltip("Vertical speed when adjusting to rising/falling terrain.")]
    public float verticalFollowSpeed = 7.0f;

    [Tooltip("Layer mask for terrain and ground colliders.")]
    public LayerMask groundLayerMask = ~0;

    private Vector3 m_CurrentTarget;
    private float m_Timer;
    private float m_PerlinSeed;

    private void Awake()
    {
        m_PerlinSeed = Random.Range(0f, 1000f);
    }

    private void Start()
    {

        PickNewIslandTarget();

        var waveMgr = FindAnyObjectByType<WaveManager>();
        bool isSandstormWave = waveMgr != null && (waveMgr.currentWave == 4 || waveMgr.currentWave == 7 || waveMgr.currentWave == 10);
        SetTornadoActive(isSandstormWave);
    }

    public void SetTornadoActive(bool active)
    {
        var renderers = GetComponentsInChildren<Renderer>(true);
        foreach (var r in renderers) r.enabled = active;

        var particles = GetComponentsInChildren<ParticleSystem>(true);
        foreach (var p in particles)
        {
            if (active) p.Play();
            else p.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        var audios = GetComponentsInChildren<AudioSource>(true);
        foreach (var a in audios)
        {
            if (active) a.Play();
            else a.Stop();
        }

        var colliders = GetComponentsInChildren<Collider>(true);
        foreach (var c in colliders) c.enabled = active;

        enabled = active;
    }

    private void Update()
    {
        m_Timer += Time.deltaTime;
        Vector3 flatCurrent = new Vector3(transform.position.x, 0, transform.position.z);
        Vector3 flatTarget = new Vector3(m_CurrentTarget.x, 0, m_CurrentTarget.z);

        if (m_Timer >= changeDirectionInterval || Vector3.Distance(flatCurrent, flatTarget) < 4.0f)
        {
            PickNewIslandTarget();
        }

        Vector3 toTarget = (m_CurrentTarget - transform.position);
        toTarget.y = 0f;

        if (toTarget.sqrMagnitude > 0.01f)
        {
            Vector3 moveDir = toTarget.normalized;

            float noiseAngle = (Mathf.PerlinNoise(Time.time * 0.3f + m_PerlinSeed, m_PerlinSeed) - 0.5f) * 50f;
            Quaternion noiseRot = Quaternion.Euler(0f, noiseAngle, 0f);
            Vector3 finalDir = noiseRot * moveDir;

            float currentSpeed = moveSpeed * (0.85f + 0.3f * Mathf.PerlinNoise(Time.time * 0.4f, m_PerlinSeed + 100f));
            Vector3 targetHorizontalPos = transform.position + finalDir * (currentSpeed * Time.deltaTime);

            transform.position = new Vector3(targetHorizontalPos.x, transform.position.y, targetHorizontalPos.z);

            if (finalDir.sqrMagnitude > 0.001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(finalDir, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * turnSpeed);
            }
        }

        ClampToGround();
    }

    public void PickNewIslandTarget()
    {
        m_Timer = 0f;
        Vector2 randomCircle = Random.insideUnitCircle * arenaRadius;
        m_CurrentTarget = new Vector3(arenaCenter.x + randomCircle.x, transform.position.y, arenaCenter.z + randomCircle.y);
    }

    private void ClampToGround()
    {
        Vector3 rayOrigin = new Vector3(transform.position.x, transform.position.y + 25.0f, transform.position.z);
        RaycastHit hit;

        if (Physics.Raycast(rayOrigin, Vector3.down, out hit, 80.0f, groundLayerMask, QueryTriggerInteraction.Ignore))
        {
            float targetY = hit.point.y - groundSinkOffset;
            float newY = Mathf.MoveTowards(transform.position.y, targetY, Time.deltaTime * verticalFollowSpeed);
            transform.position = new Vector3(transform.position.x, newY, transform.position.z);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.95f, 0.75f, 0.35f, 0.4f);
        Gizmos.DrawWireSphere(arenaCenter, arenaRadius);

        Gizmos.color = Color.red;
        Gizmos.DrawLine(transform.position, m_CurrentTarget);
        Gizmos.DrawWireSphere(m_CurrentTarget, 2.0f);
    }
}

