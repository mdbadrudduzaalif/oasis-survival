using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class GunHUD : MonoBehaviour
{
    private Gun m_Gun;
    private PlayerController m_Player;
    private WaveManager m_WaveManager;

    [Header("HUD Canvas & Styling")]
    public Font hudFont;
    private Canvas m_Canvas;
    private GameObject m_HUDCanvasObj;

    private GameObject m_CrosshairRoot;
    private Image m_CrosshairDot;
    private Image m_CrosshairTop;
    private Image m_CrosshairBottom;
    private Image m_CrosshairLeft;
    private Image m_CrosshairRight;
    private Image m_Hitmarker;
    private Image m_AmmoRadialArcBG;
    private Image m_AmmoRadialArcFill;
    private Text m_CrosshairAmmoCountText;
    private Image[] m_RadialAmmoTicks;
    private Text m_ReloadWarningText;
    private float m_HitmarkerTimer;
    private bool m_IsHeadshotHit;
    private static Sprite s_RadialRingSprite;

    private GameObject m_TopLeftGlassBox;
    private Text m_WaveInfoText;
    private Text m_WaveActionBannerText;
    private float m_StatusBannerTimer;

    private GameObject m_TopRightStatsCard;
    private Text m_TotalKillsText;
    private Text m_HeadshotsText;

    private GameObject m_HealthCard;
    private Image m_HealthBarFill;
    private Image m_HealthBarBG;
    private Text m_HealthText;
    private Text m_PotionInventoryText;

    private GameObject m_WeaponCard;
    private Text m_WeaponNameText;
    private Text m_WeaponAmmoText;
    private Image m_ReloadBarFill;
    private GameObject m_ReloadBarRoot;

    private GameObject m_BossBarRoot;
    private Image m_BossBarFill;
    private Text m_BossNameText;
    private float m_BossBarHideTime = -1f;

    private GameObject m_PickupToastObj;
    private Text m_PickupToastText;
    private float m_PickupToastTimer;

    private Image m_DamageOverlay;
    private Image m_HealOverlay;
    private Image m_SandstormOverlay;
    private GameObject m_GameOverPanel;
    private Text m_GameOverTitleText;
    private Text m_GameOverStatsText;
    private Transform m_GameOverLeaderboardContainer;

    private Text m_BottomLeftHealthText;
    private Text m_BottomLeftShieldText;
    private Text m_BerserkStatusText;

    private GameObject m_JarStudioRoot;
    private Camera m_JarCamera;
    private RenderTexture m_JarRenderTexture;
    private RawImage m_JarRawImage;
    private MeshRenderer[] m_3DJarRenderers;

    private Camera m_BerserkJarCamera;
    private RenderTexture m_BerserkJarRenderTexture;
    private RawImage m_BerserkJarRawImage;
    private MeshRenderer[] m_3DBerserkJarRenderers;

    private Material m_MatWhiteJar;
    private Material m_MatGreenJar;
    private Material m_MatRedJar;

    private int m_TotalKills = 0;
    private int m_HeadshotKills = 0;
    private bool m_BossSlayerBadge = false;
    private float m_CurrentCrosshairSpread = 8f;

    public int TotalKills => m_TotalKills;
    public int HeadshotKills => m_HeadshotKills;

    private void Awake()
    {
        if (hudFont == null)
        {
#if UNITY_EDITOR
            hudFont = UnityEditor.AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/Audiowide-Regular.ttf");
#endif
            if (hudFont == null)
            {
                hudFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            }
        }
    }

    private void Start()
    {
        m_Gun = FindAnyObjectByType<Gun>();
        m_Player = FindAnyObjectByType<PlayerController>();
        m_WaveManager = FindAnyObjectByType<WaveManager>();

        if (FindAnyObjectByType<MainMenu>() == null)
        {
            var menuObj = new GameObject("MainMenu_System");
            menuObj.AddComponent<MainMenu>();
        }
        if (FindAnyObjectByType<LeaderboardUI>() == null)
        {
            var lbObj = new GameObject("LeaderboardUI_System");
            lbObj.AddComponent<LeaderboardUI>();
        }

        foreach (var c in FindObjectsByType<Canvas>(FindObjectsInactive.Include))
        {
            if (c != null && (c.name.Contains("Crosshair") || c.name.Contains("crosshair")))
            {
                c.gameObject.SetActive(false);
                Destroy(c.gameObject);
            }
        }

        BuildHUD();
    }

    private void BuildHUD()
    {
        m_HUDCanvasObj = new GameObject("TacticalHUD_Canvas");
        m_HUDCanvasObj.transform.SetParent(transform, false);

        m_Canvas = m_HUDCanvasObj.AddComponent<Canvas>();
        m_Canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        m_Canvas.sortingOrder = 100;

        var scaler = m_HUDCanvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        m_HUDCanvasObj.AddComponent<GraphicRaycaster>();

        bool isStormOnlyScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name.ToLower().Contains("sand storm") ||
                               UnityEngine.SceneManagement.SceneManager.GetActiveScene().name.ToLower().Contains("sandstorm");

        BuildFullscreenOverlays(m_HUDCanvasObj);
        if (!isStormOnlyScene)
        {
            Build3DHealthJarStudio();
            BuildTopLeftGlassBox(m_HUDCanvasObj, hudFont);
            BuildTopRightStatsCard(m_HUDCanvasObj, hudFont);
            BuildBottomLeftCyberpunkShield(m_HUDCanvasObj, hudFont);
            BuildBottomLeftCyberpunkHealth(m_HUDCanvasObj, hudFont);
            BuildBottomRightWeaponCard(m_HUDCanvasObj, hudFont);
            BuildBossBar(m_HUDCanvasObj, hudFont);
            BuildFloatingPickupToast(m_HUDCanvasObj, hudFont);
            BuildGameOverPanel(m_HUDCanvasObj, hudFont);
        }
        BuildCrosshairAndRadialArc(m_HUDCanvasObj, hudFont);
    }

    private void BuildFullscreenOverlays(GameObject parent)
    {

        var dmgObj = new GameObject("DamageFlashOverlay");
        dmgObj.transform.SetParent(parent.transform, false);
        var dRect = dmgObj.AddComponent<RectTransform>();
        dRect.anchorMin = Vector2.zero;
        dRect.anchorMax = Vector2.one;
        dRect.sizeDelta = Vector2.zero;
        m_DamageOverlay = dmgObj.AddComponent<Image>();
        m_DamageOverlay.color = new Color(0.9f, 0.05f, 0.05f, 0.0f);
        m_DamageOverlay.raycastTarget = false;

        var healObj = new GameObject("HealFlashOverlay");
        healObj.transform.SetParent(parent.transform, false);
        var hRect = healObj.AddComponent<RectTransform>();
        hRect.anchorMin = Vector2.zero;
        hRect.anchorMax = Vector2.one;
        hRect.sizeDelta = Vector2.zero;
        m_HealOverlay = healObj.AddComponent<Image>();
        m_HealOverlay.color = new Color(0.1f, 0.95f, 0.35f, 0.0f);
        m_HealOverlay.raycastTarget = false;

        var stormObj = new GameObject("SandstormVignetteOverlay");
        stormObj.transform.SetParent(parent.transform, false);
        var sRect = stormObj.AddComponent<RectTransform>();
        sRect.anchorMin = Vector2.zero;
        sRect.anchorMax = Vector2.one;
        sRect.sizeDelta = Vector2.zero;
        m_SandstormOverlay = stormObj.AddComponent<Image>();

        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name.ToLower();
        bool isStormOnlyTestScene = sceneName.Contains("sand storm") || sceneName.Contains("sandstorm");

        m_SandstormOverlay.color = isStormOnlyTestScene ? new Color(0.85f, 0.65f, 0.35f, 0.32f) : new Color(0.85f, 0.65f, 0.35f, 0.0f);
        m_SandstormOverlay.raycastTarget = false;
    }

    private void BuildTopLeftGlassBox(GameObject parent, Font font)
    {
        m_TopLeftGlassBox = new GameObject("TopLeft_GlassStatusCard");
        m_TopLeftGlassBox.transform.SetParent(parent.transform, false);
        var rect = m_TopLeftGlassBox.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 1);
        rect.anchorMax = new Vector2(0, 1);
        rect.pivot = new Vector2(0, 1);
        rect.anchoredPosition = new Vector2(35, -35);
        rect.sizeDelta = new Vector2(460, 125);

        var bg = m_TopLeftGlassBox.AddComponent<Image>();
        bg.color = new Color(0.05f, 0.07f, 0.10f, 0.82f);

        var waveObj = new GameObject("WaveInfoText");
        waveObj.transform.SetParent(m_TopLeftGlassBox.transform, false);
        var wRect = waveObj.AddComponent<RectTransform>();
        wRect.anchorMin = new Vector2(0, 1);
        wRect.anchorMax = new Vector2(1, 1);
        wRect.pivot = new Vector2(0, 1);
        wRect.anchoredPosition = new Vector2(20, -14);
        wRect.sizeDelta = new Vector2(-40, 40);

        m_WaveInfoText = waveObj.AddComponent<Text>();
        m_WaveInfoText.font = font;
        m_WaveInfoText.fontSize = 24;
        m_WaveInfoText.text = "<b>WAVE 1 / 10</b>   <color=#FFCC00>Kills Left: 5 / 5</color>";
        m_WaveInfoText.color = Color.white;
        m_WaveInfoText.alignment = TextAnchor.MiddleLeft;

        var actionObj = new GameObject("WaveActionBannerText");
        actionObj.transform.SetParent(m_TopLeftGlassBox.transform, false);
        var aRect = actionObj.AddComponent<RectTransform>();
        aRect.anchorMin = new Vector2(0, 0);
        aRect.anchorMax = new Vector2(1, 0);
        aRect.pivot = new Vector2(0, 0);
        aRect.anchoredPosition = new Vector2(20, 14);
        aRect.sizeDelta = new Vector2(-40, 48);

        m_WaveActionBannerText = actionObj.AddComponent<Text>();
        m_WaveActionBannerText.font = font;
        m_WaveActionBannerText.fontSize = 20;
        m_WaveActionBannerText.text = "<b><color=#FF4444>ELIMINATE 5 ZOMBIES!</color></b>";
        m_WaveActionBannerText.color = new Color(1.0f, 0.85f, 0.2f, 1.0f);
        m_WaveActionBannerText.alignment = TextAnchor.MiddleLeft;
    }

    private void BuildTopRightStatsCard(GameObject parent, Font font)
    {
        m_TopRightStatsCard = new GameObject("TopRight_StatsCard");
        m_TopRightStatsCard.transform.SetParent(parent.transform, false);
        var rect = m_TopRightStatsCard.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(1, 1);
        rect.anchorMax = new Vector2(1, 1);
        rect.pivot = new Vector2(1, 1);
        rect.anchoredPosition = new Vector2(-35, -35);
        rect.sizeDelta = new Vector2(340, 110);

        var bg = m_TopRightStatsCard.AddComponent<Image>();
        bg.color = new Color(0.05f, 0.07f, 0.10f, 0.82f);

        var scoreObj = new GameObject("ScoreText");
        scoreObj.transform.SetParent(m_TopRightStatsCard.transform, false);
        var sRect = scoreObj.AddComponent<RectTransform>();
        sRect.anchorMin = new Vector2(0, 1);
        sRect.anchorMax = new Vector2(1, 1);
        sRect.pivot = new Vector2(0, 1);
        sRect.anchoredPosition = new Vector2(20, -12);
        sRect.sizeDelta = new Vector2(-40, 36);

        m_TotalKillsText = scoreObj.AddComponent<Text>();
        m_TotalKillsText.font = font;
        m_TotalKillsText.fontSize = 21;
        m_TotalKillsText.text = "👑 <b>SCORE: 0</b>  <size=15><color=#AAAAAA>(HIGH: 0)</color></size>";
        m_TotalKillsText.color = new Color(1.0f, 0.88f, 0.35f, 1.0f);
        m_TotalKillsText.alignment = TextAnchor.MiddleLeft;

        var hsObj = new GameObject("KillsAndHeadshotsText");
        hsObj.transform.SetParent(m_TopRightStatsCard.transform, false);
        var hRect = hsObj.AddComponent<RectTransform>();
        hRect.anchorMin = new Vector2(0, 0);
        hRect.anchorMax = new Vector2(1, 0);
        hRect.pivot = new Vector2(0, 0);
        hRect.anchoredPosition = new Vector2(20, 12);
        hRect.sizeDelta = new Vector2(-40, 36);

        m_HeadshotsText = hsObj.AddComponent<Text>();
        m_HeadshotsText.font = font;
        m_HeadshotsText.fontSize = 18;
        m_HeadshotsText.text = "🎯 <b>KILLS: 0</b>  |  Headshots: 0";
        m_HeadshotsText.color = new Color(0.9f, 0.9f, 0.95f, 1.0f);
        m_HeadshotsText.alignment = TextAnchor.MiddleLeft;
    }

    private static Sprite s_CircleHitmarkerSprite;
    private static Sprite GetOrCreateCircleHitmarkerSprite()
    {
        if (s_CircleHitmarkerSprite != null) return s_CircleHitmarkerSprite;
        int size = 64;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;
        Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
        float outerR = size * 0.46f;
        float innerR = size * 0.30f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center);
                if (dist <= outerR && dist >= innerR)
                {
                    float alphaOuter = Mathf.Clamp01((outerR - dist) / 1.5f);
                    float alphaInner = Mathf.Clamp01((dist - innerR) / 1.5f);
                    float alpha = Mathf.Min(alphaOuter, alphaInner);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
                else
                {
                    tex.SetPixel(x, y, Color.clear);
                }
            }
        }
        tex.Apply();
        s_CircleHitmarkerSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        return s_CircleHitmarkerSprite;
    }

    private void BuildCrosshairAndRadialArc(GameObject parent, Font font)
    {
        m_CrosshairRoot = new GameObject("CrosshairRoot");
        m_CrosshairRoot.transform.SetParent(parent.transform, false);
        var rect = m_CrosshairRoot.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(100, 100);

        m_CrosshairTop = CreateCrosshairLine("Top", new Vector2(0, 10), new Vector2(2.5f, 10), new Color(1f, 1f, 1f, 0.95f));
        m_CrosshairBottom = CreateCrosshairLine("Bottom", new Vector2(0, -10), new Vector2(2.5f, 10), new Color(1f, 1f, 1f, 0.95f));
        m_CrosshairLeft = CreateCrosshairLine("Left", new Vector2(-10, 0), new Vector2(10, 2.5f), new Color(1f, 1f, 1f, 0.95f));
        m_CrosshairRight = CreateCrosshairLine("Right", new Vector2(10, 0), new Vector2(10, 2.5f), new Color(1f, 1f, 1f, 0.95f));

        var hmObj = new GameObject("Hitmarker");
        hmObj.transform.SetParent(m_CrosshairRoot.transform, false);
        var hmRect = hmObj.AddComponent<RectTransform>();
        hmRect.anchoredPosition = Vector2.zero;
        hmRect.sizeDelta = new Vector2(28, 28);
        m_Hitmarker = hmObj.AddComponent<Image>();
        m_Hitmarker.sprite = GetOrCreateCircleHitmarkerSprite();
        m_Hitmarker.color = new Color(1f, 0.2f, 0.2f, 0f);

        int tickCount = 30;
        m_RadialAmmoTicks = new Image[tickCount];
        float radius = 48.0f;
        float startAngleDeg = -65.0f;
        float endAngleDeg = 65.0f;

        for (int i = 0; i < tickCount; i++)
        {
            float t = (float)i / (tickCount - 1);
            float angleDeg = Mathf.Lerp(startAngleDeg, endAngleDeg, t);
            float angleRad = angleDeg * Mathf.Deg2Rad;

            Vector2 pos = new Vector2(Mathf.Cos(angleRad) * radius, Mathf.Sin(angleRad) * radius);

            var tickObj = new GameObject($"AmmoTick_{i}");
            tickObj.transform.SetParent(m_CrosshairRoot.transform, false);
            var tRect = tickObj.AddComponent<RectTransform>();
            tRect.anchoredPosition = pos;
            tRect.sizeDelta = new Vector2(3.5f, 2.5f);
            tRect.localRotation = Quaternion.Euler(0, 0, angleDeg);

            var img = tickObj.AddComponent<Image>();
            img.color = new Color(1f, 1f, 1f, 0.85f);
            m_RadialAmmoTicks[i] = img;
        }

        var warnObj = new GameObject("CrosshairReloadWarning");
        warnObj.transform.SetParent(m_CrosshairRoot.transform, false);
        var wRect = warnObj.AddComponent<RectTransform>();
        wRect.anchoredPosition = new Vector2(0, -45);
        wRect.sizeDelta = new Vector2(300, 35);

        m_ReloadWarningText = warnObj.AddComponent<Text>();
        m_ReloadWarningText.font = font;
        m_ReloadWarningText.fontSize = 18;
        m_ReloadWarningText.alignment = TextAnchor.MiddleCenter;
        m_ReloadWarningText.text = "";
        m_ReloadWarningText.color = Color.clear;
        warnObj.SetActive(false);
    }

    private Image CreateCrosshairLine(string name, Vector2 pos, Vector2 size, Color color)
    {
        var lineObj = new GameObject(name);
        lineObj.transform.SetParent(m_CrosshairRoot.transform, false);
        var rect = lineObj.AddComponent<RectTransform>();
        rect.anchoredPosition = pos;
        rect.sizeDelta = size;
        var img = lineObj.AddComponent<Image>();
        img.color = color;
        return img;
    }

    private void BuildBottomLeftCyberpunkShield(GameObject parent, Font font)
    {
        var container = new GameObject("BottomLeft_CyberpunkShieldContainer");
        container.transform.SetParent(parent.transform, false);

        var cRect = container.AddComponent<RectTransform>();
        cRect.anchorMin = new Vector2(0, 0);
        cRect.anchorMax = new Vector2(0, 0);
        cRect.pivot = new Vector2(0, 0);
        cRect.anchoredPosition = new Vector2(35, 120);
        cRect.sizeDelta = new Vector2(480, 80);

        var fixedObj = new GameObject("FixedMaxShieldText");
        fixedObj.transform.SetParent(container.transform, false);
        var fRect = fixedObj.AddComponent<RectTransform>();
        fRect.anchorMin = new Vector2(0, 0);
        fRect.anchorMax = new Vector2(0, 0);
        fRect.pivot = new Vector2(0, 0);
        fRect.anchoredPosition = new Vector2(0, 0);
        fRect.sizeDelta = new Vector2(100, 60);

        var fText = fixedObj.AddComponent<Text>();
        fText.font = font;
        fText.fontSize = 36;
        fText.alignment = TextAnchor.LowerLeft;
        fText.horizontalOverflow = HorizontalWrapMode.Overflow;
        fText.verticalOverflow = VerticalWrapMode.Overflow;
        fText.raycastTarget = false;
        fText.text = "<b><color=#00E5FF>150</color></b>";

        var slashObj = new GameObject("BlueSlashSeparator");
        slashObj.transform.SetParent(container.transform, false);
        var sRect = slashObj.AddComponent<RectTransform>();
        sRect.anchorMin = new Vector2(0, 0);
        sRect.anchorMax = new Vector2(0, 0);
        sRect.pivot = new Vector2(0, 0);
        sRect.anchoredPosition = new Vector2(76, 2);
        sRect.sizeDelta = new Vector2(30, 60);

        var sText = slashObj.AddComponent<Text>();
        sText.font = font;
        sText.fontSize = 50;
        sText.alignment = TextAnchor.LowerCenter;
        sText.horizontalOverflow = HorizontalWrapMode.Overflow;
        sText.verticalOverflow = VerticalWrapMode.Overflow;
        sText.raycastTarget = false;
        sText.text = "<b><color=#00E5FF>\\</color></b>";

        var curObj = new GameObject("DynamicCurShieldText");
        curObj.transform.SetParent(container.transform, false);
        var curRect = curObj.AddComponent<RectTransform>();
        curRect.anchorMin = new Vector2(0, 0);
        curRect.anchorMax = new Vector2(0, 0);
        curRect.pivot = new Vector2(0, 0);
        curRect.anchoredPosition = new Vector2(90, 10);
        curRect.sizeDelta = new Vector2(160, 80);

        m_BottomLeftShieldText = curObj.AddComponent<Text>();
        m_BottomLeftShieldText.font = font;
        m_BottomLeftShieldText.fontSize = 70;
        m_BottomLeftShieldText.alignment = TextAnchor.LowerLeft;
        m_BottomLeftShieldText.horizontalOverflow = HorizontalWrapMode.Overflow;
        m_BottomLeftShieldText.verticalOverflow = VerticalWrapMode.Overflow;
        m_BottomLeftShieldText.raycastTarget = false;
        m_BottomLeftShieldText.text = "<b><color=#00E5FF>0</color></b>";

        var berserkObj = new GameObject("Berserk_Active_HUD_Banner");
        berserkObj.transform.SetParent(container.transform, false);
        var bRect = berserkObj.AddComponent<RectTransform>();
        bRect.anchorMin = new Vector2(0, 0);
        bRect.anchorMax = new Vector2(0, 0);
        bRect.pivot = new Vector2(0, 0);
        bRect.anchoredPosition = new Vector2(0, 96);
        bRect.sizeDelta = new Vector2(480, 32);

        m_BerserkStatusText = berserkObj.AddComponent<Text>();
        m_BerserkStatusText.font = font;
        m_BerserkStatusText.fontSize = 18;
        m_BerserkStatusText.alignment = TextAnchor.LowerLeft;
        m_BerserkStatusText.horizontalOverflow = HorizontalWrapMode.Overflow;
        m_BerserkStatusText.verticalOverflow = VerticalWrapMode.Overflow;
        m_BerserkStatusText.raycastTarget = false;
        m_BerserkStatusText.text = "";

        var berserkJarDisplayObj = new GameObject("3D_BerserkJar_Display");
        berserkJarDisplayObj.transform.SetParent(container.transform, false);
        var bjdRect = berserkJarDisplayObj.AddComponent<RectTransform>();
        bjdRect.anchorMin = new Vector2(0, 0);
        bjdRect.anchorMax = new Vector2(0, 0);
        bjdRect.pivot = new Vector2(0, 0);
        bjdRect.anchoredPosition = new Vector2(220, -6);
        bjdRect.sizeDelta = new Vector2(160, 80);

        m_BerserkJarRawImage = berserkJarDisplayObj.AddComponent<RawImage>();
        m_BerserkJarRawImage.texture = m_BerserkJarRenderTexture;
        m_BerserkJarRawImage.color = Color.white;
        m_BerserkJarRawImage.raycastTarget = false;
        berserkJarDisplayObj.SetActive(false);
    }

    private void BuildBottomLeftCyberpunkHealth(GameObject parent, Font font)
    {
        var container = new GameObject("BottomLeft_CyberpunkHealthContainer");
        container.transform.SetParent(parent.transform, false);

        var cRect = container.AddComponent<RectTransform>();
        cRect.anchorMin = new Vector2(0, 0);
        cRect.anchorMax = new Vector2(0, 0);
        cRect.pivot = new Vector2(0, 0);
        cRect.anchoredPosition = new Vector2(35, 30);
        cRect.sizeDelta = new Vector2(480, 100);

        var fixedObj = new GameObject("FixedMaxHealthText");
        fixedObj.transform.SetParent(container.transform, false);
        var fRect = fixedObj.AddComponent<RectTransform>();
        fRect.anchorMin = new Vector2(0, 0);
        fRect.anchorMax = new Vector2(0, 0);
        fRect.pivot = new Vector2(0, 0);
        fRect.anchoredPosition = new Vector2(0, 0);
        fRect.sizeDelta = new Vector2(100, 60);

        var fText = fixedObj.AddComponent<Text>();
        fText.font = font;
        fText.fontSize = 36;
        fText.alignment = TextAnchor.LowerLeft;
        fText.horizontalOverflow = HorizontalWrapMode.Overflow;
        fText.verticalOverflow = VerticalWrapMode.Overflow;
        fText.raycastTarget = false;
        fText.text = "<b><color=#00FF77>100</color></b>";

        var slashObj = new GameObject("GreenSlashSeparator");
        slashObj.transform.SetParent(container.transform, false);
        var sRect = slashObj.AddComponent<RectTransform>();
        sRect.anchorMin = new Vector2(0, 0);
        sRect.anchorMax = new Vector2(0, 0);
        sRect.pivot = new Vector2(0, 0);
        sRect.anchoredPosition = new Vector2(76, 2);
        sRect.sizeDelta = new Vector2(30, 60);

        var sText = slashObj.AddComponent<Text>();
        sText.font = font;
        sText.fontSize = 50;
        sText.alignment = TextAnchor.LowerCenter;
        sText.horizontalOverflow = HorizontalWrapMode.Overflow;
        sText.verticalOverflow = VerticalWrapMode.Overflow;
        sText.raycastTarget = false;
        sText.text = "<b><color=#00FF77>\\</color></b>";

        var curObj = new GameObject("DynamicCurHealthText");
        curObj.transform.SetParent(container.transform, false);
        var curRect = curObj.AddComponent<RectTransform>();
        curRect.anchorMin = new Vector2(0, 0);
        curRect.anchorMax = new Vector2(0, 0);
        curRect.pivot = new Vector2(0, 0);
        curRect.anchoredPosition = new Vector2(90, 10);
        curRect.sizeDelta = new Vector2(160, 80);

        m_BottomLeftHealthText = curObj.AddComponent<Text>();
        m_BottomLeftHealthText.font = font;
        m_BottomLeftHealthText.fontSize = 70;
        m_BottomLeftHealthText.alignment = TextAnchor.LowerLeft;
        m_BottomLeftHealthText.horizontalOverflow = HorizontalWrapMode.Overflow;
        m_BottomLeftHealthText.verticalOverflow = VerticalWrapMode.Overflow;
        m_BottomLeftHealthText.raycastTarget = false;
        m_BottomLeftHealthText.text = "<b><color=#00FF77>100</color></b>";

        var jarDisplayObj = new GameObject("3D_HealthJar_Display");
        jarDisplayObj.transform.SetParent(container.transform, false);
        var jdRect = jarDisplayObj.AddComponent<RectTransform>();
        jdRect.anchorMin = new Vector2(0, 0);
        jdRect.anchorMax = new Vector2(0, 0);
        jdRect.pivot = new Vector2(0, 0);
        jdRect.anchoredPosition = new Vector2(220, -6);
        jdRect.sizeDelta = new Vector2(240, 80);

        m_JarRawImage = jarDisplayObj.AddComponent<RawImage>();
        m_JarRawImage.texture = m_JarRenderTexture;
        m_JarRawImage.color = Color.white;
        m_JarRawImage.raycastTarget = false;
        jarDisplayObj.SetActive(false);
    }

    private void Build3DHealthJarStudio()
    {
        if (m_JarStudioRoot != null) Destroy(m_JarStudioRoot);

        m_JarStudioRoot = new GameObject("3D_HealthJar_UI_Studio");
        m_JarStudioRoot.transform.position = new Vector3(0f, -800f, 0f);

        var fbx = Resources.Load<GameObject>("Firstaid") ??
#if UNITY_EDITOR
                  UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/First aid jar/Models/Firstaid.fbx");
#else
                  null;
#endif
        Mesh jarMesh = fbx != null ? fbx.GetComponentInChildren<MeshFilter>()?.sharedMesh : null;

#if UNITY_EDITOR
        m_MatWhiteJar = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/First aid jar/Materials/Firstaid.mat");
        m_MatGreenJar = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/First aid jar/Materials/Firstaid_2.mat");
        m_MatRedJar = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/First aid jar/Materials/Firstaid_Berserk_Red.mat");
#endif

        var healthCamObj = new GameObject("HealthJarStudio_Camera");
        healthCamObj.transform.SetParent(m_JarStudioRoot.transform, false);
        healthCamObj.transform.localPosition = new Vector3(0f, 0.14f, -0.92f);
        healthCamObj.transform.localRotation = Quaternion.Euler(6f, 0f, 0f);

        m_JarCamera = healthCamObj.AddComponent<Camera>();
        m_JarCamera.clearFlags = CameraClearFlags.SolidColor;
        m_JarCamera.backgroundColor = new Color(0f, 0f, 0f, 0f);
        m_JarCamera.fieldOfView = 36f;
        m_JarCamera.nearClipPlane = 0.05f;
        m_JarCamera.farClipPlane = 10f;
        m_JarCamera.depth = -100;
        m_JarCamera.enabled = false;

        m_JarRenderTexture = new RenderTexture(480, 160, 16, RenderTextureFormat.ARGB32);
        m_JarRenderTexture.name = "HealthJarStudio_RT";
        m_JarRenderTexture.Create();
        m_JarCamera.targetTexture = m_JarRenderTexture;

        var lightObj = new GameObject("HealthJarStudio_Light");
        lightObj.transform.SetParent(m_JarStudioRoot.transform, false);
        lightObj.transform.localRotation = Quaternion.Euler(35f, -40f, 0f);
        var dirLight = lightObj.AddComponent<Light>();
        dirLight.type = LightType.Directional;
        dirLight.color = new Color(1.0f, 0.98f, 0.95f);
        dirLight.intensity = 1.6f;
        dirLight.shadows = LightShadows.None;

        m_3DJarRenderers = new MeshRenderer[3];
        float[] xOffsets = new float[] { -0.32f, 0.0f, 0.32f };

        for (int i = 0; i < 3; i++)
        {
            var jar = new GameObject($"3DHealthJar_{i}");
            jar.transform.SetParent(m_JarStudioRoot.transform, false);
            jar.transform.localPosition = new Vector3(xOffsets[i], 0.03f, 0f);
            jar.transform.localRotation = Quaternion.Euler(8f, 60f, -4f);
            jar.transform.localScale = Vector3.one * 2.0f;

            var mf = jar.AddComponent<MeshFilter>();
            if (jarMesh != null) mf.sharedMesh = jarMesh;

            var mr = jar.AddComponent<MeshRenderer>();
            mr.sharedMaterial = m_MatGreenJar ?? m_MatWhiteJar;
            m_3DJarRenderers[i] = mr;
            jar.SetActive(false);
        }

        var berserkRoot = new GameObject("BerserkJarStudio_Root");
        berserkRoot.transform.SetParent(m_JarStudioRoot.transform, false);
        berserkRoot.transform.localPosition = new Vector3(0f, 3.0f, 0f);

        var berserkCamObj = new GameObject("BerserkJarStudio_Camera");
        berserkCamObj.transform.SetParent(berserkRoot.transform, false);
        berserkCamObj.transform.localPosition = new Vector3(0f, 0.14f, -0.92f);
        berserkCamObj.transform.localRotation = Quaternion.Euler(6f, 0f, 0f);

        m_BerserkJarCamera = berserkCamObj.AddComponent<Camera>();
        m_BerserkJarCamera.clearFlags = CameraClearFlags.SolidColor;
        m_BerserkJarCamera.backgroundColor = new Color(0f, 0f, 0f, 0f);
        m_BerserkJarCamera.fieldOfView = 36f;
        m_BerserkJarCamera.nearClipPlane = 0.05f;
        m_BerserkJarCamera.farClipPlane = 10f;
        m_BerserkJarCamera.depth = -100;
        m_BerserkJarCamera.enabled = false;

        m_BerserkJarRenderTexture = new RenderTexture(320, 160, 16, RenderTextureFormat.ARGB32);
        m_BerserkJarRenderTexture.name = "BerserkJarStudio_RT";
        m_BerserkJarRenderTexture.Create();
        m_BerserkJarCamera.targetTexture = m_BerserkJarRenderTexture;

        var bLightObj = new GameObject("BerserkJarStudio_Light");
        bLightObj.transform.SetParent(berserkRoot.transform, false);
        bLightObj.transform.localRotation = Quaternion.Euler(35f, -40f, 0f);
        var bLight = bLightObj.AddComponent<Light>();
        bLight.type = LightType.Directional;
        bLight.color = new Color(1.0f, 0.5f, 0.5f);
        bLight.intensity = 1.8f;
        bLight.shadows = LightShadows.None;

        m_3DBerserkJarRenderers = new MeshRenderer[2];
        float[] bOffsets = new float[] { -0.16f, 0.16f };

        for (int i = 0; i < 2; i++)
        {
            var jar = new GameObject($"3DBerserkJar_{i}");
            jar.transform.SetParent(berserkRoot.transform, false);
            jar.transform.localPosition = new Vector3(bOffsets[i], 0.03f, 0f);
            jar.transform.localRotation = Quaternion.Euler(8f, 60f, -4f);
            jar.transform.localScale = Vector3.one * 2.0f;

            var mf = jar.AddComponent<MeshFilter>();
            if (jarMesh != null) mf.sharedMesh = jarMesh;

            var mr = jar.AddComponent<MeshRenderer>();
            mr.sharedMaterial = m_MatRedJar ?? m_MatWhiteJar;
            m_3DBerserkJarRenderers[i] = mr;
            jar.SetActive(false);
        }
    }

    private void BuildBottomRightWeaponCard(GameObject parent, Font font)
    {
        m_WeaponCard = new GameObject("BottomRight_WeaponCard");
        m_WeaponCard.transform.SetParent(parent.transform, false);
        var rect = m_WeaponCard.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(1, 0);
        rect.anchorMax = new Vector2(1, 0);
        rect.pivot = new Vector2(1, 0);
        rect.anchoredPosition = new Vector2(-35, 35);
        rect.sizeDelta = new Vector2(360, 110);

        var bg = m_WeaponCard.AddComponent<Image>();
        bg.color = new Color(0.05f, 0.07f, 0.10f, 0.82f);

        var nameObj = new GameObject("WeaponNameText");
        nameObj.transform.SetParent(m_WeaponCard.transform, false);
        var nRect = nameObj.AddComponent<RectTransform>();
        nRect.anchorMin = new Vector2(0, 1);
        nRect.anchorMax = new Vector2(1, 1);
        nRect.pivot = new Vector2(0, 1);
        nRect.anchoredPosition = new Vector2(20, -12);
        nRect.sizeDelta = new Vector2(-40, 28);

        m_WeaponNameText = nameObj.AddComponent<Text>();
        m_WeaponNameText.font = font;
        m_WeaponNameText.fontSize = 18;
        m_WeaponNameText.text = "<b>M4A1 TACTICAL RIFLE</b>";
        m_WeaponNameText.color = new Color(0.7f, 0.8f, 0.95f, 1f);
        m_WeaponNameText.alignment = TextAnchor.MiddleLeft;

        var ammoObj = new GameObject("WeaponAmmoText");
        ammoObj.transform.SetParent(m_WeaponCard.transform, false);
        var aRect = ammoObj.AddComponent<RectTransform>();
        aRect.anchorMin = new Vector2(0, 0);
        aRect.anchorMax = new Vector2(1, 0);
        aRect.pivot = new Vector2(0, 0);
        aRect.anchoredPosition = new Vector2(20, 16);
        aRect.sizeDelta = new Vector2(-40, 52);

        m_WeaponAmmoText = ammoObj.AddComponent<Text>();
        m_WeaponAmmoText.font = font;
        m_WeaponAmmoText.fontSize = 38;
        m_WeaponAmmoText.text = "<b>30</b> <size=24><color=#AAAAAA>| 120</color></size>  <color=#FFD700>▮</color>";
        m_WeaponAmmoText.color = Color.white;
        m_WeaponAmmoText.alignment = TextAnchor.MiddleLeft;

        m_ReloadBarRoot = new GameObject("ReloadProgressBar");
        m_ReloadBarRoot.transform.SetParent(m_WeaponCard.transform, false);
        var rRect = m_ReloadBarRoot.AddComponent<RectTransform>();
        rRect.anchorMin = new Vector2(0, 0);
        rRect.anchorMax = new Vector2(1, 0);
        rRect.anchoredPosition = new Vector2(0, 2);
        rRect.sizeDelta = new Vector2(0, 4);

        var rBg = m_ReloadBarRoot.AddComponent<Image>();
        rBg.color = new Color(0.2f, 0.2f, 0.25f, 0.8f);

        var rFillObj = new GameObject("ReloadFill");
        rFillObj.transform.SetParent(m_ReloadBarRoot.transform, false);
        var rfRect = rFillObj.AddComponent<RectTransform>();
        rfRect.anchorMin = Vector2.zero;
        rfRect.anchorMax = Vector2.one;
        rfRect.sizeDelta = Vector2.zero;
        m_ReloadBarFill = rFillObj.AddComponent<Image>();
        m_ReloadBarFill.color = new Color(1.0f, 0.85f, 0.2f, 1f);

        m_ReloadBarRoot.SetActive(false);
    }

    private void BuildBossBar(GameObject parent, Font font)
    {
        m_BossBarRoot = new GameObject("BossHealthBarRoot");
        m_BossBarRoot.transform.SetParent(parent.transform, false);
        var rect = m_BossBarRoot.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.08f);
        rect.anchorMax = new Vector2(0.5f, 0.08f);
        rect.anchoredPosition = new Vector2(0, 0);
        rect.sizeDelta = new Vector2(620, 36);

        var bg = m_BossBarRoot.AddComponent<Image>();
        bg.color = new Color(0.04f, 0.04f, 0.05f, 0.95f);

        var titleObj = new GameObject("BossName");
        titleObj.transform.SetParent(m_BossBarRoot.transform, false);
        var tRect = titleObj.AddComponent<RectTransform>();
        tRect.anchorMin = new Vector2(0, 1);
        tRect.anchorMax = new Vector2(0, 1);
        tRect.pivot = new Vector2(0, 0);
        tRect.anchoredPosition = new Vector2(8, 2);
        tRect.sizeDelta = new Vector2(500, 22);

        m_BossNameText = titleObj.AddComponent<Text>();
        m_BossNameText.font = font;
        m_BossNameText.text = "<b>GOLIATH BERSERKER</b>";
        m_BossNameText.fontSize = 15;
        m_BossNameText.color = new Color(0.96f, 0.96f, 0.98f, 1f);
        m_BossNameText.alignment = TextAnchor.LowerLeft;

        var shadow = titleObj.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.85f);
        shadow.effectDistance = new Vector2(1f, -1f);

        var fillBg = new GameObject("FillBG");
        fillBg.transform.SetParent(m_BossBarRoot.transform, false);
        var fbgRect = fillBg.AddComponent<RectTransform>();
        fbgRect.anchoredPosition = new Vector2(0, -6);
        fbgRect.sizeDelta = new Vector2(604, 12);
        var fbImg = fillBg.AddComponent<Image>();
        fbImg.color = new Color(0.10f, 0.10f, 0.12f, 1f);

        var fill = new GameObject("Fill");
        fill.transform.SetParent(fillBg.transform, false);
        var fRect = fill.AddComponent<RectTransform>();
        fRect.anchorMin = Vector2.zero;
        fRect.anchorMax = Vector2.one;
        fRect.offsetMin = Vector2.zero;
        fRect.offsetMax = Vector2.zero;
        fRect.sizeDelta = Vector2.zero;

        m_BossBarFill = fill.AddComponent<Image>();
        m_BossBarFill.sprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f));
        m_BossBarFill.type = Image.Type.Filled;
        m_BossBarFill.fillMethod = Image.FillMethod.Horizontal;
        m_BossBarFill.fillOrigin = (int)Image.OriginHorizontal.Left;
        m_BossBarFill.fillAmount = 1.0f;
        m_BossBarFill.color = new Color(0.95f, 0.15f, 0.15f, 1f);

        m_BossBarRoot.SetActive(false);
    }

    private void BuildFloatingPickupToast(GameObject parent, Font font)
    {
        m_PickupToastObj = new GameObject("FloatingPickupToast");
        m_PickupToastObj.transform.SetParent(parent.transform, false);
        var rect = m_PickupToastObj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(1, 0);
        rect.anchorMax = new Vector2(1, 0);
        rect.pivot = new Vector2(1, 0);
        rect.anchoredPosition = new Vector2(-45, 160);
        rect.sizeDelta = new Vector2(240, 48);

        var bg = m_PickupToastObj.AddComponent<Image>();
        bg.color = new Color(0.08f, 0.10f, 0.15f, 0.85f);

        var textObj = new GameObject("ToastText");
        textObj.transform.SetParent(m_PickupToastObj.transform, false);
        var tRect = textObj.AddComponent<RectTransform>();
        tRect.anchorMin = Vector2.zero;
        tRect.anchorMax = Vector2.one;
        tRect.sizeDelta = Vector2.zero;

        m_PickupToastText = textObj.AddComponent<Text>();
        m_PickupToastText.font = font;
        m_PickupToastText.fontSize = 22;
        m_PickupToastText.alignment = TextAnchor.MiddleCenter;
        m_PickupToastText.text = "<b><color=#FFD700>+60 ▮ AMMO</color></b>";

        m_PickupToastObj.SetActive(false);
    }

    private void BuildGameOverPanel(GameObject parent, Font font)
    {
        m_GameOverPanel = new GameObject("GameOverPanel");
        m_GameOverPanel.transform.SetParent(parent.transform, false);
        var rect = m_GameOverPanel.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.sizeDelta = Vector2.zero;

        var bg = m_GameOverPanel.AddComponent<Image>();
        bg.color = new Color(0.04f, 0.05f, 0.08f, 0.94f);

        var titleObj = new GameObject("GameOverTitle");
        titleObj.transform.SetParent(m_GameOverPanel.transform, false);
        var tRect = titleObj.AddComponent<RectTransform>();
        tRect.anchorMin = new Vector2(0.5f, 1f);
        tRect.anchorMax = new Vector2(0.5f, 1f);
        tRect.pivot = new Vector2(0.5f, 1f);
        tRect.anchoredPosition = new Vector2(0, -35);
        tRect.sizeDelta = new Vector2(900, 70);

        m_GameOverTitleText = titleObj.AddComponent<Text>();
        m_GameOverTitleText.font = font;
        m_GameOverTitleText.text = "<b><color=#FF2222>YOU DIED</color></b>";
        m_GameOverTitleText.fontSize = 48;
        m_GameOverTitleText.alignment = TextAnchor.MiddleCenter;

        var leftCard = new GameObject("Left_SummaryCard");
        leftCard.transform.SetParent(m_GameOverPanel.transform, false);
        var lRect = leftCard.AddComponent<RectTransform>();
        lRect.anchorMin = new Vector2(0.5f, 0.5f);
        lRect.anchorMax = new Vector2(0.5f, 0.5f);
        lRect.anchoredPosition = new Vector2(-440, 20);
        lRect.sizeDelta = new Vector2(460, 520);
        var lImg = leftCard.AddComponent<Image>();
        lImg.color = new Color(0.08f, 0.11f, 0.16f, 0.95f);

        var lhObj = new GameObject("Header");
        lhObj.transform.SetParent(leftCard.transform, false);
        var lhRect = lhObj.AddComponent<RectTransform>();
        lhRect.anchoredPosition = new Vector2(0, 220);
        lhRect.sizeDelta = new Vector2(420, 40);
        var lhText = lhObj.AddComponent<Text>();
        lhText.font = font;
        lhText.fontSize = 24;
        lhText.text = "📊 <b>MATCH DEBRIEF</b>";
        lhText.alignment = TextAnchor.MiddleCenter;
        lhText.color = new Color(1.0f, 0.85f, 0.35f, 1.0f);

        var statsObj = new GameObject("StatsText");
        statsObj.transform.SetParent(leftCard.transform, false);
        var stRect = statsObj.AddComponent<RectTransform>();
        stRect.anchoredPosition = new Vector2(0, -10);
        stRect.sizeDelta = new Vector2(400, 380);
        m_GameOverStatsText = statsObj.AddComponent<Text>();
        m_GameOverStatsText.font = font;
        m_GameOverStatsText.fontSize = 20;
        m_GameOverStatsText.alignment = TextAnchor.MiddleLeft;
        m_GameOverStatsText.color = Color.white;

        var rightCard = new GameObject("Right_LeaderboardCard");
        rightCard.transform.SetParent(m_GameOverPanel.transform, false);
        var rRect = rightCard.AddComponent<RectTransform>();
        rRect.anchorMin = new Vector2(0.5f, 0.5f);
        rRect.anchorMax = new Vector2(0.5f, 0.5f);
        rRect.anchoredPosition = new Vector2(380, 20);
        rRect.sizeDelta = new Vector2(800, 520);
        var rImg = rightCard.AddComponent<Image>();
        rImg.color = new Color(0.08f, 0.11f, 0.16f, 0.95f);

        var rhObj = new GameObject("Title");
        rhObj.transform.SetParent(rightCard.transform, false);
        var rhRect = rhObj.AddComponent<RectTransform>();
        rhRect.anchoredPosition = new Vector2(0, 220);
        rhRect.sizeDelta = new Vector2(740, 40);
        var rhText = rhObj.AddComponent<Text>();
        rhText.font = font;
        rhText.fontSize = 24;
        rhText.text = "👑 <b>GLOBAL TOP 10 HIGHSCORES</b>";
        rhText.alignment = TextAnchor.MiddleCenter;
        rhText.color = new Color(1.0f, 0.88f, 0.35f, 1.0f);

        var tblHeader = new GameObject("TableHeader");
        tblHeader.transform.SetParent(rightCard.transform, false);
        var thRect = tblHeader.AddComponent<RectTransform>();
        thRect.anchoredPosition = new Vector2(0, 175);
        thRect.sizeDelta = new Vector2(740, 30);
        var thImg = tblHeader.AddComponent<Image>();
        thImg.color = new Color(0.14f, 0.18f, 0.26f, 0.9f);

        CreateGameOverCellText(tblHeader.transform, "RankH", new Vector2(10, 0), new Vector2(75, 30), "<b>RANK</b>", TextAnchor.MiddleLeft, new Color(0.7f, 0.85f, 1f));
        CreateGameOverCellText(tblHeader.transform, "PlayerH", new Vector2(90, 0), new Vector2(220, 30), "<b>PLAYER</b>", TextAnchor.MiddleLeft, new Color(0.7f, 0.85f, 1f));
        CreateGameOverCellText(tblHeader.transform, "ScoreH", new Vector2(320, 0), new Vector2(130, 30), "<b>HIGH SCORE</b>", TextAnchor.MiddleRight, new Color(0.7f, 0.85f, 1f));
        CreateGameOverCellText(tblHeader.transform, "WaveH", new Vector2(470, 0), new Vector2(140, 30), "<b>MAX WAVE</b>", TextAnchor.MiddleCenter, new Color(0.7f, 0.85f, 1f));
        CreateGameOverCellText(tblHeader.transform, "KillsH", new Vector2(620, 0), new Vector2(100, 30), "<b>KILLS</b>", TextAnchor.MiddleRight, new Color(0.7f, 0.85f, 1f));

        var rowsContainer = new GameObject("RowsContainer");
        rowsContainer.transform.SetParent(rightCard.transform, false);
        var rcRect = rowsContainer.AddComponent<RectTransform>();
        rcRect.anchoredPosition = new Vector2(0, -10);
        rcRect.sizeDelta = new Vector2(740, 310);
        m_GameOverLeaderboardContainer = rowsContainer.transform;

        var bottomObj = new GameObject("BottomActions");
        bottomObj.transform.SetParent(m_GameOverPanel.transform, false);
        var bRect = bottomObj.AddComponent<RectTransform>();
        bRect.anchorMin = new Vector2(0.5f, 0f);
        bRect.anchorMax = new Vector2(0.5f, 0f);
        bRect.pivot = new Vector2(0.5f, 0f);
        bRect.anchoredPosition = new Vector2(0, 30);
        bRect.sizeDelta = new Vector2(900, 65);

        CreateGameOverButton(bottomObj.transform, "PlayAgainBtn", new Vector2(-240, 0), new Vector2(220, 52), "🔄 PLAY AGAIN", new Color(0.2f, 0.65f, 0.35f), () =>
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        });

        CreateGameOverButton(bottomObj.transform, "MainMenuBtn", new Vector2(30, 0), new Vector2(220, 52), "📋 MAIN MENU", new Color(0.25f, 0.45f, 0.85f), () =>
        {
            if (m_GameOverPanel != null) m_GameOverPanel.SetActive(false);
            if (MainMenu.Instance != null) MainMenu.Instance.ShowMenu();
        });

        CreateGameOverButton(bottomObj.transform, "QuitBtn", new Vector2(280, 0), new Vector2(160, 52), "✕ QUIT", new Color(0.6f, 0.2f, 0.2f), () =>
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        });

        m_GameOverPanel.SetActive(false);
    }

    private Text CreateGameOverCellText(Transform parent, string name, Vector2 pos, Vector2 size, string content, TextAnchor alignment, Color color)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        var rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 0.5f);
        rect.anchorMax = new Vector2(0f, 0.5f);
        rect.pivot = new Vector2(0f, 0.5f);
        rect.anchoredPosition = pos;
        rect.sizeDelta = size;

        var text = obj.AddComponent<Text>();
        text.font = hudFont;
        text.fontSize = 16;
        text.text = content;
        text.alignment = alignment;
        text.color = color;
        return text;
    }

    private Button CreateGameOverButton(Transform parent, string name, Vector2 pos, Vector2 size, string label, Color color, UnityEngine.Events.UnityAction onClick)
    {
        var btnObj = new GameObject(name);
        btnObj.transform.SetParent(parent, false);
        var rect = btnObj.AddComponent<RectTransform>();
        rect.anchoredPosition = pos;
        rect.sizeDelta = size;

        var img = btnObj.AddComponent<Image>();
        img.color = color;

        var btn = btnObj.AddComponent<Button>();
        btn.onClick.AddListener(onClick);

        var textObj = new GameObject("Label");
        textObj.transform.SetParent(btnObj.transform, false);
        var tRect = textObj.AddComponent<RectTransform>();
        tRect.anchorMin = Vector2.zero;
        tRect.anchorMax = Vector2.one;
        tRect.sizeDelta = Vector2.zero;
        var t = textObj.AddComponent<Text>();
        t.font = hudFont;
        t.fontSize = 18;
        t.text = $"<b>{label}</b>";
        t.alignment = TextAnchor.MiddleCenter;
        t.color = Color.white;

        return btn;
    }

    public void PopulateGameOverLeaderboard()
    {
        if (m_GameOverLeaderboardContainer == null) return;

        for (int i = m_GameOverLeaderboardContainer.childCount - 1; i >= 0; i--)
        {
            Destroy(m_GameOverLeaderboardContainer.GetChild(i).gameObject);
        }

        if (NetworkManager.Instance == null) return;

        StartCoroutine(NetworkManager.Instance.FetchLeaderboardRoutine((success, items) =>
        {
            if (m_GameOverLeaderboardContainer == null) return;

            if (success && items != null && items.Count > 0)
            {
                int count = Mathf.Min(items.Count, 10);
                float rowHeight = 30f;
                float startY = 135f;

                for (int i = 0; i < count; i++)
                {
                    var item = items[i];
                    var rowObj = new GameObject($"GRow_{i}");
                    rowObj.transform.SetParent(m_GameOverLeaderboardContainer, false);
                    var rect = rowObj.AddComponent<RectTransform>();
                    rect.anchoredPosition = new Vector2(0, startY - (i * rowHeight));
                    rect.sizeDelta = new Vector2(740, rowHeight - 2f);

                    var bg = rowObj.AddComponent<Image>();
                    bg.color = (i % 2 == 0) ? new Color(0.10f, 0.13f, 0.18f, 0.85f) : new Color(0.06f, 0.08f, 0.12f, 0.85f);

                    string rankIcon = item.rank switch
                    {
                        1 => "<color=#FFD700>#1 👑</color>",
                        2 => "<color=#C0C0C0>#2 🥈</color>",
                        3 => "<color=#CD7F32>#3 🥉</color>",
                        _ => $"#{item.rank}"
                    };

                    bool isSelf = (AuthManager.Instance != null && item.username == AuthManager.Instance.username);
                    Color textColor = isSelf ? new Color(0.3f, 1.0f, 0.5f) : Color.white;
                    string waveTag = item.maxWave == 10 ? "<color=#88FFAA>Wave 10 ★</color>" : $"Wave {item.maxWave}";

                    CreateGameOverCellText(rowObj.transform, "Rank", new Vector2(10, 0), new Vector2(75, 28), rankIcon, TextAnchor.MiddleLeft, textColor);
                    CreateGameOverCellText(rowObj.transform, "Player", new Vector2(90, 0), new Vector2(220, 28), $"<b>{item.username}</b>", TextAnchor.MiddleLeft, textColor);
                    CreateGameOverCellText(rowObj.transform, "Score", new Vector2(320, 0), new Vector2(130, 28), $"{item.bestScore:N0}", TextAnchor.MiddleRight, textColor);
                    CreateGameOverCellText(rowObj.transform, "Wave", new Vector2(470, 0), new Vector2(140, 28), waveTag, TextAnchor.MiddleCenter, textColor);
                    CreateGameOverCellText(rowObj.transform, "Kills", new Vector2(620, 0), new Vector2(100, 28), $"{item.lifetimeKills:N0}", TextAnchor.MiddleRight, textColor);
                }
            }
            else
            {
                var emptyObj = new GameObject("EmptyMessage");
                emptyObj.transform.SetParent(m_GameOverLeaderboardContainer, false);
                var rect = emptyObj.AddComponent<RectTransform>();
                rect.anchoredPosition = new Vector2(0, 30);
                rect.sizeDelta = new Vector2(700, 50);

                var t = emptyObj.AddComponent<Text>();
                t.font = hudFont;
                t.fontSize = 17;
                t.alignment = TextAnchor.MiddleCenter;
                t.text = "<i>No match records yet. Be the first on the Leaderboard!</i>";
                t.color = new Color(0.7f, 0.75f, 0.85f, 0.85f);
            }
        }));
    }

    public void RegisterKill(bool isHeadshot)
    {
        m_TotalKills++;
        if (isHeadshot) m_HeadshotKills++;
        UpdateTopRightStats();
    }

    public void SetBossSlayerBadge()
    {
        m_BossSlayerBadge = true;
        UpdateTopRightStats();
    }

    private void UpdateTopRightStats()
    {
        var scoreMgr = Score.Instance ?? FindAnyObjectByType<Score>();
        int curScore = scoreMgr != null ? scoreMgr.currentScore : 0;
        int hiScore = scoreMgr != null ? scoreMgr.highScore : 0;

        if (m_TotalKillsText != null)
        {
            string badge = m_BossSlayerBadge ? " <color=#FFD700>★ BOSS</color>" : "";
            m_TotalKillsText.text = $"👑 <b>SCORE: {curScore:N0}</b>  <size=14><color=#AAAAAA>(HIGH: {hiScore:N0})</color></size>{badge}";
        }
        if (m_HeadshotsText != null)
        {
            m_HeadshotsText.text = $"🎯 <b>KILLS: {m_TotalKills}</b>  |  Headshots: {m_HeadshotKills}";
        }
    }

    public void ShowHitmarker(bool isHeadshot)
    {
        m_HitmarkerTimer = 0.22f;
        m_IsHeadshotHit = isHeadshot;
    }

    public void TriggerDamageFlash()
    {
        if (m_DamageOverlay != null)
        {
            m_DamageOverlay.color = new Color(0.9f, 0.05f, 0.05f, 0.55f);
        }
    }

    public void TriggerHealFlash()
    {
        if (m_HealOverlay != null)
        {
            m_HealOverlay.color = new Color(0.1f, 0.95f, 0.35f, 0.45f);
        }
    }

    public void ShowPickupToast(string message, Color color)
    {
        if (m_PickupToastObj != null && m_PickupToastText != null)
        {
            m_PickupToastText.text = $"<b>{message}</b>";
            m_PickupToastText.color = color;
            m_PickupToastObj.SetActive(true);
            m_PickupToastTimer = 1.8f;
        }
    }

    public void ShowAmmoPickupToast(int amount)
    {
        ShowPickupToast($"+{amount} ▮ AMMO", new Color(1.0f, 0.85f, 0.2f, 1f));
    }

    public void SetSandstormIntensity(float intensity)
    {
        if (m_SandstormOverlay != null)
        {
            Color sc = m_SandstormOverlay.color;
            sc.a = Mathf.Clamp01(intensity * 0.32f);
            m_SandstormOverlay.color = sc;
        }
    }

    public void ShowWaveStatus(string message, Color color, float duration = 2.5f)
    {
        if (m_WaveActionBannerText != null)
        {
            m_WaveActionBannerText.text = message;
            m_WaveActionBannerText.color = color;
            m_StatusBannerTimer = duration;
        }
    }

    public void UpdateWaveHUD(int waveNumber, int maxWaves, int killsRemaining, int targetKills)
    {
        if (m_WaveInfoText != null)
        {
            m_WaveInfoText.text = $"<b>WAVE {waveNumber} / {maxWaves}</b>   <color=#FFCC00>Kills Left: <b>{killsRemaining}</b> / {targetKills}</color>";
        }
    }

    public void UpdateBossHealth(float current, float max)
    {
        if (m_BossBarRoot == null) return;

        if (max > 0 && current > 0)
        {
            m_BossBarHideTime = -1f;
            m_BossBarRoot.SetActive(true);
            float pct = Mathf.Clamp01(current / max);
            if (m_BossBarFill != null)
            {
                m_BossBarFill.enabled = true;
                m_BossBarFill.fillAmount = pct;
            }
            if (m_BossNameText != null)
            {

                m_BossNameText.text = "<b>GOLIATH BERSERKER</b>";
            }
        }
        else if (max > 0 && current <= 0)
        {

            if (m_BossBarFill != null)
            {
                m_BossBarFill.fillAmount = 0f;
                m_BossBarFill.enabled = false;
            }
            if (m_BossNameText != null)
            {
                m_BossNameText.text = "<b><color=#44FF44>GOLIATH BERSERKER (DEFEATED)</color></b>";
            }

            if (m_BossBarHideTime < 0f)
            {
                m_BossBarHideTime = Time.time + 3.0f;
            }
            else if (Time.time >= m_BossBarHideTime)
            {
                m_BossBarRoot.SetActive(false);
            }
        }
        else
        {
            m_BossBarRoot.SetActive(false);
        }
    }

    public void ShowVictoryScreen(int totalKills)
    {
        var scoreMgr = Score.Instance ?? FindAnyObjectByType<Score>();
        int curScore = scoreMgr != null ? scoreMgr.currentScore : 0;
        int hiScore = scoreMgr != null ? scoreMgr.highScore : 0;
        ShowVictoryScreen(totalKills, curScore, hiScore);
    }

    public void ShowVictoryScreen(int totalKills, int finalScore, int highScore)
    {
        if (m_GameOverPanel != null)
        {
            m_GameOverPanel.SetActive(true);
            if (m_GameOverTitleText != null)
            {
                m_GameOverTitleText.text = "<b><color=#44FF44>★ MISSION ACCOMPLISHED — VICTORY! ★</color></b>";
            }

            int dur = Mathf.RoundToInt(Time.timeSinceLevelLoad);
            int min = dur / 60;
            int sec = dur % 60;

            if (m_GameOverStatsText != null)
            {
                m_GameOverStatsText.text = $"<b>RESULT:</b> <color=#44FF44>ALL 10 WAVES CLEARED</color>\n\n" +
                                           $"<b>FINAL SCORE:</b> <color=#FFDD44>{finalScore:N0}</color> Pts\n" +
                                           $"<b>HIGH SCORE:</b>  <color=#FFFFFF>{highScore:N0}</color> Pts\n\n" +
                                           $"<b>TOTAL KILLS:</b> <color=#FF6666>{totalKills}</color>\n" +
                                           $"<b>HEADSHOTS:</b>   <color=#FFAA44>{m_HeadshotKills}</color>\n" +
                                           $"<b>SURVIVED:</b>    <color=#88FFAA>{min}m {sec:D2}s</color>";
            }

            PopulateGameOverLeaderboard();

            var player = FindAnyObjectByType<PlayerController>();
            if (player != null) player.UnlockCursor();
        }
    }

    private void Update()
    {
        if (Time.timeScale == 0f) return;

        if (m_Gun == null) m_Gun = FindAnyObjectByType<Gun>();
        if (m_Player == null) m_Player = FindAnyObjectByType<PlayerController>();

        UpdateCrosshairAndRadialAmmo();
        UpdateHitmarker();
        UpdateHealthAndPotionCard();
        UpdateWeaponShowcaseCard();
        UpdateTopRightStats();
        UpdatePickupToast();
        UpdateOverlaysFade();
        UpdateGameOverInput();
        HandleLeaderboardInput();
    }

    private void HandleLeaderboardInput()
    {
        bool togglePressed = false;
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null)
        {
            togglePressed = Keyboard.current.lKey.wasPressedThisFrame || Keyboard.current.tabKey.wasPressedThisFrame;
        }
#else
        togglePressed = Input.GetKeyDown(KeyCode.L) || Input.GetKeyDown(KeyCode.Tab);
#endif
        if (togglePressed)
        {
            if (LeaderboardUI.Instance != null)
            {
                LeaderboardUI.Instance.ToggleLeaderboard();
            }
        }
    }

    private void UpdateCrosshairAndRadialAmmo()
    {
        if (m_Gun == null || m_CrosshairRoot == null) return;

        float baseOffset = 5.0f;
        float gap = m_Gun.IsAiming ? 0.0f : (m_Player != null && m_Player.IsMoving ? 12.0f : 7.0f);
        float targetSpread = baseOffset + gap;

        m_CurrentCrosshairSpread = Mathf.Lerp(m_CurrentCrosshairSpread, targetSpread, Time.deltaTime * 20f);

        if (m_CrosshairTop != null) m_CrosshairTop.rectTransform.anchoredPosition = new Vector2(0, m_CurrentCrosshairSpread);
        if (m_CrosshairBottom != null) m_CrosshairBottom.rectTransform.anchoredPosition = new Vector2(0, -m_CurrentCrosshairSpread);
        if (m_CrosshairLeft != null) m_CrosshairLeft.rectTransform.anchoredPosition = new Vector2(-m_CurrentCrosshairSpread, 0);
        if (m_CrosshairRight != null) m_CrosshairRight.rectTransform.anchoredPosition = new Vector2(m_CurrentCrosshairSpread, 0);

        if (m_RadialAmmoTicks != null && m_RadialAmmoTicks.Length > 0)
        {
            int maxTicks = m_RadialAmmoTicks.Length;
            float ratio = (float)m_Gun.currentAmmo / Mathf.Max(1, m_Gun.clipSize);
            int activeCount = Mathf.RoundToInt(ratio * maxTicks);

            Color tickColor = ratio <= 0.20f ? new Color(1f, 0.25f, 0.25f, 0.95f) : (ratio <= 0.40f ? new Color(1f, 0.85f, 0.2f, 0.9f) : new Color(1f, 1f, 1f, 0.85f));

            for (int i = 0; i < maxTicks; i++)
            {
                if (m_RadialAmmoTicks[i] != null)
                {
                    bool isActive = i < activeCount;
                    m_RadialAmmoTicks[i].gameObject.SetActive(isActive);
                    if (isActive) m_RadialAmmoTicks[i].color = tickColor;
                }
            }
        }

        if (m_ReloadWarningText != null)
        {
            bool showReloadWarning = (m_Gun.currentAmmo < 5 || m_Gun.isReloading) && (m_Player == null || !m_Player.isDead);
            if (showReloadWarning)
            {
                if (!m_ReloadWarningText.gameObject.activeSelf) m_ReloadWarningText.gameObject.SetActive(true);
                m_ReloadWarningText.text = "<b>⚠ RELOAD [R]</b>";
                float blinkAlpha = Mathf.PingPong(Time.time * 5.0f, 1.0f);
                m_ReloadWarningText.color = new Color(1f, 0.25f, 0.25f, blinkAlpha);
            }
            else
            {
                if (m_ReloadWarningText.gameObject.activeSelf) m_ReloadWarningText.gameObject.SetActive(false);
                m_ReloadWarningText.text = "";
                m_ReloadWarningText.color = Color.clear;
            }
        }
    }

    private void UpdateHitmarker()
    {
        if (m_Hitmarker == null) return;

        if (m_HitmarkerTimer > 0f)
        {
            m_HitmarkerTimer -= Time.deltaTime;
            float progress = 1f - Mathf.Clamp01(m_HitmarkerTimer / 0.22f);
            Color c = m_IsHeadshotHit ? new Color(1f, 0.15f, 0.15f, 1f) : new Color(1f, 0.9f, 0.2f, 1f);
            c.a = Mathf.Clamp01(m_HitmarkerTimer / 0.22f);
            m_Hitmarker.color = c;

            float startScale = m_IsHeadshotHit ? 1.15f : 0.95f;
            float endScale = m_IsHeadshotHit ? 1.45f : 1.20f;
            m_Hitmarker.rectTransform.localScale = Vector3.one * Mathf.Lerp(startScale, endScale, progress);
        }
        else
        {
            m_Hitmarker.color = Color.clear;
        }
    }

    private void UpdateHealthAndPotionCard()
    {
        if (m_Player == null) return;

        if (m_BottomLeftHealthText != null)
        {
            int curHP = Mathf.Clamp(Mathf.CeilToInt(m_Player.currentHealth), 0, (int)m_Player.maxHealth);
            string curColor;
            if (curHP >= 65)
                curColor = "#00FF77";
            else if (curHP >= 30)
                curColor = "#FFDD33";
            else
                curColor = "#FF3344";

            m_BottomLeftHealthText.text = $"<b><color={curColor}>{curHP}</color></b>";
        }

        if (m_BottomLeftShieldText != null)
        {
            int curShield = Mathf.Clamp(Mathf.CeilToInt(m_Player.currentShield), 0, (int)m_Player.maxShield);
            m_BottomLeftShieldText.text = $"<b><color=#00E5FF>{curShield}</color></b>";
        }

        if (m_BerserkStatusText != null)
        {
            if (m_Player.IsBerserk)
            {
                m_BerserkStatusText.text = $"<b><color=#FF2244>★ BERSERK: {m_Player.berserkTimer:F1}s (2X DMG, 1.5X SPD) ★</color></b>";
            }
            else
            {
                m_BerserkStatusText.text = "";
            }
        }

        if (m_3DJarRenderers != null && m_JarCamera != null)
        {
            int cur = m_Player.storedPotions;

            if (m_JarRawImage != null)
            {
                m_JarRawImage.gameObject.SetActive(cur > 0);
            }

            if (cur > 0)
            {
                for (int i = 0; i < m_3DJarRenderers.Length; i++)
                {
                    if (m_3DJarRenderers[i] == null) continue;
                    if (i < cur)
                    {

                        m_3DJarRenderers[i].gameObject.SetActive(true);
                        if (m_MatGreenJar != null) m_3DJarRenderers[i].sharedMaterial = m_MatGreenJar;
                    }
                    else
                    {

                        m_3DJarRenderers[i].gameObject.SetActive(false);
                    }
                }

                m_JarCamera.Render();
            }
        }

        if (m_3DBerserkJarRenderers != null && m_BerserkJarCamera != null)
        {
            int curBerserk = m_Player.storedBerserkJars;

            if (m_BerserkJarRawImage != null)
            {
                m_BerserkJarRawImage.gameObject.SetActive(curBerserk > 0);
            }

            if (curBerserk > 0)
            {
                for (int i = 0; i < m_3DBerserkJarRenderers.Length; i++)
                {
                    if (m_3DBerserkJarRenderers[i] == null) continue;
                    if (i < curBerserk)
                    {
                        m_3DBerserkJarRenderers[i].gameObject.SetActive(true);
                        if (m_MatRedJar != null) m_3DBerserkJarRenderers[i].sharedMaterial = m_MatRedJar;
                    }
                    else
                    {
                        m_3DBerserkJarRenderers[i].gameObject.SetActive(false);
                    }
                }

                m_BerserkJarCamera.Render();
            }
        }
    }

    private void OnDestroy()
    {
        if (m_JarStudioRoot != null) Destroy(m_JarStudioRoot);
        if (m_JarRenderTexture != null) { m_JarRenderTexture.Release(); Destroy(m_JarRenderTexture); }
        if (m_BerserkJarRenderTexture != null) { m_BerserkJarRenderTexture.Release(); Destroy(m_BerserkJarRenderTexture); }
    }

    private void UpdateWeaponShowcaseCard()
    {
        if (m_Gun == null) return;

        if (m_WeaponAmmoText != null)
        {
            string ammoColor = m_Gun.currentAmmo <= 5 ? "#FF4444" : "#FFFFFF";
            m_WeaponAmmoText.text = $"<b><color={ammoColor}>{m_Gun.currentAmmo}</color></b> <size=24><color=#AAAAAA>| {m_Gun.reserveAmmo}</color></size>  <color=#FFD700>▮</color>";
        }

        if (m_ReloadBarRoot != null && m_ReloadBarFill != null)
        {
            if (m_Gun.isReloading)
            {
                m_ReloadBarRoot.SetActive(true);
                var rect = m_ReloadBarFill.rectTransform;
                rect.anchorMax = new Vector2(Mathf.Clamp01(m_Gun.reloadProgress), 1f);
            }
            else
            {
                m_ReloadBarRoot.SetActive(false);
            }
        }
    }

    private void UpdatePickupToast()
    {
        if (m_PickupToastObj == null) return;

        if (m_PickupToastTimer > 0f)
        {
            m_PickupToastTimer -= Time.deltaTime;
        }
        else
        {
            m_PickupToastObj.SetActive(false);
        }
    }

    private void UpdateOverlaysFade()
    {
        if (m_DamageOverlay != null && m_DamageOverlay.color.a > 0f)
        {
            Color c = m_DamageOverlay.color;
            c.a = Mathf.Lerp(c.a, 0f, Time.deltaTime * 3.5f);
            m_DamageOverlay.color = c;
        }

        if (m_HealOverlay != null && m_HealOverlay.color.a > 0f)
        {
            Color c = m_HealOverlay.color;
            c.a = Mathf.Lerp(c.a, 0f, Time.deltaTime * 3.0f);
            m_HealOverlay.color = c;
        }
    }

    private void UpdateGameOverInput()
    {
        if (m_Player != null && m_Player.isDead)
        {
            if (m_GameOverPanel != null && !m_GameOverPanel.activeSelf)
            {
                m_GameOverPanel.SetActive(true);
                int wave = m_WaveManager != null ? m_WaveManager.currentWave : 1;
                var scoreMgr = Score.Instance ?? FindAnyObjectByType<Score>();
                int curScore = scoreMgr != null ? scoreMgr.currentScore : 0;
                int hiScore = scoreMgr != null ? scoreMgr.highScore : 0;

                if (m_GameOverTitleText != null)
                {
                    m_GameOverTitleText.text = "<b><color=#FF2222>YOU DIED</color></b>";
                }

                int dur = Mathf.RoundToInt(Time.timeSinceLevelLoad);
                int min = dur / 60;
                int sec = dur % 60;

                if (m_GameOverStatsText != null)
                {
                    m_GameOverStatsText.text = $"<b>RESULT:</b> <color=#FF4444>FALLEN IN ACTION</color>\n\n" +
                                               $"<b>WAVE REACHED:</b> <color=#88FFAA>Wave {wave}</color>\n" +
                                               $"<b>MATCH SCORE:</b>  <color=#FFDD44>{curScore:N0}</color> Pts\n" +
                                               $"<b>HIGH SCORE:</b>   <color=#FFFFFF>{hiScore:N0}</color> Pts\n\n" +
                                               $"<b>TOTAL KILLS:</b>  <color=#FF6666>{m_TotalKills}</color>\n" +
                                               $"<b>HEADSHOTS:</b>    <color=#FFAA44>{m_HeadshotKills}</color>\n" +
                                               $"<b>SURVIVED:</b>     <color=#88FFAA>{min}m {sec:D2}s</color>";
                }

                PopulateGameOverLeaderboard();

                if (m_Player != null) m_Player.UnlockCursor();
            }
        }
    }
}

