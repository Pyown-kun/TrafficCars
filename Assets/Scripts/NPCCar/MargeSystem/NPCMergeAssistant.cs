using UnityEngine;

/// <summary>
/// Menyimpan status bantuan merge pada NPC.
///
/// Script ini TIDAK menggerakkan kendaraan.
/// Semua movement tetap dilakukan oleh NPCCarController.
///
/// MergeCoordinator hanya mengubah mode assist melalui script ini.
/// </summary>
[RequireComponent(typeof(NPCCarController))]
public class NPCMergeAssistant : MonoBehaviour
{
    public enum AssistState
    {
        None,

        // NPC di depan reservation
        // mempercepat agar tercipta ruang.
        Forward,

        // NPC di belakang reservation
        // memperlambat agar tercipta ruang.
        Rear
    }

    [Header("Assist Multiplier")]

    [Tooltip("Multiplier ketika NPC diminta maju.")]
    public float forwardMultiplier = 1.20f;

    [Tooltip("Multiplier ketika NPC diminta melambat.")]
    public float rearMultiplier = 0.75f;

    private AssistState currentState = AssistState.None;

    private NPCCarController npc;

    //-----------------------------------------------------

    private void Awake()
    {
        npc = GetComponent<NPCCarController>();
    }

    //-----------------------------------------------------
    // API
    //-----------------------------------------------------

    /// <summary>
    /// NPC diminta maju.
    /// </summary>
    public void BeginForwardAssist()
    {
        currentState = AssistState.Forward;
    }

    /// <summary>
    /// NPC diminta melambat.
    /// </summary>
    public void BeginRearAssist()
    {
        currentState = AssistState.Rear;
    }

    /// <summary>
    /// Kembali ke perilaku normal.
    /// </summary>
    public void ResetAssist()
    {
        currentState = AssistState.None;
    }

    //-----------------------------------------------------

    public AssistState GetAssistState()
    {
        return currentState;
    }

    //-----------------------------------------------------

    public bool IsForwardAssist()
    {
        return currentState == AssistState.Forward;
    }

    public bool IsRearAssist()
    {
        return currentState == AssistState.Rear;
    }

    public bool IsAssisting()
    {
        return currentState != AssistState.None;
    }

    //-----------------------------------------------------

    /// <summary>
    /// Dipanggil oleh NPCCarController
    /// setiap frame.
    /// </summary>
    public float GetSpeedMultiplier()
    {
        switch (currentState)
        {
            case AssistState.Forward:
                return forwardMultiplier;

            case AssistState.Rear:
                return rearMultiplier;

            default:
                return 1f;
        }
    }

    //-----------------------------------------------------

    public NPCCarController GetController()
    {
        return npc;
    }

    public void StopAssist()
    {
        ResetAssist();
    }
}