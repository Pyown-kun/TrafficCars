using System.Collections;
using UnityEngine;

public class PlayerDamageFlash : MonoBehaviour
{
    [Header("3D Model")]
    [SerializeField] private GameObject playerModel;

    [Header("Flash")]
    [SerializeField] private float flashDuration = 0.6f;

    [SerializeField] private float blinkInterval = 0.08f;

    private Renderer[] renderers;

    public bool IsInvincible { get; private set; }

    private Coroutine flashRoutine;

    private void Awake()
    {
        if (playerModel == null)
            playerModel = gameObject;

        renderers = playerModel.GetComponentsInChildren<Renderer>(true);
    }

    public void PlayFlash()
    {
        if (flashRoutine != null)
            StopCoroutine(flashRoutine);

        flashRoutine = StartCoroutine(FlashRoutine());
    }

    private IEnumerator FlashRoutine()
    {
        IsInvincible = true;

        float timer = 0f;
        bool visible = false;

        while (timer < flashDuration)
        {
            visible = !visible;

            foreach (Renderer renderer in renderers)
            {
                if (renderer != null)
                    renderer.enabled = visible;
            }

            yield return new WaitForSeconds(blinkInterval);

            timer += blinkInterval;
        }

        foreach (Renderer renderer in renderers)
        {
            if (renderer != null)
                renderer.enabled = true;
        }

        IsInvincible = false;
        flashRoutine = null;
    }
}