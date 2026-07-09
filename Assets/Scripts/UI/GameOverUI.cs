using UnityEngine;

public class GameOverUI : MonoBehaviour
{
    [SerializeField] private GameObject gameOverPanel;

    private VehicleHealth vehicleHealth;

    private void Awake()
    {
        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);
    }

    private void Start()
    {
        vehicleHealth = VehicleHealth.Instance;

        if (vehicleHealth != null)
        {
            vehicleHealth.OnDead += ShowGameOver;
            Debug.Log("GameOverUI Subscribe Success");
        }
        else
        {
            Debug.LogError("VehicleHealth tidak ditemukan!");
        }
    }

    private void OnDestroy()
    {
        if (vehicleHealth != null)
            vehicleHealth.OnDead -= ShowGameOver;
    }

    private void ShowGameOver()
    {
        Debug.Log("SHOW GAME OVER");

        gameOverPanel.SetActive(true);
        Time.timeScale = 0f;
    }
}