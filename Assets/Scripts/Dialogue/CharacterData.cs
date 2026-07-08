using UnityEngine;

[CreateAssetMenu(
    fileName = "Character",
    menuName = "Traffic Car/Dialogue/Character"
)]
public class CharacterData : ScriptableObject
{
    [Header("Identity")]
    public string characterID;

    public string characterName;

    [Header("Portrait")]

    public Sprite defaultPortrait;

    [Header("UI")]

    public Color nameColor = Color.white;

    [Header("Voice (Optional)")]

    public AudioClip voiceBlip;
}