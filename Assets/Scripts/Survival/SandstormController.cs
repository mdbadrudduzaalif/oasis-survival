using UnityEngine;

public class SandstormController : MonoBehaviour
{
    public static SandstormController Instance { get; private set; }

    [Header("Storm Settings")]
    public float targetIntensity = 0.0f;
    public float transitionSpeed = 0.85f;
    public Vector3 windVelocity = new Vector3(16.0f, -0.6f, 12.0f);

    [Header("Particle Systems")]
    public ParticleSystem dustCloudParticles;
    public ParticleSystem flyingSandGrains;

    [Header("Audio")]
    public AudioSource windAudioSource;
    public AudioClip windLoopClip;
    public float maxWindVolume = 0.65f;

    private float m_CurrentIntensity = 0.0f;
    private Transform m_PlayerCamTransform;
    private Vector3 m_StormOffset = new Vector3(0.0f, 0.5f, 2.5f);

    public float CurrentIntensity => m_CurrentIntensity;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        FindPlayerCamera();
        CreateSandstormSystemsIfNeeded();
        SetupAudio();
    }

    private void Start()
    {
        SetIntensityImmediate(0.0f);
    }

    private void FindPlayerCamera()
    {
        var cam = Camera.main;
        if (cam != null)
        {
            m_PlayerCamTransform = cam.transform;
        }
        else
        {
            var p = GameObject.FindWithTag("Player") ?? GameObject.Find("PlayerCapsule");
            if (p != null) m_PlayerCamTransform = p.transform;
        }
    }

    private void Update()
    {
        if (m_PlayerCamTransform == null)
        {
            FindPlayerCamera();
        }

        if (m_PlayerCamTransform != null)
        {
            transform.position = m_PlayerCamTransform.position + m_PlayerCamTransform.forward * m_StormOffset.z + Vector3.up * m_StormOffset.y;
            transform.rotation = Quaternion.identity;
        }

        if (!Mathf.Approximately(m_CurrentIntensity, targetIntensity))
        {
            m_CurrentIntensity = Mathf.MoveTowards(m_CurrentIntensity, targetIntensity, Time.deltaTime * transitionSpeed);
            ApplyIntensity(m_CurrentIntensity);
        }
    }

    public void SetIntensity(float intensity)
    {
        SetTargetIntensity(intensity);
    }

    public void SetTargetIntensity(float intensity)
    {
        targetIntensity = Mathf.Clamp01(intensity);
    }

    public void SetIntensityImmediate(float intensity)
    {
        targetIntensity = Mathf.Clamp01(intensity);
        m_CurrentIntensity = targetIntensity;
        ApplyIntensity(m_CurrentIntensity);
    }

    private void ApplyIntensity(float intensity)
    {
        if (dustCloudParticles != null)
        {
            var emission = dustCloudParticles.emission;
            emission.rateOverTime = intensity * 35.0f;

            if (intensity > 0.01f && !dustCloudParticles.isPlaying)
                dustCloudParticles.Play();
            else if (intensity <= 0.001f && dustCloudParticles.isPlaying)
                dustCloudParticles.Stop(false, ParticleSystemStopBehavior.StopEmitting);
        }

        if (flyingSandGrains != null)
        {
            var emission = flyingSandGrains.emission;
            emission.rateOverTime = intensity * 70.0f;

            if (intensity > 0.01f && !flyingSandGrains.isPlaying)
                flyingSandGrains.Play();
            else if (intensity <= 0.001f && flyingSandGrains.isPlaying)
                flyingSandGrains.Stop(false, ParticleSystemStopBehavior.StopEmitting);
        }

        if (windAudioSource != null)
        {
            windAudioSource.volume = maxWindVolume * intensity;
            if (intensity > 0.01f && !windAudioSource.isPlaying)
                windAudioSource.Play();
            else if (intensity <= 0.001f && windAudioSource.isPlaying)
                windAudioSource.Stop();
        }
    }

    private void CreateSandstormSystemsIfNeeded()
    {
        if (dustCloudParticles != null && flyingSandGrains != null) return;

        if (dustCloudParticles == null)
        {
            var dustObj = new GameObject("Sandstorm_DustClouds");
            dustObj.transform.SetParent(transform, false);
            dustCloudParticles = dustObj.AddComponent<ParticleSystem>();

            var main = dustCloudParticles.main;
            main.loop = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(1.5f, 2.8f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(12.0f, 18.0f);
            main.startSize = new ParticleSystem.MinMaxCurve(2.5f, 5.0f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, 360f * Mathf.Deg2Rad);
            main.startColor = new Color(0.92f, 0.78f, 0.52f, 0.16f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 250;

            var emission = dustCloudParticles.emission;
            emission.rateOverTime = 0f;

            var shape = dustCloudParticles.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(8f, 4.0f, 8f);
            shape.position = new Vector3(-3f, 0.5f, -1.5f);

            var velOverLifetime = dustCloudParticles.velocityOverLifetime;
            velOverLifetime.enabled = true;
            velOverLifetime.space = ParticleSystemSimulationSpace.World;
            velOverLifetime.x = new ParticleSystem.MinMaxCurve(windVelocity.x - 2f, windVelocity.x + 2f);
            velOverLifetime.y = new ParticleSystem.MinMaxCurve(windVelocity.y - 0.5f, windVelocity.y + 0.5f);
            velOverLifetime.z = new ParticleSystem.MinMaxCurve(windVelocity.z - 2f, windVelocity.z + 2f);

            var colorOverLifetime = dustCloudParticles.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient grad = new Gradient();
            grad.SetKeys(
                new GradientColorKey[] { new GradientColorKey(new Color(0.94f, 0.80f, 0.54f), 0.0f), new GradientColorKey(new Color(0.88f, 0.72f, 0.44f), 1.0f) },
                new GradientAlphaKey[] { new GradientAlphaKey(0.0f, 0.0f), new GradientAlphaKey(0.18f, 0.25f), new GradientAlphaKey(0.15f, 0.70f), new GradientAlphaKey(0.0f, 1.0f) }
            );
            colorOverLifetime.color = grad;

            var sizeOverLifetime = dustCloudParticles.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            AnimationCurve sizeCurve = new AnimationCurve();
            sizeCurve.AddKey(0f, 0.5f);
            sizeCurve.AddKey(0.5f, 1.0f);
            sizeCurve.AddKey(1f, 1.3f);
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

            var rotOverLifetime = dustCloudParticles.rotationOverLifetime;
            rotOverLifetime.enabled = true;
            rotOverLifetime.z = new ParticleSystem.MinMaxCurve(-35f * Mathf.Deg2Rad, 35f * Mathf.Deg2Rad);

            var texSheet = dustCloudParticles.textureSheetAnimation;
            texSheet.enabled = true;
            texSheet.numTilesX = 8;
            texSheet.numTilesY = 8;
            texSheet.animation = ParticleSystemAnimationType.WholeSheet;
            texSheet.timeMode = ParticleSystemAnimationTimeMode.Lifetime;
            texSheet.frameOverTime = new ParticleSystem.MinMaxCurve(0f, 63f);

            var rend = dustObj.GetComponent<ParticleSystemRenderer>();
            rend.material = CreateSandstormMaterial("DustCloudMat", true);
        }

        if (flyingSandGrains == null)
        {
            var grainObj = new GameObject("Sandstorm_FlyingGrains");
            grainObj.transform.SetParent(transform, false);
            flyingSandGrains = grainObj.AddComponent<ParticleSystem>();

            var main = flyingSandGrains.main;
            main.loop = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.3f, 0.7f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(22.0f, 34.0f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.10f);
            main.startColor = new Color(0.96f, 0.88f, 0.60f, 0.45f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 200;

            var emission = flyingSandGrains.emission;
            emission.rateOverTime = 0f;

            var shape = flyingSandGrains.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(6f, 3.0f, 6f);
            shape.position = new Vector3(-2.5f, 0.5f, -1.0f);

            var velOverLifetime = flyingSandGrains.velocityOverLifetime;
            velOverLifetime.enabled = true;
            velOverLifetime.space = ParticleSystemSimulationSpace.World;
            velOverLifetime.x = new ParticleSystem.MinMaxCurve(windVelocity.x * 1.2f, windVelocity.x * 1.5f);
            velOverLifetime.y = new ParticleSystem.MinMaxCurve(windVelocity.y - 0.8f, windVelocity.y + 0.4f);
            velOverLifetime.z = new ParticleSystem.MinMaxCurve(windVelocity.z * 1.2f, windVelocity.z * 1.4f);

            var colorOverLifetime = flyingSandGrains.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient grad = new Gradient();
            grad.SetKeys(
                new GradientColorKey[] { new GradientColorKey(new Color(0.98f, 0.90f, 0.65f), 0.0f), new GradientColorKey(new Color(0.92f, 0.80f, 0.50f), 1.0f) },
                new GradientAlphaKey[] { new GradientAlphaKey(0.0f, 0.0f), new GradientAlphaKey(0.40f, 0.3f), new GradientAlphaKey(0.0f, 1.0f) }
            );
            colorOverLifetime.color = grad;

            var rend = grainObj.GetComponent<ParticleSystemRenderer>();
            rend.renderMode = ParticleSystemRenderMode.Stretch;
            rend.lengthScale = 3.5f;
            rend.velocityScale = 0.08f;
            rend.material = CreateSandstormMaterial("SandGrainsMat", false);
        }
    }

    private Material CreateSandstormMaterial(string name, bool isCloud)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null) shader = Shader.Find("Particles/Standard Unlit") ?? Shader.Find("Legacy Shaders/Particles/Alpha Blended");

        Material mat = new Material(shader);
        mat.name = name;

        mat.SetFloat("_Surface", 1.0f);
        mat.SetFloat("_Blend", 0.0f);
        mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetFloat("_ZWrite", 0.0f);
        mat.SetInt("_ZWrite", 0);
        mat.SetOverrideTag("RenderType", "Transparent");
        mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        mat.DisableKeyword("_SURFACE_TYPE_OPAQUE");

        Texture2D tex = null;
        if (isCloud)
        {
            tex = Resources.Load<Texture2D>("TX_WispySmoke03b_8x8") ??
                  (Texture2D)UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Scenes/Oasis/VFX/TXT/TX_WispySmoke03b_8x8.png");
        }
        else
        {
            tex = Resources.Load<Texture2D>("TX_TinyStones_D") ??
                  (Texture2D)UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Scenes/Oasis/VFX/TXT/TX_TinyStones_D.png");
        }

        if (tex != null)
        {
            mat.SetTexture("_BaseMap", tex);
            mat.SetTexture("_MainTex", tex);
        }

        Color col = isCloud ? new Color(0.92f, 0.78f, 0.52f, 0.18f) : new Color(0.96f, 0.88f, 0.60f, 0.45f);
        mat.SetColor("_BaseColor", col);
        mat.SetColor("_Color", col);

        return mat;
    }

    private void SetupAudio()
    {
        if (windAudioSource == null)
        {
            windAudioSource = gameObject.AddComponent<AudioSource>();
            windAudioSource.loop = true;
            windAudioSource.playOnAwake = false;
            windAudioSource.spatialBlend = 0.0f;
        }

        if (windLoopClip == null)
        {
            windLoopClip = Resources.Load<AudioClip>("Wind_Sandstorm_Loop");
        }

        if (windLoopClip != null)
        {
            windAudioSource.clip = windLoopClip;
        }
    }
}

