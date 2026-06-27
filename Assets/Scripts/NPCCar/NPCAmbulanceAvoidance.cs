using UnityEngine;

/// <summary>
/// Tempelkan component ini DI SAMPING NPCCarController pada prefab NPC
/// (tidak menggantikan, tidak mengubah NPCCarController).
///
/// Logic ini hanya aktif untuk NPC yang berada di Lane2 atau Lane4
/// (ditentukan dari NPCCarController.laneId). NPC di lane lain akan
/// otomatis idle (tidak melakukan apa-apa, tidak ada overhead).
///
/// Alur:
/// 1. NORMAL: idle, menunggu ambulance terdeteksi.
/// 2. AMBULANCE_DETECTED: ambulance masuk radius sensor (raycast lokal,
///    arah sensor mengikuti dari mana ambulance datang sesuai lane).
/// 3. WAITING_FOR_GAP: ambulance sudah terdeteksi, tapi jalur tujuan
///    (Lane1 / Lane5) masih ada NPC yang belum lewat (cek via LaneRegistry).
/// 4. MERGING: jalur tujuan aman, NPC lerp posisi X ke jalur tujuan sambil
///    tetap berjalan normal di sumbu Z, dengan rotasi belok yang naik-turun.
/// 5. MERGED: lerp selesai, rotasi balik netral, laneId NPC diperbarui
///    permanen ke jalur baru (re-register ke LaneRegistry).
/// </summary>
[RequireComponent(typeof(NPCCarController))]
public class NPCAmbulanceAvoidance : MonoBehaviour
{
    public enum AvoidanceState
    {
        Normal,
        AmbulanceDetected,
        WaitingForGap,
        Merging,
        Merged
    }

    [Header("Sensor")]
    [Tooltip("Jarak deteksi ambulance dari posisi NPC.")]
    public float detectionRange = 25f;

    [Tooltip("Tinggi origin raycast di atas pivot NPC, agar tidak nyangkut collider jalan/ground.")]
    public float sensorHeightOffset = 0.5f;

    [Tooltip("Radius sensor (SphereCast). Beri toleransi lebar agar tetap mendeteksi walau posisi X ambulance tidak 100% sejajar NPC. Mulai dari setengah lebar mobil, misal 1-1.5.")]
    public float sensorRadius = 1.2f;

    [Tooltip("Layer/tag ambulance untuk Physics check. SEBAIKNYA diisi layer khusus 'Ambulance' saja, BUKAN 'Everything' - lihat komentar di DetectAmbulance().")]
    public LayerMask ambulanceLayerMask = ~0;

    [Header("Merge Movement")]
    [Tooltip("Jarak antar jalur di sumbu X (sesuaikan dengan lebar jalur di scene).")]
    public float laneWidth = 3.5f;

    [Tooltip("Durasi proses berpindah jalur (detik).")]
    public float mergeDuration = 1.5f;

    [Tooltip("Sudut maksimum rotasi Y saat berbelok (derajat).")]
    public float maxTurnAngle = 15f;

    [Header("Visual Sensor Line (opsional, untuk demo expo)")]
    [Tooltip("Jika diisi, LineRenderer ini akan digambar dari NPC ke ambulance saat terdeteksi.")]
    public LineRenderer sensorLineRenderer;

    [Header("State Label (opsional, untuk demo expo)")]
    [Tooltip("Jika diisi, teks ini akan menampilkan state saat ini di atas NPC.")]
    public TextMesh stateLabel;

    [Header("Scene View Debug Gizmo")]
    [Tooltip("Tampilkan garis raycast sensor di Scene view (Editor saja, tidak muncul di build).")]
    public bool showSensorGizmo = true;

    [Tooltip("Jika true, garis sensor selalu digambar sepanjang detectionRange meski belum kena apapun. Jika false, hanya digambar saat NPC aktif jadi trigger lane (Lane2/Lane4).")]
    public bool alwaysShowGizmoRay = true;

    public AvoidanceState CurrentState { get; private set; } = AvoidanceState.Normal;

    private NPCCarController npcController;
    private AmbulanceController detectedAmbulance;

    private float mergeTimer;
    private float mergeStartX;
    private float mergeTargetX;
    private LaneMarker.LaneId mergeTargetLane;
    private float mergeDirectionSign;

    // Dipakai untuk lazy-register: laneId yang tercatat di LaneRegistry saat
    // ini, agar kita bisa Unregister dari lane yang BENAR jika laneId
    // berubah di luar proses merge (mis. di-set manual via Inspector/script lain).
    private LaneMarker.LaneId registeredLane;
    private bool isRegistered = false;

    private void Awake()
    {
        npcController = GetComponent<NPCCarController>();
    }

    // PENTING: TIDAK register di OnEnable(). NPCCarSpawner mengisi
    // controller.laneId SETELAH Instantiate() (yang berarti SETELAH
    // OnEnable() Unity terpanggil otomatis). Kalau register di OnEnable(),
    // NPC akan tercatat di lane DEFAULT (Lane1) dulu, bukan lane sebenarnya
    // yang baru di-assign sesaat setelahnya oleh spawner - menyebabkan NPC
    // "nyangkut" terdaftar di lane lama selamanya (bug yang sempat terjadi).
    //
    // Sebagai gantinya, registrasi dilakukan lazy di Update() pertama kali,
    // setelah laneId final dari spawner pasti sudah ter-assign.
    void EnsureRegistered()
    {
        if (npcController == null) return;

        if (!isRegistered)
        {
            LaneRegistry.Instance.Register(npcController);
            registeredLane = npcController.laneId;
            isRegistered = true;
            return;
        }

        // Jaga-jaga: kalau laneId berubah di luar proses merge normal
        // (FinishMerge sudah handle unregister/register sendiri dengan benar),
        // sinkronkan ulang supaya tidak ada entry stale di lane lama.
        if (registeredLane != npcController.laneId)
        {
            LaneRegistry.Instance.Unregister(npcController);
            LaneRegistry.Instance.Register(npcController);
            registeredLane = npcController.laneId;
        }
    }

    private void OnDisable()
    {
        if (npcController != null && isRegistered)
        {
            LaneRegistry.Instance.Unregister(npcController);
            isRegistered = false;
        }
    }

    private void Update()
    {
        if (Time.timeScale == 0f) return;
        if (npcController == null) return;

        EnsureRegistered();

        // Hanya jalur trigger (Lane2 / Lane4) yang punya logic ini.
        // NPC di lane lain langsung idle, tidak ada overhead sensor/gap-check.
        bool isTriggerLane = npcController.laneId == LaneMarker.LaneId.Lane2 ||
                              npcController.laneId == LaneMarker.LaneId.Lane4;

        if (!isTriggerLane && CurrentState == AvoidanceState.Normal)
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
                // Tetap di Normal selamanya setelah merge (sudah di lane baru).
                CurrentState = AvoidanceState.Normal;
                break;
        }

        UpdateVisuals();
    }

    // =========================================================
    // STATE: NORMAL -> cek sensor ambulance
    // =========================================================
    void TickNormal()
    {
        AmbulanceController ambulance = DetectAmbulance();
        if (ambulance != null)
        {
            detectedAmbulance = ambulance;
            CurrentState = AvoidanceState.AmbulanceDetected;
        }
    }

    // =========================================================
    // STATE: AMBULANCE_DETECTED -> langsung cek gap ke jalur tujuan
    // =========================================================
    void TickAmbulanceDetected()
    {
        if (detectedAmbulance == null)
        {
            // Ambulance sudah hilang/destroyed sebelum NPC sempat merge.
            CurrentState = AvoidanceState.Normal;
            return;
        }

        CurrentState = AvoidanceState.WaitingForGap;
    }

    // =========================================================
    // STATE: WAITING_FOR_GAP -> tunggu NPC di lane tujuan lewat dulu
    // =========================================================
    void TickWaitingForGap()
    {
        if (detectedAmbulance == null)
        {
            CurrentState = AvoidanceState.Normal;
            return;
        }

        LaneMarker.LaneId targetLane = LaneMarker.GetMergeTargetLane(npcController.laneId);
        float referenceZ = transform.position.z;

        NPCCarController blocker = LaneRegistry.Instance.FindBlockingNpcAhead(targetLane, referenceZ, npcController);

        if (blocker == null)
        {
            StartMerge(targetLane);
        }
        // Jika masih ada blocker, tetap di state ini frame berikutnya.
    }

    void StartMerge(LaneMarker.LaneId targetLane)
    {
        mergeTargetLane = targetLane;
        mergeDirectionSign = LaneMarker.GetMergeDirectionSign(npcController.laneId);
        mergeStartX = transform.position.x;
        mergeTargetX = mergeStartX + (mergeDirectionSign * laneWidth);
        mergeTimer = 0f;
        CurrentState = AvoidanceState.Merging;
    }

    // =========================================================
    // STATE: MERGING -> lerp posisi X + rotasi belok, Z tetap mengikuti
    // gerak normal NPCCarController (tidak diubah, hanya dibaca posenya)
    // =========================================================
    void TickMerging()
    {
        mergeTimer += Time.deltaTime;
        float t = Mathf.Clamp01(mergeTimer / mergeDuration);

        // Lerp posisi X (Z & Y tetap, sudah digerakkan oleh NPCCarController.Update
        // lewat Translate sebelumnya di frame yang sama / frame sebelumnya).
        float newX = Mathf.Lerp(mergeStartX, mergeTargetX, t);
        Vector3 pos = transform.position;
        pos.x = newX;
        transform.position = pos;

        // Rotasi belok naik-turun: 0 -> maxAngle -> 0, mengikuti Sin(t * PI).
        float turnAmount = Mathf.Sin(t * Mathf.PI) * maxTurnAngle * mergeDirectionSign;
        Vector3 euler = transform.eulerAngles;
        euler.y = npcController.transform.eulerAngles.y; // basis rotasi asal NPC
        transform.rotation = Quaternion.Euler(0f, GetBaseYRotation() + turnAmount, 0f);

        if (t >= 1f)
        {
            FinishMerge();
        }
    }

    float baseYRotationCache;
    bool baseYRotationCached = false;

    float GetBaseYRotation()
    {
        if (!baseYRotationCached)
        {
            baseYRotationCache = transform.eulerAngles.y;
            baseYRotationCached = true;
        }
        return baseYRotationCache;
    }

    void FinishMerge()
    {
        // Snap posisi X final & rotasi balik netral.
        Vector3 pos = transform.position;
        pos.x = mergeTargetX;
        transform.position = pos;

        transform.rotation = Quaternion.Euler(0f, GetBaseYRotation(), 0f);

        // Update laneId NPC secara permanen ke jalur baru, dan re-register
        // ke LaneRegistry supaya NPC lain bisa query keberadaannya di lane baru.
        LaneRegistry.Instance.Unregister(npcController);
        npcController.laneId = mergeTargetLane;
        LaneRegistry.Instance.Register(npcController);
        registeredLane = mergeTargetLane;

        // Pastikan NPC yang baru pindah ke Lane1 (dari Lane2) berperilaku
        // PERSIS seperti NPC Lane1 asli - termasuk arah gerak saat player
        // brake (Vector3.forward / menjauhi player). Lane4 -> Lane5 tidak
        // butuh ini karena trafficType-nya (TowardPlayer) sudah konsisten
        // sejak awal, tidak ada percabangan perilaku brake di sana.
        if (mergeTargetLane == LaneMarker.LaneId.Lane1)
        {
            npcController.ApplySameDirectionBehavior();
        }

        detectedAmbulance = null;
        baseYRotationCached = false;
        CurrentState = AvoidanceState.Merged;
    }

    // =========================================================
    // SENSOR DETECTION (Opsi B - sensor lokal per NPC)
    // =========================================================
    AmbulanceController DetectAmbulance()
    {
        // Arah sensor tergantung lane:
        // Lane2 (SameDirection) -> ambulance datang dari belakang -> sensor ke Vector3.back
        // Lane4 (TowardPlayer)  -> ambulance datang dari depan    -> sensor ke Vector3.forward
        // PENTING: Baik NPC Lane2 (SameDirection) maupun Lane4 (TowardPlayer)
        // SAMA-SAMA bergerak Vector3.back secara default (lihat
        // NPCCarController.HandleSameDirectionNPC & HandleTowardPlayerNPC -
        // keduanya translate Vector3.back saat kondisi normal/tidak brake).
        // Sedangkan AmbulanceController SELALU bergerak Vector3.forward,
        // tidak peduli lane berapa dia spawn (tidak pernah diubah arahnya).
        //
        // Karena ambulance spawn dari titik Z kecil (spawnPointBehind) dan
        // bergerak ke Z besar (forward), sementara NPC bergerak ke Z kecil
        // (back), ambulance pada KEDUA lane akan mengejar NPC dari ARAH
        // YANG SAMA, yaitu dari belakang NPC (Z lebih kecil dari posisi NPC
        // saat ambulance belum menyusul). Maka sensor yang benar untuk
        // KEDUA lane adalah Vector3.back, BUKAN dibedakan forward/back
        // berdasarkan lane seperti asumsi sebelumnya.
        Vector3 sensorDirection = Vector3.back;

        // Angkat origin sedikit di atas pivot supaya sensor horizontal tidak
        // nyangkut ke collider jalan/ground yang sejajar permukaan.
        Vector3 origin = transform.position + Vector3.up * sensorHeightOffset;

        // PENTING: ambulanceLayerMask sebaiknya HANYA berisi layer khusus
        // ambulance (bukan "Everything"). Jika "Everything", sensor bisa
        // duluan mengenai NPC lain yang ada di antara sensor dan ambulance.
        //
        // Menggunakan SphereCastAll (bukan RaycastAll) dengan radius
        // sensorRadius - raycast garis tipis mudah meleset kalau posisi X
        // ambulance tidak 100% persis sejajar dengan X NPC (selisih kecil
        // saat setup spawn point manual di Editor sudah cukup membuat
        // raycast garis tidak pernah kena collider ambulance sama sekali).
        // SphereCast membuat sensor punya "lebar" sehingga tetap mendeteksi
        // walau sedikit tidak sejajar.
        RaycastHit[] hits = Physics.SphereCastAll(origin, sensorRadius, sensorDirection, detectionRange, ambulanceLayerMask);

        AmbulanceController closest = null;
        float closestDist = float.MaxValue;

        for (int i = 0; i < hits.Length; i++)
        {
            RaycastHit hit = hits[i];
            if (!hit.collider.CompareTag("Ambulance")) continue;

            if (hit.distance < closestDist)
            {
                AmbulanceController ambulance = hit.collider.GetComponent<AmbulanceController>();
                if (ambulance != null)
                {
                    closest = ambulance;
                    closestDist = hit.distance;
                }
            }
        }

        return closest;
    }

    // =========================================================
    // VISUAL (opsional, dipakai untuk demo expo - tidak wajib diisi)
    // =========================================================
    void UpdateVisuals()
    {
        if (sensorLineRenderer != null)
        {
            bool showLine = detectedAmbulance != null && CurrentState != AvoidanceState.Merged;
            sensorLineRenderer.enabled = showLine;

            if (showLine)
            {
                sensorLineRenderer.positionCount = 2;
                sensorLineRenderer.SetPosition(0, transform.position);
                sensorLineRenderer.SetPosition(1, detectedAmbulance.transform.position);

                Color lineColor = CurrentState switch
                {
                    AvoidanceState.AmbulanceDetected => Color.yellow,
                    AvoidanceState.WaitingForGap => new Color(1f, 0.5f, 0f), // oranye
                    AvoidanceState.Merging => Color.cyan,
                    _ => Color.white
                };
                sensorLineRenderer.startColor = lineColor;
                sensorLineRenderer.endColor = lineColor;
            }
        }

        if (stateLabel != null)
        {
            stateLabel.text = CurrentState switch
            {
                AvoidanceState.Normal => "NORMAL",
                AvoidanceState.AmbulanceDetected => "DETECTED - CHECKING LANE",
                AvoidanceState.WaitingForGap => "WAITING TO MERGE",
                AvoidanceState.Merging => "MERGING",
                AvoidanceState.Merged => "NORMAL",
                _ => ""
            };
        }
    }

    // =========================================================
    // SCENE VIEW GIZMO (Editor only, tidak muncul di build/Game view)
    // Menggambar garis raycast sensor sesuai arah & jarak deteksi,
    // dengan warna mengikuti state saat ini supaya mudah dibedakan
    // saat development/debugging langsung dari Scene view.
    // =========================================================
    void OnDrawGizmos()
    {
        if (!showSensorGizmo) return;
        if (npcController == null) npcController = GetComponent<NPCCarController>();
        if (npcController == null) return;

        bool isLane2 = npcController.laneId == LaneMarker.LaneId.Lane2;
        bool isLane4 = npcController.laneId == LaneMarker.LaneId.Lane4;
        bool isTriggerLane = isLane2 || isLane4;

        // NPC di lane lain (Lane1/Lane3/Lane5) TIDAK punya logic sensor
        // sungguhan, jadi jangan gambar garis arah apapun untuk mereka -
        // cukup titik kecil netral supaya tetap terlihat statusnya
        // "bukan trigger lane" tanpa menyesatkan seolah dia punya sensor.
        if (!isTriggerLane)
        {
            if (alwaysShowGizmoRay)
            {
                Gizmos.color = Color.gray;
                Gizmos.DrawWireSphere(transform.position + Vector3.up * sensorHeightOffset, 0.2f);
            }
            return;
        }

        // Baik Lane2 maupun Lane4 sama-sama sensor ke Vector3.back, karena
        // ambulance (lane manapun) selalu bergerak Vector3.forward dari
        // titik spawn Z kecil - lihat penjelasan lengkap di DetectAmbulance().
        Vector3 sensorDirection = Vector3.back;

        Vector3 origin = transform.position + Vector3.up * sensorHeightOffset;
        Vector3 end = origin + sensorDirection * detectionRange;

        Color gizmoColor = CurrentState switch
        {
            AvoidanceState.AmbulanceDetected => Color.yellow,
            AvoidanceState.WaitingForGap => new Color(1f, 0.5f, 0f),
            AvoidanceState.Merging => Color.cyan,
            AvoidanceState.Merged => Color.green,
            _ => Color.green
        };

        Gizmos.color = gizmoColor;
        Gizmos.DrawLine(origin, end);

        // Gambar lingkaran radius di origin & ujung sensor, merepresentasikan
        // "lebar" SphereCast yang sesungguhnya dipakai DetectAmbulance().
        // Ini membantu memvisualisasikan kenapa sensor tetap kena ambulance
        // walau posisi X tidak 100% sejajar.
        DrawWireCircleOnPlane(origin, sensorRadius, gizmoColor);
        DrawWireCircleOnPlane(end, sensorRadius, gizmoColor);

        // Kalau sedang mendeteksi ambulance, gambar garis tambahan
        // langsung ke posisi ambulance yang terdeteksi (memastikan
        // target sensor sesuai, berguna saat debug).
        if (Application.isPlaying && detectedAmbulance != null)
        {
            Gizmos.color = gizmoColor;
            Gizmos.DrawLine(origin, detectedAmbulance.transform.position);
            Gizmos.DrawWireSphere(detectedAmbulance.transform.position, 0.5f);
        }
    }

    // Helper: gambar lingkaran kecil pada plane XY (tegak lurus arah sensor Z)
    // untuk memvisualisasikan radius SphereCast di titik tertentu.
    void DrawWireCircleOnPlane(Vector3 center, float radius, Color color)
    {
        Gizmos.color = color;
        int segments = 16;
        Vector3 prevPoint = center + new Vector3(radius, 0f, 0f);
        for (int i = 1; i <= segments; i++)
        {
            float angle = (i / (float)segments) * Mathf.PI * 2f;
            Vector3 nextPoint = center + new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0f);
            Gizmos.DrawLine(prevPoint, nextPoint);
            prevPoint = nextPoint;
        }
    }
}