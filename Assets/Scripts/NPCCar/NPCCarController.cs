using UnityEngine;

/// <summary>
/// ===============================================================
/// NPCCarController
/// ===============================================================
/// Controller utama seluruh NPC Traffic.
///
/// Bertanggung jawab atas:
/// • Movement dasar NPC
/// • Brake behaviour
/// • Crosswalk behaviour
/// • Cooperative Merge Speed
/// • Lane Registry
/// • Destroy System
///
/// TIDAK menangani:
/// • Deteksi ambulance
/// • AI merge
/// • Reservation
///
/// Semua AI berada pada:
/// NPCAmbulanceAvoidance
/// MergeCoordinator
/// NPCMergeAssistant
/// ===============================================================
/// </summary>
[RequireComponent(typeof(NPCMergeAssistant))]
public class NPCCarController : MonoBehaviour
{
    //=========================================================
    // ENUM
    //=========================================================

    public enum TrafficType
    {
        SameDirection,
        TowardPlayer
    }

    //=========================================================
    // INSPECTOR
    //=========================================================

    [Header("Traffic")]

    public TrafficType trafficType =
        TrafficType.TowardPlayer;

    [Header("Lane")]

    public LaneMarker.LaneId laneId =
        LaneMarker.LaneId.Lane1;

    //---------------------------------------------------------

    [Header("Speed")]

    public float sameDirectionSpeedMultiplier = 0.95f;

    public float towardPlayerSpeedMultiplier = 1.35f;

    public float crosswalkSpeedMultiplier = 1f;

    //---------------------------------------------------------

    [Header("Brake")]

    public float sameDirectionBrakeMultiplier = 1.45f;

    [Range(0f,1f)]
    public float crosswalkCautiousBrakeMultiplier = 0.5f;

    //---------------------------------------------------------

    [Header("Acceleration")]

    public float accelerateRate = 8f;

    public float decelerateRate = 5f;

    //---------------------------------------------------------

    [Header("Destroy")]

    public float destroyBackZ = -30f;

    public float destroyFrontZ = 120f;

    //=========================================================
    // REFERENCE
    //=========================================================

    [Header("Reference")]

    public PlayerCarController playerCar;

    private NPCMergeAssistant mergeAssistant;

    private NPCAmbulanceAvoidance ambulanceAvoidance;

    //=========================================================
    // RUNTIME
    //=========================================================

    private float spawnZ;

    private bool hasMovedBackwardFromSpawn;

    private bool previousBrake;

    private float cachedBrakeWorldSpeed;

    private float currentSameDirectionSpeed;

    //---------------------------------------------------------
    // Crosswalk
    //---------------------------------------------------------

    private bool stoppedByCrosswalk;

    private PedestrianCrosswalkZone currentCrosswalkZone;

    //---------------------------------------------------------
    // Ambulance
    //---------------------------------------------------------

    private bool insideNoStopZone;

    //=========================================================
    // PROPERTIES
    //=========================================================

    float WorldSpeed
    {
        get
        {
            if (WorldSpeedManager.Instance == null)
                return 10f;

            return WorldSpeedManager.Instance
                .GetCurrentWorldSpeed();
        }
    }

    //---------------------------------------------------------

    float MergeMultiplier
    {
        get
        {
            if (mergeAssistant == null)
                return 1f;

            return mergeAssistant.GetSpeedMultiplier();
        }
    }

    //=========================================================
    // UNITY
    //=========================================================

   private void Awake()
    {
        mergeAssistant =
            GetComponent<NPCMergeAssistant>();

        gapSensor =
            GetComponent<NPCGapSensor>();

        ambulanceAvoidance =
            GetComponent<NPCAmbulanceAvoidance>();
    }

    //---------------------------------------------------------

    private void Start()
    {
        spawnZ =
            transform.position.z;

        currentSameDirectionSpeed =
            WorldSpeed *
            sameDirectionSpeedMultiplier;
    }

    //---------------------------------------------------------

    //====================================================
    // GAP SENSOR
    //====================================================

    void HandleGapMerge()
    {
        if (gapSensor == null)
            return;

        if (ambulanceAvoidance == null)
            return;

        if (!ambulanceAvoidance.IsWaitingGap())
            return;

        if (!gapSensor.IsGapAvailable())
            return;

        ambulanceAvoidance.BeginMergeFromGapSensor();
    }

    private void OnEnable()
    {
        LaneRegistry.Instance.Register(this);
    }

    //---------------------------------------------------------

    private void OnDisable()
    {
        if (LaneRegistry.Instance != null)
        {
            LaneRegistry.Instance.Unregister(this);
        }
    }

    //=========================================================
    // UPDATE
    //=========================================================

    private void Update()
    {
        if (Time.timeScale == 0f)
            return;

        if (playerCar == null)
            return;

            HandleGapMerge();

        bool braking =
            playerCar.IsBraking();

        if (braking && !previousBrake)
        {
            cachedBrakeWorldSpeed =
                WorldSpeed;
        }

        previousBrake = braking;

        switch (trafficType)
        {
            case TrafficType.SameDirection:

                HandleSameDirection(braking);

                break;

            case TrafficType.TowardPlayer:

                HandleTowardPlayer(braking);

                break;
        }
    }
    //=========================================================
    // SAME DIRECTION
    //=========================================================

    void HandleSameDirection(bool braking)
    {
        bool crosswalkActive = false;

        float targetSpeed =
            CalculateSameDirectionTargetSpeed(
                braking,
                crosswalkActive);

        UpdateSameDirectionSpeed(targetSpeed);

        if (braking)
        {
            MoveForward(currentSameDirectionSpeed);

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
            MoveBackward(currentSameDirectionSpeed);

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

    //=========================================================
    // TOWARD PLAYER
    //=========================================================

    void HandleTowardPlayer(bool braking)
    {
        if (stoppedByCrosswalk)
        {
            HandleCrosswalkMovement();
            return;
        }

        float speed =
            WorldSpeed *
            towardPlayerSpeedMultiplier *
            MergeMultiplier;

        MoveBackward(speed);

        if (transform.position.z < destroyBackZ)
        {
            Destroy(gameObject);
        }
    }

    //=========================================================
    // CROSSWALK MOVEMENT
    //=========================================================

    void HandleCrosswalkMovement()
    {
        float speed;

        if (currentCrosswalkZone != null)
        {
            speed =
                currentCrosswalkZone.GetCurrentMoveSpeed()
                *
                crosswalkSpeedMultiplier
                *
                MergeMultiplier;
        }
        else
        {
            speed =
                WorldSpeed *
                crosswalkSpeedMultiplier *
                MergeMultiplier;
        }

        MoveBackward(speed);

        if (transform.position.z < destroyBackZ)
        {
            Destroy(gameObject);
        }
    }

    //=========================================================
    // SPEED CALCULATION
    //=========================================================

    float CalculateSameDirectionTargetSpeed(
        bool braking,
        bool crosswalkActive)
    {
        float targetSpeed;

        if (braking)
        {
            targetSpeed =
                WorldSpeed *
                sameDirectionSpeedMultiplier *
                sameDirectionBrakeMultiplier;

            if (crosswalkActive)
            {
                targetSpeed *=
                    crosswalkCautiousBrakeMultiplier;
            }
        }
        else
        {
            targetSpeed =
                WorldSpeed *
                sameDirectionSpeedMultiplier;
        }

        targetSpeed *= MergeMultiplier;

        return targetSpeed;
    }

    //=========================================================
    // SPEED SMOOTHING
    //=========================================================

    void UpdateSameDirectionSpeed(
        float targetSpeed)
    {
        float rate =
            targetSpeed > currentSameDirectionSpeed
            ?
            accelerateRate
            :
            decelerateRate;

        if (IsHelpingMerge())
        {
            currentSameDirectionSpeed = targetSpeed;
        }
        else
        {
            currentSameDirectionSpeed =
                Mathf.MoveTowards(
                    currentSameDirectionSpeed,
                    targetSpeed,
                    rate * Time.deltaTime);
        }
    }

    //=========================================================
    // MOVEMENT
    //=========================================================

    void MoveForward(float speed)
    {
        transform.Translate(
            Vector3.forward *
            speed *
            Time.deltaTime,
            Space.World);
    }

    //---------------------------------------------------------

    void MoveBackward(float speed)
    {
        transform.Translate(
            Vector3.back *
            speed *
            Time.deltaTime,
            Space.World);
    }

    //=========================================================
    // DESTROY CHECK
    //=========================================================

    bool IsOutBackBoundary()
    {
        return
            transform.position.z <
            destroyBackZ;
    }

    //---------------------------------------------------------

    bool IsOutFrontBoundary()
    {
        return
            transform.position.z >
            destroyFrontZ;
    }

    //---------------------------------------------------------

    bool HasReturnedPastSpawn()
    {
        return
            hasMovedBackwardFromSpawn &&
            transform.position.z >= spawnZ;
    }

        //=========================================================
    // CROSSWALK API
    //=========================================================

    /// <summary>
    /// Mengaktifkan / menonaktifkan mode mengikuti Crosswalk.
    /// Hanya berlaku untuk TowardPlayer.
    /// </summary>
    public void SetStoppedByCrosswalk(
        bool value,
        PedestrianCrosswalkZone zone = null)
    {
        if (trafficType ==
            TrafficType.SameDirection)
        {
            return;
        }

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

    //---------------------------------------------------------

    public bool IsStoppedByCrosswalk()
    {
        return stoppedByCrosswalk;
    }

    //---------------------------------------------------------

    public PedestrianCrosswalkZone
        GetCurrentCrosswalkZone()
    {
        return currentCrosswalkZone;
    }

    //=========================================================
    // AMBULANCE API
    //=========================================================

    /// <summary>
    /// Setelah NPC selesai merge dari Lane2 ke Lane1,
    /// NPC harus berperilaku seperti SameDirection asli.
    /// </summary>
    public void ApplySameDirectionBehavior()
    {
        trafficType =
            TrafficType.SameDirection;

        stoppedByCrosswalk = false;

        currentCrosswalkZone = null;
    }

    //---------------------------------------------------------

    public bool IsInsideNoStopZone()
    {
        return insideNoStopZone;
    }

    //---------------------------------------------------------

    public void SetInsideNoStopZone(bool value)
    {
        insideNoStopZone = value;
    }

    //---------------------------------------------------------

    public bool IsAvoidingAmbulance()
    {
        if (ambulanceAvoidance == null)
            return false;

        return ambulanceAvoidance.CurrentState !=
            NPCAmbulanceAvoidance
            .AvoidanceState.Normal;
    }

    //=========================================================
    // MERGE ASSIST
    //=========================================================

    public NPCMergeAssistant
        GetMergeAssistant()
    {
        return mergeAssistant;
    }

    //---------------------------------------------------------

    public bool IsHelpingMerge()
    {
        if (mergeAssistant == null)
            return false;

        return mergeAssistant.IsAssisting();
    }

    //---------------------------------------------------------

    public float GetMergeSpeedMultiplier()
    {
        if (mergeAssistant == null)
            return 1f;

        return mergeAssistant.GetSpeedMultiplier();
    }

    //---------------------------------------------------------

    public bool IsForwardAssist()
    {
        if (mergeAssistant == null)
            return false;

        return mergeAssistant.IsForwardAssist();
    }

    //---------------------------------------------------------

    public bool IsRearAssist()
    {
        if (mergeAssistant == null)
            return false;

        return mergeAssistant.IsRearAssist();
    }

    //====================================================
    // GAP SENSOR
    //====================================================

    private NPCGapSensor gapSensor;

    //=========================================================
    // SPEED API
    //=========================================================

    public float GetCurrentSpeed()
    {
        switch (trafficType)
        {
            case TrafficType.SameDirection:
                return currentSameDirectionSpeed;

            case TrafficType.TowardPlayer:

                return
                    WorldSpeed *
                    towardPlayerSpeedMultiplier *
                    MergeMultiplier;

            default:

                return WorldSpeed;
        }
    }

    //---------------------------------------------------------

    public float GetWorldSpeed()
    {
        return WorldSpeed;
    }

    //---------------------------------------------------------

    public void ForceSameDirectionSpeed(
        float value)
    {
        currentSameDirectionSpeed =
            Mathf.Max(0f, value);
    }

    //=========================================================
    // LANE API
    //=========================================================

    public LaneMarker.LaneId GetLane()
    {
        return laneId;
    }

    //---------------------------------------------------------

    public void SetLane(
        LaneMarker.LaneId lane)
    {
        laneId = lane;
    }

    //---------------------------------------------------------

    public bool IsSameDirection()
    {
        return
            trafficType ==
            TrafficType.SameDirection;
    }

    //---------------------------------------------------------

    public bool IsTowardPlayer()
    {
        return
            trafficType ==
            TrafficType.TowardPlayer;
    }

    //=========================================================
    // DEBUG
    //=========================================================

#if UNITY_EDITOR

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;

        Gizmos.DrawWireSphere(
            transform.position,
            0.5f);

        if (mergeAssistant != null &&
            mergeAssistant.IsAssisting())
        {
            Gizmos.color = Color.magenta;

            Gizmos.DrawLine(
                transform.position,
                transform.position +
                Vector3.up * 3f);
        }
    }

#endif


    //=========================================================
    // INTERNAL RESET
    //=========================================================

    /// <summary>
    /// Digunakan ketika NPC selesai merge atau di-respawn.
    /// Mengembalikan seluruh state runtime ke kondisi normal.
    /// </summary>
    public void ResetRuntimeState()
    {
        stoppedByCrosswalk = false;

        currentCrosswalkZone = null;

        insideNoStopZone = false;

        hasMovedBackwardFromSpawn = false;

        previousBrake = false;

        cachedBrakeWorldSpeed = 0f;

        currentSameDirectionSpeed =
            WorldSpeed *
            sameDirectionSpeedMultiplier;

        if (mergeAssistant != null)
        {
            mergeAssistant.ResetAssist();
        }
    }

    //=========================================================
    // FORCE API
    //=========================================================

    public void ForceTrafficType(
        TrafficType type)
    {
        trafficType = type;
    }

    //---------------------------------------------------------

    public void ForceDestroy()
    {
        Destroy(gameObject);
    }

    //---------------------------------------------------------

    public void ForceRefreshSpeed()
    {
        currentSameDirectionSpeed =
            WorldSpeed *
            sameDirectionSpeedMultiplier;
    }

    //---------------------------------------------------------

    public void ForceRefreshMergeAssistant()
    {
        if (mergeAssistant == null)
        {
            mergeAssistant =
                GetComponent<NPCMergeAssistant>();
        }
    }

    //---------------------------------------------------------

    public void ForceRefreshAvoidance()
    {
        if (ambulanceAvoidance == null)
        {
            ambulanceAvoidance =
                GetComponent<NPCAmbulanceAvoidance>();
        }
    }

    //=========================================================
    // VALIDATION
    //=========================================================

    private void OnValidate()
    {
        accelerateRate =
            Mathf.Max(0.01f, accelerateRate);

        decelerateRate =
            Mathf.Max(0.01f, decelerateRate);

        sameDirectionSpeedMultiplier =
            Mathf.Max(0f,
            sameDirectionSpeedMultiplier);

        towardPlayerSpeedMultiplier =
            Mathf.Max(0f,
            towardPlayerSpeedMultiplier);

        crosswalkSpeedMultiplier =
            Mathf.Max(0f,
            crosswalkSpeedMultiplier);

        sameDirectionBrakeMultiplier =
            Mathf.Max(0f,
            sameDirectionBrakeMultiplier);

        destroyBackZ =
            Mathf.Min(destroyBackZ,
            destroyFrontZ - 1f);
    }

    //=========================================================
    // DEBUG INFO
    //=========================================================

    public override string ToString()
    {
        return
            "[NPC] Lane : " + laneId +
            " | Type : " + trafficType +
            " | Speed : " + GetCurrentSpeed().ToString("F2") +
            " | Merge : " +
            (mergeAssistant != null &&
             mergeAssistant.IsAssisting());
    }

    //=========================================================
    // END
    //=========================================================
}