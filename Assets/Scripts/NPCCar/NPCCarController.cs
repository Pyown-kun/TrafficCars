using UnityEngine;

public class NPCCarController : MonoBehaviour
{
    public enum TrafficType
    {
        SameDirection,
        TowardPlayer
    }

    [Header("Traffic Type")]
    public TrafficType trafficType = TrafficType.TowardPlayer;

    [Header("Lane (Ambulance Avoidance)")]
    public LaneMarker.LaneId laneId = LaneMarker.LaneId.Lane1;

    //====================================================
    // SPEED MULTIPLIER
    //====================================================

    [Header("Speed Multiplier")]

    [Tooltip("Multiplier NPC searah terhadap World Speed")]
    public float sameDirectionSpeedMultiplier = 0.95f;

    [Tooltip("Multiplier NPC lawan arah terhadap World Speed")]
    public float towardPlayerSpeedMultiplier = 1.35f;

    [Tooltip("Multiplier NPC ketika mengikuti Crosswalk")]
    public float crosswalkSpeedMultiplier = 1.0f;

    [Header("Brake")]

    [Tooltip("Multiplier SameDirection saat player brake")]
    public float sameDirectionBrakeMultiplier = 1.45f;

    [Tooltip("Jika brake saat crosswalk aktif, speed brake dikurangi")]
    [Range(0f, 1f)]
    public float crosswalkCautiousBrakeMultiplier = 0.5f;

    [Header("Same Direction Transition")]

    [Tooltip("Percepatan menuju target speed")]
    public float accelerateRate = 8f;

    [Tooltip("Perlambatan menuju target speed")]
    public float decelerateRate = 5f;

    private float currentSameDirectionSpeed;
    private float cachedBrakeWorldSpeed;
    private bool previousBrake;

    //====================================================

    [Header("Destroy Range")]
    public float destroyBackZ = -30f;
    public float destroyFrontZ = 120f;

    [Header("Reference")]
    public PlayerCarController playerCar;

    private float spawnZ;
    private bool hasMovedBackwardFromSpawn = false;

    //====================================================
    // CROSSWALK
    //====================================================

    private bool stoppedByCrosswalk = false;
    private PedestrianCrosswalkZone currentCrosswalkZone;

    //====================================================

    private bool insideNoStopZone;

    private void Start()
    {
        spawnZ = transform.position.z;

        currentSameDirectionSpeed =
            WorldSpeed * sameDirectionSpeedMultiplier;
    }

    /// <summary>
    /// Base world speed dari WorldSpeedManager.
    /// Semua movement NPC berasal dari sini.
    /// </summary>
    float WorldSpeed
    {
        get
        {
            if (WorldSpeedManager.Instance == null)
                return 10f;

            return WorldSpeedManager.Instance.GetCurrentWorldSpeed();
        }
    }

    private void Update()
    {
        if (Time.timeScale == 0f)
            return;

        if (playerCar == null)
            return;

        bool isBraking = playerCar.IsBraking();

        if (isBraking && !previousBrake)
        {
            cachedBrakeWorldSpeed = WorldSpeed;
        }

        previousBrake = isBraking;

        switch (trafficType)
        {
            case TrafficType.SameDirection:

                // SameDirection SELALU menggunakan movement normal.
                // Crosswalk tidak pernah mengambil alih movement.
                HandleSameDirectionNPC(isBraking, false);
                break;

            case TrafficType.TowardPlayer:

                // Hanya TowardPlayer yang mengikuti crosswalk.
                if (stoppedByCrosswalk)
                {
                    FollowCrosswalkWhileStopped();
                    return;
                }

                HandleTowardPlayerNPC(isBraking);
                break;
        }
    }

        void HandleSameDirectionNPC(bool isBraking, bool crosswalkActive)
    {
        float targetSpeed;

        //--------------------------------------------------
        // Menentukan target speed
        //--------------------------------------------------

        if (isBraking)
        {
            targetSpeed =
                cachedBrakeWorldSpeed *
                sameDirectionBrakeMultiplier;

            if (crosswalkActive)
            {
                targetSpeed *= crosswalkCautiousBrakeMultiplier;
            }
        }
        else
        {
            targetSpeed =
                WorldSpeed *
                sameDirectionSpeedMultiplier;
        }

        //--------------------------------------------------
        // Acceleration / Deceleration
        //--------------------------------------------------

        float rate =
            targetSpeed > currentSameDirectionSpeed
            ? accelerateRate
            : decelerateRate;

        currentSameDirectionSpeed = Mathf.MoveTowards(
            currentSameDirectionSpeed,
            targetSpeed,
            rate * Time.deltaTime);

        //--------------------------------------------------
        // Movement
        //--------------------------------------------------

        if (isBraking)
        {
            transform.Translate(
                Vector3.forward *
                currentSameDirectionSpeed *
                Time.deltaTime,
                Space.World);

            if (hasMovedBackwardFromSpawn &&
                transform.position.z >= spawnZ)
            {
                Destroy(gameObject);
                return;
            }

            if (transform.position.z > destroyFrontZ)
            {
                Destroy(gameObject);
                return;
            }
        }
        else
        {
            transform.Translate(
                Vector3.back *
                currentSameDirectionSpeed *
                Time.deltaTime,
                Space.World);

            if (transform.position.z < spawnZ)
            {
                hasMovedBackwardFromSpawn = true;
            }

            if (transform.position.z < destroyBackZ)
            {
                Destroy(gameObject);
                return;
            }
        }
    }

    void HandleTowardPlayerNPC(bool isBraking)
    {
        // TowardPlayer sekarang mengikuti WorldSpeed.
        // Ketika player brake, WorldSpeedManager yang
        // mengurangi kecepatannya sehingga tidak perlu
        // multiplier brake lagi di sini.

        float moveSpeed =
            WorldSpeed * towardPlayerSpeedMultiplier;

        transform.Translate(
            Vector3.back * moveSpeed * Time.deltaTime,
            Space.World);

        if (transform.position.z < destroyBackZ)
        {
            Destroy(gameObject);
            return;
        }
    }

    void FollowCrosswalkWhileStopped()
    {
        float crosswalkSpeed;

        if (currentCrosswalkZone != null)
        {
            crosswalkSpeed =
                currentCrosswalkZone.GetCurrentMoveSpeed()
                * crosswalkSpeedMultiplier;
        }
        else
        {
            crosswalkSpeed =
                WorldSpeed * crosswalkSpeedMultiplier;
        }

        transform.Translate(
            Vector3.back * crosswalkSpeed * Time.deltaTime,
            Space.World);

        if (transform.position.z < destroyBackZ)
        {
            Destroy(gameObject);
        }
    }

        //====================================================
    // CROSSWALK API
    //====================================================

    public void SetStoppedByCrosswalk(bool value, PedestrianCrosswalkZone zone = null)
    {
        // NPC SameDirection tidak pernah dipengaruhi crosswalk.
        if (trafficType == TrafficType.SameDirection)
            return;

        stoppedByCrosswalk = value;

        if (value)
        {
            currentCrosswalkZone = zone;
        }
        else
        {
            currentCrosswalkZone = null;
        }
    }

    public bool IsStoppedByCrosswalk()
    {
        return stoppedByCrosswalk;
    }

    //====================================================
    // AMBULANCE AVOIDANCE
    //====================================================

    /// <summary>
    /// Dipanggil setelah NPC merge ke Lane1.
    /// NPC akan berubah menjadi SameDirection dan
    /// mengabaikan seluruh logic crosswalk.
    /// </summary>
    public void ApplySameDirectionBehavior()
    {
        trafficType = TrafficType.SameDirection;

        // Reset seluruh status crosswalk
        stoppedByCrosswalk = false;
        currentCrosswalkZone = null;
    }

    public bool IsInsideNoStopZone()
    {
        return insideNoStopZone;
    }

    public void SetInsideNoStopZone(bool value)
    {
        insideNoStopZone = value;
    }
}