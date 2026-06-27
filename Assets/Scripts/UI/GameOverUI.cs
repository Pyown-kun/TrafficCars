using UnityEngine;

public class GameOverUI : MonoBehaviour
{
    public UIGameManager uiGameManager;

    public void RetryGame()
    {
        if (uiGameManager != null)
        {
            uiGameManager.RetryGame();
        }
    }

    public void BackToMainMenu()
    {
        if (uiGameManager != null)
        {
            uiGameManager.GoToMainMenu();
        }
    }
}