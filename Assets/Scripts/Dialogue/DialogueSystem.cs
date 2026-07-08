using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class DialogueSystem : MonoBehaviour
{
    public static DialogueSystem Instance { get; private set; }

    [Header("UI")]
    [SerializeField] private DialogueUI dialogueUI;

    [Header("Opening Dialogue")]
    [SerializeField] private DialogueData openingDialogue;

    [Header("Settings")]
    [SerializeField] private bool playOnStart = true;

    [SerializeField] private bool hideUIOnFinish = true;

    [Header("Input")]
    [SerializeField] private InputActionReference nextAction;
    
    private DialogueData currentDialogue;

    private int currentIndex;

    private bool isPlaying;

    public bool IsPlaying => isPlaying;

    public DialogueData CurrentDialogue => currentDialogue;

    public int CurrentIndex => currentIndex;

    private CharacterData currentLeftCharacter;

    private CharacterData currentRightCharacter;

    #region Events

    public event Action OnDialogueStarted;

    public event Action OnDialogueFinished;

    public event Action<DialogueLine> OnDialogueLineChanged;

    #endregion

    public DialogueLine CurrentLine
    {
        get
        {
            if (currentDialogue == null)
                return null;

            if (currentIndex < 0 || currentIndex >= currentDialogue.lines.Count)
                return null;

            return currentDialogue.lines[currentIndex];
        }
    }

    public bool HasDialogue =>
        currentDialogue != null;


    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        // if (playOnStart && openingDialogue != null)
        // {
        //     PlayDialogue(openingDialogue);
        // }
    }


    private void Update()
    {
        if (!isPlaying)
            return;

        if (nextAction == null)
            return;

        if (nextAction.action.WasPressedThisFrame())
        {
            NextDialogue();
        }
    }

    public void PlayOpeningDialogue()
    {
        if (openingDialogue == null)
            return;

        PlayDialogue(openingDialogue);
    }

    /// <summary>
    /// Memulai sebuah DialogueData.
    /// </summary>
    public void PlayDialogue(DialogueData dialogue)
{
    if (dialogue == null)
    {
        Debug.LogWarning("Dialogue Data is NULL.");
        return;
    }

    if (dialogue.lines == null || dialogue.lines.Count == 0)
    {
        Debug.LogWarning("Dialogue Data doesn't contain any dialogue.");
        return;
    }

    // Jika masih ada dialog yang berjalan, hentikan dulu
    if (isPlaying)
    {
        dialogueUI.StopTyping();
    }

    currentDialogue = dialogue;

    currentLeftCharacter = null;
    currentRightCharacter = null;

    currentIndex = 0;

    isPlaying = true;

    dialogueUI.Show();

    OnDialogueStarted?.Invoke();

    ShowCurrentDialogue();
}

    /// <summary>
    /// Menampilkan dialog sesuai index saat ini.
    /// </summary>
    private void ShowCurrentDialogue()
    {
        if (!isPlaying)
            return;

        if (currentDialogue == null)
            return;

        if (currentIndex < 0 ||
            currentIndex >= currentDialogue.lines.Count)
            return;

        DialogueLine line =
            ResolveLine(CurrentLine);

        dialogueUI.UpdateUI(line);

        OnDialogueLineChanged?.Invoke(line);
    }

    public void NextDialogue()
{
    if (!isPlaying)
        return;

    // Jika typewriter masih berjalan,
    // cukup tampilkan seluruh teks.
    if (dialogueUI.FinishTyping())
        return;

    currentIndex++;

    if (currentIndex >= currentDialogue.lines.Count)
    {
        EndDialogue();
        return;
    }

    ShowCurrentDialogue();
}

public void PreviousDialogue()
{
    if (!isPlaying)
        return;

    currentIndex--;

    currentIndex = Mathf.Max(currentIndex, 0);

    ShowCurrentDialogue();
}

private void EndDialogue()
{
    isPlaying = false;

    currentDialogue = null;

    currentIndex = 0;

    if (hideUIOnFinish)
        dialogueUI.Hide();

    OnDialogueFinished?.Invoke();

    if (GameplayStartManager.Instance != null)
        GameplayStartManager.Instance.StartGameplay();
}

public void OnNextButton()
{
    NextDialogue();
}

public void StopDialogue()
{
    if (!isPlaying)
        return;

    isPlaying = false;

    currentDialogue = null;

    currentIndex = 0;

    dialogueUI.Hide();

    OnDialogueFinished?.Invoke();
}

public void SkipDialogue()
{
    if (!isPlaying)
        return;

    EndDialogue();
}

public void RestartDialogue()
{
    if (currentDialogue == null)
        return;

    currentIndex = 0;

    ShowCurrentDialogue();
}

public void PlayDialogue(DialogueData dialogue, int startIndex)
{
    if (dialogue == null)
        return;

    currentDialogue = dialogue;

    currentIndex = Mathf.Clamp(
        startIndex,
        0,
        dialogue.lines.Count - 1);

    isPlaying = true;

    dialogueUI.Show();

    OnDialogueStarted?.Invoke();

    ShowCurrentDialogue();
}

private DialogueLine ResolveLine(DialogueLine source)
{
    if (source == null)
        return null;

    DialogueLine resolved = new DialogueLine();

    resolved.dialogue = source.dialogue;

    resolved.speaker = source.speaker;

    resolved.showLeftCharacter = source.showLeftCharacter;
    resolved.showRightCharacter = source.showRightCharacter;

    // LEFT
    if (source.inheritLeftCharacter)
    {
        resolved.leftCharacter = currentLeftCharacter;
    }
    else
    {
        resolved.leftCharacter = source.leftCharacter;
        currentLeftCharacter = source.leftCharacter;
    }

    // RIGHT
    if (source.inheritRightCharacter)
    {
        resolved.rightCharacter = currentRightCharacter;
    }
    else
    {
        resolved.rightCharacter = source.rightCharacter;
        currentRightCharacter = source.rightCharacter;
    }

    return resolved;
}

private void OnEnable()
{
    if (nextAction != null)
        nextAction.action.Enable();
}

private void OnDisable()
{
    if (nextAction != null)
        nextAction.action.Disable();
}

}