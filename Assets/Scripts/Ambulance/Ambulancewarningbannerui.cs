using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Banner UI peringatan "ambulance datang dari belakang", tampil di bawah
/// layar. Membaca status dari AmbulanceRearWarningTrigger.IsWarningActive
/// (static property yang sudah ada, TIDAK diubah sama sekali) lewat polling
/// di Update() - script ini tidak mengubah, tidak menyentuh, dan tidak
/// memanggil apapun di AmbulanceRearWarningTrigger atau sistem NPC manapun.
///
/// Field `triggerReference` di Inspector murni untuk VALIDASI VISUAL -
/// memastikan ada GameObject AmbulanceRearWarningTrigger yang benar-benar
/// di-setup di scene dan di-drag ke sini (kalau field ini "None", langsung
/// kelihatan di Inspector bahwa setup belum lengkap). Status warning yang
/// dibaca tetap lewat IsWarningActive (static, global) seperti sebelumnya,
/// BUKAN dibaca khusus dari instance triggerReference ini - karena status
/// itu sendiri didesain static di AmbulanceRearWarningTrigger.
///
/// Cara pasang di Unity:
/// 1. Pastikan ada Canvas (Screen Space - Overlay atau Camera) di scene.
/// 2. Buat GameObject UI baru di bawah Canvas, misal beri nama
///    "AmbulanceWarningBanner".
/// 3. Tambahkan child Image (background banner) dan child Text/TextMeshPro
///    (teks peringatan) di bawahnya, atur posisi/anchor ke bagian BAWAH
///    layar (anchor min/max Y = 0, pivot Y = 0, posisikan sedikit di atas
///    tepi bawah).
/// 4. Tempelkan script ini ke GameObject "AmbulanceWarningBanner".
/// 5. Drag root Image banner (yang akan di-enable/disable) ke field
///    `bannerRoot`. Drag komponen Text ke field `warningText` (opsional,
///    bisa dikosongkan jika hanya ingin tampilkan Image tanpa teks).
/// 6. Drag GameObject yang punya component AmbulanceRearWarningTrigger
///    (yang sudah dipasang di scene, di belakang player) ke field
///    `triggerReference` - jika field ini "None" saat Play, Console akan
///    memberi warning agar mudah diketahui setup belum lengkap.
/// 7. `bannerRoot` akan otomatis disembunyikan (SetActive(false)) saat
///    Awake, supaya tidak tampil sebelum ada warning sungguhan.
/// </summary>
public class AmbulanceWarningBannerUI : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Drag GameObject yang punya component AmbulanceRearWarningTrigger di scene. Murni untuk validasi/memastikan setup sudah benar - jika kosong (None), warning akan muncul di Console saat Play.")]
    public AmbulanceRearWarningTrigger triggerReference;

    [Tooltip("Root GameObject banner (biasanya Image background) yang akan di-aktifkan/nonaktifkan sesuai status warning.")]
    public GameObject bannerRoot;

    [Tooltip("Opsional - komponen Text (uGUI) untuk menampilkan pesan peringatan. Boleh dikosongkan jika bannerRoot sudah cukup (mis. hanya icon).")]
    public Text warningText;

    [Header("Message")]
    [Tooltip("Teks yang ditampilkan saat warning aktif.")]
    public string warningMessage = "AMBULANCE MENDEKAT DARI BELAKANG!";

    private bool lastAppliedState = false;
    private bool hasAppliedInitialState = false;

    private void Awake()
    {
        // Sembunyikan banner di awal - status awal IsWarningActive selalu
        // false saat scene baru dimulai (tidak ada ambulance yang sudah
        // trigger sebelum game berjalan).
        if (bannerRoot != null)
        {
            bannerRoot.SetActive(false);
        }

        if (warningText != null)
        {
            warningText.text = warningMessage;
        }

        if (triggerReference == null)
        {
            Debug.LogWarning($"[AmbulanceWarningBannerUI] '{name}': field 'triggerReference' kosong (None). " +
                              "Drag GameObject yang punya component AmbulanceRearWarningTrigger ke field ini " +
                              "untuk memastikan setup sudah benar. Banner tetap akan mengikuti status global " +
                              "IsWarningActive meski field ini kosong, tapi mengisi field ini membantu memastikan " +
                              "ada trigger yang benar-benar terpasang di scene.");
        }
    }

    private void Update()
    {
        // Polling - cek status setiap frame, dan hanya menerapkan
        // SetActive() saat status benar-benar berubah dari frame
        // sebelumnya (menghindari pemanggilan SetActive berulang tiap
        // frame tanpa perlu).
        bool isActive = AmbulanceRearWarningTrigger.IsWarningActive;

        if (hasAppliedInitialState && isActive == lastAppliedState) return;

        ApplyBannerState(isActive);

        lastAppliedState = isActive;
        hasAppliedInitialState = true;
    }

    private void ApplyBannerState(bool isActive)
    {
        if (bannerRoot == null) return;

        // Snap instan - konsisten dengan style icon indicator NPC
        // (NPCEmojiIndicator) yang juga muncul/hilang tanpa animasi.
        bannerRoot.SetActive(isActive);
    }
}