using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ===============================================================
/// MergeCoordinator
/// ===============================================================
///
/// Otak utama Cooperative Merge.
///
/// Tugas:
/// - Membaca seluruh MergeReservation dari LaneRegistry
/// - Menentukan NPC depan & belakang requester
/// - Memberi instruksi kepada NPCMergeAssistant
/// - Mengecek apakah gap sudah cukup
/// - Memberitahu requester kapan boleh merge
///
/// Script ini TIDAK:
/// - menggerakkan NPC
/// - mengubah speed NPC
/// - melakukan lane change
///
/// Semua aksi dilakukan oleh NPCMergeAssistant dan
/// NPCAmbulanceAvoidance.
/// ===============================================================
/// </summary>
public class MergeCoordinator : MonoBehaviour
{
    #region Singleton

    public static MergeCoordinator Instance;

    private void Awake()
    {
        if (Instance != null &&
            Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    #endregion

    //=========================================================
    // Inspector
    //=========================================================

    [Header("Update")]

    [Tooltip("Interval pengecekan reservation.")]
    public float checkInterval = 0.05f;

    [Header("Gap Requirement")]

    [Tooltip("Gap minimum di depan requester.")]
    public float desiredFrontGap = 8f;

    [Tooltip("Gap minimum di belakang requester.")]
    public float desiredRearGap = 8f;

    [Header("Debug")]

    public bool showDebugLog = false;

    private Coroutine updateRoutine;

    //=========================================================
    // Unity
    //=========================================================

    private void OnEnable()
    {
        updateRoutine =
            StartCoroutine(UpdateRoutine());
    }

    private void OnDisable()
    {
        if (updateRoutine != null)
        {
            StopCoroutine(updateRoutine);
            updateRoutine = null;
        }
    }

    IEnumerator UpdateRoutine()
    {
        WaitForSeconds wait =
            new WaitForSeconds(checkInterval);

        while (true)
        {
            ProcessReservations();

            yield return wait;
        }
    }

    //=========================================================
    // Reservation Processing
    //=========================================================

    void ProcessReservations()
    {
        List<MergeReservation> reservations =
            LaneRegistry.Instance.GetReservations();

        if (reservations == null)
        return;

        if (reservations.Count == 0)
            return;

        for (int i = 0; i < reservations.Count; i++)
        {
            MergeReservation reservation =
                reservations[i];

            if (reservation == null)
                continue;

            if (!reservation.active)
                continue;

            if (reservation.requester == null)
                continue;

            ProcessReservation(reservation);
        }
    }

        //=========================================================
    // Reservation AI
    //=========================================================

    void ProcessReservation(
        MergeReservation reservation)
    {
        List<NPCCarController> npcs =
            LaneRegistry.Instance.GetNPCInsideReservation(
                reservation);

        if (npcs == null)
            return;

        NPCCarController frontNpc =
            FindFrontNpc(reservation, npcs);

        NPCCarController rearNpc =
            FindRearNpc(reservation, npcs);

        //--------------------------------------------------
        // Beri instruksi membuka gap
        //--------------------------------------------------

        AssignAssist(frontNpc, true);
        AssignAssist(rearNpc, false);

        //--------------------------------------------------
        // Jika gap sudah cukup,
        // hentikan seluruh assist.
        //--------------------------------------------------

        if (GapReady(
            reservation,
            frontNpc,
            rearNpc))
        {
            ReleaseAssist(frontNpc);
            ReleaseAssist(rearNpc);

            LaneRegistry.Instance.NotifyMergeStarted(
                reservation);
        }

        Debug.Log("Coordinator Tick");
    }

    //=========================================================
    // FRONT NPC
    //=========================================================

    NPCCarController FindFrontNpc(
        MergeReservation reservation,
        List<NPCCarController> npcs)
    {
        NPCCarController closest = null;

        float closestDistance =
            float.MaxValue;

        foreach (NPCCarController npc in npcs)
        {
            if (npc == null)
                continue;

            if (npc == reservation.requester)
                continue;

            float dz =
                reservation.targetZ -
                npc.transform.position.z;

            if (dz <= 0f)
                continue;

            if (dz < closestDistance)
            {
                closestDistance = dz;
                closest = npc;
            }
        }

        return closest;
    }

    //=========================================================
    // REAR NPC
    //=========================================================

    NPCCarController FindRearNpc(
        MergeReservation reservation,
        List<NPCCarController> npcs)
    {
        NPCCarController closest = null;

        float closestDistance =
            float.MaxValue;

        foreach (NPCCarController npc in npcs)
        {
            if (npc == null)
                continue;

            if (npc == reservation.requester)
                continue;

            float dz =
                npc.transform.position.z -
                reservation.targetZ;

            if (dz <= 0f)
                continue;

            if (dz < closestDistance)
            {
                closestDistance = dz;
                closest = npc;
            }
        }

        return closest;
    }

    //=========================================================
    // ASSIGN ASSIST
    //=========================================================

    void AssignAssist(
        NPCCarController npc,
        bool front)
    {
        if (npc == null)
            return;

        NPCMergeAssistant assist =
            npc.GetComponent<NPCMergeAssistant>();

        if (assist == null)
            return;

        if (assist.IsAssisting())
            return;

        if (front)
        {
            assist.BeginForwardAssist();
        }
        else
        {
            assist.BeginRearAssist();
        }
    }

    //=========================================================
    // RELEASE ASSIST
    //=========================================================

    void ReleaseAssist(
        NPCCarController npc)
    {
        if (npc == null)
            return;

        NPCMergeAssistant assist =
            npc.GetComponent<NPCMergeAssistant>();

        if (assist == null)
            return;

        assist.StopAssist();
    }

    //=========================================================
    // GAP CHECK
    //=========================================================

    bool GapReady(
        MergeReservation reservation,
        NPCCarController frontNpc,
        NPCCarController rearNpc)
    {
        float frontGap =
            float.MaxValue;

        float rearGap =
            float.MaxValue;

        if (frontNpc != null)
        {
            frontGap =
                reservation.targetZ -
                frontNpc.transform.position.z;
        }

        if (rearNpc != null)
        {
            rearGap =
                rearNpc.transform.position.z -
                reservation.targetZ;
        }

        bool frontReady =
            frontGap >= desiredFrontGap;

        bool rearReady =
            rearGap >= desiredRearGap;

        return
            frontReady &&
            rearReady;
    }

        //------------------------------------------------------
    // MEMBERIKAN INSTRUKSI KE NPC
    //------------------------------------------------------

    void CommandFrontNPC(NPCCarController npc)
    {
        if (npc == null)
            return;

        NPCMergeAssistant assistant =
            npc.GetMergeAssistant();

        if (assistant == null)
            return;

        assistant.BeginForwardAssist();
    }

    //------------------------------------------------------

    void CommandRearNPC(NPCCarController npc)
    {
        if (npc == null)
            return;

        NPCMergeAssistant assistant =
            npc.GetMergeAssistant();

        if (assistant == null)
            return;

        assistant.BeginRearAssist();
    }

    //------------------------------------------------------

    public void NotifyMergeFinished(
        MergeReservation reservation)
    {
        if (reservation == null)
            return;

        LaneRegistry.Instance.NotifyMergeStarted(
            reservation);
    }

    //------------------------------------------------------

    public void ForceRefresh()
    {
        ProcessReservations();
    }

    //------------------------------------------------------

    public bool HasActiveReservation()
    {
        List<MergeReservation> reservations =
            LaneRegistry.Instance.GetReservations();

        if (reservations == null)
            return false;

        return reservations.Count > 0;
    }
}