using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
#endif

public class PauseMenu : MonoBehaviour
{
    public static PauseMenu Instance { get; private set; }

    [Header("UI References")]
    [Tooltip("The main pause menu panel")]
    public GameObject pauseMenuPanel;

    [Tooltip("Optional Options/Settings panel")]
    public GameObject optionsPanel;

    [Header("Audio Settings")]
    public Slider volumeSlider;
    public TextMeshProUGUI volumePercentText;

    [Header("State")]
    public bool isPaused = false;
    private PlayerController m_PlayerController;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Time.timeScale = 1.0f;
        isPaused = false;

        EnsureEventSystem();
        FindPlayerReferences();
        AutoFindPanelsAndBindButtons();

        if (pauseMenuPanel != null)
        {
            pauseMenuPanel.SetActive(false);
        }
        if (optionsPanel != null)
        {
            optionsPanel.SetActive(false);
        }

        if (volumeSlider != null)
        {
            volumeSlider.value = AudioListener.volume;
            volumeSlider.onValueChanged.AddListener(OnVolumeSliderChanged);
            UpdateVolumeText(volumeSlider.value);
        }
    }

    private void EnsureEventSystem()
    {
        if (EventSystem.current == null)
        {
            var eventObj = new GameObject("EventSystem");
            eventObj.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
            eventObj.AddComponent<InputSystemUIInputModule>();
#else
            eventObj.AddComponent<StandaloneInputModule>();
#endif
        }
    }

    private void FindPlayerReferences()
    {
        var player = GameObject.FindWithTag("Player") ?? GameObject.Find("PlayerCapsule");
        if (player != null)
        {
            m_PlayerController = player.GetComponent<PlayerController>();
        }
    }

    private void AutoFindPanelsAndBindButtons()
    {

        if (pauseMenuPanel == null)
        {
            var p = transform.Find("PausePanel") ?? transform.Find("PauseMenu") ?? transform.Find("Pause Menu") ?? transform.Find("Panel");
            if (p != null) pauseMenuPanel = p.gameObject;
        }

        var canvas = GetComponentInParent<Canvas>() ?? GetComponentInChildren<Canvas>(true);
        if (canvas != null)
        {
            canvas.sortingOrder = 700;
            if (canvas.GetComponent<GraphicRaycaster>() == null)
            {
                canvas.gameObject.AddComponent<GraphicRaycaster>();
            }
        }

        if (pauseMenuPanel != null)
        {
            var buttons = pauseMenuPanel.GetComponentsInChildren<Button>(true);
            foreach (var btn in buttons)
            {
                string btnName = btn.gameObject.name.ToLower();
                var btnText = btn.GetComponentInChildren<Text>();
                var btnTMP = btn.GetComponentInChildren<TextMeshProUGUI>();
                string label = (btnText != null ? btnText.text : (btnTMP != null ? btnTMP.text : "")).ToLower();

                btn.onClick.RemoveAllListeners();

                if (btnName.Contains("resume") || label.Contains("resume"))
                {
                    btn.onClick.AddListener(ResumeGame);
                }
                else if (btnName.Contains("restart") || label.Contains("restart"))
                {
                    btn.onClick.AddListener(RestartLevel);
                }
                else if (btnName.Contains("audio") || btnName.Contains("option") || btnName.Contains("setting") || label.Contains("audio") || label.Contains("setting") || label.Contains("option"))
                {
                    btn.onClick.AddListener(OpenOptions);
                }
                else if (btnName.Contains("menu") || label.Contains("menu"))
                {
                    btn.onClick.AddListener(OpenMainMenu);
                }
                else if (btnName.Contains("quit") || btnName.Contains("exit") || label.Contains("quit") || label.Contains("exit") || label.Contains("desktop"))
                {
                    btn.onClick.AddListener(QuitGame);
                }
            }
        }

        if (optionsPanel != null)
        {
            var optButtons = optionsPanel.GetComponentsInChildren<Button>(true);
            foreach (var btn in optButtons)
            {
                string name = btn.gameObject.name.ToLower();
                var t = btn.GetComponentInChildren<Text>();
                var tmp = btn.GetComponentInChildren<TextMeshProUGUI>();
                string label = (t != null ? t.text : (tmp != null ? tmp.text : "")).ToLower();

                if (name.Contains("back") || name.Contains("close") || label.Contains("back") || label.Contains("close"))
                {
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(CloseOptions);
                }
            }
        }
    }

    private void Update()
    {
        bool escapePressed = false;

#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && (Keyboard.current.escapeKey.wasPressedThisFrame || Keyboard.current.pKey.wasPressedThisFrame))
        {
            escapePressed = true;
        }
#else
        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P))
        {
            escapePressed = true;
        }
#endif

        if (escapePressed)
        {

            if (MainMenu.Instance != null && MainMenu.Instance.IsMenuOpen)
            {
                return;
            }

            if (optionsPanel != null && optionsPanel.activeSelf)
            {
                CloseOptions();
            }
            else if (isPaused)
            {
                ResumeGame();
            }
            else
            {
                PauseGame();
            }
        }

        if (isPaused)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    public void PauseGame()
    {
        isPaused = true;
        Time.timeScale = 0f;

        if (pauseMenuPanel != null)
        {
            pauseMenuPanel.SetActive(true);
        }

        if (m_PlayerController != null)
        {
            m_PlayerController.UnlockCursor();
            m_PlayerController.enabled = false;
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void ResumeGame()
    {
        isPaused = false;
        Time.timeScale = 1f;

        if (pauseMenuPanel != null)
        {
            pauseMenuPanel.SetActive(false);
        }
        if (optionsPanel != null)
        {
            optionsPanel.SetActive(false);
        }

        if (m_PlayerController != null)
        {
            m_PlayerController.enabled = true;
            m_PlayerController.LockCursor();
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    public void RestartLevel()
    {
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void OpenOptions()
    {
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
        if (optionsPanel != null) optionsPanel.SetActive(true);
    }

    public void OpenMainMenu()
    {
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
        if (optionsPanel != null) optionsPanel.SetActive(false);
        isPaused = false;
        if (MainMenu.Instance != null)
        {
            MainMenu.Instance.ShowMenu();
        }
    }

    public void CloseOptions()
    {
        if (optionsPanel != null) optionsPanel.SetActive(false);
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(true);
    }

    public void OnVolumeSliderChanged(float val)
    {
        AudioListener.volume = val;
        UpdateVolumeText(val);
    }

    private void UpdateVolumeText(float val)
    {
        if (volumePercentText != null)
        {
            volumePercentText.text = Mathf.RoundToInt(val * 100f) + "%";
        }
    }

    public void QuitGame()
    {
        Time.timeScale = 1f;
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}

