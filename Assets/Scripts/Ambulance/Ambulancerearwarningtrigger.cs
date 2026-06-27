using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Trigger collider STATIS (diam di posisi tertentu di scene, TIDAK mengikuti
/// player) yang mendeteksi lewatnya ambulance di belakang player. SISTEM INI
/// SEPENUHNYA TERPISAH dari sensor NPC (NPCAmbulanceAvoidance.DetectAmbulance()
/// tidak disentuh/diubah sama sekali, tetap berjalan independen).
///
/// Tujuan tunggal script ini: expose data/event sederhana yang nanti bisa
/// dibaca oleh script UI terpisah (belum dibuat) untuk menampilkan peringatan
/// "ambulance datang dari belakang" ke player.
///
/// Cara pasang di Unity:
/// 1. Buat GameObject baru di posisi yang diinginkan, di belakang area player
///    (mis. titik tetap beberapa unit di belakang area mulai permainan).
/// 2. Tambahkan BoxCollider (akan otomatis di-set isTrigger=true lewat Awake/Reset),
///    atur Size sesuai lebar jalan yang ingin dicakup (lebar 5 lane) dan
///    panjang secukupnya di sumbu Z.
/// 3. Tempelkan script ini ke GameObject tersebut.
/// 4. Pastikan prefab Ambulance bertag "Ambulance" (sudah konsisten dengan
///    AmbulanceController/AmbulanceTrigger yang ada).
///
/// Cara UI nanti membaca sinyal ini (belum diimplementasikan, contoh saja):
///   - Subscribe ke event statis: AmbulanceRearWarningTrigger.OnWarningStateChanged
///   - Atau polling: AmbulanceRearWarningTrigger.IsWarningActive (static property)
/// </summary>
[RequireComponent(typeof(BoxCollider))]
public class AmbulanceRearWarningTrigger : MonoBehaviour
{
    [Header("Warning Duration")]
    [Tooltip("Berapa lama warning tetap aktif setelah ambulance masuk trigger, MAKSIMAL - warning akan hilang lebih cepat jika ambulance sudah keluar trigger sebelum durasi ini habis (mana yang lebih dulu terjadi).")]
    public float maxWarningDuration = 4f;

    [Header("Debug")]
    [Tooltip("Tampilkan area trigger ini di Scene view.")]
    public bool showGizmo = true;

    // =========================================================
    // STATIC EVENT/STATE - bisa diakses oleh script UI manapun nanti,
    // tanpa perlu referensi langsung ke instance GameObject ini.
    // Didesain static karena biasanya hanya ada 1 titik warning relevan
    // di satu waktu, dan ini menyederhanakan cara UI mengaksesnya.
    // =========================================================

    /// <summary>
    /// True selama warning seharusnya tampil ke player. UI bisa polling
    /// nilai ini tiap frame, atau subscribe ke OnWarningStateChanged untuk
    /// reaksi event-based (lebih efisien daripada polling).
    /// </summary>
    public static bool IsWarningActive { get; private set; } = false;

    /// <summary>
    /// Dipanggil setiap kali IsWarningActive berubah (true->false atau
    /// false->true). Parameter bool = nilai baru IsWarningActive.
    /// Contoh subscribe dari script UI lain:
    ///   void OnEnable() { AmbulanceRearWarningTrigger.OnWarningStateChanged += HandleWarning; }
    ///   void OnDisable() { AmbulanceRearWarningTrigger.OnWarningStateChanged -= HandleWarning; }
    ///   void HandleWarning(bool active) { warningPanel.SetActive(active); }
    /// </summary>
    public static event System.Action<bool> OnWarningStateChanged;

    // === TAMBAHAN BARU (instance property, tidak mengubah logic apapun) ===
    /// <summary>
    /// Sama seperti IsWarningActive, tapi PER-INSTANCE (bukan static global).
    /// Berguna ketika ada lebih dari satu AmbulanceRearWarningTrigger di
    /// scene (mis. dipasang di beberapa lane/jalur berbeda) dan script lain
    /// perlu tahu status SPESIFIK milik instance ini saja, bukan status
    /// gabungan semua trigger. Dihitung langsung dari ambulancesInside.Count
    /// milik instance ini - tidak ada logic/behavior lain yang diubah.
    /// </summary>
    public bool IsWarningActiveInstance => ambulancesInside.Count > 0;
    // === END TAMBAHAN BARU ===

    // Ambulance yang sedang berada di dalam trigger ini saat ini.
    private readonly HashSet<Collider> ambulancesInside = new HashSet<Collider>();
    private float warningTimer = 0f;

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
        if (Time.timeScale == 0f) return;
        if (!IsWarningActive) return;

        // Hilangkan warning lebih cepat jika SEMUA ambulance yang sempat
        // memicu warning ini sudah keluar trigger - bersihkan referensi
        // null (ambulance destroyed) lebih dulu.
        ambulancesInside.RemoveWhere(c => c == null);

        if (ambulancesInside.Count == 0)
        {
            SetWarningActive(false);
            return;
        }

        // Timer maksimum sebagai batas atas - warning tidak akan menyala
        // lebih lama dari maxWarningDuration sejak ambulance PERTAMA masuk,
        // meski ambulance itu (atau ambulance lain) masih ada di trigger.
        warningTimer += Time.deltaTime;
        if (warningTimer >= maxWarningDuration)
        {
            SetWarningActive(false);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Ambulance")) return;

        bool wasEmpty = ambulancesInside.Count == 0;
        ambulancesInside.Add(other);

        // Mulai/reset timer hanya saat ambulance PERTAMA masuk (trigger
        // dari kondisi kosong ke terisi) - jika sudah ada ambulance lain
        // di dalam, ambulance baru ini tidak memperpanjang timer yang
        // sedang berjalan, supaya durasi warning tetap predictable.
        if (wasEmpty)
        {
            warningTimer = 0f;
            SetWarningActive(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Ambulance")) return;

        ambulancesInside.Remove(other);

        if (ambulancesInside.Count == 0)
        {
            SetWarningActive(false);
        }
    }

    private void OnDisable()
    {
        // Pengaman: jika GameObject ini dinonaktifkan/destroyed saat
        // warning sedang aktif, pastikan UI tidak nyangkut dalam state
        // "warning aktif" selamanya.
        ambulancesInside.Clear();
        SetWarningActive(false);
    }

    static void SetWarningActive(bool active)
    {
        if (IsWarningActive == active) return;

        IsWarningActive = active;
        OnWarningStateChanged?.Invoke(active);
    }

    private void OnDrawGizmos()
    {
        if (!showGizmo) return;

        BoxCollider col = GetComponent<BoxCollider>();
        if (col == null) return;

        Gizmos.color = IsWarningActive ? new Color(1f, 0.3f, 0.1f, 0.5f) : new Color(1f, 0.8f, 0f, 0.25f);
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawCube(col.center, col.size);

        Gizmos.color = IsWarningActive ? Color.red : Color.yellow;
        Gizmos.DrawWireCube(col.center, col.size);
    }
}