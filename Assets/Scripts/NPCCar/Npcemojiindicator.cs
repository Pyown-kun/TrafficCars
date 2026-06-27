using UnityEngine;

/// <summary>
/// Tempelkan component ini DI SAMPING NPCAmbulanceAvoidance (dan NPCCarController)
/// pada prefab NPC. Component ini membaca status dari instance
/// AmbulanceRearWarningTrigger SPESIFIK yang diteruskan lewat field
/// `relevantWarningTrigger` (diisi otomatis oleh NPCCarSpawner berdasarkan
/// LaneMarker.rearWarningTrigger pada spawn point, atau bisa di-drag manual
/// langsung di prefab) - tidak mengubah, tidak menyentuh, dan tidak memanggil
/// apapun di NPCAmbulanceAvoidance/NPCCarController/AmbulanceRearWarningTrigger.
/// Murni layer visual tambahan di atas sistem yang sudah berjalan.
///
/// PENTING: menggunakan AmbulanceRearWarningTrigger.IsWarningActiveInstance
/// (PER-INSTANCE, milik trigger spesifik yang di-assign), BUKAN
/// IsWarningActive (static/global) - karena bisa ada lebih dari satu
/// AmbulanceRearWarningTrigger di scene (mis. dipasang berbeda per lane),
/// dan status global tidak cukup presisi untuk membedakan trigger mana
/// yang relevan untuk NPC tertentu.
///
/// HANYA AKTIF untuk NPC di laneId Lane2 atau Lane4 (satu-satunya lane yang
/// relevan dengan sistem ambulance avoidance) - filter ini dicek eksplisit
/// di kode supaya tetap aman walau component ini ke depannya ditempel ke
/// semua NPC tanpa pengecualian manual di Editor.
///
/// Logic tampilan icon (MURNI berbasis status trigger, TIDAK terkait
/// CurrentState NPCAmbulanceAvoidance sama sekali - sensor NPC sendiri
/// bisa belum tentu aktif tepat saat ambulance sudah di area trigger,
/// karena posisi/radius keduanya independen):
///   1. ambulanceDetectedSprite -> tampil saat:
///        relevantWarningTrigger.IsWarningActiveInstance == true
///      (ambulance sedang berada di area trigger ini)
///   2. swervingSprite -> tampil saat:
///        relevantWarningTrigger.IsWarningActiveInstance == false
///        DAN NPC ini sebelumnya SUDAH PERNAH mengalami ambulanceDetectedSprite
///      (warning sudah berakhir, dan ini bukan NPC yang baru spawn tanpa histori)
///   3. Tidak tampil -> warning belum aktif DAN NPC belum pernah punya histori
///      ambulanceDetectedSprite, ATAU relevantWarningTrigger belum di-assign
///
/// Cara pasang di Unity:
/// 1. Tempelkan component ini ke root NPC (sejajar NPCAmbulanceAvoidance).
/// 2. Buat child GameObject baru bernama misal "EmojiIndicator", tempelkan
///    SpriteRenderer kosong di situ.
/// 3. Drag child SpriteRenderer itu ke field `indicatorRenderer` di Inspector.
/// 4. Drag 2 sprite (icon_ambulance_detected.png, icon_swerving.png) yang
///    sudah diimport sebagai Sprite ke field `ambulanceDetectedSprite` dan
///    `swervingSprite`.
/// 5. Atur `verticalOffset` sesuai tinggi model NPC supaya icon muncul pas
///    di atas atap mobil.
/// 6. `relevantWarningTrigger` BIASANYA TIDAK PERLU diisi manual di prefab -
///    field ini akan otomatis diisi oleh NPCCarSpawner saat NPC spawn,
///    berdasarkan field `rearWarningTrigger` yang di-drag di LaneMarker pada
///    spawn point lane tersebut. Hanya isi manual jika NPC ini di-spawn
///    lewat cara lain (bukan lewat NPCCarSpawner).
/// </summary>
[RequireComponent(typeof(NPCAmbulanceAvoidance))]
public class NPCEmojiIndicator : MonoBehaviour
{
    [Header("References")]
    [Tooltip("SpriteRenderer pada child object yang akan menampilkan icon. Dikosongkan secara default (sprite = null) saat tidak ada momen yang relevan.")]
    public SpriteRenderer indicatorRenderer;

    [Tooltip("Instance AmbulanceRearWarningTrigger yang relevan untuk lane NPC ini. BIASANYA diisi otomatis oleh NPCCarSpawner saat spawn (dari LaneMarker.rearWarningTrigger pada spawn point) - lihat dokumentasi class di atas. Jika None, tidak ada icon yang akan tampil sama sekali.")]
    public AmbulanceRearWarningTrigger relevantWarningTrigger;

    [Header("Sprites (drag PNG yang sudah diimport sebagai Sprite)")]
    [Tooltip("Ditampilkan saat NPC mendeteksi ambulance (state AmbulanceDetected/WaitingForGap) DAN relevantWarningTrigger.IsWarningActiveInstance masih true.")]
    public Sprite ambulanceDetectedSprite;

    [Tooltip("Ditampilkan saat relevantWarningTrigger.IsWarningActiveInstance sudah false (warning dari trigger ini sudah berakhir).")]
    public Sprite swervingSprite;

    [Header("Positioning")]
    [Tooltip("Jarak vertikal icon di atas pivot NPC (sesuaikan dengan tinggi model mobil).")]
    public float verticalOffset = 2.5f;

    [Header("Billboard")]
    [Tooltip("Kamera acuan untuk billboard. Jika dikosongkan, otomatis pakai Camera.main.")]
    public Camera billboardCamera;

    private NPCAmbulanceAvoidance avoidance;
    private NPCCarController npcController;

    // Dipakai untuk hindari kerja berulang tiap frame jika kombinasi
    // (sprite yang seharusnya tampil) belum berubah sejak frame sebelumnya.
    private Sprite lastAppliedSprite;
    private bool hasAppliedInitialSprite = false;

    // PENTING: melacak apakah NPC INI sudah pernah benar-benar masuk fase
    // ambulanceDetectedSprite (CurrentState AmbulanceDetected/WaitingForGap
    // DAN warning trigger sedang aktif). swervingSprite HANYA boleh tampil
    // jika flag ini sudah true - mencegah NPC yang baru spawn (yang belum
    // pernah berurusan dengan ambulance sama sekali) langsung menampilkan
    // swervingSprite hanya karena kondisi default IsWarningActiveInstance
    // == false (yang berlaku untuk hampir semua waktu/NPC).
    private bool hasEverBeenDetected = false;

    private void Awake()
    {
        avoidance = GetComponent<NPCAmbulanceAvoidance>();
        npcController = GetComponent<NPCCarController>();

        if (billboardCamera == null)
        {
            billboardCamera = Camera.main;
        }
    }

    private void LateUpdate()
    {
        if (avoidance == null) return;
        if (npcController == null) return;
        if (indicatorRenderer == null) return;

        // Posisikan indicator tepat di atas pivot NPC setiap frame -
        // tidak mengasumsikan parent-child offset statis, supaya tetap
        // benar walau NPC ini ikut digerakkan komponen lain (mis. lerp
        // X dari TickMerging() di NPCAmbulanceAvoidance).
        indicatorRenderer.transform.position = transform.position + Vector3.up * verticalOffset;

        ApplyBillboard();
        ApplySpriteForCurrentState();
    }

    void ApplyBillboard()
    {
        if (billboardCamera == null)
        {
            billboardCamera = Camera.main;
            if (billboardCamera == null) return;
        }

        // Billboard sederhana: hadapkan indicator ke kamera, tapi kunci
        // sumbu Y supaya sprite tidak ikut miring/terbalik aneh - cukup
        // rotasi menghadap kamera secara horizontal.
        Vector3 toCamera = billboardCamera.transform.position - indicatorRenderer.transform.position;
        toCamera.y = 0f;

        if (toCamera.sqrMagnitude > 0.001f)
        {
            indicatorRenderer.transform.rotation = Quaternion.LookRotation(-toCamera.normalized, Vector3.up);
        }
    }

    void ApplySpriteForCurrentState()
    {
        // === FILTER LANE ===
        // Hanya relevan untuk NPC di Lane2/Lane4 (satu-satunya lane yang
        // terlibat sistem ambulance avoidance). NPC di lane lain tidak
        // pernah menampilkan icon apapun dari component ini, apapun
        // CurrentState atau status warning saat ini.
        bool isRelevantLane = npcController.laneId == LaneMarker.LaneId.Lane2
                            || npcController.laneId == LaneMarker.LaneId.Lane4;

        if (!isRelevantLane)
        {
            ApplySpriteIfChanged(null);
            return;
        }

        // === FILTER REFERENSI TRIGGER ===
        // Jika relevantWarningTrigger belum di-assign (None) - baik karena
        // LaneMarker pada spawn point belum di-set rearWarningTrigger-nya,
        // atau NPC ini di-spawn lewat cara lain di luar NPCCarSpawner -
        // tidak ada cara mengetahui status warning yang benar, jadi jangan
        // tampilkan icon apapun daripada salah membaca status global/trigger
        // yang tidak relevan.
        if (relevantWarningTrigger == null)
        {
            ApplySpriteIfChanged(null);
            return;
        }

        // === LOGIC ICON (revisi) ===
        // ambulanceDetectedSprite: murni dari IsWarningActiveInstance saja,
        // TIDAK terkait CurrentState NPCAmbulanceAvoidance sama sekali.
        // Ini karena CurrentState NPC (sensor SphereCast miliknya sendiri)
        // bisa belum tentu AmbulanceDetected tepat saat ambulance sudah
        // masuk area AmbulanceRearWarningTrigger (posisi/jarak trigger
        // berbeda dengan radius sensor NPC) - mensyaratkan keduanya
        // bersamaan (AND) sebelumnya membuat ambulanceDetectedSprite hampir
        // tidak pernah tampil.
        bool isWarningActive = relevantWarningTrigger.IsWarningActiveInstance;

        Sprite targetSprite;

        if (isWarningActive)
        {
            // Ambulance ada di area trigger ini. Catat bahwa NPC ini SUDAH
            // PERNAH mengalami momen ini - dipakai sebagai syarat untuk
            // swervingSprite di bawah, supaya NPC yang baru spawn (belum
            // pernah mengalami momen ini sama sekali) tidak langsung dapat
            // swervingSprite hanya karena kondisi default isWarningActive
            // == false.
            hasEverBeenDetected = true;
            targetSprite = ambulanceDetectedSprite;
        }
        else if (hasEverBeenDetected)
        {
            // Warning dari trigger spesifik ini sudah berakhir, DAN NPC ini
            // sebelumnya memang sudah pernah masuk fase ambulanceDetectedSprite -
            // baru tampilkan icon swerving. Tanpa syarat hasEverBeenDetected,
            // NPC yang baru spawn akan langsung dapat swervingSprite hanya
            // karena IsWarningActiveInstance == false (kondisi default yang
            // berlaku hampir selalu, sebelum ambulance manapun lewat).
            targetSprite = swervingSprite;
        }
        else
        {
            // Sisa kasus: warning tidak aktif DAN NPC ini belum pernah
            // hasEverBeenDetected sama sekali (baru spawn, belum pernah
            // ada ambulance lewat trigger ini) - tidak tampilkan apapun.
            targetSprite = null;
        }

        ApplySpriteIfChanged(targetSprite);
    }

    void ApplySpriteIfChanged(Sprite targetSprite)
    {
        // Hindari pemanggilan SetIndicator() berulang tiap frame jika
        // sprite yang seharusnya tampil belum berubah sejak frame sebelumnya.
        if (hasAppliedInitialSprite && targetSprite == lastAppliedSprite) return;

        SetIndicator(targetSprite);

        lastAppliedSprite = targetSprite;
        hasAppliedInitialSprite = true;
    }

    void SetIndicator(Sprite sprite)
    {
        indicatorRenderer.sprite = sprite;
        indicatorRenderer.enabled = sprite != null;
    }
}