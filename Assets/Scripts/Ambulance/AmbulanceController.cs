using UnityEngine;

public class AmbulanceController : MonoBehaviour
{
    [Header("Reference")]
    public PlayerCarController playerCar;

    [Header("Speed Multiplier")]

    [Tooltip("Kecepatan ambulans terhadap World Speed")]
    public float speedMultiplier = 1.6f;

    [Tooltip("Multiplier saat player mulai brake")]
    public float brakeSpeedMultiplier = 1.8f;

    [Header("Acceleration")]

    [Tooltip("Percepatan menuju target speed")]
    public float accelerateRate = 10f;

    [Tooltip("Perlambatan menuju target speed")]
    public float decelerateRate = 8f;

    [Header("Destroy")]
    public float destroyFrontZ = 140f;

    private float currentSpeed;
    private float cachedBrakeWorldSpeed;
    private bool previousBrake;
    private bool eventFinished = false;

    float WorldSpeed
    {
        get
        {
            if (WorldSpeedManager.Instance == null)
                return 10f;

            return WorldSpeedManager.Instance.GetCurrentWorldSpeed();
        }
    }

    // === TAMBAHAN BARU ===
    // Diisi otomatis oleh AmbulanceTrigger saat instantiate, dibaca oleh
    // NPCAmbulanceAvoidance untuk menentukan arah sensor NPC yang relevan.
    [Header("Lane (untuk Ambulance Avoidance System)")]
    public LaneMarker.LaneId laneId = LaneMarker.LaneId.Lane2;
    // === END TAMBAHAN BARU ===

    private void Start()
    {
        currentSpeed = WorldSpeed * speedMultiplier;
    }

    private void Update()
    {
        if (Time.timeScale == 0f)
            return;

        if (playerCar == null)
            return;

        bool braking = playerCar.IsBraking();

        // Simpan WorldSpeed ketika player baru mulai brake
        if (braking && !previousBrake)
        {
            cachedBrakeWorldSpeed = WorldSpeed;
        }

        previousBrake = braking;

        float targetSpeed;

        if (braking)
        {
            targetSpeed =
                cachedBrakeWorldSpeed *
                brakeSpeedMultiplier;
        }
        else
        {
            targetSpeed =
                WorldSpeed *
                speedMultiplier;
        }

        float rate =
            targetSpeed > currentSpeed
            ? accelerateRate
            : decelerateRate;

        currentSpeed = Mathf.MoveTowards(
            currentSpeed,
            targetSpeed,
            rate * Time.deltaTime);

        transform.Translate(
            Vector3.forward *
            currentSpeed *
            Time.deltaTime,
            Space.World);

        if (transform.position.z > destroyFrontZ)
        {
            eventFinished = true;
            Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        if (!eventFinished)
            return;

        if (WorldEventManager.Instance == null)
            return;

        WorldEventManager.Instance.NotifyEventFinished();
    }
}