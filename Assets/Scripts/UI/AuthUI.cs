using UnityEngine;
using UnityEngine.UI;

public class AuthUI : MonoBehaviour
{
    public static AuthUI Instance { get; private set; }

    [Header("Styling")]
    public Font uiFont;

    private GameObject m_AuthModalObj;
    private InputField m_UsernameInput;
    private InputField m_PasswordInput;
    private Text m_StatusText;
    private Button m_LoginBtn;
    private Button m_RegisterBtn;
    private Button m_GuestBtn;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        if (uiFont == null)
        {
            uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
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

        BuildAuthModal();
        if (m_AuthModalObj != null) m_AuthModalObj.SetActive(false);
    }

    public void ShowModal()
    {
        if (m_AuthModalObj != null)
        {
            m_AuthModalObj.SetActive(true);
            var player = FindAnyObjectByType<PlayerController>();
            if (player != null) player.UnlockCursor();
        }
    }

    public void HideModal()
    {
        if (m_AuthModalObj != null)
        {
            m_AuthModalObj.SetActive(false);
            var player = FindAnyObjectByType<PlayerController>();
            if (player != null && !player.isDead) player.LockCursor();
        }
    }

    private void BuildAuthModal()
    {
        m_AuthModalObj = new GameObject("Auth_Modal_Root");
        m_AuthModalObj.transform.SetParent(transform, false);

        var canvas = m_AuthModalObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 500;

        var scaler = m_AuthModalObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        m_AuthModalObj.AddComponent<GraphicRaycaster>();

        var backdrop = new GameObject("Backdrop");
        backdrop.transform.SetParent(m_AuthModalObj.transform, false);
        var bRect = backdrop.AddComponent<RectTransform>();
        bRect.anchorMin = Vector2.zero;
        bRect.anchorMax = Vector2.one;
        bRect.sizeDelta = Vector2.zero;
        var bImg = backdrop.AddComponent<Image>();
        bImg.color = new Color(0.04f, 0.05f, 0.08f, 0.88f);

        var card = new GameObject("DialogCard");
        card.transform.SetParent(backdrop.transform, false);
        var cRect = card.AddComponent<RectTransform>();
        cRect.anchorMin = new Vector2(0.5f, 0.5f);
        cRect.anchorMax = new Vector2(0.5f, 0.5f);
        cRect.anchoredPosition = Vector2.zero;
        cRect.sizeDelta = new Vector2(580, 480);
        var cImg = card.AddComponent<Image>();
        cImg.color = new Color(0.08f, 0.11f, 0.16f, 0.95f);

        var titleObj = new GameObject("Title");
        titleObj.transform.SetParent(card.transform, false);
        var tRect = titleObj.AddComponent<RectTransform>();
        tRect.anchoredPosition = new Vector2(0, 190);
        tRect.sizeDelta = new Vector2(540, 50);
        var title = titleObj.AddComponent<Text>();
        title.font = uiFont;
        title.fontSize = 28;
        title.text = "<b>OASIS SURVIVAL — PLAYER LOGIN</b>";
        title.alignment = TextAnchor.MiddleCenter;
        title.color = new Color(1.0f, 0.85f, 0.25f, 1.0f);

        var subObj = new GameObject("Subtitle");
        subObj.transform.SetParent(card.transform, false);
        var sRect = subObj.AddComponent<RectTransform>();
        sRect.anchoredPosition = new Vector2(0, 150);
        sRect.sizeDelta = new Vector2(540, 30);
        var sub = subObj.AddComponent<Text>();
        sub.font = uiFont;
        sub.fontSize = 16;
        sub.text = "<color=#AAAAAA>Connected to Microsoft SQL Server Database</color>";
        sub.alignment = TextAnchor.MiddleCenter;

        m_UsernameInput = CreateInputField(card.transform, "UsernameInput", new Vector2(0, 75), "Enter Username...", false);

        m_PasswordInput = CreateInputField(card.transform, "PasswordInput", new Vector2(0, 0), "Enter Password...", true);

        var statusObj = new GameObject("StatusText");
        statusObj.transform.SetParent(card.transform, false);
        var stRect = statusObj.AddComponent<RectTransform>();
        stRect.anchoredPosition = new Vector2(0, -60);
        stRect.sizeDelta = new Vector2(500, 35);
        m_StatusText = statusObj.AddComponent<Text>();
        m_StatusText.font = uiFont;
        m_StatusText.fontSize = 16;
        m_StatusText.alignment = TextAnchor.MiddleCenter;
        m_StatusText.text = "";

        m_LoginBtn = CreateButton(card.transform, "LoginBtn", new Vector2(-150, -135), new Vector2(140, 48), "LOGIN", new Color(0.2f, 0.65f, 0.35f), OnLoginClick);
        m_RegisterBtn = CreateButton(card.transform, "RegisterBtn", new Vector2(0, -135), new Vector2(140, 48), "REGISTER", new Color(0.2f, 0.45f, 0.85f), OnRegisterClick);
        m_GuestBtn = CreateButton(card.transform, "GuestBtn", new Vector2(150, -135), new Vector2(140, 48), "GUEST", new Color(0.35f, 0.35f, 0.40f), OnGuestClick);

        var netNote = new GameObject("NetNote");
        netNote.transform.SetParent(card.transform, false);
        var nRect = netNote.AddComponent<RectTransform>();
        nRect.anchoredPosition = new Vector2(0, -205);
        nRect.sizeDelta = new Vector2(500, 30);
        var nText = netNote.AddComponent<Text>();
        nText.font = uiFont;
        nText.fontSize = 14;
        nText.alignment = TextAnchor.MiddleCenter;
        nText.text = "<color=#88FFAA>● 3-Tier Web API Online</color>  |  <color=#AAAAAA>Port 5000</color>";
    }

    private InputField CreateInputField(Transform parent, string name, Vector2 pos, string placeholder, bool isPassword)
    {
        var inputObj = new GameObject(name);
        inputObj.transform.SetParent(parent, false);
        var rect = inputObj.AddComponent<RectTransform>();
        rect.anchoredPosition = pos;
        rect.sizeDelta = new Vector2(440, 50);

        var bg = inputObj.AddComponent<Image>();
        bg.color = new Color(0.12f, 0.15f, 0.22f, 1f);

        var field = inputObj.AddComponent<InputField>();

        var textObj = new GameObject("Text");
        textObj.transform.SetParent(inputObj.transform, false);
        var tRect = textObj.AddComponent<RectTransform>();
        tRect.anchorMin = Vector2.zero;
        tRect.anchorMax = Vector2.one;
        tRect.sizeDelta = new Vector2(-20, -10);
        var text = textObj.AddComponent<Text>();
        text.font = uiFont;
        text.fontSize = 20;
        text.color = Color.white;
        text.alignment = TextAnchor.MiddleLeft;

        var phObj = new GameObject("Placeholder");
        phObj.transform.SetParent(inputObj.transform, false);
        var phRect = phObj.AddComponent<RectTransform>();
        phRect.anchorMin = Vector2.zero;
        phRect.anchorMax = Vector2.one;
        phRect.sizeDelta = new Vector2(-20, -10);
        var ph = phObj.AddComponent<Text>();
        ph.font = uiFont;
        ph.fontSize = 18;
        ph.text = placeholder;
        ph.color = new Color(0.6f, 0.6f, 0.7f, 0.6f);
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
        t.font = uiFont;
        t.fontSize = 18;
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
            SetStatus("Please enter username and password.", Color.yellow);
            return;
        }

        SetStatus("Authenticating with SQL Server...", Color.cyan);
        StartCoroutine(NetworkManager.Instance.LoginRoutine(user, pass, (success, username, id, bestScore, wave) =>
        {
            if (success)
            {
                AuthManager.Instance.SetLoggedInUser(id, username, bestScore, wave);
                SetStatus($"Welcome back, {username}!", Color.green);
                Invoke(nameof(HideModal), 0.8f);
            }
            else
            {
                SetStatus("Invalid username or password.", Color.red);
            }
        }));
    }

    private void OnRegisterClick()
    {
        string user = m_UsernameInput.text.Trim();
        string pass = m_PasswordInput.text;

        if (string.IsNullOrEmpty(user) || string.IsNullOrEmpty(pass))
        {
            SetStatus("Please enter username and password.", Color.yellow);
            return;
        }

        SetStatus("Creating account in SQL Server...", Color.cyan);
        StartCoroutine(NetworkManager.Instance.RegisterRoutine(user, pass, (success, username, id) =>
        {
            if (success)
            {
                AuthManager.Instance.SetLoggedInUser(id, username);
                SetStatus($"Account created! Logged in as {username}.", Color.green);
                Invoke(nameof(HideModal), 0.8f);
            }
            else
            {
                SetStatus("Registration failed. Username may be taken.", Color.red);
            }
        }));
    }

    private void OnGuestClick()
    {
        AuthManager.Instance.Logout();
        SetStatus("Playing as Guest.", Color.gray);
        Invoke(nameof(HideModal), 0.4f);
    }

    private void SetStatus(string msg, Color col)
    {
        if (m_StatusText != null)
        {
            m_StatusText.text = msg;
            m_StatusText.color = col;
        }
    }
}

