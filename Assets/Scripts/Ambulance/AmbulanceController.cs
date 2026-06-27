using UnityEngine;

public class AmbulanceController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 16f;
    public float destroyFrontZ = 140f;

    [Header("Reference")]
    public PlayerCarController playerCar;

    [Header("Brake Effect")]
    [Tooltip("Saat player ngerem, ambulans makin cepat menyalip ke depan")]
    public float brakeSpeedMultiplier = 1.8f;

    // === TAMBAHAN BARU ===
    // Diisi otomatis oleh AmbulanceTrigger saat instantiate, dibaca oleh
    // NPCAmbulanceAvoidance untuk menentukan arah sensor NPC yang relevan.
    [Header("Lane (untuk Ambulance Avoidance System)")]
    public LaneMarker.LaneId laneId = LaneMarker.LaneId.Lane2;
    // === END TAMBAHAN BARU ===

    private void Update()
    {
        if (Time.timeScale == 0f) return;
        if (playerCar == null) return;

        float currentSpeed = moveSpeed;

        if (playerCar.IsBraking())
        {
            currentSpeed *= brakeSpeedMultiplier;
        }

        transform.Translate(Vector3.forward * currentSpeed * Time.deltaTime, Space.World);

        if (transform.position.z > destroyFrontZ)
        {
            Destroy(gameObject);
        }
    }
}