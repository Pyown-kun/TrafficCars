using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class CrosswalkTrafficTrigger : MonoBehaviour
{
    public enum StopTarget
    {
        SameDirectionOnly,
        TowardPlayerOnly,
        BothDirections
    }

    [Header("Reference")]
    public PedestrianCrosswalkZone crosswalkZone;

    [Header("NPC Filter")]
    [Tooltip("Trigger ini menghentikan NPC tipe apa")]
    public StopTarget stopTarget = StopTarget.SameDirectionOnly;

    // === TAMBAHAN BARU ===
    [Header("Player Handling")]
    [Tooltip("Jika true, instance trigger ini SAMA SEKALI tidak menangani player (tidak lock/unlock, tidak memanggil FreezeCrosswalkForCrossing). Aktifkan ini untuk instance yang dipasang di area Front (atau area lain yang sudah punya trigger khusus seperti FrontCrosswalkTrafficTrigger untuk menangani player), sehingga trigger ini HANYA menahan NPC sesuai stopTarget tanpa ikut mengunci player.")]
    public bool ignorePlayer = false;
    // === END TAMBAHAN BARU ===

    private readonly HashSet<NPCCarController> npcsInside = new HashSet<NPCCarController>();
    private bool playerInside = false;

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
        if (crosswalkZone == null) return;

        npcsInside.RemoveWhere(npc => npc == null);

        // =========================================================
        // NPC yang ada di trigger SELALU berhenti,
        // tidak peduli player brake atau tidak
        //
        // === FIX ===
        // Sebelumnya dipanggil tanpa parameter zone
        // (npc.SetStoppedByCrosswalk(true);), yang membuat
        // currentCrosswalkZone NPC di-overwrite jadi null setiap frame
        // (karena default parameter zone = null di NPCCarController),
        // walau stoppedByCrosswalk tetap true. Akibatnya
        // FollowCrosswalkWhileStopped() selalu return lebih awal
        // (currentCrosswalkZone == null) tanpa pernah translate apapun -
        // NPC freeze total di tempat. Sekarang selalu kirim crosswalkZone,
        // konsisten dengan FrontCrosswalkTrafficTrigger & RearCrosswalkTrafficTrigger.
        // =========================================================
        foreach (var npc in npcsInside)
        {
            if (npc != null)
            {
                npc.SetStoppedByCrosswalk(true, crosswalkZone);
            }
        }

        if (crosswalkZone.playerCar == null) return;

        // === FIX ===
        // Jika ignorePlayer aktif, instance trigger ini sama sekali tidak
        // ikut campur dengan lock/unlock player atau memanggil
        // FreezeCrosswalkForCrossing - biarkan trigger khusus lain
        // (mis. FrontCrosswalkTrafficTrigger / RearCrosswalkTrafficTrigger)
        // yang menangani player di lokasi ini.
        if (ignorePlayer) return;

        // =========================================================
        // Player hanya bisa berhenti jika:
        // - berada di dalam trigger
        // - sedang brake
        // - kecepatan dunia sudah cukup rendah
        // =========================================================
        if (playerInside &&
            crosswalkZone.playerCar.IsBraking() &&
            crosswalkZone.CanFreezePlayer())
        {
            crosswalkZone.FreezeCrosswalkForCrossing();
        }
        else
        {
            if (!crosswalkZone.IsFrozenForCrossing())
            {
                crosswalkZone.playerCar.SetCrosswalkMovementLock(false);
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (crosswalkZone == null) return;

        // =========================
        // PLAYER
        // =========================
        if (other.CompareTag("Player"))
        {
            // Jika ignorePlayer aktif, jangan track playerInside sama sekali
            // - tidak ada efek apapun terhadap player dari instance ini.
            if (!ignorePlayer)
            {
                playerInside = true;
            }
            return;
        }

        // =========================
        // NPC
        // =========================
        NPCCarController npc = other.GetComponentInParent<NPCCarController>();
        if (npc == null) return;

        if (!ShouldStopNPC(npc))
            return;

        npcsInside.Add(npc);
        crosswalkZone.RegisterNPCInsideStopArea(npc);

        // === FIX ===
        // Sebelumnya: npc.SetStoppedByCrosswalk(true); tanpa zone.
        // Sekarang konsisten kirim crosswalkZone agar currentCrosswalkZone
        // NPC tidak ter-overwrite jadi null.
        npc.SetStoppedByCrosswalk(true, crosswalkZone);
    }

    private void OnTriggerExit(Collider other)
    {
        if (crosswalkZone == null) return;

        // =========================
        // PLAYER
        // =========================
        if (other.CompareTag("Player"))
        {
            playerInside = false;

            // Jika ignorePlayer aktif, instance ini tidak pernah mengunci
            // player sejak awal, jadi tidak perlu (dan tidak boleh) ikut
            // memanggil unlock juga - biarkan trigger khusus lain yang
            // menangani siklus lock/unlock player di lokasi ini.
            if (ignorePlayer) return;

            // === FIX ===
            // Sebelumnya lock hanya dilepas jika !IsFrozenForCrossing(),
            // sehingga jika player brake saat pedestrian masih menyeberang
            // lalu keluar trigger sebelum pedestrian selesai, lock bisa
            // bertahan sampai player jauh di area lain (mis. Front).
            // Sesuai keputusan: player tidak boleh terkunci di luar area
            // trigger ini, jadi selalu unlock begitu keluar secara fisik.
            if (crosswalkZone.playerCar != null)
            {
                crosswalkZone.playerCar.SetCrosswalkMovementLock(false);
            }

            return;
        }

        // =========================
        // NPC
        // =========================
        NPCCarController npc = other.GetComponentInParent<NPCCarController>();
        if (npc == null) return;

        if (npcsInside.Contains(npc))
        {
            npcsInside.Remove(npc);
            crosswalkZone.UnregisterNPCInsideStopArea(npc);
        }
    }

    bool ShouldStopNPC(NPCCarController npc)
    {
        switch (stopTarget)
        {
            case StopTarget.SameDirectionOnly:
                return npc.trafficType == NPCCarController.TrafficType.SameDirection;

            case StopTarget.TowardPlayerOnly:
                return npc.trafficType == NPCCarController.TrafficType.TowardPlayer;

            case StopTarget.BothDirections:
                return true;
        }

        return false;
    }
}