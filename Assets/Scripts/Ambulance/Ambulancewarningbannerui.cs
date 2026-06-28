using UnityEngine;
using UnityEngine.UI;

public class AmbulanceWarningBannerUI : MonoBehaviour
{
    [Header("References")]
    public AmbulanceRearWarningTrigger triggerReference;

    public GameObject bannerRoot;

    public Text warningText;

    [Header("Message")]
    public string warningMessage =
        "AMBULANCE MENDEKAT DARI BELAKANG!";

    private void Awake()
    {
        if (bannerRoot != null)
        {
            bannerRoot.SetActive(false);
        }

        if (warningText != null)
        {
            warningText.text = warningMessage;
        }

        if (triggerReference == null)
        {
            Debug.LogWarning(
                $"[{nameof(AmbulanceWarningBannerUI)}] " +
                $"{name} belum memiliki Trigger Reference."
            );
        }
    }

    private void OnEnable()
    {
        if (triggerReference == null)
            return;

        triggerReference.OnWarningStateChanged += HandleWarningStateChanged;
    }

    private void Start()
    {
        if (triggerReference == null)
            return;

        ApplyBannerState(triggerReference.IsWarningActive);
    }

    private void OnDisable()
    {
        if (triggerReference == null)
            return;

        triggerReference.OnWarningStateChanged -= HandleWarningStateChanged;
    }

    private void HandleWarningStateChanged(bool active)
    {
        ApplyBannerState(active);
    }

    private void ApplyBannerState(bool active)
    {
        if (bannerRoot == null)
            return;

        bannerRoot.SetActive(active);
    }
}