using UnityEngine;

public class PedestrianController : MonoBehaviour
{
    public enum PedestrianState
    {
        Waiting,
        Crossing,
        Finished
    }

    [Header("Movement")]
    public float crossSpeed = 2f;
    public float reachDistance = 0.1f;

    [Header("Runtime References")]
    public Transform startPoint;
    public Transform endPoint;
    public PlayerCarController playerCar;
    public PedestrianCrosswalkZone crosswalkZone;

    public PedestrianState CurrentState => currentState;

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

        // Posisi awal mengikuti Start Point
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

            case PedestrianState.Finished:
                HandleFinished();
                break;
        }
    }

    /// <summary>
    /// Menunggu sampai pemain berhenti di zebra cross.
    /// </summary>
    private void HandleWaiting()
    {
        if (playerCar == null)
            return;

        if (!crosswalkZone.IsPlayerNearCrosswalk())
            return;

        if (crosswalkZone.IsAmbulanceBlocking())
            return;

        if (!crosswalkZone.IsPlayerFrozen())
            return;

        currentState = PedestrianState.Crossing;
    }

    /// <summary>
    /// Bergerak menuju titik akhir.
    /// </summary>
    private void HandleCrossing()
    {
        if (endPoint == null)
            return;

        transform.localPosition = Vector3.MoveTowards(
            transform.localPosition,
            endPoint.localPosition,
            crossSpeed * Time.deltaTime
        );

        Vector3 currentFlat = new Vector3(
            transform.localPosition.x,
            0f,
            transform.localPosition.z);

        Vector3 targetFlat = new Vector3(
            endPoint.localPosition.x,
            0f,
            endPoint.localPosition.z);

        if (Vector3.Distance(currentFlat, targetFlat) <= reachDistance)
        {
            FinishCrossing();
        }
    }

    /// <summary>
    /// Setelah berhasil menyeberang.
    /// </summary>
    private void FinishCrossing()
    {
        // Pastikan posisi tepat di titik tujuan
        transform.localPosition = endPoint.localPosition;

        // Ubah state menjadi selesai
        currentState = PedestrianState.Finished;

        // Beri tahu Crosswalk bahwa penyebrang selesai
        if (crosswalkZone != null)
        {
            crosswalkZone.NotifyPedestrianFinished();
        }

        // Tidak di-Destroy agar NPC tetap berada di lokasi
    }

    /// <summary>
    /// Penyebrang selesai menyeberang.
    /// </summary>
    private void HandleFinished()
    {
        // Sengaja dikosongkan.
        // Bisa ditambahkan animasi idle,
        // melihat sekitar,
        // atau berjalan ke tujuan lain.
    }

    /// <summary>
    /// Mengembalikan penyebrang ke posisi awal.
    /// Berguna jika nanti ingin menggunakan object pooling.
    /// </summary>
    public void ResetPedestrian()
    {
        if (startPoint == null)
            return;

        transform.localPosition = startPoint.localPosition;

        currentState = PedestrianState.Waiting;
    }

    /// <summary>
    /// Mengecek apakah penyebrang sedang berjalan.
    /// </summary>
    public bool IsCrossing()
    {
        return currentState == PedestrianState.Crossing;
    }

    /// <summary>
    /// Mengecek apakah penyebrang sudah selesai.
    /// </summary>
    public bool HasFinishedCrossing()
    {
        return currentState == PedestrianState.Finished;
    }
}