using UnityEngine;

/// <summary>
/// Tempelkan component ini DI SAMPING NPCCarController (dan NPCAmbulanceAvoidance)
/// pada prefab NPC. Logic ini HANYA relevan untuk NPC trafficType = TowardPlayer
/// yang sedang berhenti karena crosswalk (stoppedByCrosswalk == true).
///
/// Tujuannya: mencegah NPC TowardPlayer menumpuk/menabrak NPC lain saat
/// berhenti di FrontCrosswalkTrafficTrigger. Mengikuti POLA YANG SAMA dengan
/// NPCAmbulanceAvoidance.DetectAmbulance() - sensor fisik (SphereCast) ke
/// arah depan, bukan query data registry. Ini lebih reliable dibanding
/// pendekatan LaneRegistry sebelumnya karena collider fisik selalu
/// merepresentasikan posisi NPC yang sebenarnya saat ini, tanpa risiko
/// timing/registrasi yang telat.
///
/// Cara kerja per frame (di LateUpdate, SETELAH NPCCarController.Update
/// selesai translate Vector3.back dengan crosswalkSpeed):
/// 1. SENSOR: SphereCast ke arah Vector3.back (arah gerak TowardPlayer)
///    sejauh detectionRange, mencari collider bertag "NPCCar" selain
///    diri sendiri - persis pola DetectAmbulance(), hanya target tag
///    dan arahnya yang berbeda.
/// 2. Jika tidak ada NPC terdeteksi -> tidak ada koreksi, translate
///    normal dari NPCCarController dibiarkan apa adanya.
/// 3. Jika ada NPC terdeteksi -> posisi Z di-override dengan Lerp
///    eksponensial menuju (aheadZ + stopGap), sehingga NPC mengerem
///    bertahap dan TIDAK PERNAH menembus/menabrak NPC di depannya.
/// </summary>
[RequireComponent(typeof(NPCCarController))]
public class NPCCrosswalkQueue : MonoBehaviour
{
    [Header("Queue Sensor (sensor fisik, sama pola dengan NPCAmbulanceAvoidance)")]
    [Tooltip("Jarak mulai mendeteksi NPC di depan dan mulai melambat secara gradual. Di luar jarak ini, NPC jalan normal mengikuti speed crosswalk biasa.")]
    public float detectionRange = 10f;

    [Tooltip("Radius SphereCast sensor. Beri toleransi lebar agar tetap mendeteksi walau posisi X NPC lain tidak 100% sejajar.")]
    public float sensorRadius = 1.2f;

    [Tooltip("Tinggi origin sensor di atas pivot NPC, agar tidak nyangkut collider jalan/ground.")]
    public float sensorHeightOffset = 0.5f;

    [Tooltip("Layer untuk filter sensor. SEBAIKNYA diisi layer yang dipakai NPC (bukan 'Everything'), supaya sensor tidak nyangkut ke ground/objek lain.")]
    public LayerMask npcLayerMask = ~0;

    [Tooltip("Jarak aman akhir ke NPC di depan saat berhenti total (tidak overlap).")]
    public float stopGap = 4f;

    [Tooltip("Semakin besar, semakin responsif/cepat NPC menyesuaikan ke posisi target dalam zona deselerasi. Semakin kecil, semakin halus/lambat transisinya.")]
    public float decelerationSharpness = 4f;

    [Header("Debug")]
    [Tooltip("Tampilkan garis sensor ke NPC di depan di Scene view.")]
    public bool showSensorGizmo = true;

    [Tooltip("Tampilkan panel teks debug di Game view (pojok kiri atas) berisi info sensor real-time untuk SEMUA NPC yang aktif logic ini.")]
    public bool showDebugUI = true;

    // Data debug yang diisi tiap frame, dibaca oleh OnGUI di bawah.
    private float debugGapToAhead = -1f;
    private float debugTargetZ = 0f;
    private bool debugWasDetectedThisFrame = false;

    private NPCCarController npcController;
    private NPCCarController detectedAhead;
    private Collider myCollider;

    private void Awake()
    {
        npcController = GetComponent<NPCCarController>();
        myCollider = GetComponent<Collider>();
    }

    // PENTING: dipasang di LateUpdate(), bukan Update(). Urutan eksekusi
    // Update() antar component/GameObject TIDAK terjamin oleh Unity kecuali
    // diatur lewat Script Execution Order. Karena logic ini perlu mengoreksi
    // posisi SETELAH NPCCarController.Update() selesai melakukan
    // Translate(Vector3.back * crosswalkSpeed) di FollowCrosswalkWhileStopped(),
    // LateUpdate() menjamin urutan itu - LateUpdate selalu berjalan setelah
    // SEMUA Update() di frame yang sama selesai, terlepas urutan antar script.
    private void LateUpdate()
    {
        if (Time.timeScale == 0f) return;
        if (npcController == null) return;

        // Logic ini relevan untuk SEMUA NPC TowardPlayer, TIDAK HANYA yang
        // sudah stoppedByCrosswalk. Sensor harus tetap aktif walau NPC ini
        // sendiri masih jalan normal (belum masuk trigger crosswalk),
        // karena tujuannya justru MENCEGAH dia menabrak NPC LAIN yang
        // sudah berhenti di depannya - jika sensor hanya aktif setelah
        // diri sendiri stoppedByCrosswalk, NPC akan tetap full speed dan
        // menabrak dulu sebelum logic ini punya kesempatan bereaksi
        // (persis gejala yang terjadi sebelum perbaikan ini: NPC dari
        // belakang menabrak NPC yang sudah berhenti di depan, padahal
        // dirinya sendiri belum masuk area trigger sama sekali).
        if (npcController.trafficType != NPCCarController.TrafficType.TowardPlayer)
        {
            detectedAhead = null;
            debugWasDetectedThisFrame = false;
            return;
        }

        ApplyQueueDeceleration();
    }

    void ApplyQueueDeceleration()
    {
        NPCCarController ahead = DetectNpcAhead();
        detectedAhead = ahead;

        // Tidak ada NPC terdeteksi di depan -> NPC ini paling depan di
        // antrian (atau belum ada yang dekat), biarkan
        // NPCCarController.FollowCrosswalkWhileStopped() bekerja normal
        // tanpa campur tangan apapun dari component ini.
        if (ahead == null)
        {
            debugGapToAhead = -1f;
            debugWasDetectedThisFrame = false;
            return;
        }

        float myZ = transform.position.z;
        float aheadZ = ahead.transform.position.z;

        // === VALIDASI KEDUA (defense in depth) ===
        // Meski DetectNpcAhead() sudah memvalidasi otherZ < myZ, cek ulang
        // di sini sebagai pengaman tambahan. Jika somehow aheadZ >= myZ,
        // JANGAN proses sebagai target sama sekali - ini akan menyebabkan
        // targetZ > myZ, yang berarti NPC akan di-lerp MENJAUH/maju, bukan
        // mengerem - gejala persis seperti "teleport mendekat tiba-tiba"
        // yang dilaporkan. Lebih aman tidak melakukan apapun di frame ini
        // daripada menerapkan koreksi yang salah arah.
        if (aheadZ >= myZ)
        {
            debugGapToAhead = -1f;
            debugWasDetectedThisFrame = false;
            return;
        }

        float gapToAhead = myZ - aheadZ;

        debugGapToAhead = gapToAhead;
        debugTargetZ = aheadZ + stopGap;
        debugWasDetectedThisFrame = true;

        float targetZ = aheadZ + stopGap;

        // === DESELERASI GRADUAL ===
        // Lerp posisi Z menuju targetZ, dengan faktor yang bergantung pada
        // decelerationSharpness dan Time.deltaTime - ini menghasilkan kurva
        // eksponensial yang melambat secara alami: langkah besar saat masih
        // jauh dari target, langkah kecil saat sudah dekat, dan TIDAK PERNAH
        // melompat melewati targetZ (selalu mendekat secara halus, tidak
        // pernah menembus/menabrak NPC di depannya).
        float lerpFactor = 1f - Mathf.Exp(-decelerationSharpness * Time.deltaTime);
        float newZ = Mathf.Lerp(myZ, targetZ, lerpFactor);

        // Jaga-jaga: jangan sampai hasil lerp justru lebih kecil dari targetZ
        // (overshoot ke belakang NPC depan) akibat floating point di kasus
        // ekstrem - clamp minimal ke targetZ.
        newZ = Mathf.Max(newZ, targetZ);

        Vector3 pos = transform.position;
        pos.z = newZ;
        transform.position = pos;
    }

    // =========================================================
    // SENSOR FISIK - persis pola NPCAmbulanceAvoidance.DetectAmbulance(),
    // hanya target tag ("NPCCar" bukan "Ambulance") dan arah sensor yang
    // disesuaikan (Vector3.back, arah gerak NPC TowardPlayer itu sendiri).
    // =========================================================
    NPCCarController DetectNpcAhead()
    {
        // NPC TowardPlayer bergerak Vector3.back, jadi "di depan" (arah
        // gerak menuju player) juga Vector3.back.
        Vector3 sensorDirection = Vector3.back;
        Vector3 origin = transform.position + Vector3.up * sensorHeightOffset;
        float myZ = transform.position.z;

        RaycastHit[] hits = Physics.SphereCastAll(origin, sensorRadius, sensorDirection, detectionRange, npcLayerMask);

        NPCCarController closest = null;
        float closestDist = float.MaxValue;

        for (int i = 0; i < hits.Length; i++)
        {
            RaycastHit hit = hits[i];
            if (!hit.collider.CompareTag("NPCCar")) continue;

            // Jangan deteksi diri sendiri (collider milik GameObject ini).
            if (myCollider != null && hit.collider == myCollider) continue;
            if (hit.collider.transform == transform) continue;
            if (hit.collider.transform.IsChildOf(transform)) continue;

            NPCCarController other = hit.collider.GetComponentInParent<NPCCarController>();
            if (other == null || other == npcController) continue;

            // === VALIDASI WAJIB ===
            // SphereCast dengan radius bisa saja "menangkap" collider yang
            // overlap di sekitar titik origin (hit.distance kecil/0) meski
            // collider itu sebenarnya tidak murni di depan secara Z - mis.
            // sedikit di belakang tapi overlap radius. Untuk mencegah NPC
            // yang sebenarnya PALING DEPAN (tidak ada NPC lain di depannya)
            // tetap "terdeteksi" mendapat target yang salah lalu melompat
            // mendekat secara tidak masuk akal, kita WAJIB validasi posisi
            // Z kandidat benar-benar lebih kecil dari Z NPC ini sendiri.
            // Kandidat yang gagal validasi ini diabaikan total, apapun
            // hit.distance yang dilaporkan SphereCast.
            float otherZ = other.transform.position.z;
            if (otherZ >= myZ) continue;

            if (hit.distance < closestDist)
            {
                closest = other;
                closestDist = hit.distance;
            }
        }

        return closest;
    }

    private void OnDrawGizmos()
    {
        if (!showSensorGizmo) return;
        if (npcController == null) npcController = GetComponent<NPCCarController>();
        if (npcController == null) return;

        bool isToward = npcController.trafficType == NPCCarController.TrafficType.TowardPlayer;
        if (!isToward) return;

        Vector3 origin = transform.position + Vector3.up * sensorHeightOffset;
        Vector3 end = origin + Vector3.back * detectionRange;

        Gizmos.color = detectedAhead != null ? Color.yellow : Color.green;
        Gizmos.DrawLine(origin, end);
        Gizmos.DrawWireSphere(end, sensorRadius);

        if (detectedAhead != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(origin, detectedAhead.transform.position);
            Gizmos.DrawWireSphere(detectedAhead.transform.position, 0.4f);
        }
    }

    // =========================================================
    // RUNTIME DEBUG UI (Game view, bukan cuma Scene-view gizmo)
    // =========================================================
    private static readonly System.Collections.Generic.List<NPCCrosswalkQueue> activeInstances
        = new System.Collections.Generic.List<NPCCrosswalkQueue>();

    private void OnEnable()
    {
        if (!activeInstances.Contains(this)) activeInstances.Add(this);
    }

    private void OnDisable()
    {
        activeInstances.Remove(this);
    }

    private void OnGUI()
    {
        if (!showDebugUI) return;
        if (activeInstances.Count == 0 || activeInstances[0] != this) return;

        GUIStyle style = new GUIStyle(GUI.skin.box)
        {
            alignment = TextAnchor.UpperLeft,
            fontSize = 12,
            normal = { textColor = Color.white }
        };

        var relevant = new System.Collections.Generic.List<NPCCrosswalkQueue>();
        foreach (var instance in activeInstances)
        {
            if (instance == null || instance.npcController == null) continue;
            bool isToward = instance.npcController.trafficType == NPCCarController.TrafficType.TowardPlayer;
            if (!isToward) continue;

            // Tampilkan NPC yang relevan untuk debugging: sedang berhenti
            // karena crosswalk, ATAU sensornya sedang mendeteksi NPC lain
            // di depan (meski dirinya sendiri masih jalan normal/belum
            // masuk trigger) - kasus terakhir ini penting ditampilkan
            // karena ini persis skenario yang sebelumnya menyebabkan
            // tabrakan tak terdeteksi.
            bool stopped = instance.npcController.IsStoppedByCrosswalk();
            if (stopped || instance.debugWasDetectedThisFrame)
            {
                relevant.Add(instance);
            }
        }

        float y = 10f;
        float panelHeight = 24f + Mathf.Max(relevant.Count, 1) * 20f;
        GUI.Box(new Rect(10, y, 420, panelHeight), "");
        GUI.Label(new Rect(16, y + 2, 410, 18), "[NPCCrosswalkQueue] Sensor Debug (physical SphereCast)", style);
        y += 22f;

        if (relevant.Count == 0)
        {
            GUI.Label(new Rect(16, y + 2, 410, 18), "(tidak ada NPC TowardPlayer yang relevan saat ini)", style);
            return;
        }

        foreach (var instance in relevant)
        {
            string stoppedTag = instance.npcController.IsStoppedByCrosswalk() ? "[STOPPED]" : "[JALAN]";
            string label;

            if (!instance.debugWasDetectedThisFrame)
            {
                label = $"{instance.name} {stoppedTag}: AHEAD=NULL Z={instance.transform.position.z:F2}";
            }
            else
            {
                label = $"{instance.name} {stoppedTag}: gap={instance.debugGapToAhead:F2} target={instance.debugTargetZ:F2} stopGap={instance.stopGap:F1}";
            }

            GUI.Label(new Rect(16, y + 2, 410, 18), label, style);
            y += 20f;
        }
    }
}