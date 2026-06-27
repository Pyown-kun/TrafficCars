using UnityEngine;

public class NPCCarController : MonoBehaviour
{
    public enum TrafficType
    {
        SameDirection, // mobil searah player
        TowardPlayer   // mobil lawan arah / menuju player
    }

    [Header("Traffic Type")]
    public TrafficType trafficType = TrafficType.TowardPlayer;

    // === TAMBAHAN BARU ===
    // Diisi otomatis oleh NPCCarSpawner saat instantiate, dibaca oleh
    // LaneRegistry & NPCAmbulanceAvoidance. Tidak memengaruhi logic
    // gerak/brake/destroy yang sudah ada.
    [Header("Lane (untuk Ambulance Avoidance System)")]
    public LaneMarker.LaneId laneId = LaneMarker.LaneId.Lane1;
    // === END TAMBAHAN BARU ===

    [Header("Movement")]
    [Tooltip("Kecepatan NPC yang searah dengan player (SameDirection)")]
    public float sameDirectionMoveSpeed = 10f;

    [Tooltip("Kecepatan NPC yang menuju player (TowardPlayer)")]
    public float towardPlayerMoveSpeed = 14f;

    [Tooltip("Kecepatan NPC saat mengikuti pergerakan Crosswalk")]
    public float crosswalkMoveSpeed = 12f;

    [Tooltip("Kecepatan NPC searah saat player brake dan NPC menjauh ke depan")]
    public float sameDirectionBrakeSpeed = 14f;

    [Tooltip("Multiplier NPC toward-player saat player brake. < 1 = melambat")]
    public float towardPlayerBrakeMultiplier = 0.7f;

    // === TAMBAHAN BARU ===
    [Tooltip("Multiplier tambahan untuk sameDirectionBrakeSpeed, KHUSUS saat player brake DAN crosswalk masih aktif (pedestrian masih menyeberang). < 1 = lebih pelan dari brake biasa, sebagai representasi 'tetap hati-hati' walau tetap maju ke depan.")]
    public float crosswalkCautiousBrakeMultiplier = 0.5f;
    // === END TAMBAHAN BARU ===

    [Header("Destroy Range")]
    [Tooltip("Destroy jika NPC sudah jauh di belakang player")]
    public float destroyBackZ = -30f;

    [Tooltip("Destroy jika NPC same-direction sudah terlalu jauh di depan")]
    public float destroyFrontZ = 120f;

    [Header("Reference")]
    public PlayerCarController playerCar;

    private float spawnZ;
    private bool hasMovedBackwardFromSpawn = false;

    // =========================
    // CROSSWALK STOP STATE
    // =========================
    private bool stoppedByCrosswalk = false;
    private PedestrianCrosswalkZone currentCrosswalkZone;

    private void Start()
    {
        spawnZ = transform.position.z;
    }

   private void Update()
{
    if (Time.timeScale == 0f) return;
    if (playerCar == null) return;

    bool isBraking = playerCar.IsBraking();

    switch (trafficType)
    {
        case TrafficType.SameDirection:

            // SameDirection SELALU menggunakan behaviour normal.
            // Crosswalk tidak pernah mengambil alih movement.
            HandleSameDirectionNPC(isBraking, false);
            break;

        case TrafficType.TowardPlayer:

            // Hanya TowardPlayer yang mengikuti crosswalk.
            if (stoppedByCrosswalk)
            {
                FollowCrosswalkWhileStopped();
                return;
            }

            HandleTowardPlayerNPC(isBraking);
            break;
    }
}

    void HandleSameDirectionNPC(bool isBraking, bool crosswalkActive)
    {
        if (isBraking)
        {
            // Saat player brake, NPC searah menjauh ke depan.
            // Jika crosswalk masih aktif (pedestrian masih menyeberang),
            // tetap maju ke depan TAPI dengan speed dikurangi
            // (crosswalkCautiousBrakeMultiplier) sebagai representasi
            // "tetap hati-hati" walau brake tetap menang dan NPC tetap maju.
            float effectiveBrakeSpeed = crosswalkActive
                ? sameDirectionBrakeSpeed * crosswalkCautiousBrakeMultiplier
                : sameDirectionBrakeSpeed;

            transform.Translate(Vector3.forward * effectiveBrakeSpeed * Time.deltaTime, Space.World);

            // Kalau sebelumnya sempat turun ke belakang dari titik spawn,
            // lalu sekarang balik lagi ke area spawn / melewati spawn -> destroy
            if (hasMovedBackwardFromSpawn && transform.position.z >= spawnZ)
            {
                Destroy(gameObject);
                return;
            }

            if (transform.position.z > destroyFrontZ)
            {
                Destroy(gameObject);
                return;
            }
        }
        else
        {
            // Normal: NPC searah bergerak ke belakang player
            transform.Translate(
            Vector3.back * sameDirectionMoveSpeed * Time.deltaTime,
            Space.World
        );

            if (transform.position.z < spawnZ)
            {
                hasMovedBackwardFromSpawn = true;
            }

            if (transform.position.z < destroyBackZ)
            {
                Destroy(gameObject);
                return;
            }
        }
    }

    void HandleTowardPlayerNPC(bool isBraking)
    {
        float currentSpeed = towardPlayerMoveSpeed;

        if (isBraking)
        {
            // Toward-player sedikit melambat saat player brake
            currentSpeed *= towardPlayerBrakeMultiplier;
        }

        // tetap ke belakang player / mendekati player
        transform.Translate(Vector3.back * currentSpeed * Time.deltaTime, Space.World);

        if (transform.position.z < destroyBackZ)
        {
            Destroy(gameObject);
            return;
        }
    }

    void FollowCrosswalkWhileStopped()
    {
        // === FIX (pengaman tambahan) ===
        // Sebelumnya: jika currentCrosswalkZone null, method langsung
        // return tanpa translate apapun -> NPC freeze total di tempat.
        // Ini bisa terjadi jika SetStoppedByCrosswalk(true) dipanggil
        // tanpa parameter zone dari script trigger manapun (root cause
        // asli sudah diperbaiki di CrosswalkTrafficTrigger.cs), namun
        // sebagai pengaman tambahan kita tetap gerakkan NPC dengan
        // moveSpeed default (bukan diam total) supaya tidak ada skenario
        // freeze permanen meski referensi zone hilang.
        float crosswalkSpeed = currentCrosswalkZone != null
        ? currentCrosswalkZone.GetCurrentMoveSpeed()
        : crosswalkMoveSpeed;

        // NPC ikut bergeser bersama crosswalk
        transform.Translate(Vector3.back * crosswalkSpeed * Time.deltaTime, Space.World);

        // tetap pakai destroy belakang sebagai safety
        if (transform.position.z < destroyBackZ)
        {
            Destroy(gameObject);
        }
    }

    // =========================================================
    // API untuk Crosswalk
    // =========================================================

    public void SetStoppedByCrosswalk(bool value, PedestrianCrosswalkZone zone = null)
{
    // SameDirection mengabaikan crosswalk.
    if (trafficType == TrafficType.SameDirection)
        return;

    stoppedByCrosswalk = value;

    if (value)
    {
        currentCrosswalkZone = zone;
    }
    else
    {
        currentCrosswalkZone = null;
    }
}

    public bool IsStoppedByCrosswalk()
    {
        return stoppedByCrosswalk;
    }

    // =========================================================
    // API untuk Ambulance Avoidance (Lane Merge)
    // =========================================================

    /// <summary>
    /// Dipanggil secara eksplisit saat NPC selesai pindah jalur ke Lane1
    /// (hasil merge dari Lane2 karena menghindari ambulance). Memastikan
    /// NPC ini berperilaku PERSIS seperti NPC Lane1 asli: trafficType
    /// di-set SameDirection (sehingga saat player brake, NPC bergerak
    /// Vector3.forward / menjauhi player - sama seperti HandleSameDirectionNPC),
    /// dan status crosswalk dipastikan tidak nyangkut dari histori sebelumnya
    /// di Lane2 sebelum proses merge terjadi.
    ///
    /// Catatan: spawnZ dan hasMovedBackwardFromSpawn TIDAK direset di sini -
    /// keduanya tetap memakai histori asli NPC sejak spawn pertama di Lane2,
    /// sesuai keputusan desain yang sudah diambil.
    /// </summary>
    public void ApplySameDirectionBehavior()
    {
        trafficType = TrafficType.SameDirection;

        // Pastikan tidak ada status crosswalk yang nyangkut dari histori
        // sebelum merge, supaya tidak salah masuk ke FollowCrosswalkWhileStopped()
        // dan melewatkan logic brake sama sekali.
        if (stoppedByCrosswalk)
        {
            SetStoppedByCrosswalk(false);
        }
    }
}