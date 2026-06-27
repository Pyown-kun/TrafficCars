using UnityEngine;

public class PedestrianSpawner : MonoBehaviour
{
    [Header("Crosswalk Zone Prefab")]
    public GameObject crosswalkZonePrefab;

    [Header("Pedestrian Prefab")]
    public GameObject pedestrianPrefab;

    [Header("Spawn Timing")]
    public float minSpawnInterval = 10f;
    public float maxSpawnInterval = 18f;

    [Header("Spawn Position")]
    [Tooltip("Jarak spawn zone di depan player pada sumbu Z")]
    public float spawnDistanceAhead = 35f;

    [Tooltip("Posisi X zone saat spawn")]
    public float spawnX = 0f;

    [Tooltip("Posisi Y zone saat spawn")]
    public float spawnY = 0f;

    [Header("References")]
    public PlayerCarController playerCar;

    private float timer;
    private float nextSpawnTime;
    private PedestrianCrosswalkZone activeZone;

    private void Start()
    {
        SetNextSpawnTime();
    }

    private void Update()
    {
        if (Time.timeScale == 0f) return;
        if (crosswalkZonePrefab == null) return;
        if (pedestrianPrefab == null) return;
        if (playerCar == null) return;

        // Kalau zone sudah hilang, kosongkan ref
        if (activeZone != null && activeZone.gameObject == null)
        {
            activeZone = null;
        }

        timer += Time.deltaTime;

        if (timer >= nextSpawnTime)
        {
            TrySpawnCrosswalkZone();
            timer = 0f;
            SetNextSpawnTime();
        }
    }

    void TrySpawnCrosswalkZone()
    {
        // Sementara 1 crosswalk aktif saja
        if (activeZone != null) return;

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

        PedestrianCrosswalkZone zone = zoneObj.GetComponent<PedestrianCrosswalkZone>();
        if (zone == null)
        {
            Debug.LogWarning("CrosswalkZone prefab tidak punya PedestrianCrosswalkZone component.");
            Destroy(zoneObj);
            return;
        }

        zone.playerCar = playerCar;
        activeZone = zone;

        SpawnPedestrianForZone(zone);
    }

    void SpawnPedestrianForZone(PedestrianCrosswalkZone zone)
    {
        if (zone == null) return;
        if (!zone.CanSpawnPedestrian()) return;

        zone.GetSpawnPath(out Transform startPoint, out Transform endPoint);

        if (startPoint == null || endPoint == null)
        {
            Debug.LogWarning("CrosswalkZone path belum lengkap. Pastikan start/end point terpasang.");
            return;
        }

        // Spawn pedestrian sebagai CHILD dari zone
        GameObject pedestrianObj = Instantiate(
            pedestrianPrefab,
            zone.transform
        );

        // Tempatkan di posisi local start point
        pedestrianObj.transform.localPosition = startPoint.localPosition;
        pedestrianObj.transform.localRotation = Quaternion.identity;

        PedestrianController controller = pedestrianObj.GetComponent<PedestrianController>();
        if (controller == null)
        {
            Debug.LogWarning("Pedestrian prefab tidak punya PedestrianController.");
            Destroy(pedestrianObj);
            return;
        }

        controller.Setup(startPoint, endPoint, playerCar, zone);
        zone.RegisterPedestrian(controller);
    }

    void SetNextSpawnTime()
    {
        nextSpawnTime = Random.Range(minSpawnInterval, maxSpawnInterval);
    }
}