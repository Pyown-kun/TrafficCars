using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class RearCrosswalkTrafficTrigger : MonoBehaviour
{
    [Header("Reference")]
    public PedestrianCrosswalkZone crosswalkZone;

    private readonly HashSet<NPCCarController> sameDirectionNPCs = new HashSet<NPCCarController>();
    private bool playerInside = false;

    private void Reset()
    {
        BoxCollider col = GetComponent<BoxCollider>();
        col.isTrigger = true;
    }

    private void Awake()
    {
        BoxCollider col = GetComponent<BoxCollider>();
        col.isTrigger = true;
    }

    private void Update()
    {
        if (crosswalkZone == null) return;

        sameDirectionNPCs.RemoveWhere(npc => npc == null);

        // NPC hanya berhenti ketika pedestrian benar-benar
        // sedang menyeberang.
        foreach (var npc in sameDirectionNPCs)
        {
            if (npc == null)
                continue;

            if (crosswalkZone.IsFrozenForCrossing())
            {
                npc.SetStoppedByCrosswalk(true, crosswalkZone);
            }
            else
            {
                npc.SetStoppedByCrosswalk(false);
            }
        }

        if (crosswalkZone.playerCar == null) return;

        // Player hanya berhenti jika sedang brake
        // dan masih berada di area Rear Trigger.
        if (playerInside &&
            !crosswalkZone.playerCar.IsInsideCrosswalkNoStopZone() &&
            crosswalkZone.playerCar.IsBraking() &&
            crosswalkZone.CanFreezePlayer())
        {
            crosswalkZone.FreezeCrosswalkForCrossing();
        }
        else
        {
            if (!crosswalkZone.IsFrozenForCrossing())
            {
                crosswalkZone.playerCar.SetCrosswalkMovementLock(false);
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (crosswalkZone == null) return;

        // =========================
        // PLAYER
        // =========================
        if (other.CompareTag("Player"))
        {
            playerInside = true;
            return;
        }

        // =========================
        // NPC SAME DIRECTION
        // =========================
        NPCCarController npc = other.GetComponentInParent<NPCCarController>();
        if (npc == null) return;

        if (npc.trafficType != NPCCarController.TrafficType.SameDirection)
            return;

        sameDirectionNPCs.Add(npc);
        crosswalkZone.RegisterNPCInsideStopArea(npc);

        // Tidak langsung menghentikan NPC.
        // NPC tetap berjalan sampai pedestrian mulai menyeberang.
    }

    private void OnTriggerExit(Collider other)
    {
        if (crosswalkZone == null) return;

        // =========================
        // PLAYER
        // =========================
        if (other.CompareTag("Player"))
        {
            playerInside = false;

            if (crosswalkZone.playerCar != null)
            {
                crosswalkZone.playerCar.SetCrosswalkMovementLock(false);
            }

            return;
        }

        // =========================
        // NPC
        // =========================
        NPCCarController npc = other.GetComponentInParent<NPCCarController>();
        if (npc == null) return;

        if (sameDirectionNPCs.Remove(npc))
        {
            npc.SetStoppedByCrosswalk(false);
            crosswalkZone.UnregisterNPCInsideStopArea(npc);
        }
    }
}