using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class FrontCrosswalkTrafficTrigger : MonoBehaviour
{
    [Header("Reference")]
    public PedestrianCrosswalkZone crosswalkZone;

    private readonly HashSet<NPCCarController> towardPlayerNPCs = new HashSet<NPCCarController>();

    private bool playerAlreadyPenalized;

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
        if (crosswalkZone == null)
            return;

        towardPlayerNPCs.RemoveWhere(npc => npc == null);

        foreach (var npc in towardPlayerNPCs)
        {
            if (npc == null)
                continue;

            if(!npc.IsInsideNoStopZone())
            {
                npc.SetStoppedByCrosswalk(true, crosswalkZone);
            }
            else
            {
                npc.SetStoppedByCrosswalk(false);
            }
        }

    }

    private void OnTriggerEnter(Collider other)
    {
        if (crosswalkZone == null)
            return;

        // PLAYER
        if (other.CompareTag("Player"))
        {
            // Penyebrang sudah selesai atau tidak ada event
            if (!crosswalkZone.CanPlayerReceivePenalty())
                return;

            // Sudah pernah didenda pada crosswalk ini
            if (playerAlreadyPenalized)
                return;

            playerAlreadyPenalized = true;

            ViolationManager violation =
                other.GetComponent<ViolationManager>();

            violation?.TryAddViolation(gameObject);

            return;
        }

        // NPC
        NPCCarController npc = other.GetComponentInParent<NPCCarController>();
        if (npc == null)
            return;

        // Hanya NPC lawan arah / toward-player
        if (npc.trafficType != NPCCarController.TrafficType.TowardPlayer)
            return;

        towardPlayerNPCs.Add(npc);
        crosswalkZone.RegisterNPCInsideStopArea(npc);
    }

    private void OnTriggerExit(Collider other)
    {
        if (crosswalkZone == null) return;

        NPCCarController npc = other.GetComponentInParent<NPCCarController>();
        if (npc == null) return;

        if (towardPlayerNPCs.Contains(npc))
        {
            towardPlayerNPCs.Remove(npc);
            crosswalkZone.UnregisterNPCInsideStopArea(npc);
        }
    }

}