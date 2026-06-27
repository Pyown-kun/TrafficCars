using UnityEngine;

public class AmbulanceTrigger : MonoBehaviour
{
    [Header("Ambulance Spawn")]
    public GameObject ambulancePrefab;
    public Transform spawnPointBehind;

    [Header("Reference")]
    public PlayerCarController playerCar;

    [Header("Spawn Timer")]
    public float minSpawnTime = 12f;
    public float maxSpawnTime = 20f;

    [Header("Anti Overlap")]
    public float spawnCheckRadius = 4f;
    public LayerMask vehicleLayerMask = ~0;

    [Header("Player Safety")]
    [Tooltip("Minimal jarak spawn ambulance dari player")]
    public float minDistanceFromPlayer = 8f;

    private float timer;
    private float nextSpawnTime;

    private void Start()
    {
        SetNextSpawnTime();
    }

    private void Update()
    {
        if (Time.timeScale == 0f) return;
        if (ambulancePrefab == null || spawnPointBehind == null) return;

        timer += Time.deltaTime;

        if (timer >= nextSpawnTime)
        {
            TrySpawnAmbulance();
            timer = 0f;
            SetNextSpawnTime();
        }
    }

    void TrySpawnAmbulance()
    {
        if (playerCar != null)
        {
            float distanceToPlayer = Vector3.Distance(
                spawnPointBehind.position,
                playerCar.transform.position
            );

            if (distanceToPlayer < minDistanceFromPlayer)
            {
                return;
            }
        }

        if (IsSpawnPointBlocked(spawnPointBehind.position))
        {
            return;
        }

        SpawnAmbulance();
    }

    void SpawnAmbulance()
    {
        GameObject ambulance = Instantiate(
            ambulancePrefab,
            spawnPointBehind.position,
            spawnPointBehind.rotation
        );

        AmbulanceController controller = ambulance.GetComponent<AmbulanceController>();
        if (controller != null)
        {
            controller.playerCar = playerCar;

            // === TAMBAHAN BARU ===
            // Ambil LaneMarker dari spawn point (jika ada) lalu teruskan
            // laneId-nya ke ambulance yang baru spawn. Jika spawnPointBehind
            // belum dikasih LaneMarker, laneId akan tetap default (Lane2).
            //
            // Catatan: untuk mendukung ambulance muncul di Lane2 DAN Lane4
            // secara independen (sesuai rencana), tambahkan satu lagi
            // Transform spawn point + AmbulanceTrigger component terpisah
            // di scene untuk Lane4, masing-masing dengan LaneMarker sendiri.
            // Script ini tidak diubah strukturnya, cukup di-duplicate
            // GameObject-nya di scene dengan target spawn point berbeda.
            LaneMarker marker = spawnPointBehind.GetComponent<LaneMarker>();
            if (marker != null)
            {
                controller.laneId = marker.laneId;
            }
            // === END TAMBAHAN BARU ===
        }
    }

    bool IsSpawnPointBlocked(Vector3 spawnPosition)
    {
        Collider[] hits = Physics.OverlapSphere(
            spawnPosition,
            spawnCheckRadius,
            vehicleLayerMask
        );

        for (int i = 0; i < hits.Length; i++)
        {
            GameObject hitObject = hits[i].gameObject;

            if (hitObject.CompareTag("NPCCar") || hitObject.CompareTag("Ambulance"))
            {
                return true;
            }
        }

        return false;
    }

    void SetNextSpawnTime()
    {
        nextSpawnTime = Random.Range(minSpawnTime, maxSpawnTime);
    }

    private void OnDrawGizmosSelected()
    {
        if (spawnPointBehind == null) return;

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(spawnPointBehind.position, spawnCheckRadius);
    }
}