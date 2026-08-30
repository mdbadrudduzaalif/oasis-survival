using UnityEngine;
using System.Collections;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float walkSpeed = 4.5f;
    public float sprintSpeed = 7.5f;
    public float jumpHeight = 1.2f;
    public float gravity = -20.0f;

    [Header("Look Settings")]
    public Transform playerCamera;
    public float mouseSensitivity = 0.06f;
    public float adsSensitivityMultiplier = 0.55f;
    public float minPitch = -85.0f;
    public float maxPitch = 85.0f;

    [Header("Head Bob Settings")]
    public float bobFrequency = 10.0f;
    public float bobHorizontalAmplitude = 0.025f;
    public float bobVerticalAmplitude = 0.035f;

    [Header("Footsteps & Movement Audio")]
    public AudioClip walkAudioClip;
    public AudioClip sprintAudioClip;
    public AudioClip[] footstepSounds;
    public float footstepInterval = 0.48f;
    [Range(0f, 1f)] public float movementAudioVolume = 0.85f;

    [Header("Health & Stats")]
    public float maxHealth = 100.0f;
    public float currentHealth = 100.0f;
    public bool isDead = false;

    [Header("Shield System (150 Pts)")]
    public float maxShield = 150.0f;
    public float currentShield = 0.0f;

    [Header("Berserk Mode (1.5x Speed, 2x Dmg, Q Key Activation)")]
    public float berserkTimer = 0.0f;
    public bool IsBerserk => berserkTimer > 0.0f;
    public int storedBerserkJars = 0;
    public int maxBerserkJars = 2;
    public AudioClip berserkActivateSound;

    [Header("Health Potion Inventory (E Key Activation)")]
    public int storedPotions = 0;
    public int maxPotions = 3;
    public AudioClip potionDrinkSound;

    private CharacterController m_Controller;
    private Gun m_EquippedGun;
    private AudioSource m_AudioSource;
    private AudioSource m_MovementAudioSource;

    private Vector3 m_Velocity;
    private bool m_IsGrounded;
    private float m_Pitch;
    private float m_BobTimer;
    private float m_StepTimer;
    private Vector3 m_CamDefaultLocalPos;
    private bool m_IsMoving;
    public bool IsMoving => m_IsMoving;
    public bool IsSprinting { get; private set; }

    public void AddShield(float amount)
    {
        currentShield = Mathf.Min(maxShield, currentShield + amount);
        var hud = FindAnyObjectByType<GunHUD>();
        if (hud != null) hud.ShowWaveStatus($"<b><color=#00E5FF>+ {Mathf.RoundToInt(amount)} SHIELD RESTORED!</color></b>", new Color(0f, 0.9f, 1f), 1.5f);
    }

    public void ActivateBerserkMode(float duration = 12.0f)
    {
        berserkTimer = duration;
        var hud = FindAnyObjectByType<GunHUD>();
        if (hud != null) hud.ShowWaveStatus("<b><color=#FF2244>★ BERSERK MODE ACTIVATED! (2X DMG, 1.5X SPEED) ★</color></b>", new Color(1f, 0.15f, 0.25f), 2.5f);
    }

    private void Awake()
    {
        m_Controller = GetComponent<CharacterController>();
        m_AudioSource = GetComponent<AudioSource>();
        if (m_AudioSource == null) m_AudioSource = gameObject.AddComponent<AudioSource>();
        m_AudioSource.spatialBlend = 0.0f;
        m_AudioSource.playOnAwake = false;

        m_MovementAudioSource = gameObject.AddComponent<AudioSource>();
        m_MovementAudioSource.spatialBlend = 0.0f;
        m_MovementAudioSource.loop = true;
        m_MovementAudioSource.playOnAwake = false;
        m_MovementAudioSource.volume = 0f;

#if UNITY_EDITOR
        if (walkAudioClip == null)
            walkAudioClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Raw/sand player running.mp3");
        if (sprintAudioClip == null)
            sprintAudioClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Raw/sand player runnng.mp3");
#endif

        currentHealth = maxHealth;

        if (playerCamera != null)
        {
            m_CamDefaultLocalPos = playerCamera.localPosition;
        }

        LockCursor();
    }

    private void Start()
    {
        m_EquippedGun = GetComponentInChildren<Gun>();
    }

    private void Update()
    {
        if (isDead) return;

        if (berserkTimer > 0f)
        {
            berserkTimer -= Time.deltaTime;
        }

        HandlePotionInput();
        HandleLook();
        HandleMovement();
        HandleHeadBob();
        HandleFootsteps();
    }

    private void HandlePotionInput()
    {
        bool healthPotionPressed = false;
        bool berserkPressed = false;

#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null)
        {
            if (Keyboard.current.eKey.wasPressedThisFrame || Keyboard.current.hKey.wasPressedThisFrame || Keyboard.current.digit4Key.wasPressedThisFrame || Keyboard.current.numpad4Key.wasPressedThisFrame)
                healthPotionPressed = true;
            if (Keyboard.current.qKey.wasPressedThisFrame || Keyboard.current.gKey.wasPressedThisFrame || Keyboard.current.digit3Key.wasPressedThisFrame || Keyboard.current.numpad3Key.wasPressedThisFrame)
                berserkPressed = true;
        }
#else
        if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.H) || Input.GetKeyDown(KeyCode.Alpha4) || Input.GetKeyDown(KeyCode.Keypad4))
            healthPotionPressed = true;
        if (Input.GetKeyDown(KeyCode.Q) || Input.GetKeyDown(KeyCode.G) || Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3))
            berserkPressed = true;
#endif

        if (healthPotionPressed)
        {
            UseHealthPotion();
        }

        if (berserkPressed)
        {
            UseBerserkJar();
        }
    }

    public bool AddBerserkJar(int count = 1)
    {
        if (storedBerserkJars >= maxBerserkJars) return false;

        storedBerserkJars = Mathf.Min(maxBerserkJars, storedBerserkJars + count);
        var hud = FindAnyObjectByType<GunHUD>();
        if (hud != null)
        {
            hud.ShowPickupToast($"+{count} Berserk Jar [Press Q]", new Color(1f, 0.25f, 0.35f, 1f));
        }
        return true;
    }

    public bool UseBerserkJar()
    {
        if (isDead) return false;

        if (storedBerserkJars <= 0)
        {
            var hudWarn = FindAnyObjectByType<GunHUD>();
            if (hudWarn != null) hudWarn.ShowWaveStatus("No Berserk Jars stored! Collect Red Jars.", new Color(1f, 0.4f, 0.4f), 1.2f);
            return false;
        }

        storedBerserkJars--;
        ActivateBerserkMode(12.0f);

        AudioClip sfx = berserkActivateSound ?? potionDrinkSound;
        if (sfx != null && m_AudioSource != null)
        {
            m_AudioSource.PlayOneShot(sfx, 1.0f);
        }

        return true;
    }

    public bool AddHealthPotion(int count = 1)
    {
        if (storedPotions >= maxPotions) return false;

        storedPotions = Mathf.Min(maxPotions, storedPotions + count);
        var hud = FindAnyObjectByType<GunHUD>();
        if (hud != null)
        {
            hud.ShowPickupToast($"+{count} Health Potion [Press E]", new Color(0.2f, 0.95f, 0.4f, 1f));
        }
        return true;
    }

    public bool UseHealthPotion()
    {
        if (isDead || storedPotions <= 0) return false;
        if (currentHealth >= maxHealth)
        {
            var hudWarn = FindAnyObjectByType<GunHUD>();
            if (hudWarn != null) hudWarn.ShowWaveStatus("Health is already Full!", new Color(1f, 0.85f, 0.3f, 1f), 1.2f);
            return false;
        }

        storedPotions--;
        float healAmount = 50.0f;
        currentHealth = Mathf.Min(maxHealth, currentHealth + healAmount);

        AudioClip sfx = potionDrinkSound ?? SupplyPickup.GetOrCreateCalmHealthChime();
        if (sfx != null && m_AudioSource != null)
        {
            m_AudioSource.PlayOneShot(sfx, 0.95f);
        }

        var hud = FindAnyObjectByType<GunHUD>();
        if (hud != null)
        {
            hud.TriggerHealFlash();
            hud.ShowPickupToast("+50 HP Healed", new Color(0.3f, 1.0f, 0.5f, 1f));
        }

        return true;
    }

    public void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void UnlockCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void HandleLook()
    {
        if (playerCamera == null) return;

        Vector2 mouseDelta = Vector2.zero;
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null)
        {
            mouseDelta = Mouse.current.delta.ReadValue();
        }
#else
        mouseDelta = new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y")) * 10.0f;
#endif

        float sens = mouseSensitivity;
        if (m_EquippedGun != null && m_EquippedGun.IsAiming)
        {
            sens *= adsSensitivityMultiplier;
        }

        float mouseX = mouseDelta.x * sens;
        float mouseY = mouseDelta.y * sens;

        m_Pitch -= mouseY;
        m_Pitch = Mathf.Clamp(m_Pitch, minPitch, maxPitch);

        playerCamera.localRotation = Quaternion.Euler(m_Pitch, 0f, 0f);
        transform.Rotate(Vector3.up * mouseX);
    }

    private void HandleMovement()
    {
        m_IsGrounded = m_Controller.isGrounded;
        if (m_IsGrounded && m_Velocity.y < 0)
        {
            m_Velocity.y = -2.0f;
        }

        Vector2 moveInput = Vector2.zero;
        bool sprintHeld = false;
        bool jumpPressed = false;

#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null)
        {
            if (Keyboard.current.wKey.isPressed) moveInput.y += 1;
            if (Keyboard.current.sKey.isPressed) moveInput.y -= 1;
            if (Keyboard.current.dKey.isPressed) moveInput.x += 1;
            if (Keyboard.current.aKey.isPressed) moveInput.x -= 1;

            sprintHeld = Keyboard.current.leftShiftKey.isPressed;
            jumpPressed = Keyboard.current.spaceKey.wasPressedThisFrame;
        }
#else
        moveInput.x = Input.GetAxisRaw("Horizontal");
        moveInput.y = Input.GetAxisRaw("Vertical");
        sprintHeld = Input.GetKey(KeyCode.LeftShift);
        jumpPressed = Input.GetKeyDown(KeyCode.Space);
#endif

        moveInput.Normalize();
        m_IsMoving = moveInput.sqrMagnitude > 0.05f;
        IsSprinting = sprintHeld && moveInput.y > 0.1f && !m_EquippedGun?.IsAiming == true;

        float targetSpeed = (IsSprinting ? sprintSpeed : walkSpeed) * (IsBerserk ? 1.5f : 1.0f);
        Vector3 moveDir = transform.right * moveInput.x + transform.forward * moveInput.y;
        m_Controller.Move(moveDir * (targetSpeed * Time.deltaTime));

        if (jumpPressed && m_IsGrounded)
        {
            m_Velocity.y = Mathf.Sqrt(jumpHeight * -2.0f * gravity);
        }

        m_Velocity.y += gravity * Time.deltaTime;
        m_Controller.Move(m_Velocity * Time.deltaTime);
    }

    private void HandleHeadBob()
    {
        if (playerCamera == null) return;

        if (IsMoving && m_IsGrounded)
        {
            float speedMult = IsSprinting ? 1.4f : 1.0f;
            m_BobTimer += Time.deltaTime * bobFrequency * speedMult;

            float horizontalBob = Mathf.Cos(m_BobTimer * 0.5f) * bobHorizontalAmplitude;
            float verticalBob = Mathf.Sin(m_BobTimer) * bobVerticalAmplitude;

            playerCamera.localPosition = m_CamDefaultLocalPos + new Vector3(horizontalBob, verticalBob, 0f);
        }
        else
        {
            m_BobTimer = 0f;
            playerCamera.localPosition = Vector3.Lerp(playerCamera.localPosition, m_CamDefaultLocalPos, Time.deltaTime * 8.0f);
        }
    }

    private void HandleFootsteps()
    {
        if (m_MovementAudioSource == null) return;

        if (m_IsGrounded && m_IsMoving && !isDead)
        {
            AudioClip targetClip = IsSprinting ? (sprintAudioClip ?? walkAudioClip) : (walkAudioClip ?? sprintAudioClip);
            if (targetClip != null)
            {
                if (m_MovementAudioSource.clip != targetClip)
                {
                    m_MovementAudioSource.clip = targetClip;
                    m_MovementAudioSource.time = 0f;
                    m_MovementAudioSource.Play();
                }
                else if (!m_MovementAudioSource.isPlaying)
                {
                    m_MovementAudioSource.Play();
                }

                float targetPitch = IsSprinting ? 1.15f : 1.0f;
                m_MovementAudioSource.pitch = Mathf.Lerp(m_MovementAudioSource.pitch, targetPitch, Time.deltaTime * 8f);
                m_MovementAudioSource.volume = Mathf.Lerp(m_MovementAudioSource.volume, movementAudioVolume, Time.deltaTime * 12f);
            }
        }
        else
        {
            if (m_MovementAudioSource.isPlaying)
            {
                m_MovementAudioSource.volume = Mathf.Lerp(m_MovementAudioSource.volume, 0f, Time.deltaTime * 16f);
                if (m_MovementAudioSource.volume < 0.01f)
                {
                    m_MovementAudioSource.Stop();
                }
            }
        }
    }

    public void TakeDamage(float amount)
    {
        if (isDead) return;

        if (currentShield > 0f)
        {
            if (currentShield >= amount)
            {
                currentShield -= amount;
                amount = 0f;
            }
            else
            {
                amount -= currentShield;
                currentShield = 0f;
            }
        }

        if (amount > 0f)
        {
            currentHealth = Mathf.Max(0f, currentHealth - amount);
        }

        var hud = FindAnyObjectByType<GunHUD>();
        if (hud != null) hud.TriggerDamageFlash();

        var gun = FindAnyObjectByType<Gun>();
        if (gun != null) gun.TriggerScreenShake(0.18f, 1.2f);

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        isDead = true;
        UnlockCursor();

        if (m_MovementAudioSource != null && m_MovementAudioSource.isPlaying)
        {
            m_MovementAudioSource.Stop();
        }

        var scoreMgr = Score.Instance ?? FindAnyObjectByType<Score>();
        var waveMgr = FindAnyObjectByType<WaveManager>();
        var authMgr = AuthManager.Instance ?? FindAnyObjectByType<AuthManager>();
        var netMgr = NetworkManager.Instance ?? FindAnyObjectByType<NetworkManager>();

        if (netMgr != null && authMgr != null && authMgr.isLoggedIn)
        {
            int pId = authMgr.playerId;
            int score = scoreMgr != null ? scoreMgr.currentScore : 0;
            int wave = waveMgr != null ? waveMgr.currentWave : 1;
            var hud = FindAnyObjectByType<GunHUD>();
            int totKills = hud != null ? hud.TotalKills : 0;
            int hs = hud != null ? hud.HeadshotKills : 0;
            int dur = Mathf.RoundToInt(Time.timeSinceLevelLoad);

            StartCoroutine(netMgr.SubmitMatchResultRoutine(pId, score, wave, totKills, hs, dur, false));
        }

        if (m_EquippedGun == null) m_EquippedGun = GetComponentInChildren<Gun>();
        if (m_EquippedGun != null)
        {
            m_EquippedGun.enabled = false;
        }

        StartCoroutine(DeathCameraFallRoutine());
    }

    private IEnumerator DeathCameraFallRoutine()
    {
        if (playerCamera == null) yield break;

        Vector3 startPos = playerCamera.localPosition;
        Vector3 endPos = new Vector3(startPos.x, 0.25f, startPos.z);
        Quaternion startRot = playerCamera.localRotation;
        Quaternion endRot = Quaternion.Euler(startRot.eulerAngles.x, startRot.eulerAngles.y, 22.0f);

        float elapsed = 0f;
        float duration = 1.0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            playerCamera.localPosition = Vector3.Lerp(startPos, endPos, t);
            playerCamera.localRotation = Quaternion.Slerp(startRot, endRot, t);
            yield return null;
        }
    }
}

