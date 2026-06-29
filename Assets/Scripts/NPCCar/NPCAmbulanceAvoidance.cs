using UnityEngine;

/// <summary>
/// NPC Ambulance Avoidance
///
/// Mengatur state machine NPC ketika ambulance mendekat.
/// Script ini tidak menggerakkan kendaraan.
/// Seluruh movement tetap dilakukan oleh NPCCarController.
/// </summary>
[RequireComponent(typeof(NPCCarController))]
public class NPCAmbulanceAvoidance : MonoBehaviour
{
    //========================================================
    // STATE
    //========================================================

    public enum AvoidanceState
    {
        Normal,
        AmbulanceDetected,
        WaitingForGap,
        Merging,
        Merged
    }

    //========================================================
    // SENSOR
    //========================================================

    [Header("Sensor")]

    [Tooltip("Jarak deteksi ambulance.")]
    public float detectionRange = 25f;

    [Tooltip("Offset origin raycast.")]
    public float sensorHeightOffset = 0.5f;

    [Tooltip("Radius SphereCast.")]
    public float sensorRadius = 1.2f;

    [Tooltip("Layer ambulance.")]
    public LayerMask ambulanceLayerMask = ~0;

    //========================================================
    // MERGE
    //========================================================

    [Header("Merge")]

    public float laneWidth = 3.5f;

    public float mergeDuration = 1.5f;

    public float maxTurnAngle = 15f;

    //========================================================
    // COOPERATIVE MERGE
    //========================================================

    [Header("Cooperative Merge")]

    [Tooltip("Radius reservation pada lane tujuan.")]
    public float mergeCheckRadius = 12f;

    //========================================================
    // VISUAL
    //========================================================

    [Header("Visual")]

    public LineRenderer sensorLineRenderer;

    public TextMesh stateLabel;

    //========================================================
    // GIZMO
    //========================================================

    [Header("Debug")]

    public bool showSensorGizmo = true;

    public bool alwaysShowGizmoRay = true;

    //========================================================
    // PROPERTY
    //========================================================

    public AvoidanceState CurrentState
    {
        get;
        private set;
    } = AvoidanceState.Normal;

    //========================================================
    // COMPONENT
    //========================================================

    private NPCCarController npcController;

    private AmbulanceController detectedAmbulance;

    //========================================================
    // MERGE DATA
    //========================================================

    private float mergeTimer;

    private float mergeStartX;

    private float mergeTargetX;

    private float mergeDirectionSign;

    private LaneMarker.LaneId mergeTargetLane;

    private float waitingTimer;
    public float maxWaitingTime = 1.5f;
    //========================================================
    // REGISTRY
    //========================================================

    private bool isRegistered;

    private LaneMarker.LaneId registeredLane;

    //========================================================
    // RESERVATION
    //========================================================

    private MergeReservation reservation;

    private bool reservationCreated;

    //========================================================
    // ROTATION
    //========================================================

    private bool baseYRotationCached;

    private float baseYRotationCache;

    //========================================================
    // UNITY
    //========================================================

    private void Awake()
    {
        npcController =
            GetComponent<NPCCarController>();
    }

    private void OnDisable()
    {
        RemoveReservation();

        if (!isRegistered)
            return;

        LaneRegistry.Instance.Unregister(
            npcController);

        isRegistered = false;
    }

    //========================================================
    // REGISTER
    //========================================================

    void EnsureRegistered()
    {
        if (npcController == null)
            return;

        if (!isRegistered)
        {
            LaneRegistry.Instance.Register(
                npcController);

            registeredLane =
                npcController.laneId;

            isRegistered = true;

            return;
        }

        if (registeredLane != npcController.laneId)
        {
            LaneRegistry.Instance.Unregister(
                npcController);

            LaneRegistry.Instance.Register(
                npcController);

            registeredLane =
                npcController.laneId;
        }
    }

    //========================================================
    // UPDATE
    //========================================================

    private void Update()
    {

        waitingTimer = 0f;

        if (Time.timeScale == 0f)
            return;

        if (npcController == null)
            return;

        EnsureRegistered();

        bool triggerLane =
            npcController.laneId ==
            LaneMarker.LaneId.Lane2 ||

            npcController.laneId ==
            LaneMarker.LaneId.Lane4;

        if (!triggerLane &&
            CurrentState ==
            AvoidanceState.Normal)
        {
            UpdateVisuals();
            return;
        }

        switch (CurrentState)
        {
            case AvoidanceState.Normal:

                TickNormal();
                break;

            case AvoidanceState.AmbulanceDetected:

                TickAmbulanceDetected();
                break;

            case AvoidanceState.WaitingForGap:

                TickWaitingForGap();
                break;

            case AvoidanceState.Merging:

                TickMerging();
                break;

            case AvoidanceState.Merged:

                CurrentState =
                    AvoidanceState.Normal;
                break;
        }

        UpdateVisuals();
    }

        //========================================================
    // NORMAL
    //========================================================

    void TickNormal()
    {
        AmbulanceController ambulance =
            DetectAmbulance();

        if (ambulance == null)
            return;

        detectedAmbulance = ambulance;

        CurrentState =
            AvoidanceState.AmbulanceDetected;
    }

    //========================================================
    // AMBULANCE DETECTED
    //========================================================

    void TickAmbulanceDetected()
    {
        if (!IsAmbulanceStillRelevant())
        {
            RemoveReservation();

            detectedAmbulance = null;

            CurrentState =
                AvoidanceState.Normal;

            return;
        }

        if (!reservationCreated)
        {
            CreateMergeReservation();
        }

        CurrentState =
            AvoidanceState.WaitingForGap;
    }

    //========================================================
    // WAITING FOR GAP
    //========================================================

    void TickWaitingForGap()
    {

        waitingTimer += Time.deltaTime;

        if (!IsAmbulanceStillRelevant())
        {
            RemoveReservation();

            detectedAmbulance = null;

            CurrentState =
                AvoidanceState.Normal;

            return;
        }

        //------------------------------------------
        // Reservation selalu mengikuti posisi NPC
        //------------------------------------------

        UpdateReservation();

        LaneRegistry.Instance.UpdateReservation(
            reservation);

        //------------------------------------------
        // Cari blocker pada lane tujuan
        //------------------------------------------

        LaneMarker.LaneId targetLane =
            LaneMarker.GetMergeTargetLane(
                npcController.laneId);

        NPCCarController frontNpc =
            LaneRegistry.Instance.FindClosestAhead(
                targetLane,
                transform.position.z,
                npcController);

        NPCCarController rearNpc =
            LaneRegistry.Instance.FindClosestBehind(
                targetLane,
                transform.position.z,
                npcController);

        bool frontReady = true;
        bool rearReady = true;

        if (frontNpc != null)
        {
            float frontGap =
                transform.position.z -
                frontNpc.transform.position.z;

            frontReady = frontGap >= 7f;
        }

        if (rearNpc != null)
        {
            float rearGap =
                rearNpc.transform.position.z -
                transform.position.z;

            rearReady = rearGap >= 7f;
        }

        if (frontReady && rearReady)
        {
            StartMerge(targetLane);
        }
    }

    //========================================================
    // START MERGE
    //========================================================

    void StartMerge(
        LaneMarker.LaneId targetLane)
    {
        mergeTargetLane =
            targetLane;

        mergeDirectionSign =
            LaneMarker.GetMergeDirectionSign(
                npcController.laneId);

        mergeStartX =
            transform.position.x;

        mergeTargetX =
            mergeStartX +
            mergeDirectionSign * laneWidth;

        mergeTimer = 0f;

        //------------------------------------------
        // Beritahu coordinator bahwa merge dimulai
        //------------------------------------------

        if (reservation != null)
        {
            LaneRegistry.Instance.NotifyMergeStarted(
                reservation);
        }

        CurrentState =
            AvoidanceState.Merging;
    }

        //========================================================
    // MERGING
    //========================================================

    void TickMerging()
    {
        mergeTimer += Time.deltaTime;

        float t =
            Mathf.Clamp01(
                mergeTimer /
                mergeDuration);

        //------------------------------------------
        // Lerp posisi X
        //------------------------------------------

        float newX =
            Mathf.Lerp(
                mergeStartX,
                mergeTargetX,
                t);

        Vector3 pos =
            transform.position;

        pos.x = newX;

        transform.position = pos;

        //------------------------------------------
        // Rotasi kendaraan
        //------------------------------------------

        float turn =
            Mathf.Sin(t * Mathf.PI) *
            maxTurnAngle *
            mergeDirectionSign;

        transform.rotation =
            Quaternion.Euler(
                0f,
                GetBaseYRotation() + turn,
                0f);

        //------------------------------------------

        if (t >= 1f)
        {
            FinishMerge();
        }
    }

    //========================================================
    // BASE ROTATION
    //========================================================

    float GetBaseYRotation()
    {
        if (!baseYRotationCached)
        {
            baseYRotationCache =
                transform.eulerAngles.y;

            baseYRotationCached = true;
        }

        return baseYRotationCache;
    }

    //========================================================
    // FINISH MERGE
    //========================================================

    void FinishMerge()
    {
        //------------------------------------------
        // Snap posisi akhir
        //------------------------------------------

        Vector3 pos =
            transform.position;

        pos.x =
            mergeTargetX;

        transform.position =
            pos;

        transform.rotation =
            Quaternion.Euler(
                0f,
                GetBaseYRotation(),
                0f);

        //------------------------------------------
        // Update Lane Registry
        //------------------------------------------

        LaneRegistry.Instance.Unregister(
            npcController);

        npcController.laneId =
            mergeTargetLane;

        LaneRegistry.Instance.Register(
            npcController);

        registeredLane =
            mergeTargetLane;

        //------------------------------------------
        // Sinkronisasi behaviour lane baru
        //------------------------------------------

        switch (mergeTargetLane)
        {
            case LaneMarker.LaneId.Lane1:

                npcController.ApplySameDirectionBehavior();
                break;

            case LaneMarker.LaneId.Lane5:

                // Lane5 tetap menggunakan
                // behaviour TowardPlayer.
                break;
        }

        //------------------------------------------
        // Bersihkan reservation
        //------------------------------------------

        RemoveReservation();

        //------------------------------------------

        detectedAmbulance = null;

        mergeTimer = 0f;

        baseYRotationCached = false;

        CurrentState =
            AvoidanceState.Merged;
    }

    //========================================================
    // DETECT AMBULANCE
    //========================================================

    AmbulanceController DetectAmbulance()
    {
        Vector3 direction =
            Vector3.back;

        Vector3 origin =
            transform.position +
            Vector3.up *
            sensorHeightOffset;

        RaycastHit[] hits =
            Physics.SphereCastAll(
                origin,
                sensorRadius,
                direction,
                detectionRange,
                ambulanceLayerMask);

        AmbulanceController closest = null;

        float closestDistance =
            float.MaxValue;

        foreach (RaycastHit hit in hits)
        {
            if (!hit.collider.CompareTag("Ambulance"))
                continue;

            AmbulanceController ambulance =
                hit.collider.GetComponent<AmbulanceController>();

            if (ambulance == null)
                continue;

            if (hit.distance < closestDistance)
            {
                closestDistance =
                    hit.distance;

                closest =
                    ambulance;
            }
        }

        return closest;
    }

    //========================================================
    // VISUAL
    //========================================================

    void UpdateVisuals()
    {
        if (sensorLineRenderer != null)
        {
            bool show =
                detectedAmbulance != null &&
                CurrentState != AvoidanceState.Merged;

            sensorLineRenderer.enabled = show;

            if (show)
            {
                sensorLineRenderer.positionCount = 2;

                sensorLineRenderer.SetPosition(
                    0,
                    transform.position);

                sensorLineRenderer.SetPosition(
                    1,
                    detectedAmbulance.transform.position);

                Color color = Color.white;

                switch (CurrentState)
                {
                    case AvoidanceState.AmbulanceDetected:
                        color = Color.yellow;
                        break;

                    case AvoidanceState.WaitingForGap:
                        color = new Color(1f, 0.5f, 0f);
                        break;

                    case AvoidanceState.Merging:
                        color = Color.cyan;
                        break;
                }

                sensorLineRenderer.startColor =
                    color;

                sensorLineRenderer.endColor =
                    color;
            }
        }

        if (stateLabel != null)
        {
            stateLabel.text =
                CurrentState.ToString();
        }
    }

        //========================================================
    // GIZMO
    //========================================================

    private void OnDrawGizmos()
    {
        if (!showSensorGizmo)
            return;

        if (npcController == null)
            npcController =
                GetComponent<NPCCarController>();

        if (npcController == null)
            return;

        bool triggerLane =
            npcController.laneId == LaneMarker.LaneId.Lane2 ||
            npcController.laneId == LaneMarker.LaneId.Lane4;

        if (!triggerLane)
            return;

        Vector3 origin =
            transform.position +
            Vector3.up * sensorHeightOffset;

        Vector3 end =
            origin +
            Vector3.back * detectionRange;

        Color gizmoColor = Color.green;

        switch (CurrentState)
        {
            case AvoidanceState.AmbulanceDetected:
                gizmoColor = Color.yellow;
                break;

            case AvoidanceState.WaitingForGap:
                gizmoColor = new Color(1f, 0.5f, 0f);
                break;

            case AvoidanceState.Merging:
                gizmoColor = Color.cyan;
                break;

            case AvoidanceState.Merged:
                gizmoColor = Color.green;
                break;
        }

        Gizmos.color = gizmoColor;

        Gizmos.DrawLine(origin, end);

        Gizmos.DrawWireSphere(
            origin,
            sensorRadius);

        Gizmos.DrawWireSphere(
            end,
            sensorRadius);

        if (Application.isPlaying &&
            detectedAmbulance != null)
        {
            Gizmos.DrawLine(
                origin,
                detectedAmbulance.transform.position);

            Gizmos.DrawWireSphere(
                detectedAmbulance.transform.position,
                0.4f);
        }
    }

    //========================================================
    // RESERVATION
    //========================================================

    void CreateMergeReservation()
    {
        if (reservationCreated)
            return;

        LaneMarker.LaneId targetLane =
            LaneMarker.GetMergeTargetLane(
                npcController.laneId);

        reservation =
            LaneRegistry.Instance.CreateReservation(
                npcController,
                targetLane,
                mergeCheckRadius);

        reservationCreated = true;
    }

    void UpdateReservation()
    {
        if (reservation == null)
            return;

        reservation.targetZ =
            transform.position.z;
    }

    void RemoveReservation()
    {
        if (reservation == null)
            return;

        LaneRegistry.Instance.RemoveReservation(
            reservation);

        reservation = null;

        reservationCreated = false;
    }

    //========================================================
    // HELPER
    //========================================================

    bool IsAmbulanceStillRelevant()
    {
        if (detectedAmbulance == null)
            return false;

        float npcZ =
            transform.position.z;

        float ambulanceZ =
            detectedAmbulance.transform.position.z;

        // Ambulance masih dianggap relevan selama
        // belum melewati NPC terlalu jauh.

        return ambulanceZ <
               npcZ + detectionRange;
    }

    public bool HasActiveReservation()
    {
        return reservation != null &&
               reservation.active;
    }

    public MergeReservation GetReservation()
    {
        return reservation;
    }

    public AmbulanceController GetDetectedAmbulance()
    {
        return detectedAmbulance;
    }

    public bool IsMerging()
    {
        return CurrentState ==
               AvoidanceState.Merging;
    }

    public bool IsWaitingGap()
    {
        return CurrentState ==
               AvoidanceState.WaitingForGap;
    }

    public void ForceCancelAvoidance()
    {
        RemoveReservation();

        detectedAmbulance = null;

        mergeTimer = 0f;

        baseYRotationCached = false;

        CurrentState =
            AvoidanceState.Normal;
    }
}