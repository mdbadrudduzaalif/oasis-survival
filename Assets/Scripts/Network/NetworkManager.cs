using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System;

public class NetworkManager : MonoBehaviour
{
    public static NetworkManager Instance { get; private set; }

    [Header("Backend Server Configuration")]
    public string apiBaseUrl = "http://localhost:5000";

    [Header("Network Status")]
    public bool isServerConnected = false;

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

        if (PlayerPrefs.HasKey("OASIS_SERVER_URL"))
        {
            apiBaseUrl = PlayerPrefs.GetString("OASIS_SERVER_URL");
        }

        StartCoroutine(CheckServerHealthRoutine());
    }

    public void SetServerUrl(string newUrl)
    {
        apiBaseUrl = newUrl.TrimEnd('/');
        PlayerPrefs.SetString("OASIS_SERVER_URL", apiBaseUrl);
        PlayerPrefs.Save();
        StartCoroutine(CheckServerHealthRoutine());
    }

    public IEnumerator CheckServerHealthRoutine(Action<bool> onComplete = null)
    {
        using var req = UnityWebRequest.Get($"{apiBaseUrl}/api/health");
        req.timeout = 4;
        yield return req.SendWebRequest();

        isServerConnected = (req.result == UnityWebRequest.Result.Success);
        onComplete?.Invoke(isServerConnected);
    }

    public IEnumerator RegisterRoutine(string username, string password, Action<bool, string, int> callback)
    {
        var payload = JsonUtility.ToJson(new AuthPayload { username = username, password = password });
        using var req = new UnityWebRequest($"{apiBaseUrl}/api/auth/register", "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(payload);
        req.uploadHandler = new UploadHandlerRaw(bodyRaw);
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");
        req.timeout = 6;

        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success)
        {
            var res = JsonUtility.FromJson<AuthResponse>(req.downloadHandler.text);
            callback?.Invoke(true, res.username, res.playerId);
        }
        else
        {
            callback?.Invoke(false, req.downloadHandler.text, -1);
        }
    }

    public IEnumerator LoginRoutine(string username, string password, Action<bool, string, int, int, int> callback)
    {
        var payload = JsonUtility.ToJson(new AuthPayload { username = username, password = password });
        using var req = new UnityWebRequest($"{apiBaseUrl}/api/auth/login", "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(payload);
        req.uploadHandler = new UploadHandlerRaw(bodyRaw);
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");
        req.timeout = 6;

        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success)
        {
            var res = JsonUtility.FromJson<LoginResponse>(req.downloadHandler.text);
            callback?.Invoke(true, res.username, res.playerId, res.bestScore, res.maxWave);
        }
        else
        {
            callback?.Invoke(false, req.error, -1, 0, 1);
        }
    }

    public IEnumerator SubmitMatchResultRoutine(int playerId, int score, int highestWave, int kills, int headshots, int durationSec, bool isVictory, Action<bool, bool, int> callback = null)
    {
        var payload = JsonUtility.ToJson(new MatchResultPayload
        {
            playerId = playerId,
            score = score,
            highestWave = highestWave,
            totalKills = kills,
            headshots = headshots,
            durationSeconds = durationSec,
            isVictory = isVictory
        });

        using var req = new UnityWebRequest($"{apiBaseUrl}/api/game/match-result", "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(payload);
        req.uploadHandler = new UploadHandlerRaw(bodyRaw);
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");
        req.timeout = 6;

        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success)
        {
            var res = JsonUtility.FromJson<MatchResultResponse>(req.downloadHandler.text);
            callback?.Invoke(true, res.isNewHighScore, res.highestScore);
        }
        else
        {
            callback?.Invoke(false, false, score);
        }
    }

    public IEnumerator FetchLeaderboardRoutine(Action<bool, List<LeaderboardItem>> callback)
    {
        using var req = UnityWebRequest.Get($"{apiBaseUrl}/api/game/leaderboard");
        req.timeout = 6;
        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success)
        {
            string json = "{\"items\":" + req.downloadHandler.text + "}";
            var wrapper = JsonUtility.FromJson<LeaderboardWrapper>(json);
            callback?.Invoke(true, wrapper.items);
        }
        else
        {
            callback?.Invoke(false, null);
        }
    }

    [Serializable] private class AuthPayload { public string username; public string password; }
    [Serializable] private class AuthResponse { public bool success; public int playerId; public string username; }
    [Serializable] private class LoginResponse { public bool success; public int playerId; public string username; public int bestScore; public int maxWave; }
    [Serializable] private class MatchResultPayload { public int playerId; public int score; public int highestWave; public int totalKills; public int headshots; public int durationSeconds; public bool isVictory; }
    [Serializable] private class MatchResultResponse { public bool success; public bool isNewHighScore; public int highestScore; }
    [Serializable] private class LeaderboardWrapper { public List<LeaderboardItem> items; }
}

[Serializable]
public class LeaderboardItem
{
    public int rank;
    public int playerId;
    public string username;
    public int bestScore;
    public int maxWave;
    public int lifetimeKills;
    public int lifetimeHeadshots;
    public int matchesPlayed;
}

