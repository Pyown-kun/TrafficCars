using System;
using UnityEngine;
using UnityEngine.UI;

public class TofuQuality : MonoBehaviour
{
    public static TofuQuality Instance;

    [Header("Quality")]
    [SerializeField] private float maxQuality = 100f;
    [SerializeField] private float currentQuality;

    [Header("UI")]
    [SerializeField] private Image qualityFillImage;

    public event Action<float> OnQualityChanged;
    public event Action OnQualityEmpty;

    public float CurrentQuality => currentQuality;
    public float MaxQuality => maxQuality;
    public bool IsEmpty => currentQuality <= 0f;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    private void Start()
    {
        ResetQuality();
    }

    public void ReduceQuality(float value)
    {
        if (IsEmpty)
            return;

        currentQuality -= value;
        currentQuality = Mathf.Clamp(currentQuality, 0f, maxQuality);

        UpdateUI();

        OnQualityChanged?.Invoke(currentQuality);

        if (currentQuality <= 0f)
        {
            OnQualityEmpty?.Invoke();
        }
    }

    public void RestoreQuality(float value)
    {
        if (IsEmpty)
            return;

        currentQuality += value;
        currentQuality = Mathf.Clamp(currentQuality, 0f, maxQuality);

        UpdateUI();

        OnQualityChanged?.Invoke(currentQuality);
    }

    public void ResetQuality()
    {
        currentQuality = maxQuality;

        UpdateUI();

        OnQualityChanged?.Invoke(currentQuality);
    }

    private void UpdateUI()
    {
        if (qualityFillImage != null)
        {
            qualityFillImage.fillAmount = currentQuality / maxQuality;
        }
    }
}