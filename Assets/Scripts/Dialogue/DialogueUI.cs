using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class DialogueUI : MonoBehaviour
{
    public static DialogueUI Instance { get; private set; }

    [Header("Root")]
    [SerializeField] private GameObject root;

    [Header("Left Character")]
    [SerializeField] private GameObject leftRoot;

    //[SerializeField] private CanvasGroup leftCanvasGroup;

    [SerializeField] private Image leftPortrait;

    [SerializeField] private RectTransform leftTransform;

    [Header("Right Character")]
    [SerializeField] private GameObject rightRoot;

    //[SerializeField] private CanvasGroup rightCanvasGroup;

    [SerializeField] private Image rightPortrait;

    [SerializeField] private RectTransform rightTransform;

    [Header("Dialogue")]

    [SerializeField] private TMP_Text characterNameText;

    [SerializeField] private TMP_Text dialogueText;

    [Header("Highlight")]

    [SerializeField]
    private Color activeColor = Color.white;

    [SerializeField]
    private Color inactiveColor = new Color(0.45f, 0.45f, 0.45f, 1f);

    [Header("Scale")]

    [SerializeField]
    private float activeScale = 1f;

    [SerializeField]
    private float inactiveScale = 0.9f;

    [Header("Animation")]

    [SerializeField]
    private float fadeSpeed = 8f;

    [SerializeField]
    private float scaleSpeed = 8f;

    [Header("Typewriter")]

    [SerializeField]
    private float typeSpeed = 0.03f;

    #region Properties

    public TMP_Text DialogueText => dialogueText;

    public TMP_Text CharacterNameText => characterNameText;

    public Image LeftPortrait => leftPortrait;

    public Image RightPortrait => rightPortrait;

    //public CanvasGroup LeftCanvasGroup => leftCanvasGroup;

    //public CanvasGroup RightCanvasGroup => rightCanvasGroup;

    public RectTransform LeftTransform => leftTransform;

    public RectTransform RightTransform => rightTransform;

    public float FadeSpeed => fadeSpeed;

    public float ScaleSpeed => scaleSpeed;

    public float ActiveScale => activeScale;

    public float InactiveScale => inactiveScale;

    public float TypeSpeed => typeSpeed;

    public Color ActiveColor => activeColor;

    public Color InactiveColor => inactiveColor;

    public bool IsTyping => isTyping;

    #endregion

    private Coroutine leftFadeRoutine;
    private Coroutine rightFadeRoutine;

    private Coroutine leftScaleRoutine;
    private Coroutine rightScaleRoutine;

    private Coroutine typingRoutine;

    private bool isTyping;

    private string currentDialogue;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        Initialize();
    }

    /// <summary>
    /// Mengembalikan UI ke kondisi awal.
    /// </summary>
    private void Initialize()
    {
        root.SetActive(false);

        // leftCanvasGroup.alpha = 0f;
        // rightCanvasGroup.alpha = 0f;

        leftTransform.localScale = Vector3.one * inactiveScale;
        rightTransform.localScale = Vector3.one * inactiveScale;

        leftPortrait.color = inactiveColor;
        rightPortrait.color = inactiveColor;

        characterNameText.text = "";
        dialogueText.text = "";
    }

    /// <summary>
    /// Menampilkan panel dialog.
    /// </summary>
    public void Show()
    {
        root.SetActive(true);
    }

    /// <summary>
    /// Menyembunyikan panel dialog.
    /// </summary>
    public void Hide()
    {
        StopTyping();

        root.SetActive(false);

        dialogueText.text = "";
        characterNameText.text = "";
    }

    public void UpdateUI(DialogueLine line)
    {
        if (line == null)
            return;

        //==========================
        // LEFT CHARACTER
        //==========================

        if (line.showLeftCharacter && line.leftCharacter != null)
        {
            leftRoot.SetActive(true);
            leftPortrait.sprite = line.leftCharacter.defaultPortrait;

            //StartFade(leftCanvasGroup, 1f, ref leftFadeRoutine);
        }
        else
        {
            // StartFade(leftCanvasGroup, 0f, ref leftFadeRoutine);
        }

        //==========================
        // RIGHT CHARACTER
        //==========================

        if (line.showRightCharacter && line.rightCharacter != null)
        {
            rightRoot.SetActive(true);
            rightPortrait.sprite = line.rightCharacter.defaultPortrait;

            //StartFade(rightCanvasGroup, 1f, ref rightFadeRoutine);
        }
        else
        {
            //StartFade(rightCanvasGroup, 0f, ref rightFadeRoutine);
        }

        UpdateSpeaker(line);

    DisplayDialogue(line.dialogue);
    }

    private void UpdateSpeaker(DialogueLine line)
{
    if (line.speaker == SpeakerSide.Left)
    {
        if (line.leftCharacter != null)
        {
            characterNameText.text = line.leftCharacter.characterName;
            characterNameText.color = line.leftCharacter.nameColor;
        }

        leftPortrait.color = activeColor;
        rightPortrait.color = inactiveColor;

        StartScale(leftTransform, activeScale, ref leftScaleRoutine);
        StartScale(rightTransform, inactiveScale, ref rightScaleRoutine);
    }
    else
    {
        if (line.rightCharacter != null)
        {
            characterNameText.text = line.rightCharacter.characterName;
            characterNameText.color = line.rightCharacter.nameColor;
        }

        leftPortrait.color = inactiveColor;
        rightPortrait.color = activeColor;

        StartScale(leftTransform, inactiveScale, ref leftScaleRoutine);
        StartScale(rightTransform, activeScale, ref rightScaleRoutine);
    }
}

private void StartFade(
    CanvasGroup canvas,
    float target,
    ref Coroutine routine)
{
    if (routine != null)
        StopCoroutine(routine);

    routine = StartCoroutine(
        FadeRoutine(canvas, target));
}

private IEnumerator FadeRoutine(
    CanvasGroup canvas,
    float targetAlpha)
{
    while (Mathf.Abs(canvas.alpha - targetAlpha) > 0.01f)
    {
        canvas.alpha = Mathf.MoveTowards(
            canvas.alpha,
            targetAlpha,
            fadeSpeed * Time.deltaTime);

        yield return null;
    }

    canvas.alpha = targetAlpha;

    // if (Mathf.Approximately(targetAlpha, 0f))
    // {
    //     if (canvas == leftCanvasGroup)
    //         leftRoot.SetActive(false);

    //     if (canvas == rightCanvasGroup)
    //         rightRoot.SetActive(false);
    // }
}

private void StartScale(
    RectTransform target,
    float scale,
    ref Coroutine routine)
{
    if (routine != null)
        StopCoroutine(routine);

    routine = StartCoroutine(
        ScaleRoutine(target, scale));
}

private IEnumerator ScaleRoutine(
    RectTransform target,
    float targetScale)
{
    Vector3 endScale = Vector3.one * targetScale;

    while (Vector3.Distance(target.localScale, endScale) > 0.001f)
    {
        target.localScale = Vector3.Lerp(
            target.localScale,
            endScale,
            scaleSpeed * Time.deltaTime);

        yield return null;
    }

    target.localScale = endScale;
}

public void DisplayDialogue(string dialogue)
{
    currentDialogue = dialogue;

    if (typingRoutine != null)
        StopCoroutine(typingRoutine);

    typingRoutine = StartCoroutine(TypeRoutine());
}

private IEnumerator TypeRoutine()
{
    isTyping = true;

    dialogueText.text = "";

    foreach (char c in currentDialogue)
    {
        dialogueText.text += c;

        yield return new WaitForSeconds(typeSpeed);
    }

    isTyping = false;
}

public bool FinishTyping()
{
    if (!isTyping)
        return false;

    if (typingRoutine != null)
        StopCoroutine(typingRoutine);

    dialogueText.text = currentDialogue;

    isTyping = false;

    return true;
}

public void StopTyping()
{
    if (typingRoutine != null)
    {
        StopCoroutine(typingRoutine);
        typingRoutine = null;
    }

    isTyping = false;
}
}