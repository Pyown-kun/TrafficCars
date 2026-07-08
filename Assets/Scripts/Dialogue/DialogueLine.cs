using System;
using UnityEngine;

[Serializable]
public class DialogueLine
{
    [Header("Left Character")]

    public bool showLeftCharacter = true;

    public bool inheritLeftCharacter = true;

    public CharacterData leftCharacter;

    [Space]

    [Header("Right Character")]

    public bool showRightCharacter = true;

    public bool inheritRightCharacter = true;

    public CharacterData rightCharacter;

    [Space]

    [Header("Speaker")]

    public SpeakerSide speaker;

    [Space]

    [Header("Dialogue")]

    [TextArea(3, 6)]
    public string dialogue;
}