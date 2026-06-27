using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Lane Registry terpusat. Tiap NPC yang punya laneId akan auto-register
/// dirinya ke registry ini saat spawn (OnEnable) dan auto-unregister saat
/// destroy/disable (OnDisable). Dipakai oleh NPCAmbulanceAvoidance untuk
/// query "siapa NPC terdekat di jalur tujuan" tanpa perlu Physics query
/// atau FindObjectsOfType tiap frame.
///
/// Tidak perlu di-assign manual di Inspector. Cukup satu GameObject kosong
/// di scene dengan component ini terpasang (atau biarkan dibuat otomatis
/// lewat lazy singleton di bawah).
/// </summary>
public class LaneRegistry : MonoBehaviour
{
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

    // Daftar NPC aktif per lane.
    private Dictionary<LaneMarker.LaneId, List<NPCCarController>> laneOccupants =
        new Dictionary<LaneMarker.LaneId, List<NPCCarController>>();

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
    }

    public void Register(NPCCarController npc)
    {
        if (npc == null) return;

        if (!laneOccupants.TryGetValue(npc.laneId, out List<NPCCarController> list))
        {
            list = new List<NPCCarController>();
            laneOccupants[npc.laneId] = list;
        }

        if (!list.Contains(npc))
        {
            list.Add(npc);
        }
    }

    public void Unregister(NPCCarController npc)
    {
        if (npc == null) return;

        if (laneOccupants.TryGetValue(npc.laneId, out List<NPCCarController> list))
        {
            list.Remove(npc);
        }
    }

    /// <summary>
    /// Cari NPC terdekat di targetLane yang posisi Z-nya masih lebih besar
    /// (artinya belum lewat) dibanding referenceZ milik NPC yang sedang menunggu.
    /// Return null jika tidak ada NPC yang menghalangi (jalur aman untuk merge).
    /// </summary>
    /// <param name="excludeSelf">NPC pemanggil, dikecualikan dari pencarian
    /// sebagai pengaman tambahan jika suatu saat entry stale muncul lagi
    /// di lane yang salah (mis. karena perubahan laneId di luar flow normal).</param>
    public NPCCarController FindBlockingNpcAhead(LaneMarker.LaneId targetLane, float referenceZ, NPCCarController excludeSelf = null)
    {
        if (!laneOccupants.TryGetValue(targetLane, out List<NPCCarController> list))
        {
            return null;
        }

        NPCCarController closestBlocker = null;
        float closestZ = float.MaxValue;

        for (int i = 0; i < list.Count; i++)
        {
            NPCCarController other = list[i];
            if (other == null) continue; // sudah destroyed tapi belum ter-unregister
            if (other == excludeSelf) continue; // jangan anggap diri sendiri sebagai blocker

            float otherZ = other.transform.position.z;

            // Belum lewat = posisi Z dia masih >= referenceZ (di jalur tujuan,
            // arah gerak adalah Vector3.back, jadi "lewat" berarti Z-nya
            // sudah turun di bawah referenceZ).
            if (otherZ >= referenceZ && otherZ < closestZ)
            {
                closestZ = otherZ;
                closestBlocker = other;
            }
        }

        return closestBlocker;
    }

    /// <summary>
    /// Cari NPC TERDEKAT DI DEPAN (Z lebih kecil) dari referenceZ, di lane
    /// yang sama (searchLane). Berbeda dari FindBlockingNpcAhead (yang
    /// mencari Z lebih BESAR/belum lewat, untuk kebutuhan gap-checking
    /// ambulance), method ini untuk kebutuhan car-following/antrian biasa:
    /// "siapa NPC paling dekat tepat di depan saya, di lane saya sendiri".
    /// Return null jika tidak ada NPC lain di depan (artinya NPC ini
    /// adalah yang paling depan di antrian).
    /// </summary>
    public NPCCarController FindClosestAhead(LaneMarker.LaneId searchLane, float referenceZ, NPCCarController excludeSelf = null)
    {
        if (!laneOccupants.TryGetValue(searchLane, out List<NPCCarController> list))
        {
            return null;
        }

        NPCCarController closest = null;
        float closestZ = float.MinValue;

        for (int i = 0; i < list.Count; i++)
        {
            NPCCarController other = list[i];
            if (other == null) continue;
            if (other == excludeSelf) continue;

            float otherZ = other.transform.position.z;

            // "Di depan" untuk NPC yang bergerak Vector3.back berarti
            // Z lebih kecil. Cari yang Z-nya lebih kecil dari referenceZ
            // TAPI paling besar di antara yang lebih kecil itu (paling dekat).
            if (otherZ < referenceZ && otherZ > closestZ)
            {
                closestZ = otherZ;
                closest = other;
            }
        }

        return closest;
    }
}