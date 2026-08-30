using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class BulletShell : MonoBehaviour
{
    public float lifeTime = 8.0f;

    private Rigidbody m_Rb;
    private AudioSource m_AudioSource;
    private bool m_HasClinked;

    private void Awake()
    {
        m_Rb = GetComponent<Rigidbody>();
        if (m_Rb != null)
        {
            m_Rb.mass = 0.04f;
            m_Rb.linearDamping = 1.2f;
            m_Rb.angularDamping = 1.5f;
            m_Rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        }

        m_AudioSource = GetComponent<AudioSource>();
        if (m_AudioSource == null) m_AudioSource = gameObject.AddComponent<AudioSource>();
        m_AudioSource.spatialBlend = 1.0f;
        m_AudioSource.minDistance = 1.0f;
        m_AudioSource.maxDistance = 10.0f;
        m_AudioSource.playOnAwake = false;

        var myCol = GetComponent<Collider>();
        if (myCol != null)
        {
            var player = GameObject.FindWithTag("Player") ?? GameObject.Find("PlayerCapsule");
            if (player != null)
            {
                foreach (var pc in player.GetComponentsInChildren<Collider>(true))
                {
                    if (pc != null) Physics.IgnoreCollision(myCol, pc);
                }
            }
        }

        Destroy(gameObject, lifeTime);
    }

    public void Eject(Vector3 velocity, Vector3 angularVelocity)
    {
        if (m_Rb != null)
        {
            m_Rb.linearVelocity = velocity;
            m_Rb.angularVelocity = angularVelocity;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!m_HasClinked && m_Rb != null && m_Rb.linearVelocity.sqrMagnitude > 0.2f)
        {
            m_HasClinked = true;
            if (m_AudioSource != null)
            {
                m_AudioSource.pitch = Random.Range(1.2f, 1.6f);
                m_AudioSource.volume = 0.35f;
            }
        }
    }
}

