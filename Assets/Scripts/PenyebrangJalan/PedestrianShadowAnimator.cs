using UnityEngine;

[RequireComponent(typeof(PedestrianController))]
public class PedestrianShadowAnimator : MonoBehaviour
{
    [Header("Renderer")]
    [SerializeField] private SpriteRenderer shadowRenderer;

    [Header("Shadow Animation")]
    [SerializeField] private Sprite idleShadow;
    [SerializeField] private Sprite[] walkShadowSprites;

    [SerializeField]
    private float animationFPS = 8f;

    [Header("Direction")]
    [SerializeField]
    private bool autoFlip = true;

    private PedestrianController controller;

    private float timer;
    private int frame;

    private void Awake()
    {
        controller = GetComponentInParent<PedestrianController>();

        if (shadowRenderer == null)
            shadowRenderer = GetComponent<SpriteRenderer>();
    }

    private void Update()
    {
        if (controller == null)
            return;

        UpdateDirection();

        switch (controller.CurrentState)
        {
            case PedestrianController.PedestrianState.Waiting:

                shadowRenderer.sprite = idleShadow;
                ResetAnimation();
                break;

            case PedestrianController.PedestrianState.Crossing:

                AnimateWalk();
                break;

            case PedestrianController.PedestrianState.Finished:

                shadowRenderer.sprite = idleShadow;
                ResetAnimation();
                break;
        }
    }

    private void AnimateWalk()
    {
        if (walkShadowSprites == null ||
            walkShadowSprites.Length == 0)
            return;

        timer += Time.deltaTime;

        if (timer >= 1f / animationFPS)
        {
            timer = 0f;

            frame++;

            if (frame >= walkShadowSprites.Length)
                frame = 0;

            shadowRenderer.sprite =
                walkShadowSprites[frame];
        }
    }

    private void UpdateDirection()
    {
        if (!autoFlip)
            return;

        if (controller.startPoint == null ||
            controller.endPoint == null)
            return;

        Vector3 dir =
            controller.endPoint.position -
            controller.startPoint.position;

        shadowRenderer.flipX = dir.x < 0f;
    }

    private void ResetAnimation()
    {
        timer = 0f;
        frame = 0;
    }
}