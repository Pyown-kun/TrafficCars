using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CanvasGroup))]
public class FinishManager : MonoBehaviour
{
    public static FinishManager Instance { get; private set; }

    [Header("Finish Screen")]
    [SerializeField] private float fadeInDuration = 0.5f;

    [SerializeField] private float delayAfterCamera = 0.3f;

    [Header("Camera Outro")]
    [SerializeField] private CameraOutroController cameraOutro;

    [Header("Disable When Finish Tile Spawned")]
    [Tooltip("Spawner NPC, World Event, Pedestrian Spawner, dll.")]
    [SerializeField]
    private Behaviour[] finishPhaseComponents;

    [Header("Disable When Player Reaches Finish")]
    [Tooltip("PlayerController, CarController, CameraFollow, dll.")]
    [SerializeField]
    private Behaviour[] playerComponents;

    [Header("Disable Input")]
    [SerializeField]
    private InputActionReference[] inputActions;

    private CanvasGroup canvasGroup;

    private Coroutine finishRoutine;

    private bool finishPhaseStarted;
    private bool finishStarted;

    public bool FinishPhaseStarted => finishPhaseStarted;
    public bool FinishStarted => finishStarted;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        canvasGroup = GetComponent<CanvasGroup>();

        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;
    }

    //==========================================================
    // Dipanggil saat Finish Tile berhasil di-spawn
    //==========================================================

    public void BeginFinishPhase()
    {
        if (finishPhaseStarted)
            return;

        finishPhaseStarted = true;

        foreach (Behaviour component in finishPhaseComponents)
        {
            if (component != null)
                component.enabled = false;
        }

        Debug.Log("Finish Phase Started");
    }

    //==========================================================
    // Dipanggil oleh FinishPoint
    //==========================================================

    public void ShowFinish()
    {
        if (finishStarted)
            return;

        finishRoutine = StartCoroutine(FinishRoutine());
    }

    private IEnumerator FinishRoutine()
    {
        finishStarted = true;

        //----------------------------------------
        // Disable Player
        //----------------------------------------

        SetInputsEnabled(false);

        foreach (Behaviour component in playerComponents)
        {
            if (component != null)
                component.enabled = false;
        }

        //----------------------------------------
        // Camera Outro
        //----------------------------------------

        if (cameraOutro != null)
            yield return cameraOutro.PlayOutro();

        //----------------------------------------
        // Fade In Finish Background
        //----------------------------------------

        canvasGroup.blocksRaycasts = true;
        canvasGroup.interactable = true;

        float time = 0f;

        while (time < fadeInDuration)
        {
            time += Time.deltaTime;

            canvasGroup.alpha = Mathf.Lerp(
                0f,
                1f,
                time / fadeInDuration);

            yield return null;
        }

        canvasGroup.alpha = 1f;

        yield return new WaitForSeconds(delayAfterCamera);

        //----------------------------------------
        // TODO
        // Result UI
        // Dialogue
        // Next Level
        //----------------------------------------
    }

    //==========================================================
    // Input
    //==========================================================

    private void SetInputsEnabled(bool enable)
    {
        if (inputActions == null)
            return;

        foreach (InputActionReference input in inputActions)
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