using UnityEngine;

public class PedestrianHitbox : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        // if (other.CompareTag("Player"))
        // {
        //     if (GameManager.Instance != null)
        //     {
        //         GameManager.Instance.GameOver();
        //     }
        //     else
        //     {
        //         Debug.Log("Player hit pedestrian -> Game Over");
        //     }
        // }
    }
}