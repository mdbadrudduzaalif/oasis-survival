using UnityEngine;

public class Score : MonoBehaviour
{
    public static Score Instance { get; private set; }

    [Header("Score Values")]
    public int baseKillPoints = 100;
    public int headshotBonusPoints = 150;
    public int bossKillPoints = 2000;
    public int waveClearMultiplier = 500;

    [Header("Combo Streak System")]
    public float comboDuration = 3.5f;
    public float maxComboMultiplier = 4.0f;

    [Header("Current Run State")]
    public int currentScore = 0;
    public int highScore = 0;
    public int comboStreak = 0;
    public float currentMultiplier = 1.0f;
    public float comboTimer = 0f;

    private const string HIGH_SCORE_KEY = "OASIS_ZOMBIE_HIGH_SCORE";

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        LoadHighScore();
    }

    private void Update()
    {
        if (comboTimer > 0f)
        {
            comboTimer -= Time.deltaTime;
            if (comboTimer <= 0f)
            {
                ResetCombo();
            }
        }
    }

    public void AddKillScore(bool isHeadshot, bool isBoss = false)
    {

        comboStreak++;
        comboTimer = comboDuration;
        currentMultiplier = Mathf.Min(maxComboMultiplier, 1.0f + (comboStreak - 1) * 0.25f);

        int rawPoints = isBoss ? bossKillPoints : baseKillPoints;
        if (isHeadshot && !isBoss)
        {
            rawPoints += headshotBonusPoints;
        }

        int totalAwarded = Mathf.RoundToInt(rawPoints * currentMultiplier);
        currentScore += totalAwarded;

        CheckAndUpdateHighScore();

        var hud = FindAnyObjectByType<GunHUD>();
        if (hud != null)
        {
            string label = isBoss ? "BOSS SLAIN! +" : (isHeadshot ? "HEADSHOT! +" : "+");
            string comboText = comboStreak > 1 ? $" ({currentMultiplier:0.0}x Combo)" : "";
            hud.ShowPickupToast($"{label}{totalAwarded}{comboText}", isBoss ? new Color(1f, 0.3f, 0.3f) : (isHeadshot ? new Color(1f, 0.85f, 0.2f) : new Color(0.9f, 0.9f, 0.9f)));
        }
    }

    public void AddWaveClearScore(int waveNumber)
    {
        int wavePoints = waveNumber * waveClearMultiplier;
        currentScore += wavePoints;
        CheckAndUpdateHighScore();

        var hud = FindAnyObjectByType<GunHUD>();
        if (hud != null)
        {
            hud.ShowPickupToast($"Wave {waveNumber} Bonus: +{wavePoints}", new Color(0.3f, 0.95f, 0.4f));
        }
    }

    private void ResetCombo()
    {
        comboStreak = 0;
        currentMultiplier = 1.0f;
        comboTimer = 0f;
    }

    private void CheckAndUpdateHighScore()
    {
        if (currentScore > highScore)
        {
            highScore = currentScore;
            PlayerPrefs.SetInt(HIGH_SCORE_KEY, highScore);
            PlayerPrefs.Save();
        }
    }

    private void LoadHighScore()
    {
        highScore = PlayerPrefs.GetInt(HIGH_SCORE_KEY, 0);
    }

    public void ResetHighScore()
    {
        highScore = 0;
        PlayerPrefs.DeleteKey(HIGH_SCORE_KEY);
        PlayerPrefs.Save();
    }
}

