using System.Collections.Generic;
using UnityEngine;

public class PedestrianCrosswalkZone : MonoBehaviour
{
    [Header("Pedestrian Path")]
    public Transform leftStartPoint;
    public Transform rightEndPoint;

    [Header("Optional Reverse Path")]
    public Transform rightStartPoint;
    public Transform leftEndPoint;

    [Header("Movement")]
    public float moveSpeed = 8f;
    public float destroyBehindPlayerZ = -25f;

    [Header("Activation")]
    public float activationDistanceFront = 6f;
    public float activationDistanceBack = 3f;

    [Header("References")]
    public PlayerCarController playerCar;

    [Header("Ambulance Block")]
    public bool ambulanceBlocking = false;

    private PedestrianController currentPedestrian;
    private bool isFrozenForCrossing = false;

    // semua NPC yang sedang ditahan crosswalk
    private readonly HashSet<NPCCarController> stoppedNPCs = new HashSet<NPCCarController>();

    public bool HasActivePedestrian()
    {
        return currentPedestrian != null;
    }

    public bool IsAmbulanceBlocking()
    {
        return ambulanceBlocking;
    }

    public bool IsFrozenForCrossing()
    {
        return isFrozenForCrossing;
    }

    public float GetCurrentMoveSpeed()
    {
        // kalau freeze, crosswalk diam
        if (isFrozenForCrossing)
            return 0f;

        return moveSpeed;
    }

    public bool CanSpawnPedestrian()
    {
        if (HasActivePedestrian()) return false;
        if (ambulanceBlocking) return false;
        return true;
    }

    public void RegisterPedestrian(PedestrianController pedestrian)
    {
        currentPedestrian = pedestrian;
    }

    public void NotifyPedestrianFinished()
    {
        currentPedestrian = null;
        ResumeCrosswalkAfterCrossing();
    }

    public void SetAmbulanceBlocking(bool value)
    {
        ambulanceBlocking = value;
    }

    public bool IsPlayerNearCrosswalk()
    {
        if (playerCar == null) return false;

        float relativeZ = transform.position.z - playerCar.transform.position.z;
        return relativeZ <= activationDistanceFront && relativeZ >= -activationDistanceBack;
    }

    public void GetSpawnPath(out Transform startPoint, out Transform endPoint)
    {
        bool useLeftToRight = true;

        if (rightStartPoint != null && leftEndPoint != null)
        {
            useLeftToRight = Random.value > 0.5f;
        }

        if (useLeftToRight || rightStartPoint == null || leftEndPoint == null)
        {
            startPoint = leftStartPoint;
            endPoint = rightEndPoint;
        }
        else
        {
            startPoint = rightStartPoint;
            endPoint = leftEndPoint;
        }
    }

    // =========================================================
    // CROSSWALK CONTROL
    // =========================================================

    public void FreezeCrosswalkForCrossing()
    {
        // === FIX (safety log) ===
        // Method ini SEHARUSNYA hanya benar-benar membuat isFrozenForCrossing
        // = true jika ambulanceBlocking == false DAN HasActivePedestrian() == true.
        // Jika crosswalk terlihat freeze padahal pedestrian belum aktif,
        // log ini akan membantu konfirmasi apakah method ini terpanggil
        // di kondisi yang seharusnya tidak valid (misal dipanggil berulang
        // dari trigger lain saat player overlap area yang tidak disangka).
        if (ambulanceBlocking)
        {
            Debug.LogWarning($"[CrosswalkZone] {name} FreezeCrosswalkForCrossing() dipanggil tapi ambulanceBlocking=true, diabaikan.");
            return;
        }
        if (!HasActivePedestrian())
        {
            Debug.LogWarning($"[CrosswalkZone] {name} FreezeCrosswalkForCrossing() dipanggil tapi TIDAK ADA pedestrian aktif, diabaikan. " +
                              $"Jika crosswalk terasa freeze saat ini, ini BUKAN sebabnya (method ini tidak akan freeze apapun).");
            return;
        }

        isFrozenForCrossing = true;

        if (playerCar != null)
        {
            playerCar.SetCrosswalkMovementLock(true);
        }
    }

    public void ResumeCrosswalkAfterCrossing()
    {
        isFrozenForCrossing = false;

        if (playerCar != null)
        {
            playerCar.SetCrosswalkMovementLock(false);
        }

        foreach (var npc in stoppedNPCs)
        {
            if (npc != null)
            {
                npc.SetStoppedByCrosswalk(false, null);
            }
        }

        stoppedNPCs.Clear();
    }

    // === TAMBAHAN BARU ===
    // Safety net: jika karena alasan apapun isFrozenForCrossing tetap true
    // padahal tidak ada pedestrian aktif (kondisi yang seharusnya tidak
    // pernah terjadi berkat guard di FreezeCrosswalkForCrossing di atas),
    // method ini bisa dipanggil untuk memaksa crosswalk berjalan lagi.
    // Dipanggil otomatis dari Update() sebagai pengaman tambahan.
    void EnsureNotStuckFrozen()
    {
        if (isFrozenForCrossing && !HasActivePedestrian() && !ambulanceBlocking)
        {
            Debug.LogWarning($"[CrosswalkZone] {name} isFrozenForCrossing=true tanpa pedestrian aktif & tanpa ambulance block - " +
                              $"kondisi tidak valid, dipaksa resume agar crosswalk tidak freeze permanen.");
            ResumeCrosswalkAfterCrossing();
        }
    }
    // === END TAMBAHAN BARU ===

    // =========================================================
    // NPC STOP / RELEASE
    // =========================================================

    public void RegisterNPCInsideStopArea(NPCCarController npc)
    {
        if (npc == null) return;

        stoppedNPCs.Add(npc);
        npc.SetStoppedByCrosswalk(true, this);
    }

    public void UnregisterNPCInsideStopArea(NPCCarController npc)
    {
        if (npc == null) return;

        if (stoppedNPCs.Contains(npc))
        {
            stoppedNPCs.Remove(npc);
        }

        // jika crosswalk belum sedang crossing, NPC boleh dilepas saat keluar trigger
        if (!isFrozenForCrossing)
        {
            npc.SetStoppedByCrosswalk(false, null);
        }
    }

    private void Update()
    {
        if (Time.timeScale == 0f) return;
        if (playerCar == null) return;

        // Pengaman: pastikan tidak ada kondisi stuck-frozen tanpa alasan valid
        // sebelum dipakai untuk menentukan translate di bawah.
        EnsureNotStuckFrozen();

        if (!isFrozenForCrossing)
        {
            transform.Translate(Vector3.back * moveSpeed * Time.deltaTime, Space.World);
        }

        float relativeZ = transform.position.z - playerCar.transform.position.z;
        if (relativeZ < destroyBehindPlayerZ)
        {
            Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        if (playerCar != null)
        {
            playerCar.SetCrosswalkMovementLock(false);
        }

        foreach (var npc in stoppedNPCs)
        {
            if (npc != null)
            {
                npc.SetStoppedByCrosswalk(false, null);
            }
        }

        stoppedNPCs.Clear();
    }

    private void OnDrawGizmosSelected()
    {
        if (leftStartPoint != null && rightEndPoint != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(leftStartPoint.position, rightEndPoint.position);
        }

        if (rightStartPoint != null && leftEndPoint != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(rightStartPoint.position, leftEndPoint.position);
        }
    }
}