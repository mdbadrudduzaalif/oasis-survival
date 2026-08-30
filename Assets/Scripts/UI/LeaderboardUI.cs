using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class LeaderboardUI : MonoBehaviour
{
    public static LeaderboardUI Instance { get; private set; }

    [Header("Styling")]
    public Font uiFont;

    private GameObject m_LeaderboardModalObj;
    private Transform m_RowsContainer;
    private Text m_StatusText;
    private Button m_RefreshBtn;
    private Button m_CloseBtn;

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
        BuildLeaderboardModal();
        m_LeaderboardModalObj.SetActive(false);
    }

    public void ShowLeaderboard()
    {
        if (m_LeaderboardModalObj != null)
        {
            m_LeaderboardModalObj.SetActive(true);
            var player = FindAnyObjectByType<PlayerController>();
            if (player != null) player.UnlockCursor();
            RefreshData();
        }
    }

    public void HideLeaderboard()
    {
        if (m_LeaderboardModalObj != null)
        {
            m_LeaderboardModalObj.SetActive(false);
            var player = FindAnyObjectByType<PlayerController>();
            if (player != null && !player.isDead) player.LockCursor();
        }
    }

    public void ToggleLeaderboard()
    {
        if (m_LeaderboardModalObj != null)
        {
            if (m_LeaderboardModalObj.activeSelf) HideLeaderboard();
            else ShowLeaderboard();
        }
    }

    private void BuildLeaderboardModal()
    {
        m_LeaderboardModalObj = new GameObject("Leaderboard_Modal_Root");
        m_LeaderboardModalObj.transform.SetParent(transform, false);

        var canvas = m_LeaderboardModalObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 450;

        var scaler = m_LeaderboardModalObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        m_LeaderboardModalObj.AddComponent<GraphicRaycaster>();

        var backdrop = new GameObject("Backdrop");
        backdrop.transform.SetParent(m_LeaderboardModalObj.transform, false);
        var bRect = backdrop.AddComponent<RectTransform>();
        bRect.anchorMin = Vector2.zero;
        bRect.anchorMax = Vector2.one;
        bRect.sizeDelta = Vector2.zero;
        var bImg = backdrop.AddComponent<Image>();
        bImg.color = new Color(0.04f, 0.05f, 0.08f, 0.85f);

        var card = new GameObject("LeaderboardCard");
        card.transform.SetParent(backdrop.transform, false);
        var cRect = card.AddComponent<RectTransform>();
        cRect.anchorMin = new Vector2(0.5f, 0.5f);
        cRect.anchorMax = new Vector2(0.5f, 0.5f);
        cRect.anchoredPosition = Vector2.zero;
        cRect.sizeDelta = new Vector2(880, 620);
        var cImg = card.AddComponent<Image>();
        cImg.color = new Color(0.08f, 0.11f, 0.16f, 0.95f);

        var titleObj = new GameObject("Title");
        titleObj.transform.SetParent(card.transform, false);
        var tRect = titleObj.AddComponent<RectTransform>();
        tRect.anchoredPosition = new Vector2(0, 265);
        tRect.sizeDelta = new Vector2(800, 50);
        var title = titleObj.AddComponent<Text>();
        title.font = uiFont;
        title.fontSize = 30;
        title.text = "👑 <b>GLOBAL TOP 10 HIGHSCORES</b>";
        title.alignment = TextAnchor.MiddleCenter;
        title.color = new Color(1.0f, 0.88f, 0.35f, 1.0f);

        var headerObj = new GameObject("TableHeader");
        headerObj.transform.SetParent(card.transform, false);
        var hRect = headerObj.AddComponent<RectTransform>();
        hRect.anchoredPosition = new Vector2(0, 215);
        hRect.sizeDelta = new Vector2(800, 36);
        var hImg = headerObj.AddComponent<Image>();
        hImg.color = new Color(0.14f, 0.18f, 0.26f, 0.9f);

        CreateCellText(headerObj.transform, "RankH", new Vector2(15, 0), new Vector2(80, 36), "<b>RANK</b>", TextAnchor.MiddleLeft, new Color(0.7f, 0.85f, 1f));
        CreateCellText(headerObj.transform, "PlayerH", new Vector2(100, 0), new Vector2(240, 36), "<b>PLAYER</b>", TextAnchor.MiddleLeft, new Color(0.7f, 0.85f, 1f));
        CreateCellText(headerObj.transform, "ScoreH", new Vector2(350, 0), new Vector2(140, 36), "<b>HIGH SCORE</b>", TextAnchor.MiddleRight, new Color(0.7f, 0.85f, 1f));
        CreateCellText(headerObj.transform, "WaveH", new Vector2(510, 0), new Vector2(150, 36), "<b>MAX WAVE</b>", TextAnchor.MiddleCenter, new Color(0.7f, 0.85f, 1f));
        CreateCellText(headerObj.transform, "KillsH", new Vector2(680, 0), new Vector2(105, 36), "<b>KILLS</b>", TextAnchor.MiddleRight, new Color(0.7f, 0.85f, 1f));

        var containerObj = new GameObject("RowsContainer");
        containerObj.transform.SetParent(card.transform, false);
        var ctRect = containerObj.AddComponent<RectTransform>();
        ctRect.anchoredPosition = new Vector2(0, 10);
        ctRect.sizeDelta = new Vector2(800, 350);
        m_RowsContainer = containerObj.transform;

        m_RefreshBtn = CreateButton(card.transform, "RefreshBtn", new Vector2(-120, -260), new Vector2(180, 44), "REFRESH", new Color(0.2f, 0.5f, 0.85f), RefreshData);
        m_CloseBtn = CreateButton(card.transform, "CloseBtn", new Vector2(120, -260), new Vector2(180, 44), "CLOSE", new Color(0.6f, 0.2f, 0.2f), HideLeaderboard);
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
        text.font = uiFont;
        text.fontSize = 17;
        text.text = content;
        text.alignment = alignment;
        text.color = color;
        return text;
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
        t.fontSize = 17;
        t.text = $"<b>{label}</b>";
        t.alignment = TextAnchor.MiddleCenter;
        t.color = Color.white;

        return btn;
    }

    public void RefreshData()
    {
        if (m_RowsContainer != null)
        {
            for (int i = m_RowsContainer.childCount - 1; i >= 0; i--)
            {
                Destroy(m_RowsContainer.GetChild(i).gameObject);
            }
        }

        if (NetworkManager.Instance == null) return;

        StartCoroutine(NetworkManager.Instance.FetchLeaderboardRoutine((success, items) =>
        {
            if (success && items != null && items.Count > 0)
            {
                PopulateRows(items);
            }
            else
            {
                ShowEmptyMessage();
            }
        }));
    }

    private void ShowEmptyMessage()
    {
        if (m_RowsContainer == null) return;

        var emptyObj = new GameObject("EmptyMessage");
        emptyObj.transform.SetParent(m_RowsContainer, false);
        var rect = emptyObj.AddComponent<RectTransform>();
        rect.anchoredPosition = new Vector2(0, 40);
        rect.sizeDelta = new Vector2(750, 60);

        var t = emptyObj.AddComponent<Text>();
        t.font = uiFont;
        t.fontSize = 18;
        t.alignment = TextAnchor.MiddleCenter;
        t.text = "<i>No match records yet. Be the first to survive!</i>";
        t.color = new Color(0.7f, 0.75f, 0.85f, 0.85f);
    }

    private void PopulateRows(List<LeaderboardItem> items)
    {
        int count = Mathf.Min(items.Count, 10);
        float rowHeight = 34f;
        float startY = 160f;

        for (int i = 0; i < count; i++)
        {
            var item = items[i];
            var rowObj = new GameObject($"Row_{i}");
            rowObj.transform.SetParent(m_RowsContainer, false);
            var rect = rowObj.AddComponent<RectTransform>();
            rect.anchoredPosition = new Vector2(0, startY - (i * rowHeight));
            rect.sizeDelta = new Vector2(800, rowHeight - 3f);

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

            CreateCellText(rowObj.transform, "Rank", new Vector2(15, 0), new Vector2(80, 32), rankIcon, TextAnchor.MiddleLeft, textColor);
            CreateCellText(rowObj.transform, "Player", new Vector2(100, 0), new Vector2(240, 32), $"<b>{item.username}</b>", TextAnchor.MiddleLeft, textColor);
            CreateCellText(rowObj.transform, "Score", new Vector2(350, 0), new Vector2(140, 32), $"{item.bestScore:N0}", TextAnchor.MiddleRight, textColor);
            CreateCellText(rowObj.transform, "Wave", new Vector2(510, 0), new Vector2(150, 32), waveTag, TextAnchor.MiddleCenter, textColor);
            CreateCellText(rowObj.transform, "Kills", new Vector2(680, 0), new Vector2(105, 32), $"{item.lifetimeKills:N0}", TextAnchor.MiddleRight, textColor);
        }
    }
}

