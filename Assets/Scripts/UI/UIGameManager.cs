using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class UIGameManager : MonoBehaviour
{
    [Header("UI Text")]
    public TMP_Text scoreText;
    public TMP_Text finalScoreText;

    [Header("Panels")]
    public GameObject gameOverPanel;
    public GameObject pausePanel;

    private float distanceScore;
    private bool isGameOver = false;

    private void Start()
    {
        Time.timeScale = 1f;

        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);

        if (pausePanel != null)
            pausePanel.SetActive(false);

        UpdateScoreUI();
    }

    public void AddDistanceScore(float amount)
    {
        if (isGameOver) return;

        distanceScore += amount;
        UpdateScoreUI();
    }

    void UpdateScoreUI()
    {
        if (scoreText != null)
            scoreText.text = "Score: " + Mathf.FloorToInt(distanceScore).ToString();
    }

    public void GameOver()
    {
        if (isGameOver) return;

        isGameOver = true;
        Time.timeScale = 0f;

        if (gameOverPanel != null)
            gameOverPanel.SetActive(true);

        if (finalScoreText != null)
            finalScoreText.text = "Final Score: " + Mathf.FloorToInt(distanceScore).ToString();
    }

    public void TogglePause()
    {
        if (isGameOver) return;

        bool isPaused = Time.timeScale == 0f;

        if (isPaused)
        {
            Time.timeScale = 1f;
            if (pausePanel != null) pausePanel.SetActive(false);
        }
        else
        {
            Time.timeScale = 0f;
            if (pausePanel != null) pausePanel.SetActive(true);
        }
    }

    public void RetryGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void GoToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }
}