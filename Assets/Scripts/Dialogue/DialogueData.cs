using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "Dialogue",
    menuName = "Traffic Car/Dialogue/Dialogue Data"
)]
public class DialogueData : ScriptableObject
{
    [Header("Dialogue Lines")]
    public List<DialogueLine> lines = new();
}