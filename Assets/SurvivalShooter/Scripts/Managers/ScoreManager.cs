using UnityEngine;
using UnityEngine.UI;

public class ScoreManager : MonoBehaviour
{
    public static int score;
    public static int highScore;

    Text text;

    void Awake()
    {
        text = GetComponent<Text>();
        score = 0;
        highScore = PlayerPrefs.GetInt("HighScore", 0);
    }

    void Update()
    {
        text.text = "Score: " + score;
    }
}
