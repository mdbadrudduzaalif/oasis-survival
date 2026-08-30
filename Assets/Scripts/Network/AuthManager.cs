using UnityEngine;

public class AuthManager : MonoBehaviour
{
    public static AuthManager Instance { get; private set; }

    [Header("Current Player Session")]
    public int playerId = 1;
    public string username = "Guest_Player";
    public bool isLoggedIn = false;
    public int bestScore = 0;
    public int maxWave = 1;

    private const string SAVED_USER_KEY = "OASIS_SAVED_USERNAME";
    private const string SAVED_PLAYER_ID_KEY = "OASIS_SAVED_PLAYER_ID";

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        LoadSavedSession();
    }

    public void SetLoggedInUser(int id, string user, int high = 0, int wave = 1)
    {
        playerId = id;
        username = user;
        isLoggedIn = true;
        bestScore = high;
        maxWave = wave;

        PlayerPrefs.SetInt(SAVED_PLAYER_ID_KEY, playerId);
        PlayerPrefs.SetString(SAVED_USER_KEY, username);
        PlayerPrefs.Save();
    }

    public void Logout()
    {
        playerId = -1;
        username = "Guest_Player";
        isLoggedIn = false;
        bestScore = 0;
        maxWave = 1;

        PlayerPrefs.DeleteKey(SAVED_PLAYER_ID_KEY);
        PlayerPrefs.DeleteKey(SAVED_USER_KEY);
        PlayerPrefs.Save();
    }

    private void LoadSavedSession()
    {
        if (PlayerPrefs.HasKey(SAVED_PLAYER_ID_KEY) && PlayerPrefs.HasKey(SAVED_USER_KEY))
        {
            playerId = PlayerPrefs.GetInt(SAVED_PLAYER_ID_KEY);
            username = PlayerPrefs.GetString(SAVED_USER_KEY);
            isLoggedIn = true;
        }
    }
}

