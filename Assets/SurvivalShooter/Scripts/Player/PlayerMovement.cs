using UnityEngine;
using UnityEngine.InputSystem;

// Di chuyển bằng WASD/phím mũi tên, xoay người chơi theo con trỏ chuột
[RequireComponent(typeof(Rigidbody))]
public class PlayerMovement : MonoBehaviour
{
    public float speed = 6f;

    Vector3 movement;
    Animator anim;
    Rigidbody playerRigidbody;
    Camera cam;
    PlayerStats stats;
    int floorMask;

    void Awake()
    {
        stats = GetComponent<PlayerStats>();
        floorMask = LayerMask.GetMask("Floor");
        anim = GetComponent<Animator>();
        playerRigidbody = GetComponent<Rigidbody>();
        cam = Camera.main;
    }

    void FixedUpdate()
    {
        Vector2 input = ReadMoveInput();
        Move(input.x, input.y);
        Turning();
        anim.SetBool("IsWalking", input.sqrMagnitude > 0.01f);
    }

    Vector2 ReadMoveInput()
    {
        Vector2 v = Vector2.zero;
        var kb = Keyboard.current;
        if (kb != null)
        {
            if (kb.wKey.isPressed || kb.upArrowKey.isPressed) v.y += 1;
            if (kb.sKey.isPressed || kb.downArrowKey.isPressed) v.y -= 1;
            if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) v.x += 1;
            if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) v.x -= 1;
        }
        var pad = Gamepad.current;
        if (pad != null && pad.leftStick.ReadValue().sqrMagnitude > 0.04f)
            v = pad.leftStick.ReadValue();
        return Vector2.ClampMagnitude(v, 1f);
    }

    void Move(float h, float v)
    {
        movement.Set(h, 0f, v);
        movement = movement * speed * stats.moveSpeedMultiplier * Time.deltaTime;
        playerRigidbody.MovePosition(transform.position + movement);
    }

    void Turning()
    {
        if (Mouse.current == null || cam == null) return;
        Ray camRay = cam.ScreenPointToRay(Mouse.current.position.ReadValue());
        if (Physics.Raycast(camRay, out RaycastHit floorHit, 100f, floorMask))
        {
            Vector3 playerToMouse = floorHit.point - transform.position;
            playerToMouse.y = 0f;
            if (playerToMouse.sqrMagnitude < 0.001f) return;
            playerRigidbody.MoveRotation(Quaternion.LookRotation(playerToMouse));
        }
    }
}
