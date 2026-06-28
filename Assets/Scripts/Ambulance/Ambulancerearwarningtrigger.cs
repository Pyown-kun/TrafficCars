using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class AmbulanceRearWarningTrigger : MonoBehaviour
{
    [Header("Warning Duration")]
    [Tooltip("Durasi maksimum warning aktif.")]
    public float maxWarningDuration = 4f;

    [Header("Debug")]
    public bool showGizmo = true;

    /// <summary>
    /// Status warning milik trigger ini saja.
    /// </summary>
    public bool IsWarningActive { get; private set; }

    /// <summary>
    /// Event milik trigger ini saja.
    /// UI dapat subscribe ke trigger yang ditugaskan.
    /// </summary>
    public event Action<bool> OnWarningStateChanged;

    /// <summary>
    /// Status warning khusus trigger ini.
    /// Tidak mempengaruhi trigger lain.
    /// </summary>
    public bool IsWarningActiveInstance => ambulancesInside.Count > 0;

    /// <summary>
    /// Event khusus trigger ini.
    /// Digunakan bila ada script yang ingin subscribe ke trigger tertentu.
    /// </summary>
    public event System.Action<bool> OnWarningStateChangedInstance;

    private readonly HashSet<Collider> ambulancesInside = new();

    private float warningTimer;

    private void Reset()
    {
        BoxCollider col = GetComponent<BoxCollider>();
        col.isTrigger = true;
    }

    private void Awake()
    {
        BoxCollider col = GetComponent<BoxCollider>();
        col.isTrigger = true;
    }

    private void Update()
    {
        if (Time.timeScale == 0f)
            return;

        if (!IsWarningActive)
            return;

        ambulancesInside.RemoveWhere(c => c == null);

        if (ambulancesInside.Count == 0)
        {
            SetWarningActive(false);
            return;
        }

        warningTimer += Time.deltaTime;

        if (warningTimer >= maxWarningDuration)
        {
            SetWarningActive(false);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Ambulance"))
            return;

        bool wasEmpty = ambulancesInside.Count == 0;

        ambulancesInside.Add(other);

        if (wasEmpty)
        {
            warningTimer = 0f;
            SetWarningActive(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Ambulance"))
            return;

        ambulancesInside.Remove(other);

        if (ambulancesInside.Count == 0)
        {
            SetWarningActive(false);
        }
    }

    private void OnDisable()
    {
        ambulancesInside.Clear();
        SetWarningActive(false);
    }

    private void SetWarningActive(bool active)
    {
        // Instance Event
        OnWarningStateChangedInstance?.Invoke(active);

        // Static (kompatibilitas)
        if (IsWarningActive != active)
        {
            IsWarningActive = active;
            OnWarningStateChanged?.Invoke(active);
        }
    }

    private void OnDrawGizmos()
    {
        if (!showGizmo)
            return;

        BoxCollider col = GetComponent<BoxCollider>();

        if (col == null)
            return;

        Gizmos.matrix = transform.localToWorldMatrix;

        Gizmos.color = IsWarningActive
            ? new Color(1f, 0.3f, 0.1f, 0.5f)
            : new Color(1f, 0.8f, 0f, 0.25f);

        Gizmos.DrawCube(col.center, col.size);

        Gizmos.color = IsWarningActive
            ? Color.red
            : Color.yellow;

        Gizmos.DrawWireCube(col.center, col.size);
    }
}