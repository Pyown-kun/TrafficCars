using System;
using System.Collections.Generic;
using UnityEngine;

public class DamageReceiver : MonoBehaviour
{
    [SerializeField] private PlayerDamageFlash damageFlash;

    [Serializable]
    public class DamageSetting
    {
        [Header("Detection")]
        public string tagName;
        public LayerMask layerMask;

        [Header("Damage")]
        public float vehicleDamage = 10f;
        public float tofuDamage = 5f;
        public float slowdown = 2f;
    }

    [Header("Damage Settings")]
    [SerializeField]
    private List<DamageSetting> damageSettings = new();

    [Header("Cooldown")]
    [SerializeField]
    private float collisionCooldown = 0.5f;

    private float lastCollisionTime;

    private void Awake()
    {
        damageFlash = GetComponent<PlayerDamageFlash>();
    }

    private void OnCollisionEnter(Collision collision)
    {
        TryApplyDamage(collision.gameObject);
    }

    private void OnTriggerEnter(Collider other)
    {
        TryApplyDamage(other.gameObject);
    }

    private void TryApplyDamage(GameObject target)
    {

        if (damageFlash != null && damageFlash.IsInvincible)
        return;

        if (Time.time < lastCollisionTime + collisionCooldown)
            return;

        foreach (DamageSetting setting in damageSettings)
        {
            bool tagMatched = !string.IsNullOrEmpty(setting.tagName) &&
                              target.CompareTag(setting.tagName);

            bool layerMatched =
                ((1 << target.layer) & setting.layerMask.value) != 0;

            if (!tagMatched && !layerMatched)
                continue;

            VehicleHealth.Instance.TakeDamage(setting.vehicleDamage);

            TofuQuality.Instance.ReduceQuality(setting.tofuDamage);

            WorldSpeedManager.Instance.ApplyCollisionSlowdown(setting.slowdown);

            damageFlash?.PlayFlash();

            lastCollisionTime = Time.time;

            Debug.Log(
                $"Hit {target.name} | HP -{setting.vehicleDamage} | " +
                $"Tofu -{setting.tofuDamage}");

            return;
        }
    }
}