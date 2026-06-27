using UnityEngine;

public class PlayerCollision : MonoBehaviour
{
    public UIGameManager uiGameManager;
    public PlayerCarController playerCarController;

    private bool hasCrashed = false;

    private void OnCollisionEnter(Collision collision)
    {
        if (hasCrashed) return;

        if (collision.gameObject.CompareTag("NPCCar") ||
            collision.gameObject.CompareTag("Ambulance") ||
            collision.gameObject.CompareTag("Pedestrian"))
        {
            hasCrashed = true;

            if (playerCarController != null)
                playerCarController.StopPlayer();

            if (uiGameManager != null)
                uiGameManager.GameOver();
        }
    }
}