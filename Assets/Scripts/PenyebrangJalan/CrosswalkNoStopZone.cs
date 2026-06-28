using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class CrosswalkNoStopZone : MonoBehaviour
{
    private void Reset()
    {
        GetComponent<BoxCollider>().isTrigger = true;
    }

    private void Awake()
    {
        GetComponent<BoxCollider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        PlayerCarController player = other.GetComponent<PlayerCarController>();

        if (player != null)
        {
            player.SetInsideCrosswalkNoStopZone(true);
            return;
        }

        NPCCarController npc = other.GetComponentInParent<NPCCarController>();

        if (npc != null)
        {
            npc.SetInsideNoStopZone(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        PlayerCarController player = other.GetComponent<PlayerCarController>();

        if (player != null)
        {
            player.SetInsideCrosswalkNoStopZone(false);
            return;
        }

        NPCCarController npc = other.GetComponentInParent<NPCCarController>();

        if (npc != null)
        {
            npc.SetInsideNoStopZone(false);
        }
    }
}