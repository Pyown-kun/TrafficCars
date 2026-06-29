using UnityEngine;

/// <summary>
/// ===============================================================
/// NPCGapSensor
/// ===============================================================
///
/// Bertugas mengecek apakah lane tujuan memiliki ruang
/// yang cukup untuk merge.
///
/// Script ini TIDAK:
/// - mendeteksi ambulance
/// - membuat reservation
/// - menggerakkan kendaraan
/// - mengubah state avoidance
///
/// Script ini HANYA melakukan:
/// - mencari NPC depan
/// - mencari NPC belakang
/// - menghitung gap
/// - menentukan apakah merge aman
/// ===============================================================
/// </summary>
[RequireComponent(typeof(NPCCarController))]
public class NPCGapSensor : MonoBehaviour
{
    //=========================================================
    // Inspector
    //=========================================================

    [Header("Gap Detection")]

    [Tooltip("Radius maksimum pencarian NPC pada lane tujuan.")]
    public float gapDetectionRadius = 12f;

    [Tooltip("Gap minimum depan.")]
    public float desiredFrontGap = 7f;

    [Tooltip("Gap minimum belakang.")]
    public float desiredRearGap = 7f;

    [Header("Debug")]

    public bool showDebugLog = false;

    //=========================================================
    // References
    //=========================================================

    private NPCCarController npc;

    private NPCAmbulanceAvoidance avoidance;

    //=========================================================
    // Runtime Data
    //=========================================================

    private LaneMarker.LaneId targetLane;

    private NPCCarController frontNPC;

    private NPCCarController rearNPC;

    private float frontGap;

    private float rearGap;

    private bool gapReady;

    //=========================================================
    // Unity
    //=========================================================

    private void Awake()
    {
        npc =
            GetComponent<NPCCarController>();

        avoidance =
            GetComponent<NPCAmbulanceAvoidance>();
    }

    //=========================================================
    // Public API
    //=========================================================

    /// <summary>
    /// Dipanggil ketika ambulance telah terdeteksi.
    /// Menentukan lane tujuan merge.
    /// </summary>
    public void InitializeGapCheck()
    {
        if (npc == null)
            return;

        targetLane =
            LaneMarker.GetMergeTargetLane(
                npc.laneId);

        frontNPC = null;
        rearNPC = null;

        frontGap = 0f;
        rearGap = 0f;

        gapReady = false;
    }

    /// <summary>
    /// Dipanggil setiap frame selama WaitingForGap.
    /// </summary>
    public void UpdateGapStatus()
    {
        FindNearestVehicles();

        CalculateGap();

        EvaluateGap();
    }

    /// <summary>
    /// True jika gap sudah cukup.
    /// </summary>
    public bool IsGapReady()
    {
        return gapReady;
    }

    //=========================================================
    // Getter
    //=========================================================

    public NPCCarController GetFrontNPC()
    {
        return frontNPC;
    }

    public NPCCarController GetRearNPC()
    {
        return rearNPC;
    }

    public float GetFrontGap()
    {
        return frontGap;
    }

    public float GetRearGap()
    {
        return rearGap;
    }

    public LaneMarker.LaneId GetTargetLane()
    {
        return targetLane;
    }

    /// <summary>
    /// Menentukan lane yang akan dicek.
    /// Dipanggil oleh NPCAmbulanceAvoidance sebelum sensor mulai bekerja.
    /// </summary>
    public void SetTargetLane(LaneMarker.LaneId lane)
    {
        targetLane = lane;
    }

    /// <summary>
    /// Mengembalikan hasil evaluasi gap terakhir.
    /// Jika diperlukan, sensor akan melakukan evaluasi ulang.
    /// </summary>
    public bool IsGapAvailable()
    {
        UpdateGapStatus();
        return gapReady;
    }

    //=========================================================
    // Internal
    //=========================================================

    /// <summary>
    /// Mencari NPC depan dan belakang pada lane tujuan.
    /// Implementasi lengkap dilanjutkan pada Part 2.
    /// </summary>
    void FindNearestVehicles()
    {
        if (npc == null)
            return;

        float referenceZ =
            transform.position.z;

        //--------------------------------------------------
        // Cari NPC paling dekat di depan
        //--------------------------------------------------

        frontNPC =
            LaneRegistry.Instance.FindClosestAhead(
                targetLane,
                referenceZ,
                npc);

        if (frontNPC != null)
        {
            float distance =
                Mathf.Abs(
                    frontNPC.transform.position.z -
                    referenceZ);

            // Abaikan jika terlalu jauh
            if (distance > gapDetectionRadius)
            {
                frontNPC = null;
            }
        }

        //--------------------------------------------------
        // Cari NPC paling dekat di belakang
        //--------------------------------------------------

        rearNPC =
            LaneRegistry.Instance.FindClosestBehind(
                targetLane,
                referenceZ,
                npc);

        if (rearNPC != null)
        {
            float distance =
                Mathf.Abs(
                    rearNPC.transform.position.z -
                    referenceZ);

            // Abaikan jika terlalu jauh
            if (distance > gapDetectionRadius)
            {
                rearNPC = null;
            }
        }
    }

    //=========================================================
    // GAP CALCULATION
    //=========================================================

    void CalculateGap()
    {
        float referenceZ =
            transform.position.z;

        //------------------------------------------
        // FRONT GAP
        //------------------------------------------

        if (frontNPC == null)
        {
            frontGap =
                float.MaxValue;
        }
        else
        {
            frontGap =
                referenceZ -
                frontNPC.transform.position.z;
        }

        //------------------------------------------
        // REAR GAP
        //------------------------------------------

        if (rearNPC == null)
        {
            rearGap =
                float.MaxValue;
        }
        else
        {
            rearGap =
                rearNPC.transform.position.z -
                referenceZ;
        }
    }

    //=========================================================
    // GAP EVALUATION
    //=========================================================

    void EvaluateGap()
    {
        bool frontReady =
            frontGap >= desiredFrontGap;

        bool rearReady =
            rearGap >= desiredRearGap;

        gapReady =
            frontReady &&
            rearReady;

        if (!showDebugLog)
            return;

        Debug.Log(
            "[GapSensor] " +
            gameObject.name +
            "\nFront Gap : " +
            frontGap.ToString("F2") +
            "\nRear Gap : " +
            rearGap.ToString("F2") +
            "\nGap Ready : " +
            gapReady);
    }

    //=========================================================
    // DEBUG API
    //=========================================================

    public string GetGapInfo()
    {
        return
            "Front : " +
            frontGap.ToString("F2") +
            " | Rear : " +
            rearGap.ToString("F2") +
            " | Ready : " +
            gapReady;
    }
}