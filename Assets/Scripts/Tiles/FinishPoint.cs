using UnityEngine;

[RequireComponent(typeof(Collider))]
public class FinishPoint : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private string playerTag = "Player";

    [SerializeField]
    private bool triggerOnlyOnce = true;

    private bool triggered;

    private void Reset()
    {
        Collider col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (triggered && triggerOnlyOnce)
            return;

        if (!other.CompareTag(playerTag))
            return;

        triggered = true;

        if (FinishManager.Instance != null)
        {
            FinishManager.Instance.ShowFinish();
        }
        else
        {
            Debug.LogWarning("FinishManager tidak ditemukan di scene.");
        }
    }
}