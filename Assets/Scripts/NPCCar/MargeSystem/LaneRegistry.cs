using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Registry pusat seluruh NPC Traffic.
///
/// Menyimpan:
/// 1. NPC aktif pada setiap lane
/// 2. Merge Reservation yang sedang aktif
///
/// Tidak mengandung AI.
/// Hanya menjadi pusat data yang dibaca oleh
/// NPCAmbulanceAvoidance,
/// NPCMergeAssistant,
/// MergeCoordinator.
/// </summary>
public class LaneRegistry : MonoBehaviour
{
    #region Singleton

    private static LaneRegistry _instance;

    public static LaneRegistry Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject go = new GameObject("LaneRegistry (Auto)");
                _instance = go.AddComponent<LaneRegistry>();
            }

            return _instance;
        }
    }

    #endregion

    #region Fields

    /// <summary>
    /// Seluruh NPC aktif berdasarkan lane.
    /// </summary>
    private readonly Dictionary<
        LaneMarker.LaneId,
        List<NPCCarController>>
        laneOccupants =
        new Dictionary<
            LaneMarker.LaneId,
            List<NPCCarController>>();

    /// <summary>
    /// Seluruh reservation aktif.
    /// Hanya SATU list.
    /// Ini menjadi source of truth.
    /// </summary>
    private readonly List<MergeReservation>
        reservations =
        new List<MergeReservation>();

    #endregion

    #region Unity

    private void Awake()
    {
        if (_instance != null &&
            _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
    }

    private void Update()
    {
        CleanupReservations();
        CleanupNullNPC();
    }

    #endregion

    //----------------------------------------------------
    // NPC REGISTER
    //----------------------------------------------------

    #region Register

    public void Register(
        NPCCarController npc)
    {
        if (npc == null)
            return;

        if (!laneOccupants.TryGetValue(
            npc.laneId,
            out List<NPCCarController> list))
        {
            list =
                new List<NPCCarController>();

            laneOccupants.Add(
                npc.laneId,
                list);
        }

        if (!list.Contains(npc))
        {
            list.Add(npc);
        }
    }

    public void Unregister(
        NPCCarController npc)
    {
        if (npc == null)
            return;

        if (!laneOccupants.TryGetValue(
            npc.laneId,
            out List<NPCCarController> list))
        {
            return;
        }

        list.Remove(npc);
    }

    #endregion

    //----------------------------------------------------
    // RESERVATION
    //----------------------------------------------------

    #region Reservation

    public MergeReservation CreateReservation(
        NPCCarController requester,
        LaneMarker.LaneId targetLane,
        float radius)
    {
        if (requester == null)
            return null;

        MergeReservation reservation =
            new MergeReservation(
                requester,
                targetLane,
                requester.transform.position.z,
                radius);

        reservation.active = true;

        reservations.Add(reservation);

        return reservation;
    }

    public void UpdateReservation(
        MergeReservation reservation)
    {
        if (reservation == null)
            return;

        if (!reservation.active)
            return;

        if (reservation.requester == null)
            return;

        reservation.targetZ =
            reservation.requester.transform.position.z;
    }

    public void RemoveReservation(
        MergeReservation reservation)
    {
        if (reservation == null)
            return;

        reservation.active = false;
    }


    public bool HasReservation(
        NPCCarController requester)
    {
        return GetReservationByRequester(
            requester) != null;
    }

    public bool HasReservation(
        LaneMarker.LaneId lane)
    {
        foreach (MergeReservation reservation
            in reservations)
        {
            if (reservation == null)
                continue;

            if (!reservation.active)
                continue;

            if (reservation.targetLane == lane)
                return true;
        }

        return false;
    }

    public MergeReservation GetReservation(
        LaneMarker.LaneId lane)
    {
        foreach (MergeReservation reservation
            in reservations)
        {
            if (reservation == null)
                continue;

            if (!reservation.active)
                continue;

            if (reservation.targetLane != lane)
                continue;

            return reservation;
        }

        return null;
    }

    public MergeReservation GetReservationByRequester(
        NPCCarController requester)
    {
        if (requester == null)
            return null;

        foreach (MergeReservation reservation
            in reservations)
        {
            if (reservation == null)
                continue;

            if (!reservation.active)
                continue;

            if (reservation.requester == requester)
                return reservation;
        }

        return null;
    }

    public void ClearAllReservations()
    {
        reservations.Clear();
    }

    #endregion

    //----------------------------------------------------
    // CLEANUP
    //----------------------------------------------------

    #region Cleanup

    private void CleanupReservations()
    {
        for (int i = reservations.Count - 1;
             i >= 0;
             i--)
        {
            MergeReservation reservation =
                reservations[i];

            if (reservation == null)
            {
                reservations.RemoveAt(i);
                continue;
            }

            if (!reservation.active)
            {
                reservations.RemoveAt(i);
                continue;
            }

            if (reservation.requester == null)
            {
                reservations.RemoveAt(i);
            }
        }
    }

    private void CleanupNullNPC()
    {
        foreach (var pair
            in laneOccupants)
        {
            List<NPCCarController> list =
                pair.Value;

            for (int i = list.Count - 1;
                 i >= 0;
                 i--)
            {
                if (list[i] == null)
                {
                    list.RemoveAt(i);
                }
            }
        }
    }

    #endregion

    //----------------------------------------------------
    // QUERY
    //----------------------------------------------------

    #region Query

    /// <summary>
    /// Mengembalikan seluruh NPC pada lane tertentu.
    /// Dipakai oleh MergeCoordinator.
    /// </summary>
    public List<NPCCarController> GetLaneOccupants(
        LaneMarker.LaneId lane)
    {
        if (laneOccupants.TryGetValue(
            lane,
            out List<NPCCarController> list))
        {
            return list;
        }

        return null;
    }

    /// <summary>
    /// NPC terdekat di depan.
    /// Untuk traffic yang bergerak Vector3.back,
    /// depan = Z lebih kecil.
    /// </summary>
    public NPCCarController FindClosestAhead(
        LaneMarker.LaneId lane,
        float referenceZ,
        NPCCarController excludeSelf = null)
    {
        if (!laneOccupants.TryGetValue(
            lane,
            out List<NPCCarController> list))
        {
            return null;
        }

        NPCCarController closest = null;
        float closestZ = float.MinValue;

        for (int i = 0; i < list.Count; i++)
        {
            NPCCarController other = list[i];

            if (other == null)
                continue;

            if (other == excludeSelf)
                continue;

            float z = other.transform.position.z;

            if (z < referenceZ &&
                z > closestZ)
            {
                closest = other;
                closestZ = z;
            }
        }

        return closest;
    }

    /// <summary>
    /// NPC terdekat di belakang.
    /// Untuk traffic Vector3.back,
    /// belakang = Z lebih besar.
    /// </summary>
    public NPCCarController FindClosestBehind(
        LaneMarker.LaneId lane,
        float referenceZ,
        NPCCarController excludeSelf = null)
    {
        if (!laneOccupants.TryGetValue(
            lane,
            out List<NPCCarController> list))
        {
            return null;
        }

        NPCCarController closest = null;
        float closestZ = float.MaxValue;

        for (int i = 0; i < list.Count; i++)
        {
            NPCCarController other = list[i];

            if (other == null)
                continue;

            if (other == excludeSelf)
                continue;

            float z = other.transform.position.z;

            if (z > referenceZ &&
                z < closestZ)
            {
                closest = other;
                closestZ = z;
            }
        }

        return closest;
    }

    /// <summary>
    /// Dipakai sistem ambulance lama.
    /// Sementara dipertahankan agar backward compatible.
    /// Nantinya seluruh pengecekan gap akan dilakukan
    /// MergeCoordinator.
    /// </summary>
    public NPCCarController FindBlockingNpcAhead(
        LaneMarker.LaneId lane,
        float referenceZ,
        NPCCarController excludeSelf = null)
    {
        if (!laneOccupants.TryGetValue(
            lane,
            out List<NPCCarController> list))
        {
            return null;
        }

        NPCCarController closest = null;
        float closestZ = float.MaxValue;

        for (int i = 0; i < list.Count; i++)
        {
            NPCCarController other = list[i];

            if (other == null)
                continue;

            if (other == excludeSelf)
                continue;

            float z = other.transform.position.z;

            if (z >= referenceZ &&
                z < closestZ)
            {
                closest = other;
                closestZ = z;
            }
        }

        return closest;
    }

    /// <summary>
    /// Mengembalikan seluruh NPC yang berada
    /// di dalam reservation zone.
    /// Nantinya dipakai MergeCoordinator
    /// untuk memberi instruksi membuka gap.
    /// </summary>
    public List<NPCCarController> GetNPCInsideReservation(
        MergeReservation reservation)
    {
        List<NPCCarController> result =
            new List<NPCCarController>();

        if (reservation == null)
            return result;

        if (!reservation.active)
            return result;

        if (!laneOccupants.TryGetValue(
            reservation.targetLane,
            out List<NPCCarController> list))
        {
            return result;
        }

        float min =
            reservation.targetZ -
            reservation.reservationRadius;

        float max =
            reservation.targetZ +
            reservation.reservationRadius;

        for (int i = 0; i < list.Count; i++)
        {
            NPCCarController npc = list[i];

            if (npc == null)
                continue;

            if (npc == reservation.requester)
                continue;

            float z = npc.transform.position.z;

            if (z >= min &&
                z <= max)
            {
                result.Add(npc);
            }
        }

        return result;
    }

    #endregion

    //----------------------------------------------------
    // RESERVATION API
    //----------------------------------------------------

    #region Reservation API

    /// <summary>
    /// Mengembalikan seluruh reservation aktif.
    /// Dipakai MergeCoordinator.
    /// </summary>
    public List<MergeReservation> GetReservations()
    {
        CleanupReservations();
        return reservations;
    }

    /// <summary>
    /// Menonaktifkan reservation.
    /// Coordinator akan menghapusnya otomatis.
    /// </summary>
    public void CancelReservation(
        MergeReservation reservation)
    {
        if (reservation == null)
            return;

        reservation.active = false;
    }

    /// <summary>
    /// Membatalkan reservation berdasarkan requester.
    /// </summary>
    public void CancelReservationByRequester(
        NPCCarController requester)
    {
        MergeReservation reservation =
            GetReservationByRequester(requester);

        if (reservation == null)
            return;

        reservation.active = false;
    }

    /// <summary>
    /// Mengecek apakah lane sedang memiliki reservation aktif.
    /// </summary>
    public bool IsLaneReserved(
        LaneMarker.LaneId lane)
    {
        CleanupReservations();

        for (int i = 0; i < reservations.Count; i++)
        {
            MergeReservation reservation =
                reservations[i];

            if (reservation.targetLane == lane)
                return true;
        }

        return false;
    }

    /// <summary>
    /// Jumlah reservation aktif.
    /// Berguna untuk debugging.
    /// </summary>
    public int GetReservationCount()
    {
        CleanupReservations();
        return reservations.Count;
    }

    /// <summary>
    /// Dipanggil ketika requester mulai merge.
    /// Reservation langsung dinonaktifkan sehingga
    /// coordinator dapat fokus pada reservation lain.
    /// </summary>
    public void NotifyMergeStarted(
        MergeReservation reservation)
    {
        if (reservation == null)
            return;

        reservation.active = false;
    }

    #endregion

}