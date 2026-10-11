using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

// Hiện màn Game Over khi người chơi chết, lưu điểm cao, rồi chơi lại
public class GameOverManager : MonoBehaviour
{
    public PlayerHealth playerHealth;
    public CanvasGroup gameOverGroup;
    public Text highScoreText;
    public float fadeSpeed = 1.5f;
    public float restartDelay = 5f;

    float restartTimer;
    bool shown;

    void Update()
    {
        if (playerHealth.currentHealth > 0) return;

        if (!shown)
        {
            shown = true;
            if (ScoreManager.score > ScoreManager.highScore)
            {
                ScoreManager.highScore = ScoreManager.score;
                PlayerPrefs.SetInt("HighScore", ScoreManager.highScore);
            }
            if (highScoreText != null)
                highScoreText.text = "Best: " + ScoreManager.highScore;
        }

        gameOverGroup.alpha = Mathf.MoveTowards(gameOverGroup.alpha, 1f, fadeSpeed * Time.deltaTime);

        restartTimer += Time.deltaTime;
        if (restartTimer >= restartDelay)
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
