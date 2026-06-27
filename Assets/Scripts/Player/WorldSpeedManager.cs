using UnityEngine;

public class WorldSpeedManager : MonoBehaviour
{
    public static WorldSpeedManager Instance;

    [Header("World Speed")]
    [SerializeField] private float currentWorldSpeed = 10f;

    [Tooltip("Kecepatan normal dunia.")]
    public float normalWorldSpeed = 10f;

    [Tooltip("Kecepatan minimum saat player mengerem.")]
    public float brakeWorldSpeed = 3f;

    [Tooltip("Seberapa cepat dunia melambat.")]
    public float brakeAcceleration = 12f;

    [Tooltip("Seberapa cepat dunia kembali normal.")]
    public float recoverAcceleration = 8f;

    private bool braking;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);

        currentWorldSpeed = normalWorldSpeed;
    }

    private void Update()
    {
        float targetSpeed = braking
            ? brakeWorldSpeed
            : normalWorldSpeed;

        float accel = braking
            ? brakeAcceleration
            : recoverAcceleration;

        currentWorldSpeed = Mathf.MoveTowards(
            currentWorldSpeed,
            targetSpeed,
            accel * Time.deltaTime);
    }

    public void SetBraking(bool value)
    {
        braking = value;
    }

    public bool IsBraking()
    {
        return braking;
    }

    public float GetCurrentWorldSpeed()
    {
        return currentWorldSpeed;
    }

    public float GetSpeedRatio()
    {
        if (normalWorldSpeed <= 0.01f)
            return 1f;

        return currentWorldSpeed / normalWorldSpeed;
    }
}