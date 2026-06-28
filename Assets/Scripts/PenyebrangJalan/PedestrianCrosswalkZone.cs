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
    [Tooltip("Multiplier terhadap World Speed")]
    public float worldSpeedMultiplier = 1f;

    public float destroyBehindPlayerZ = -25f;

    [Header("Activation")]
    public float activationDistanceFront = 6f;
    public float activationDistanceBack = 3f;

    [Header("Crosswalk Stop")]
    [Tooltip("Kecepatan maksimum WorldSpeed agar player masih boleh berhenti di crosswalk.")]
    public float maxWorldSpeedToFreeze = 3.5f;

    [Header("References")]
    public PlayerCarController playerCar;

    [Header("Ambulance Block")]
    public bool ambulanceBlocking = false;

    private PedestrianController currentPedestrian;
    private bool isFrozenForCrossing = false;
    private bool eventRegistered = false;

    // Semua NPC yang sedang berada pada area stop crosswalk
    private readonly HashSet<NPCCarController> stoppedNPCs =
        new HashSet<NPCCarController>();

    /// <summary>
    /// Mengambil kecepatan dunia dari WorldSpeedManager.
    /// Jika manager belum ada, gunakan fallback.
    /// </summary>
    float WorldSpeed
    {
        get
        {
            if (WorldSpeedManager.Instance == null)
                return 10f;

            return WorldSpeedManager.Instance.GetCurrentWorldSpeed();
        }
    }

    /// <summary>
    /// Crosswalk hanya boleh meng-freeze player jika
    /// kecepatan dunia sudah cukup rendah.
    /// </summary>
    public bool CanFreezePlayer()
    {
        return WorldSpeed <= maxWorldSpeedToFreeze;
    }

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

    /// <summary>
    /// Dipakai NPC TowardPlayer ketika mengikuti movement crosswalk.
    /// Saat freeze nilainya 0 sehingga NPC ikut berhenti.
    /// </summary>
    public float GetCurrentMoveSpeed()
    {
        if (isFrozenForCrossing)
            return 0f;

        return WorldSpeed * worldSpeedMultiplier;
    }

    public bool CanSpawnPedestrian()
    {
        if (HasActivePedestrian())
            return false;

        if (ambulanceBlocking)
            return false;

        return true;
    }

    public void RegisterPedestrian(PedestrianController pedestrian)
    {
        currentPedestrian = pedestrian;

        if (!eventRegistered &&
            WorldEventManager.Instance != null)
        {
            WorldEventManager.Instance.NotifyCrosswalkSpawned();
            eventRegistered = true;
        }
    }

    public void NotifyPedestrianFinished()
    {
        currentPedestrian = null;

        ResumeCrosswalkAfterCrossing();

        if (eventRegistered &&
            WorldEventManager.Instance != null)
        {
            WorldEventManager.Instance.FinishCurrentEvent();
            eventRegistered = false;
        }
    }

    public void SetAmbulanceBlocking(bool value)
    {
        ambulanceBlocking = value;
    }

    public bool IsPlayerNearCrosswalk()
    {
        if (playerCar == null)
            return false;

        float relativeZ = transform.position.z - playerCar.transform.position.z;

        return relativeZ <= activationDistanceFront &&
               relativeZ >= -activationDistanceBack;
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
        if (ambulanceBlocking)
            return;

        if (!HasActivePedestrian())
            return;

        // Player hanya boleh berhenti jika kecepatan dunia
        // sudah cukup rendah.
        if (!CanFreezePlayer())
            return;

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
                npc.SetStoppedByCrosswalk(false);
            }
        }

        stoppedNPCs.Clear();
    }

    /// <summary>
    /// Safety net apabila freeze aktif tetapi pedestrian sudah tidak ada.
    /// </summary>
    void EnsureNotStuckFrozen()
    {
        if (!isFrozenForCrossing)
            return;

        if (HasActivePedestrian())
            return;

        if (ambulanceBlocking)
            return;

        ResumeCrosswalkAfterCrossing();
    }

    // =========================================================
    // NPC STOP / RELEASE
    // =========================================================

    public void RegisterNPCInsideStopArea(NPCCarController npc)
    {
        if (npc == null)
            return;

        if (!stoppedNPCs.Contains(npc))
        {
            stoppedNPCs.Add(npc);
        }

        npc.SetStoppedByCrosswalk(true, this);
    }

    public void UnregisterNPCInsideStopArea(NPCCarController npc)
    {
        if (npc == null)
            return;

        stoppedNPCs.Remove(npc);

        // Jika crossing belum berlangsung,
        // NPC langsung dilepas saat keluar trigger.
        if (!isFrozenForCrossing)
        {
            npc.SetStoppedByCrosswalk(false);
        }
    }

        private void Update()
    {
        if (Time.timeScale == 0f)
            return;

        if (playerCar == null)
            return;

        // Safety apabila terjadi kondisi freeze yang tidak valid.
        EnsureNotStuckFrozen();

        // Crosswalk bergerak mengikuti kecepatan dunia.
        if (!isFrozenForCrossing)
        {
            transform.Translate(
                Vector3.back *
                (WorldSpeed * worldSpeedMultiplier) *
                Time.deltaTime,
                Space.World);
        }

        float relativeZ = transform.position.z - playerCar.transform.position.z;

        if (relativeZ < destroyBehindPlayerZ)
        {
            Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        if (eventRegistered &&
            WorldEventManager.Instance != null)
        {
            WorldEventManager.Instance.FinishCurrentEvent();
            eventRegistered = false;
        }

        if (playerCar != null)
        {
            playerCar.SetCrosswalkMovementLock(false);
        }

        foreach (var npc in stoppedNPCs)
        {
            if (npc != null)
            {
                npc.SetStoppedByCrosswalk(false);
            }
        }

        stoppedNPCs.Clear();
    }

    private void OnDrawGizmosSelected()
    {
        if (leftStartPoint != null && rightEndPoint != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(
                leftStartPoint.position,
                rightEndPoint.position);
        }

        if (rightStartPoint != null && leftEndPoint != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(
                rightStartPoint.position,
                leftEndPoint.position);
        }
    }

    public bool IsPlayerFrozen()
    {
        if (playerCar == null)
            return false;

        return playerCar.IsMovementLockedByCrosswalk();
    }

    public bool IsEventRegistered()
    {
        return eventRegistered;
    }
}