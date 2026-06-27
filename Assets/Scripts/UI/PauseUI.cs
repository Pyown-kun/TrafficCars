using UnityEngine;

public class PauseUI : MonoBehaviour
{
    public UIGameManager uiGameManager;

    public void TogglePause()
    {
        if (uiGameManager != null)
        {
            uiGameManager.TogglePause();
        }
    }

    public void ResumeGame()
    {
        if (uiGameManager != null && Time.timeScale == 0f)
        {
            uiGameManager.TogglePause();
        }
    }
}