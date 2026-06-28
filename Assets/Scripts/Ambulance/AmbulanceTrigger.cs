using UnityEngine;

public class AmbulanceTrigger : MonoBehaviour
{
    [Header("Ambulance Spawn")]
    public GameObject ambulancePrefab;
    public Transform spawnPointBehind;

    [Header("Reference")]
    public PlayerCarController playerCar;

    [Header("Anti Overlap")]
    public float spawnCheckRadius = 4f;
    public LayerMask vehicleLayerMask = ~0;

    [Header("Player Safety")]
    [Tooltip("Minimal jarak spawn ambulance dari player")]
    public float minDistanceFromPlayer = 8f;

    /// <summary>
    /// Dipanggil oleh WorldEventManager.
    /// Return true jika spawn berhasil.
    /// </summary>
    public bool RequestSpawn()
    {
        if (ambulancePrefab == null)
            return false;

        if (spawnPointBehind == null)
            return false;

        if (playerCar != null)
        {
            float distance = Vector3.Distance(
                spawnPointBehind.position,
                playerCar.transform.position);

            if (distance < minDistanceFromPlayer)
                return false;
        }

        if (IsSpawnPointBlocked(spawnPointBehind.position))
            return false;

        SpawnAmbulance();

        return true;
    }

    void SpawnAmbulance()
    {
        GameObject ambulance = Instantiate(
            ambulancePrefab,
            spawnPointBehind.position,
            spawnPointBehind.rotation);

        AmbulanceController controller =
            ambulance.GetComponent<AmbulanceController>();

        if (controller != null)
        {
            controller.playerCar = playerCar;

            LaneMarker marker =
                spawnPointBehind.GetComponent<LaneMarker>();

            if (marker != null)
            {
                controller.laneId = marker.laneId;
            }
        }
    }

    bool IsSpawnPointBlocked(Vector3 spawnPosition)
    {
        Collider[] hits = Physics.OverlapSphere(
            spawnPosition,
            spawnCheckRadius,
            vehicleLayerMask);

        foreach (Collider hit in hits)
        {
            if (hit.CompareTag("NPCCar"))
                return true;

            if (hit.CompareTag("Ambulance"))
                return true;
        }

        return false;
    }

    private void OnDrawGizmosSelected()
    {
        if (spawnPointBehind == null)
            return;

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(
            spawnPointBehind.position,
            spawnCheckRadius);
    }
}