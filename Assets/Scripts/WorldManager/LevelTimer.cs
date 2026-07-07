using UnityEngine;
using UnityEngine.UI;
using System;

public class LevelTimer : MonoBehaviour
{
    public static LevelTimer Instance;

    [Header("Level Time")]
    [SerializeField] private float levelDuration = 90f;

    [SerializeField]
    [Range(0f, 1f)]
    private float moveThresholdRatio = 0.4f;

    [Header("UI")]
    [SerializeField] private Image progressImage;

    [Header("Optional")]
    [SerializeField] private bool useGraceTime = true;

    [SerializeField]
    private float graceTime = 2f;

    public event Action OnTimerFinished;

    private float remainingTime;
    private float idleTimer;

    public bool IsFinished { get; private set; }

    public float RemainingTime => remainingTime;

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
        remainingTime = levelDuration;
        UpdateUI();
    }

    private void Update()
    {
        if (IsFinished)
            return;

        bool moving =
            WorldSpeedManager.Instance.GetSpeedRatio() >= moveThresholdRatio;

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
            {
                TickTimer();
            }
        }
    }

    private void TickTimer()
    {
        remainingTime -= Time.deltaTime;

        remainingTime = Mathf.Max(0f, remainingTime);

        UpdateUI();

        if (remainingTime <= 0f)
        {
            FinishTimer();
        }
    }

    private void UpdateUI()
    {
        if (progressImage != null)
            progressImage.fillAmount = remainingTime / levelDuration;
    }

    private void FinishTimer()
    {
        if (IsFinished)
            return;

        IsFinished = true;

        UpdateUI();

        OnTimerFinished?.Invoke();

        Debug.Log("Timer Finished");
    }

    public void ResetTimer()
    {
        remainingTime = levelDuration;
        idleTimer = 0f;
        IsFinished = false;

        UpdateUI();
    }

    public void AddTime(float seconds)
    {
        remainingTime += seconds;
        remainingTime = Mathf.Min(remainingTime, levelDuration);

        UpdateUI();
    }

    public void ReduceTime(float seconds)
    {
        remainingTime -= seconds;
        remainingTime = Mathf.Max(0f, remainingTime);

        UpdateUI();

        if (remainingTime <= 0f)
            FinishTimer();
    }
}