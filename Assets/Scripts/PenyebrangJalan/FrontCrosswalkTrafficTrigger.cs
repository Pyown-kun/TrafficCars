using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class FrontCrosswalkTrafficTrigger : MonoBehaviour
{
    [Header("Reference")]
    public PedestrianCrosswalkZone crosswalkZone;

    private readonly HashSet<NPCCarController> towardPlayerNPCs = new HashSet<NPCCarController>();

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

        towardPlayerNPCs.RemoveWhere(npc => npc == null);

        // NPC toward-player di trigger depan selalu berhenti
        foreach (var npc in towardPlayerNPCs)
        {
            if (npc != null)
            {
                npc.SetStoppedByCrosswalk(true, crosswalkZone);
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (crosswalkZone == null) return;

        // Player TIDAK diproses di trigger depan
        if (other.CompareTag("Player"))
        {
            return;
        }

        NPCCarController npc = other.GetComponentInParent<NPCCarController>();
        if (npc == null) return;

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