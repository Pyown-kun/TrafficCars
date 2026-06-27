using UnityEngine;

/// <summary>
/// Tempelkan component ini ke GameObject spawn point (yang transform-nya
/// sudah dipakai di NPCCarSpawner.laneASpawnPoints / laneCSpawnPoints,
/// atau di AmbulanceTrigger.spawnPointBehind).
///
/// Cukup pilih LaneId dari dropdown Inspector sesuai posisi spawn point
/// tersebut di scene. Tidak mengubah logic spawner/controller yang sudah ada;
/// ini murni data tambahan yang dibaca saat proses instantiate.
/// </summary>
public class LaneMarker : MonoBehaviour
{
    public enum LaneId
    {
        Lane1, // SameDirection
        Lane2, // SameDirection - jalur trigger ambulance
        Lane3, // TowardPlayer
        Lane4, // TowardPlayer - jalur trigger ambulance
        Lane5  // TowardPlayer
    }

    [Tooltip("Jalur ke berapa spawn point ini. Sesuaikan dengan posisi aslinya di scene (kanan ke kiri: 1,2,3,4,5).")]
    public LaneId laneId = LaneId.Lane1;

    // === TAMBAHAN BARU ===
    [Header("Ambulance Rear Warning (opsional)")]
    [Tooltip("Drag GameObject yang punya component AmbulanceRearWarningTrigger yang RELEVAN untuk lane ini. NPCCarSpawner akan meneruskan referensi ini ke NPCEmojiIndicator pada NPC yang spawn di lane ini, supaya icon emoji NPC mengikuti status warning dari trigger SPESIFIK ini (bukan status global). Boleh dikosongkan jika lane ini tidak relevan dengan ambulance avoidance (Lane1/Lane3/Lane5).")]
    public AmbulanceRearWarningTrigger rearWarningTrigger;
    // === END TAMBAHAN BARU ===

    /// <summary>
    /// True jika jalur ini adalah jalur yang memicu kemunculan ambulance (Lane2 / Lane4).
    /// </summary>
    public bool IsAmbulanceTriggerLane()
    {
        return laneId == LaneId.Lane2 || laneId == LaneId.Lane4;
    }

    /// <summary>
    /// Jalur tujuan merge untuk NPC yang menghindar dari ambulance.
    /// Lane2 -> Lane1, Lane4 -> Lane5. Jalur lain tidak punya target (return jalur itu sendiri).
    /// </summary>
    public static LaneId GetMergeTargetLane(LaneId currentLane)
    {
        switch (currentLane)
        {
            case LaneId.Lane2:
                return LaneId.Lane1;
            case LaneId.Lane4:
                return LaneId.Lane5;
            default:
                return currentLane;
        }
    }

    /// <summary>
    /// Arah belok (relatif world X) saat merge dari jalur ini.
    /// Lane2 -> Lane1 dianggap belok kiri (-1), Lane4 -> Lane5 dianggap belok kanan (+1).
    /// Sesuaikan tanda jika arah X di scene kamu kebalik.
    /// </summary>
    public static float GetMergeDirectionSign(LaneId currentLane)
    {
        switch (currentLane)
        {
            case LaneId.Lane2:
                return -1f; // ke kiri
            case LaneId.Lane4:
                return 1f;  // ke kanan
            default:
                return 0f;
        }
    }
}