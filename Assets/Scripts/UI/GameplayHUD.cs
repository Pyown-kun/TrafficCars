using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameplayHUD : MonoBehaviour
{
    [Header("Health UI")]
    [SerializeField] private Image healthFill;
    [SerializeField] private TMP_Text healthPercentText;

    [Header("Tofu Quality UI")]
    [SerializeField] private Image tofuFill;
    [SerializeField] private TMP_Text tofuPercentText;

    [Header("Violation UI")]
    [SerializeField] private TMP_Text violationText;

    [Header("References")]
    [SerializeField] private VehicleHealth vehicleHealth;
    [SerializeField] private TofuQuality tofuQuality;
    [SerializeField] private ViolationManager violationManager;

    private void Awake()
    {
        if (vehicleHealth == null)
            vehicleHealth = VehicleHealth.Instance;

        if (tofuQuality == null)
            tofuQuality = TofuQuality.Instance;

        if (violationManager == null)
            violationManager = FindFirstObjectByType<ViolationManager>();
    }

    private void OnEnable()
    {
        if (vehicleHealth != null)
            vehicleHealth.OnHealthChanged += UpdateHealthUI;

        if (tofuQuality != null)
            tofuQuality.OnQualityChanged += UpdateTofuUI;

        if (violationManager != null)
            violationManager.OnPenaltyChanged += UpdatePenaltyUI;
    }

    private void Start()
    {
        if (vehicleHealth != null)
            UpdateHealthUI(vehicleHealth.CurrentHealth);

        if (tofuQuality != null)
            UpdateTofuUI(tofuQuality.CurrentQuality);

        if (violationManager != null)
            UpdatePenaltyUI(violationManager.TotalPenalty);
    }

    private void OnDisable()
    {
        if (vehicleHealth != null)
            vehicleHealth.OnHealthChanged -= UpdateHealthUI;

        if (tofuQuality != null)
            tofuQuality.OnQualityChanged -= UpdateTofuUI;

        if (violationManager != null)
            violationManager.OnPenaltyChanged -= UpdatePenaltyUI;
    }

    private void UpdateHealthUI(float currentHealth)
    {
        float max = vehicleHealth.MaxHealth;
        float percent = currentHealth / max;

        if (healthFill != null)
            healthFill.fillAmount = percent;

        if (healthPercentText != null)
            healthPercentText.text = $"{Mathf.RoundToInt(percent * 100)}%";
    }

    private void UpdateTofuUI(float currentQuality)
    {
        float max = tofuQuality.MaxQuality;
        float percent = currentQuality / max;

        if (tofuFill != null)
            tofuFill.fillAmount = percent;

        if (tofuPercentText != null)
            tofuPercentText.text = $"{Mathf.RoundToInt(percent * 100)}%";
    }

    private void UpdatePenaltyUI(int totalPenalty)
    {
        if (violationText != null)
            violationText.text = $"{totalPenalty:N0}";
    }
}