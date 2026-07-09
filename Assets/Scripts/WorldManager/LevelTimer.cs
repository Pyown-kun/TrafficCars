using System;
using UnityEngine;
using UnityEngine.UI;

public class LevelTimer : MonoBehaviour
{
    public static LevelTimer Instance;

    [Header("Level Time")]
    [SerializeField] private float levelDuration = 90f;

    [SerializeField]
    [Range(0f, 1f)]
    private float moveThresholdRatio = 0.4f;

    [Header("UI")]
    [SerializeField] private Slider progressSlider;

    [Header("Pause Settings")]
    [SerializeField] private bool useGraceTime = true;

    [SerializeField] private float graceTime = 2f;

    [Header("Smooth UI")]
    [SerializeField] private bool smoothSlider = true;

    [SerializeField] private float smoothSpeed = 10f;

    public static event Action OnTimerFinished;

    private float remainingTime;
    private float idleTimer;
    private float displayedProgress = 1f;

    public bool IsFinished { get; private set; }

    public float Progress => remainingTime / levelDuration;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    private void Start()
    {
        if (progressSlider != null)
        {
            progressSlider.minValue = 0f;
            progressSlider.maxValue = 1f;
        }

        ResetTimer();
    }

    private void Update()
    {
        UpdateSliderUI();

        if (IsFinished)
            return;

        if (WorldSpeedManager.Instance == null)
            return;

        bool moving = WorldSpeedManager.Instance.GetSpeedRatio() >= moveThresholdRatio;

        if (moving)
        {
            idleTimer = 0f;
            TickTimer();
        }
        else
        {
            if (!useGraceTime)
                return;

            idleTimer += Time.deltaTime;

            if (idleTimer < graceTime)
                TickTimer();
        }
    }

    private void TickTimer()
    {
        remainingTime -= Time.deltaTime;
        remainingTime = Mathf.Max(remainingTime, 0f);

        if (!smoothSlider)
            UpdateSliderInstant();

        if (remainingTime <= 0f)
            FinishTimer();
    }

    private void UpdateSliderInstant()
    {
        if (progressSlider == null)
            return;

        progressSlider.value = Progress;
    }

    private void UpdateSliderUI()
    {
        if (progressSlider == null)
            return;

        if (smoothSlider)
        {
            displayedProgress = Mathf.Lerp(
                displayedProgress,
                Progress,
                Time.deltaTime * smoothSpeed);

            progressSlider.value = displayedProgress;
        }
        else
        {
            progressSlider.value = Progress;
        }
    }

    private void FinishTimer()
    {
        Debug.Log("FinishTimer dipanggil");

        if (IsFinished)
            return;

        IsFinished = true;

        UpdateSliderInstant();

        OnTimerFinished?.Invoke();
    }

    public void ResetTimer()
    {
        remainingTime = levelDuration;
        idleTimer = 0f;
        IsFinished = false;

        displayedProgress = 1f;

        UpdateSliderInstant();
    }

    public void AddTime(float seconds)
    {
        remainingTime = Mathf.Min(levelDuration, remainingTime + seconds);

        if (!smoothSlider)
            UpdateSliderInstant();
    }

    public void ReduceTime(float seconds)
    {
        remainingTime = Mathf.Max(0f, remainingTime - seconds);

        if (!smoothSlider)
            UpdateSliderInstant();

        if (remainingTime <= 0f)
            FinishTimer();
    }
}