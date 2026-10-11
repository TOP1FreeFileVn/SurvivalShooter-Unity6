using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

// Màn kết thúc: thua (chết) hoặc thắng (qua hết wave). Lưu wave/điểm cao nhất, bấm nút hoặc R để chơi lại.
public class GameOverManager : MonoBehaviour
{
    public PlayerHealth playerHealth;
    public CanvasGroup gameOverGroup;
    public Text titleText;
    public Text summaryText;
    public Button restartButton;
    public float fadeSpeed = 1.5f;

    bool shown;

    void Awake()
    {
        restartButton.onClick.AddListener(Restart);
    }

    void Update()
    {
        var wm = WaveManager.Instance;
        bool lost = playerHealth.IsDead;
        bool won = wm != null && wm.Current == WaveManager.State.Won;
        if (!lost && !won) return;

        if (!shown)
        {
            shown = true;
            int wave = wm != null ? wm.Wave : 0;
            if (won) wave = wm.totalWaves;

            if (ScoreManager.score > ScoreManager.highScore)
            {
                ScoreManager.highScore = ScoreManager.score;
                PlayerPrefs.SetInt("HighScore", ScoreManager.highScore);
            }
            int bestWave = Mathf.Max(PlayerPrefs.GetInt("BestWave", 0), wave);
            PlayerPrefs.SetInt("BestWave", bestWave);

            titleText.text = won ? "CHIEN THANG!" : "GAME OVER!";
            titleText.color = won ? new Color(0.5f, 1f, 0.5f) : new Color(1f, 0.85f, 0.3f);
            summaryText.text =
                (won ? "Song sot qua " + wave + " wave" : "Guc nga o wave " + wave) +
                "\nCap " + PlayerStats.Instance.level + "   -   Diem " + ScoreManager.score +
                "\nKy luc: " + ScoreManager.highScore + " diem, wave " + bestWave;

            gameOverGroup.blocksRaycasts = true;
            gameOverGroup.interactable = true;
            if (won) Time.timeScale = 0f;
        }

        gameOverGroup.alpha = Mathf.MoveTowards(gameOverGroup.alpha, 1f, fadeSpeed * Time.unscaledDeltaTime);

        if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
            Restart();
    }

    void Restart()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
