using UnityEngine;

/// <summary>
/// Base class seluruh object yang bergerak mengikuti WorldSpeed.
/// Menyediakan acceleration, deceleration,
/// dan cached brake speed.
/// </summary>
public abstract class WorldSpeedObject : MonoBehaviour
{
    [Header("Speed")]

    [SerializeField]
    protected float speedMultiplier = 1f;

    [SerializeField]
    protected float brakeSpeedMultiplier = 1f;

    [Header("Transition")]

    [SerializeField]
    protected float accelerateRate = 8f;

    [SerializeField]
    protected float decelerateRate = 6f;

    protected float currentSpeed;
    protected float cachedBrakeWorldSpeed;

    bool previousBrake;

    protected float WorldSpeed
    {
        get
        {
            if (WorldSpeedManager.Instance == null)
                return 10f;

            return WorldSpeedManager.Instance.GetCurrentWorldSpeed();
        }
    }

    protected virtual void Start()
    {
        currentSpeed = WorldSpeed * speedMultiplier;
    }

    /// <summary>
    /// Menghasilkan speed yang sudah dihaluskan.
    /// </summary>
    protected float GetSmoothSpeed(bool braking)
    {
        if (braking && !previousBrake)
        {
            cachedBrakeWorldSpeed = WorldSpeed;
        }

        previousBrake = braking;

        float targetSpeed =
            braking
            ? cachedBrakeWorldSpeed * brakeSpeedMultiplier
            : WorldSpeed * speedMultiplier;

        float rate =
            targetSpeed > currentSpeed
            ? accelerateRate
            : decelerateRate;

        currentSpeed = Mathf.MoveTowards(
            currentSpeed,
            targetSpeed,
            rate * Time.deltaTime);

        return currentSpeed;
    }
}