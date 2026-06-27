using UnityEngine;

public class NPCCarSpawner : MonoBehaviour
{
    [Header("NPC Prefabs")]
    public GameObject[] npcCarPrefabs;

    [Header("Lane A Spawn Points (TowardPlayer Only)")]
    public Transform[] laneASpawnPoints;

    [Header("Lane C Spawn Points (SameDirection Only)")]
    public Transform[] laneCSpawnPoints;

    [Header("Spawn Timing")]
    public float laneASpawnInterval = 2f;
    public float laneCSpawnInterval = 2.5f;

    [Header("Reference")]
    public PlayerCarController playerCar;

    [Header("Anti Overlap")]
    [Tooltip("Radius cek kendaraan di sekitar spawn point")]
    public float spawnCheckRadius = 3f;

    [Tooltip("Berapa kali mencoba cari spawn point kosong per lane sebelum batal spawn")]
    public int maxSpawnAttempts = 5;

    [Tooltip("Layer kendaraan. Kosongkan jika ingin pakai semua layer.")]
    public LayerMask vehicleLayerMask = ~0;

    [Header("Crosswalk Rules")]
    [Tooltip("Jika true, lane C (SameDirection) tidak akan spawn saat crosswalk aktif.")]
    public bool blockSameDirectionSpawnWhenCrosswalkActive = true;

    [Tooltip("Jika true, lane A juga ikut berhenti spawn saat crosswalk aktif.")]
    public bool blockTowardPlayerSpawnWhenCrosswalkActive = false;

    private float laneATimer;
    private float laneCTimer;

    private void Update()
    {
        if (Time.timeScale == 0f) return;
        if (npcCarPrefabs == null || npcCarPrefabs.Length == 0) return;

        HandleLaneASpawn();
        HandleLaneCSpawn();
    }

    void HandleLaneASpawn()
    {
        if (laneASpawnPoints == null || laneASpawnPoints.Length == 0) return;

        laneATimer += Time.deltaTime;

        if (laneATimer >= laneASpawnInterval)
        {
            bool crosswalkActive = IsAnyCrosswalkActive();

            if (!blockTowardPlayerSpawnWhenCrosswalkActive || !crosswalkActive)
            {
                TrySpawnFromLane(
                    laneASpawnPoints,
                    NPCCarController.TrafficType.TowardPlayer
                );
            }

            laneATimer = 0f;
        }
    }

    void HandleLaneCSpawn()
    {
        if (laneCSpawnPoints == null || laneCSpawnPoints.Length == 0) return;

        laneCTimer += Time.deltaTime;

        if (laneCTimer >= laneCSpawnInterval)
        {
            bool crosswalkActive = IsAnyCrosswalkActive();

            // SameDirection tidak boleh spawn saat crosswalk aktif
            if (!blockSameDirectionSpawnWhenCrosswalkActive || !crosswalkActive)
            {
                TrySpawnFromLane(
                    laneCSpawnPoints,
                    NPCCarController.TrafficType.SameDirection
                );
            }

            laneCTimer = 0f;
        }
    }

    void TrySpawnFromLane(Transform[] spawnPoints, NPCCarController.TrafficType trafficType)
    {
        if (spawnPoints == null || spawnPoints.Length == 0) return;

        for (int attempt = 0; attempt < maxSpawnAttempts; attempt++)
        {
            int spawnIndex = Random.Range(0, spawnPoints.Length);
            Transform spawnPoint = spawnPoints[spawnIndex];

            // Lewati spawn point yang sudah destroyed/missing di array
            // (mis. GameObject-nya sempat dihapus/diganti di Editor)
            // supaya tidak melempar MissingReferenceException.
            if (spawnPoint == null) continue;

            if (!IsSpawnPointBlocked(spawnPoint.position))
            {
                SpawnNPCAt(spawnPoint, trafficType);
                return;
            }
        }
    }

    void SpawnNPCAt(Transform spawnPoint, NPCCarController.TrafficType trafficType)
    {
        int prefabIndex = Random.Range(0, npcCarPrefabs.Length);

        GameObject npc = Instantiate(
            npcCarPrefabs[prefabIndex],
            spawnPoint.position,
            spawnPoint.rotation
        );

        NPCCarController controller = npc.GetComponent<NPCCarController>();
        if (controller != null)
        {
            controller.playerCar = playerCar;
            controller.trafficType = trafficType;

            // === TAMBAHAN BARU ===
            // Ambil LaneMarker dari spawn point (jika ada) lalu teruskan
            // laneId-nya ke NPC yang baru spawn. Tidak mengubah logic spawn
            // yang sudah ada; jika spawn point belum dikasih LaneMarker,
            // controller.laneId akan tetap default (Lane1) seperti biasa.
            LaneMarker marker = spawnPoint.GetComponent<LaneMarker>();
            if (marker != null)
            {
                controller.laneId = marker.laneId;

                // Teruskan juga referensi AmbulanceRearWarningTrigger yang
                // relevan untuk lane ini (jika di-set di LaneMarker) ke
                // NPCEmojiIndicator pada NPC yang baru spawn, supaya icon
                // emoji NPC mengikuti status warning dari trigger SPESIFIK
                // ini (per-lane), bukan status global statis.
                NPCEmojiIndicator emojiIndicator = npc.GetComponent<NPCEmojiIndicator>();
                if (emojiIndicator != null && marker.rearWarningTrigger != null)
                {
                    emojiIndicator.relevantWarningTrigger = marker.rearWarningTrigger;
                }
            }
            // === END TAMBAHAN BARU ===
        }
    }

    bool IsAnyCrosswalkActive()
    {
        PedestrianCrosswalkZone[] zones = FindObjectsOfType<PedestrianCrosswalkZone>();
        if (zones == null || zones.Length == 0) return false;

        for (int i = 0; i < zones.Length; i++)
        {
            PedestrianCrosswalkZone zone = zones[i];
            if (zone == null) continue;

            // Crosswalk dianggap aktif jika:
            // 1. zone dekat player
            // 2. ada pedestrian aktif ATAU crosswalk sedang freeze crossing
            if (zone.IsPlayerNearCrosswalk() &&
                (zone.HasActivePedestrian() || zone.IsFrozenForCrossing()))
            {
                return true;
            }
        }

        return false;
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

    private void OnDrawGizmosSelected()
    {
        DrawLaneSpawnGizmos(laneASpawnPoints, Color.red);
        DrawLaneSpawnGizmos(laneCSpawnPoints, Color.green);
    }

    void DrawLaneSpawnGizmos(Transform[] spawnPoints, Color color)
    {
        if (spawnPoints == null) return;

        Gizmos.color = color;

        for (int i = 0; i < spawnPoints.Length; i++)
        {
            if (spawnPoints[i] != null)
            {
                Gizmos.DrawWireSphere(spawnPoints[i].position, spawnCheckRadius);
            }
        }
    }
}