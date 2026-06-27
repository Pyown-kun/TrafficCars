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

    private bool canMove = true;
    private float horizontalInput;

    [Header("Crosswalk Lock")]
    [SerializeField] private bool movementLockedByCrosswalk = false;

    private void Update()
    {
        if (Time.timeScale == 0f)
            return;

        HandleInput();

        if (canMove)
        {
            MoveHorizontal();
        }

        UpdateWorldBrakeState();
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

        isBraking =
            keyboard.spaceKey.isPressed ||
            keyboard.sKey.isPressed;
    }

    void MoveHorizontal()
    {
        if (movementLockedByCrosswalk)
            return;

        transform.Translate(
            Vector3.right *
            horizontalInput *
            moveSpeed *
            Time.deltaTime,
            Space.World);

        Vector3 pos = transform.position;
        pos.x = Mathf.Clamp(pos.x, minX, maxX);
        transform.position = pos;
    }

    /// <summary>
    /// Mengirim status brake player ke WorldSpeedManager.
    /// Player tetap diam, yang melambat adalah dunia.
    /// </summary>
    void UpdateWorldBrakeState()
    {
        if (WorldSpeedManager.Instance == null)
            return;

        // Saat crosswalk mengunci player,
        // dunia tetap dianggap sedang brake.
        WorldSpeedManager.Instance.SetBraking(IsBraking());
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
    /// Dipakai animator.
    /// True hanya jika player benar-benar menekan brake.
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

    public float GetHorizontalInput()
    {
        return horizontalInput;
    }

    public bool CanMove()
    {
        return canMove;
    }
}