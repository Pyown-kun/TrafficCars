using UnityEngine;

public class PlayerCarVisualController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerCarController playerController;
    [SerializeField] private Animator animator;

    [Header("Animator Parameters")]
    [SerializeField] private string horizontalParam = "HorizontalInput";
    [SerializeField] private string isBrakingParam = "IsBraking";
    [SerializeField] private string isCrosswalkLockedParam = "IsCrosswalkLocked";
    [SerializeField] private string playAfterBrakeTrigger = "PlayAfterBrake";

    [Header("Turn Settings")]
    [SerializeField] private float turnThreshold = 0.1f;
    [SerializeField] private float horizontalSmoothSpeed = 12f;

    private float currentHorizontalAnimValue;
    private bool previousManualBraking;

    private void Reset()
    {
        if (playerController == null)
            playerController = GetComponentInParent<PlayerCarController>();

        if (animator == null)
            animator = GetComponent<Animator>();
    }

    private void Awake()
    {
        if (playerController == null)
            playerController = GetComponentInParent<PlayerCarController>();

        if (animator == null)
            animator = GetComponent<Animator>();
    }

    private void Update()
    {
        if (playerController == null || animator == null)
            return;

        UpdateAnimatorStates();
    }

    void UpdateAnimatorStates()
    {
        bool isCrosswalkLocked = playerController.IsMovementLockedByCrosswalk();
        bool isManualBraking = playerController.IsManualBraking();

        float targetHorizontal = 0f;

        // Saat crosswalk lock atau brake manual aktif,
        // paksa turn animator kembali ke netral.
        if (!isCrosswalkLocked && !isManualBraking)
        {
            float rawHorizontal = playerController.GetHorizontalInput();

            if (Mathf.Abs(rawHorizontal) >= turnThreshold)
                targetHorizontal = rawHorizontal;
        }

        currentHorizontalAnimValue = Mathf.MoveTowards(
            currentHorizontalAnimValue,
            targetHorizontal,
            horizontalSmoothSpeed * Time.deltaTime
        );

        animator.SetBool(isCrosswalkLockedParam, isCrosswalkLocked);
        animator.SetBool(isBrakingParam, isManualBraking);
        animator.SetFloat(horizontalParam, currentHorizontalAnimValue);

        // Trigger AfterBrake:
        // frame sebelumnya brake manual = true
        // frame sekarang brake manual = false
        // dan tidak sedang crosswalk lock
        if (previousManualBraking && !isManualBraking && !isCrosswalkLocked)
        {
            animator.SetTrigger(playAfterBrakeTrigger);
        }

        previousManualBraking = isManualBraking;
    }
}