using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class MainMenu : MonoBehaviour
{
    public static MainMenu Instance { get; private set; }

    [Header("Styling")]
    public Font menuFont;

    private GameObject m_MenuRootObj;
    private Canvas m_Canvas;

    private GameObject m_AuthCard;
    private InputField m_UsernameInput;
    private InputField m_PasswordInput;
    private Text m_AuthStatusText;
    private Text m_ProfileTitleText;
    private Text m_ProfileStatsText;
    private GameObject m_LoginFormObj;
    private GameObject m_ProfileViewObj;

    private GameObject m_LeaderboardCard;
    private Transform m_LeaderboardRowsContainer;
    private Text m_LeaderboardStatusText;

    private GameObject m_ControlsModalObj;

    private bool m_IsMenuOpen = true;
    public bool IsMenuOpen => m_IsMenuOpen;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        if (menuFont == null)
        {
            menuFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
        }
    }

    private void Start()
    {

        if (NetworkManager.Instance == null)
        {
            var netObj = new GameObject("NetworkManager");
            netObj.AddComponent<NetworkManager>();
        }
        if (AuthManager.Instance == null)
        {
            var authObj = new GameObject("AuthManager");
            authObj.AddComponent<AuthManager>();
        }

        BuildMainMenu();
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name.ToLower().Contains("boss"))
        {
            HideMenu();
        }
        else
        {
            ShowMenu();
        }
    }

    public void ShowMenu()
    {
        m_IsMenuOpen = true;
        if (m_MenuRootObj != null) m_MenuRootObj.SetActive(true);

        Time.timeScale = 0f;

        var player = FindAnyObjectByType<PlayerController>();
        if (player != null) player.UnlockCursor();

        UpdateProfileUI();
        RefreshLeaderboard();
    }

    public void HideMenu()
    {
        m_IsMenuOpen = false;
        if (m_MenuRootObj != null) m_MenuRootObj.SetActive(false);

        Time.timeScale = 1f;

        var player = FindAnyObjectByType<PlayerController>();
        if (player != null && !player.isDead) player.LockCursor();
    }

    private void BuildMainMenu()
    {
        m_MenuRootObj = new GameObject("MainMenu_CanvasRoot");
        m_MenuRootObj.transform.SetParent(transform, false);

        m_Canvas = m_MenuRootObj.AddComponent<Canvas>();
        m_Canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        m_Canvas.sortingOrder = 600;

        var scaler = m_MenuRootObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        m_MenuRootObj.AddComponent<GraphicRaycaster>();

        var backdrop = new GameObject("Backdrop");
        backdrop.transform.SetParent(m_MenuRootObj.transform, false);
        var bRect = backdrop.AddComponent<RectTransform>();
        bRect.anchorMin = Vector2.zero;
        bRect.anchorMax = Vector2.one;
        bRect.sizeDelta = Vector2.zero;
        var bImg = backdrop.AddComponent<Image>();
        bImg.color = new Color(0.04f, 0.05f, 0.08f, 0.90f);

        BuildHeader(backdrop.transform);

        BuildAuthAndProfilePanel(backdrop.transform);

        BuildLeaderboardPanel(backdrop.transform);

        BuildBottomActionButtons(backdrop.transform);

        BuildControlsModal(backdrop.transform);
    }

    private void BuildHeader(Transform parent)
    {
        var titleObj = new GameObject("GameTitle");
        titleObj.transform.SetParent(parent, false);
        var tRect = titleObj.AddComponent<RectTransform>();
        tRect.anchorMin = new Vector2(0.5f, 1f);
        tRect.anchorMax = new Vector2(0.5f, 1f);
        tRect.pivot = new Vector2(0.5f, 1f);
        tRect.anchoredPosition = new Vector2(0, -45);
        tRect.sizeDelta = new Vector2(1200, 70);

        var title = titleObj.AddComponent<Text>();
        title.font = menuFont;
        title.fontSize = 48;
        title.text = "<b><color=#FFD700>OASIS</color>  <color=#FF4444>SURVIVAL</color></b>";
        title.alignment = TextAnchor.MiddleCenter;
    }

    private void BuildAuthAndProfilePanel(Transform parent)
    {
        m_AuthCard = new GameObject("Left_AuthAndProfileCard");
        m_AuthCard.transform.SetParent(parent, false);
        var rect = m_AuthCard.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(-460, 20);
        rect.sizeDelta = new Vector2(480, 560);

        var bg = m_AuthCard.AddComponent<Image>();
        bg.color = new Color(0.08f, 0.11f, 0.16f, 0.95f);

        var hObj = new GameObject("Header");
        hObj.transform.SetParent(m_AuthCard.transform, false);
        var hRect = hObj.AddComponent<RectTransform>();
        hRect.anchoredPosition = new Vector2(0, 240);
        hRect.sizeDelta = new Vector2(440, 45);
        var hText = hObj.AddComponent<Text>();
        hText.font = menuFont;
        hText.fontSize = 24;
        hText.text = "👤 <b>PLAYER ACCOUNT</b>";
        hText.color = new Color(1.0f, 0.85f, 0.35f, 1.0f);
        hText.alignment = TextAnchor.MiddleCenter;

        m_LoginFormObj = new GameObject("LoginForm");
        m_LoginFormObj.transform.SetParent(m_AuthCard.transform, false);
        var lfRect = m_LoginFormObj.AddComponent<RectTransform>();
        lfRect.anchorMin = Vector2.zero;
        lfRect.anchorMax = Vector2.one;
        lfRect.sizeDelta = Vector2.zero;

        m_UsernameInput = CreateInputField(m_LoginFormObj.transform, "UsernameInput", new Vector2(0, 130), "Enter Username...", false);
        m_PasswordInput = CreateInputField(m_LoginFormObj.transform, "PasswordInput", new Vector2(0, 55), "Enter Password...", true);

        var stObj = new GameObject("StatusText");
        stObj.transform.SetParent(m_LoginFormObj.transform, false);
        var stRect = stObj.AddComponent<RectTransform>();
        stRect.anchoredPosition = new Vector2(0, -10);
        stRect.sizeDelta = new Vector2(420, 35);
        m_AuthStatusText = stObj.AddComponent<Text>();
        m_AuthStatusText.font = menuFont;
        m_AuthStatusText.fontSize = 15;
        m_AuthStatusText.alignment = TextAnchor.MiddleCenter;
        m_AuthStatusText.text = "";

        CreateButton(m_LoginFormObj.transform, "LoginBtn", new Vector2(-110, -75), new Vector2(180, 46), "LOGIN", new Color(0.2f, 0.65f, 0.35f), OnLoginClick);
        CreateButton(m_LoginFormObj.transform, "RegisterBtn", new Vector2(110, -75), new Vector2(180, 46), "REGISTER", new Color(0.2f, 0.45f, 0.85f), OnRegisterClick);
        CreateButton(m_LoginFormObj.transform, "GuestBtn", new Vector2(0, -140), new Vector2(400, 44), "PLAY AS GUEST", new Color(0.3f, 0.3f, 0.36f), OnGuestClick);

        m_ProfileViewObj = new GameObject("ProfileView");
        m_ProfileViewObj.transform.SetParent(m_AuthCard.transform, false);
        var pvRect = m_ProfileViewObj.AddComponent<RectTransform>();
        pvRect.anchorMin = Vector2.zero;
        pvRect.anchorMax = Vector2.one;
        pvRect.sizeDelta = Vector2.zero;

        var pTitleObj = new GameObject("ProfileTitle");
        pTitleObj.transform.SetParent(m_ProfileViewObj.transform, false);
        var ptRect = pTitleObj.AddComponent<RectTransform>();
        ptRect.anchoredPosition = new Vector2(0, 110);
        ptRect.sizeDelta = new Vector2(420, 50);
        m_ProfileTitleText = pTitleObj.AddComponent<Text>();
        m_ProfileTitleText.font = menuFont;
        m_ProfileTitleText.fontSize = 26;
        m_ProfileTitleText.alignment = TextAnchor.MiddleCenter;
        m_ProfileTitleText.color = new Color(0.3f, 1.0f, 0.5f);

        var pStatsObj = new GameObject("ProfileStats");
        pStatsObj.transform.SetParent(m_ProfileViewObj.transform, false);
        var psRect = pStatsObj.AddComponent<RectTransform>();
        psRect.anchoredPosition = new Vector2(0, 0);
        psRect.sizeDelta = new Vector2(420, 140);
        m_ProfileStatsText = pStatsObj.AddComponent<Text>();
        m_ProfileStatsText.font = menuFont;
        m_ProfileStatsText.fontSize = 19;
        m_ProfileStatsText.alignment = TextAnchor.MiddleCenter;
        m_ProfileStatsText.color = Color.white;

        CreateButton(m_ProfileViewObj.transform, "LogoutBtn", new Vector2(0, -120), new Vector2(260, 46), "LOGOUT", new Color(0.7f, 0.25f, 0.25f), OnLogoutClick);

        m_ProfileViewObj.SetActive(false);
    }

    private void BuildLeaderboardPanel(Transform parent)
    {
        m_LeaderboardCard = new GameObject("Right_LeaderboardCard");
        m_LeaderboardCard.transform.SetParent(parent, false);
        var rect = m_LeaderboardCard.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(380, 20);
        rect.sizeDelta = new Vector2(800, 560);

        var bg = m_LeaderboardCard.AddComponent<Image>();
        bg.color = new Color(0.08f, 0.11f, 0.16f, 0.95f);

        var titleObj = new GameObject("Title");
        titleObj.transform.SetParent(m_LeaderboardCard.transform, false);
        var tRect = titleObj.AddComponent<RectTransform>();
        tRect.anchoredPosition = new Vector2(0, 240);
        tRect.sizeDelta = new Vector2(740, 45);
        var title = titleObj.AddComponent<Text>();
        title.font = menuFont;
        title.fontSize = 26;
        title.text = "👑 <b>GLOBAL TOP 10 HIGHSCORES</b>";
        title.color = new Color(1.0f, 0.88f, 0.35f, 1.0f);
        title.alignment = TextAnchor.MiddleCenter;

        var headerObj = new GameObject("TableHeader");
        headerObj.transform.SetParent(m_LeaderboardCard.transform, false);
        var hRect = headerObj.AddComponent<RectTransform>();
        hRect.anchoredPosition = new Vector2(0, 195);
        hRect.sizeDelta = new Vector2(740, 32);
        var hImg = headerObj.AddComponent<Image>();
        hImg.color = new Color(0.14f, 0.18f, 0.26f, 0.9f);

        CreateCellText(headerObj.transform, "RankH", new Vector2(10, 0), new Vector2(75, 32), "<b>RANK</b>", TextAnchor.MiddleLeft, new Color(0.7f, 0.85f, 1f));
        CreateCellText(headerObj.transform, "PlayerH", new Vector2(90, 0), new Vector2(220, 32), "<b>PLAYER</b>", TextAnchor.MiddleLeft, new Color(0.7f, 0.85f, 1f));
        CreateCellText(headerObj.transform, "ScoreH", new Vector2(320, 0), new Vector2(130, 32), "<b>HIGH SCORE</b>", TextAnchor.MiddleRight, new Color(0.7f, 0.85f, 1f));
        CreateCellText(headerObj.transform, "WaveH", new Vector2(470, 0), new Vector2(140, 32), "<b>MAX WAVE</b>", TextAnchor.MiddleCenter, new Color(0.7f, 0.85f, 1f));
        CreateCellText(headerObj.transform, "KillsH", new Vector2(620, 0), new Vector2(100, 32), "<b>KILLS</b>", TextAnchor.MiddleRight, new Color(0.7f, 0.85f, 1f));

        var containerObj = new GameObject("RowsContainer");
        containerObj.transform.SetParent(m_LeaderboardCard.transform, false);
        var ctRect = containerObj.AddComponent<RectTransform>();
        ctRect.anchoredPosition = new Vector2(0, 10);
        ctRect.sizeDelta = new Vector2(740, 320);
        m_LeaderboardRowsContainer = containerObj.transform;

        var statusObj = new GameObject("StatusText");
        statusObj.transform.SetParent(m_LeaderboardCard.transform, false);
        var stRect = statusObj.AddComponent<RectTransform>();
        stRect.anchoredPosition = new Vector2(0, -180);
        stRect.sizeDelta = new Vector2(600, 30);
        m_LeaderboardStatusText = statusObj.AddComponent<Text>();
        m_LeaderboardStatusText.font = menuFont;
        m_LeaderboardStatusText.fontSize = 14;
        m_LeaderboardStatusText.alignment = TextAnchor.MiddleCenter;
        m_LeaderboardStatusText.color = Color.white;
        m_LeaderboardStatusText.text = "";

        CreateButton(m_LeaderboardCard.transform, "RefreshBtn", new Vector2(0, -225), new Vector2(240, 42), "REFRESH LEADERBOARD", new Color(0.2f, 0.5f, 0.85f), RefreshLeaderboard);
    }

    private void BuildBottomActionButtons(Transform parent)
    {
        var botObj = new GameObject("BottomActions");
        botObj.transform.SetParent(parent, false);
        var bRect = botObj.AddComponent<RectTransform>();
        bRect.anchorMin = new Vector2(0.5f, 0f);
        bRect.anchorMax = new Vector2(0.5f, 0f);
        bRect.pivot = new Vector2(0.5f, 0f);
        bRect.anchoredPosition = new Vector2(0, 30);
        bRect.sizeDelta = new Vector2(1000, 80);

        CreateButton(botObj.transform, "PlayBtn", new Vector2(-180, 0), new Vector2(320, 60), "▶ START SURVIVAL MISSION", new Color(0.85f, 0.55f, 0.1f), OnPlayMissionClick);

        CreateButton(botObj.transform, "ControlsBtn", new Vector2(120, 0), new Vector2(200, 60), "⚙ CONTROLS", new Color(0.25f, 0.35f, 0.5f), OnControlsClick);

        CreateButton(botObj.transform, "QuitBtn", new Vector2(300, 0), new Vector2(120, 60), "✕ QUIT", new Color(0.55f, 0.2f, 0.2f), OnQuitClick);
    }

    private void BuildControlsModal(Transform parent)
    {
        m_ControlsModalObj = new GameObject("ControlsModal");
        m_ControlsModalObj.transform.SetParent(parent, false);
        var rect = m_ControlsModalObj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(680, 480);

        var bg = m_ControlsModalObj.AddComponent<Image>();
        bg.color = new Color(0.05f, 0.08f, 0.12f, 0.98f);

        var titleObj = new GameObject("Title");
        titleObj.transform.SetParent(m_ControlsModalObj.transform, false);
        var tRect = titleObj.AddComponent<RectTransform>();
        tRect.anchoredPosition = new Vector2(0, 190);
        tRect.sizeDelta = new Vector2(600, 40);
        var title = titleObj.AddComponent<Text>();
        title.font = menuFont;
        title.fontSize = 26;
        title.text = "<b>⚙ CONTROLS & HOW TO PLAY</b>";
        title.alignment = TextAnchor.MiddleCenter;
        title.color = new Color(1.0f, 0.88f, 0.35f, 1.0f);

        var textObj = new GameObject("Content");
        textObj.transform.SetParent(m_ControlsModalObj.transform, false);
        var cRect = textObj.AddComponent<RectTransform>();
        cRect.anchoredPosition = new Vector2(0, 10);
        cRect.sizeDelta = new Vector2(600, 290);
        var text = textObj.AddComponent<Text>();
        text.font = menuFont;
        text.fontSize = 18;
        text.alignment = TextAnchor.MiddleLeft;
        text.text = "<b>W / A / S / D</b>  — Move & Strafe\n" +
                    "<b>Left Shift</b>  — Tactical Sprint\n" +
                    "<b>Spacebar</b>    — Jump\n" +
                    "<b>Left Click</b>  — Fire M4A1 (Full-Auto)\n" +
                    "<b>Right Click</b> — Aim Down Sights (ADS Zoom)\n" +
                    "<b>R</b>           — Reload (With animated mag swap)\n" +
                    "<b>H or 4</b>      — Drink Stored Health Potion (+50 HP)\n" +
                    "<b>Tab / L</b>     — Toggle Live Leaderboard\n" +
                    "<b>Escape</b>      — Pause Menu / Return to Menu";
        text.color = new Color(0.9f, 0.95f, 1.0f);

        CreateButton(m_ControlsModalObj.transform, "CloseBtn", new Vector2(0, -185), new Vector2(160, 44), "CLOSE", new Color(0.6f, 0.2f, 0.2f), () => m_ControlsModalObj.SetActive(false));

        m_ControlsModalObj.SetActive(false);
    }

    private Text CreateCellText(Transform parent, string name, Vector2 pos, Vector2 size, string content, TextAnchor alignment, Color color)
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
        text.font = menuFont;
        text.fontSize = 16;
        text.text = content;
        text.alignment = alignment;
        text.color = color;
        return text;
    }

    private InputField CreateInputField(Transform parent, string name, Vector2 pos, string placeholder, bool isPassword)
    {
        var inputObj = new GameObject(name);
        inputObj.transform.SetParent(parent, false);
        var rect = inputObj.AddComponent<RectTransform>();
        rect.anchoredPosition = pos;
        rect.sizeDelta = new Vector2(400, 48);

        var bg = inputObj.AddComponent<Image>();
        bg.color = new Color(0.12f, 0.16f, 0.24f, 1f);

        var field = inputObj.AddComponent<InputField>();

        var textObj = new GameObject("Text");
        textObj.transform.SetParent(inputObj.transform, false);
        var tRect = textObj.AddComponent<RectTransform>();
        tRect.anchorMin = Vector2.zero;
        tRect.anchorMax = Vector2.one;
        tRect.sizeDelta = new Vector2(-20, -10);
        var text = textObj.AddComponent<Text>();
        text.font = menuFont;
        text.fontSize = 19;
        text.color = Color.white;
        text.alignment = TextAnchor.MiddleLeft;

        var phObj = new GameObject("Placeholder");
        phObj.transform.SetParent(inputObj.transform, false);
        var phRect = phObj.AddComponent<RectTransform>();
        phRect.anchorMin = Vector2.zero;
        phRect.anchorMax = Vector2.one;
        phRect.sizeDelta = new Vector2(-20, -10);
        var ph = phObj.AddComponent<Text>();
        ph.font = menuFont;
        ph.fontSize = 17;
        ph.text = placeholder;
        ph.color = new Color(0.6f, 0.65f, 0.75f, 0.55f);
        ph.fontStyle = FontStyle.Italic;
        ph.alignment = TextAnchor.MiddleLeft;

        field.textComponent = text;
        field.placeholder = ph;
        if (isPassword) field.contentType = InputField.ContentType.Password;

        return field;
    }

    private Button CreateButton(Transform parent, string name, Vector2 pos, Vector2 size, string label, Color color, UnityEngine.Events.UnityAction onClick)
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
        t.font = menuFont;
        t.fontSize = 17;
        t.text = $"<b>{label}</b>";
        t.alignment = TextAnchor.MiddleCenter;
        t.color = Color.white;

        return btn;
    }

    private void OnLoginClick()
    {
        string user = m_UsernameInput.text.Trim();
        string pass = m_PasswordInput.text;

        if (string.IsNullOrEmpty(user) || string.IsNullOrEmpty(pass))
        {
            SetAuthStatus("Please enter username and password.", Color.yellow);
            return;
        }

        SetAuthStatus("Authenticating...", Color.cyan);
        StartCoroutine(NetworkManager.Instance.LoginRoutine(user, pass, (success, username, id, bestScore, wave) =>
        {
            if (success)
            {
                AuthManager.Instance.SetLoggedInUser(id, username, bestScore, wave);
                SetAuthStatus($"Logged in as {username}!", Color.green);
                UpdateProfileUI();
                RefreshLeaderboard();
            }
            else
            {
                SetAuthStatus("Invalid username or password.", Color.red);
            }
        }));
    }

    private void OnRegisterClick()
    {
        string user = m_UsernameInput.text.Trim();
        string pass = m_PasswordInput.text;

        if (string.IsNullOrEmpty(user) || string.IsNullOrEmpty(pass))
        {
            SetAuthStatus("Please enter username and password.", Color.yellow);
            return;
        }

        SetAuthStatus("Creating profile...", Color.cyan);
        StartCoroutine(NetworkManager.Instance.RegisterRoutine(user, pass, (success, username, id) =>
        {
            if (success)
            {
                AuthManager.Instance.SetLoggedInUser(id, username);
                SetAuthStatus($"Account created! Logged in as {username}.", Color.green);
                UpdateProfileUI();
                RefreshLeaderboard();
            }
            else
            {
                SetAuthStatus("Registration failed. Username may be taken.", Color.red);
            }
        }));
    }

    private void OnGuestClick()
    {
        AuthManager.Instance.Logout();
        SetAuthStatus("Playing as Guest.", Color.gray);
        UpdateProfileUI();
    }

    private void OnLogoutClick()
    {
        AuthManager.Instance.Logout();
        UpdateProfileUI();
    }

    private void OnPlayMissionClick()
    {
        HideMenu();
    }

    private void OnControlsClick()
    {
        if (m_ControlsModalObj != null)
        {
            m_ControlsModalObj.SetActive(true);
        }
    }

    private void OnQuitClick()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void SetAuthStatus(string msg, Color col)
    {
        if (m_AuthStatusText != null)
        {
            m_AuthStatusText.text = msg;
            m_AuthStatusText.color = col;
        }
    }

    private void UpdateProfileUI()
    {
        bool loggedIn = (AuthManager.Instance != null && AuthManager.Instance.isLoggedIn);
        if (m_LoginFormObj != null) m_LoginFormObj.SetActive(!loggedIn);
        if (m_ProfileViewObj != null) m_ProfileViewObj.SetActive(loggedIn);

        if (loggedIn && m_ProfileTitleText != null && m_ProfileStatsText != null)
        {
            var auth = AuthManager.Instance;
            m_ProfileTitleText.text = $"👑 <b>{auth.username}</b>";
            m_ProfileStatsText.text = $"Personal Best: <color=#FFDD44>{auth.bestScore:N0}</color> Pts\n" +
                                      $"Highest Wave: <color=#88FFAA>Wave {auth.maxWave}</color>";
        }
    }

    public void RefreshLeaderboard()
    {
        if (m_LeaderboardRowsContainer != null)
        {
            for (int i = m_LeaderboardRowsContainer.childCount - 1; i >= 0; i--)
            {
                Destroy(m_LeaderboardRowsContainer.GetChild(i).gameObject);
            }
        }

        if (NetworkManager.Instance == null) return;

        StartCoroutine(NetworkManager.Instance.FetchLeaderboardRoutine((success, items) =>
        {
            if (success && items != null && items.Count > 0)
            {
                PopulateLeaderboardRows(items);
            }
            else
            {
                ShowEmptyLeaderboardMessage();
            }
        }));
    }

    private void ShowEmptyLeaderboardMessage()
    {
        if (m_LeaderboardRowsContainer == null) return;

        var emptyObj = new GameObject("EmptyMessage");
        emptyObj.transform.SetParent(m_LeaderboardRowsContainer, false);
        var rect = emptyObj.AddComponent<RectTransform>();
        rect.anchoredPosition = new Vector2(0, 40);
        rect.sizeDelta = new Vector2(700, 60);

        var t = emptyObj.AddComponent<Text>();
        t.font = menuFont;
        t.fontSize = 17;
        t.alignment = TextAnchor.MiddleCenter;
        t.text = "<i>No match records yet.\nStart a survival mission to set the first High Score!</i>";
        t.color = new Color(0.7f, 0.75f, 0.85f, 0.85f);
    }

    private void PopulateLeaderboardRows(List<LeaderboardItem> items)
    {
        int count = Mathf.Min(items.Count, 10);
        float rowHeight = 32f;
        float startY = 145f;

        for (int i = 0; i < count; i++)
        {
            var item = items[i];
            var rowObj = new GameObject($"Row_{i}");
            rowObj.transform.SetParent(m_LeaderboardRowsContainer, false);
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

            CreateCellText(rowObj.transform, "Rank", new Vector2(10, 0), new Vector2(75, 30), rankIcon, TextAnchor.MiddleLeft, textColor);
            CreateCellText(rowObj.transform, "Player", new Vector2(90, 0), new Vector2(220, 30), $"<b>{item.username}</b>", TextAnchor.MiddleLeft, textColor);
            CreateCellText(rowObj.transform, "Score", new Vector2(320, 0), new Vector2(130, 30), $"{item.bestScore:N0}", TextAnchor.MiddleRight, textColor);
            CreateCellText(rowObj.transform, "Wave", new Vector2(470, 0), new Vector2(140, 30), waveTag, TextAnchor.MiddleCenter, textColor);
            CreateCellText(rowObj.transform, "Kills", new Vector2(620, 0), new Vector2(100, 30), $"{item.lifetimeKills:N0}", TextAnchor.MiddleRight, textColor);
        }
    }
}

