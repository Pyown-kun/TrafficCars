using UnityEngine;

[System.Serializable]
public class MergeReservation
{
    /// <summary>
    /// NPC yang ingin merge.
    /// </summary>
    public NPCCarController requester;

    /// <summary>
    /// Lane tujuan.
    /// </summary>
    public LaneMarker.LaneId targetLane;

    /// <summary>
    /// Posisi X requester saat reservation dibuat.
    /// </summary>
    public float targetX;

    /// <summary>
    /// Posisi Z requester saat reservation dibuat.
    /// </summary>
    public float targetZ;

    /// <summary>
    /// Radius merge zone.
    /// </summary>
    public float reservationRadius = 12f;

    /// <summary>
    /// True jika reservation masih aktif.
    /// </summary>
    public bool active = true;

    /// <summary>
    /// Waktu reservation dibuat.
    /// </summary>
    public float createTime;

    /// <summary>
    /// Timeout reservation.
    /// </summary>
    public float timeout = 8f;

    public MergeReservation(
    NPCCarController requester,
    LaneMarker.LaneId lane,
    float targetZ,
    float radius)
    {
        this.requester = requester;
        this.targetLane = lane;
        this.targetZ = targetZ;
        reservationRadius = radius;
    }

    /// <summary>
    /// Reservation masih valid.
    /// </summary>
    public bool IsValid()
    {
        if (!active)
            return false;

        if (requester == null)
            return false;

        if (Time.time - createTime >= timeout)
            return false;

        return true;
    }

    /// <summary>
    /// Menonaktifkan reservation.
    /// </summary>
    public void Cancel()
    {
        active = false;
    }
}