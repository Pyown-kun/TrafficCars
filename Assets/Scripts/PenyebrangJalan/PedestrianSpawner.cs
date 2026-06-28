using UnityEngine;

public class PedestrianSpawner : MonoBehaviour
{
    [Header("Crosswalk Zone Prefab")]
    public GameObject crosswalkZonePrefab;

    [Header("Pedestrian Prefab")]
    public GameObject pedestrianPrefab;

    [Header("Spawn Position")]
    [Tooltip("Jarak spawn zone di depan player pada sumbu Z")]
    public float spawnDistanceAhead = 35f;

    [Tooltip("Posisi X zone saat spawn")]
    public float spawnX = 0f;

    [Tooltip("Posisi Y zone saat spawn")]
    public float spawnY = 0f;

    [Header("References")]
    public PlayerCarController playerCar;

    private PedestrianCrosswalkZone activeZone;

    /// <summary>
    /// Dipanggil oleh WorldEventManager.
    /// Return TRUE jika crosswalk berhasil di-spawn.
    /// </summary>
    public bool RequestSpawn()
    {
        if (crosswalkZonePrefab == null)
            return false;

        if (pedestrianPrefab == null)
            return false;

        if (playerCar == null)
            return false;

        // Bersihkan reference jika zone sudah dihancurkan
        if (activeZone != null && activeZone.gameObject == null)
        {
            activeZone = null;
        }

        // Masih ada crosswalk aktif
        if (activeZone != null)
            return false;

        return TrySpawnCrosswalkZone();
    }

    bool TrySpawnCrosswalkZone()
    {
        Vector3 spawnPos = new Vector3(
            spawnX,
            spawnY,
            playerCar.transform.position.z + spawnDistanceAhead
        );

        GameObject zoneObj = Instantiate(
            crosswalkZonePrefab,
            spawnPos,
            Quaternion.identity
        );

        PedestrianCrosswalkZone zone =
            zoneObj.GetComponent<PedestrianCrosswalkZone>();

        if (zone == null)
        {
            Debug.LogWarning(
                "CrosswalkZone prefab tidak memiliki PedestrianCrosswalkZone."
            );

            Destroy(zoneObj);
            return false;
        }

        zone.playerCar = playerCar;

        activeZone = zone;

        if (!SpawnPedestrianForZone(zone))
        {
            Destroy(zoneObj);
            activeZone = null;
            return false;
        }

        return true;
    }

    bool SpawnPedestrianForZone(PedestrianCrosswalkZone zone)
    {
        if (zone == null)
            return false;

        if (!zone.CanSpawnPedestrian())
            return false;

        zone.GetSpawnPath(
            out Transform startPoint,
            out Transform endPoint
        );

        if (startPoint == null || endPoint == null)
        {
            Debug.LogWarning(
                "Crosswalk path belum lengkap."
            );

            return false;
        }

        GameObject pedestrianObj = Instantiate(
            pedestrianPrefab,
            zone.transform
        );

        pedestrianObj.transform.localPosition =
            startPoint.localPosition;

        pedestrianObj.transform.localRotation =
            Quaternion.identity;

        PedestrianController controller =
            pedestrianObj.GetComponent<PedestrianController>();

        if (controller == null)
        {
            Debug.LogWarning(
                "Pedestrian prefab tidak memiliki PedestrianController."
            );

            Destroy(pedestrianObj);
            return false;
        }

        controller.Setup(
            startPoint,
            endPoint,
            playerCar,
            zone
        );

        zone.RegisterPedestrian(controller);

        return true;
    }

    /// <summary>
    /// Dipanggil PedestrianCrosswalkZone ketika event selesai.
    /// </summary>
    public void ClearActiveZone(PedestrianCrosswalkZone zone)
    {
        if (activeZone == zone)
        {
            activeZone = null;
        }
    }

    public bool HasActiveCrosswalk()
    {
        return activeZone != null;
    }

    public PedestrianCrosswalkZone GetActiveZone()
    {
        return activeZone;
    }
}