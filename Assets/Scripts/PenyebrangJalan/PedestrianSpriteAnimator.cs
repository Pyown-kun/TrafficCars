using UnityEngine;

[RequireComponent(typeof(PedestrianController))]
public class PedestrianSpriteAnimator : MonoBehaviour
{
    [Header("Renderer")]
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Header("Animation")]
    [SerializeField] private Sprite idleSprite;
    [SerializeField] private Sprite[] walkSprites;

    [Header("Animation")]
    [SerializeField] private float animationFPS = 8f;

    [Header("Direction")]
    [SerializeField] private bool autoFlip = true;

    private PedestrianController controller;

    private float timer;
    private int frame;

    private void Awake()
    {
        controller = GetComponent<PedestrianController>();

        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    void Update()
    {
        if (controller == null)
            return;

        UpdateDirection();

        switch (controller.CurrentState)
        {
            case PedestrianController.PedestrianState.Waiting:

                spriteRenderer.sprite = idleSprite;
                ResetWalkAnimation();
                break;

            case PedestrianController.PedestrianState.Crossing:

                AnimateWalk();
                break;

            case PedestrianController.PedestrianState.Finished:

                spriteRenderer.sprite = idleSprite;
                ResetWalkAnimation();
                break;
        }
    }

    void AnimateWalk()
    {
        if (walkSprites == null || walkSprites.Length == 0)
            return;

        timer += Time.deltaTime;

        if (timer >= 1f / animationFPS)
        {
            timer = 0f;

            frame = (frame + 1) % walkSprites.Length;

            spriteRenderer.sprite = walkSprites[frame];
        }
    }

    void UpdateDirection()
    {
        if (!autoFlip)
            return;

        if (controller.startPoint == null ||
            controller.endPoint == null)
            return;

        Vector3 dir =
            controller.endPoint.position -
            controller.startPoint.position;

        // Kanan
        if (dir.x > 0f)
            spriteRenderer.flipX = true;

        // Kiri
        else if (dir.x < 0f)
            spriteRenderer.flipX = false;
    }

    void ResetWalkAnimation()
    {
        timer = 0f;
        frame = 0;
    }
}