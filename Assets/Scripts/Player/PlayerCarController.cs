using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerCarController : MonoBehaviour
{
    [Header("Horizontal Movement")]
    public float moveSpeed = 8f;
    public float minX = -4f;
    public float maxX = 4f;

    [Header("Brake")]
    public bool isBraking;
    public float brakeSlowMultiplier = 0.5f;

    private bool canMove = true;
    private float horizontalInput;

    [Header("Crosswalk Lock")]
    [SerializeField] private bool movementLockedByCrosswalk = false;

    private void Update()
    {
        if (!canMove) return;
        if (Time.timeScale == 0f) return;

        HandleInput();
        MoveHorizontal();
    }

    void HandleInput()
    {
        Keyboard keyboard = Keyboard.current;

        if (keyboard == null)
        {
            horizontalInput = 0f;
            isBraking = false;
            return;
        }

        if (movementLockedByCrosswalk)
        {
            horizontalInput = 0f;
            isBraking = true;
            return;
        }

        horizontalInput = 0f;

        if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)
            horizontalInput = -1f;

        if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed)
            horizontalInput = 1f;

        isBraking = keyboard.spaceKey.isPressed || keyboard.sKey.isPressed;
    }

    void MoveHorizontal()
    {
        if (movementLockedByCrosswalk)
            return;

        transform.Translate(Vector3.right * horizontalInput * moveSpeed * Time.deltaTime, Space.World);

        Vector3 pos = transform.position;
        pos.x = Mathf.Clamp(pos.x, minX, maxX);
        transform.position = pos;
    }

    public void StopPlayer()
    {
        canMove = false;
    }

    public void ResumePlayer()
    {
        canMove = true;
    }

    /// <summary>
    /// Dipakai gameplay / NPC / traffic logic.
    /// True jika player brake manual ATAU sedang dikunci crosswalk.
    /// </summary>
    public bool IsBraking()
    {
        return isBraking || movementLockedByCrosswalk;
    }

    /// <summary>
    /// Dipakai khusus animator / visual.
    /// True hanya saat player benar-benar menekan brake manual,
    /// bukan saat brake dipaksa oleh crosswalk lock.
    /// </summary>
    public bool IsManualBraking()
    {
        return isBraking && !movementLockedByCrosswalk;
    }

    public void SetCrosswalkMovementLock(bool value)
    {
        movementLockedByCrosswalk = value;

        if (value)
        {
            horizontalInput = 0f;
            isBraking = true;
        }
    }

    public bool IsMovementLockedByCrosswalk()
    {
        return movementLockedByCrosswalk;
    }

    /// <summary>
    /// Untuk animator turn left / right.
    /// Nilai umumnya -1, 0, 1.
    /// </summary>
    public float GetHorizontalInput()
    {
        return horizontalInput;
    }

    public bool CanMove()
    {
        return canMove;
    }
}