using UnityEngine;

public class PedestrianController : MonoBehaviour
{
    public enum PedestrianState
    {
        Waiting,
        Crossing
    }

    [Header("Movement")]
    public float crossSpeed = 2f;
    public float reachDistance = 0.1f;

    [Header("Runtime References")]
    public Transform startPoint;
    public Transform endPoint;
    public PlayerCarController playerCar;
    public PedestrianCrosswalkZone crosswalkZone;

    private PedestrianState currentState = PedestrianState.Waiting;
    private bool initialized = false;

    public void Setup(
        Transform start,
        Transform end,
        PlayerCarController player,
        PedestrianCrosswalkZone zone
    )
    {
        startPoint = start;
        endPoint = end;
        playerCar = player;
        crosswalkZone = zone;

        // Penting: pedestrian harus child dari zone
        // lalu posisinya mengikuti local point
        transform.localPosition = startPoint.localPosition;

        initialized = true;
        currentState = PedestrianState.Waiting;
    }

    private void Update()
    {
        if (!initialized) return;
        if (Time.timeScale == 0f) return;
        if (crosswalkZone == null) return;

        switch (currentState)
        {
            case PedestrianState.Waiting:
                HandleWaiting();
                break;

            case PedestrianState.Crossing:
                HandleCrossing();
                break;
        }
    }

   void HandleWaiting()
    {
        if (playerCar == null)
            return;

        if (!crosswalkZone.IsPlayerNearCrosswalk())
            return;

        if (crosswalkZone.IsAmbulanceBlocking())
            return;

        // Player harus benar-benar sudah freeze
        if (!crosswalkZone.IsPlayerFrozen())
            return;

        currentState = PedestrianState.Crossing;
    }

    void HandleCrossing()
    {
        if (endPoint == null) return;

        // Gerak dalam LOCAL SPACE zone
        transform.localPosition = Vector3.MoveTowards(
            transform.localPosition,
            endPoint.localPosition,
            crossSpeed * Time.deltaTime
        );

        Vector3 currentFlat = new Vector3(transform.localPosition.x, 0f, transform.localPosition.z);
        Vector3 targetFlat = new Vector3(endPoint.localPosition.x, 0f, endPoint.localPosition.z);

        if (Vector3.Distance(currentFlat, targetFlat) <= reachDistance)
        {
            FinishCrossing();
        }
    }

    void FinishCrossing()
    {
        if (crosswalkZone != null)
        {
            crosswalkZone.NotifyPedestrianFinished();
        }

        Destroy(gameObject);
    }
}