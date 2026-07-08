using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CanvasGroup))]
public class LoadingManager : MonoBehaviour
{
    public static LoadingManager Instance { get; private set; }

    [Header("Loading")]
    [SerializeField] private bool showOnStart = true;

    [SerializeField, Min(0.1f)]
    private float loadingDuration = 2f;

    [SerializeField, Min(0f)]
    private float fadeOutDuration = 0.5f;

    [Header("Dialogue")]
    [SerializeField] private DialogueUI dialogueUI;

    [SerializeField] private DialogueSystem DialogueSystem;

    [Header("Camera Intro")]
    [SerializeField] private CameraIntroController cameraIntro;

    [SerializeField]
    private float delayAfterCamera = 0.3f;

    [Header("Disable During Loading")]
    [Tooltip("InputAction yang akan dinonaktifkan selama loading.")]
    [SerializeField]
    private InputActionReference[] inputActions;

    private CanvasGroup canvasGroup;
    private Coroutine loadingRoutine;

    public bool IsLoading { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        canvasGroup = GetComponent<CanvasGroup>();

        if (showOnStart)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = true;
            canvasGroup.interactable = true;
        }
        else
        {
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
        }

        if (dialogueUI != null)
        dialogueUI.Hide();
    }

    private void Start()
    {
        if (showOnStart)
            Show();
    }

    public void Show()
    {
        Show(loadingDuration);
    }

    public void Show(float duration)
    {
        if (loadingRoutine != null)
            StopCoroutine(loadingRoutine);

        loadingRoutine = StartCoroutine(LoadingRoutine(duration));
    }

    public void Hide()
    {
        if (loadingRoutine != null)
            StopCoroutine(loadingRoutine);

        StartCoroutine(FadeOut());
    }

    private IEnumerator LoadingRoutine(float duration)
    {
        IsLoading = true;

        SetInputsEnabled(false);

        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;
        canvasGroup.interactable = true;

        if (dialogueUI != null)
            dialogueUI.Hide();

        yield return new WaitForSeconds(duration);

        yield return FadeOut();

        if (cameraIntro != null)
            yield return cameraIntro.PlayIntro();

            if (DialogueSystem != null)
        {
            DialogueSystem.PlayOpeningDialogue();
        }

        yield return new WaitForSeconds(delayAfterCamera);

        SetInputsEnabled(true);

        IsLoading = false;
    }

    private IEnumerator FadeOut()
    {
        float time = 0f;

        while (time < fadeOutDuration)
        {
            time += Time.deltaTime;

            canvasGroup.alpha = Mathf.Lerp(1f, 0f, time / fadeOutDuration);

            yield return null;
        }

        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;
    }

    private void SetInputsEnabled(bool enable)
    {
        if (inputActions == null)
            return;

        foreach (var input in inputActions)
        {
            if (input == null || input.action == null)
                continue;

            if (enable)
                input.action.Enable();
            else
                input.action.Disable();
        }
    }
}