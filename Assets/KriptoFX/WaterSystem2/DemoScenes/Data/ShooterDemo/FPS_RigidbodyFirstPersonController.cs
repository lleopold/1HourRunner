using System;
using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
public class FPS_RigidbodyFirstPersonController : MonoBehaviour
{
    public Camera cam;

    [Header("Movement")]
    public float walkSpeed = 6f;
    public float runSpeed = 10f;
    public float acceleration = 50f;
    public float deceleration = 50f;
    public float airControl = 0.3f;

    [Header("Jump")]
    public float jumpForce = 7f;
    public float groundCheckDistance = 0.2f;

    [Header("Mouse Look")]
    public float sensX = 2f;
    public float sensY = 2f;

    private Rigidbody rb;
    private CapsuleCollider capsule;

    private float yaw;
    private float pitch;

    private bool grounded;
    private bool jumpRequest;


#if UNITY_6000_0_OR_NEWER
    private Vector3 RbVelocity
    {
        get => rb.linearVelocity;
        set => rb.linearVelocity = value;
    }
#else
    private Vector3 RbVelocity
    {
        get => rb.velocity;
        set => rb.velocity = value;
    }
#endif

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;

        capsule = GetComponent<CapsuleCollider>();

        yaw = transform.eulerAngles.y;
        pitch = cam.transform.localEulerAngles.x;
        
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible   = false;
    }

    void Update()
    {
        HandleMouseLook();

#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current.spaceKey.wasPressedThisFrame && grounded)
#else
        if (Input.GetKeyDown(KeyCode.Space) && grounded)
#endif
            jumpRequest = true;

#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current.escapeKey.wasPressedThisFrame)
#else
        if (Input.GetKeyDown(KeyCode.Escape))
#endif
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible   = true;
        }
    }

    void FixedUpdate()
    {
        CheckGround();
        HandleMovement();
        HandleJump();
    }

    void HandleMouseLook()
    {
#if ENABLE_INPUT_SYSTEM
        Vector2 mouseDelta = Mouse.current.delta.ReadValue();
        float mx = mouseDelta.x * sensX * 0.1f;
        float my = mouseDelta.y * sensY * 0.1f;
#else
        float mx = Input.GetAxisRaw("Mouse X") * sensX;
        float my = Input.GetAxisRaw("Mouse Y") * sensY;
#endif

        yaw += mx;
        pitch -= my;
        pitch = Mathf.Clamp(pitch, -89f, 89f);

        transform.rotation = Quaternion.Euler(0, yaw, 0);
        cam.transform.localRotation = Quaternion.Euler(pitch, 0, 0);
    }

    void HandleMovement()
    {
#if ENABLE_INPUT_SYSTEM
        var kb = Keyboard.current;
        float h = (kb.dKey.isPressed ? 1f : 0f) - (kb.aKey.isPressed ? 1f : 0f);
        float v = (kb.wKey.isPressed ? 1f : 0f) - (kb.sKey.isPressed ? 1f : 0f);
        bool running = kb.leftShiftKey.isPressed;
#else
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        bool running = Input.GetKey(KeyCode.LeftShift);
#endif

        Vector2 input   = new Vector2(h, v);
        Vector3 moveDir = transform.forward * input.y + transform.right * input.x;
        if (moveDir.magnitude > 1f)
            moveDir.Normalize();

        float   targetSpeed    = running ? runSpeed : walkSpeed;
        Vector3 targetVelocity = moveDir * targetSpeed;
        Vector3 velocityXZ     = new Vector3(RbVelocity.x, 0, RbVelocity.z);

        if (grounded)
        {
            Vector3 newVel = Vector3.MoveTowards(
                velocityXZ,
                moveDir.magnitude > 0.1f ? targetVelocity : Vector3.zero,
                (moveDir.magnitude > 0.1f ? acceleration : deceleration) * Time.fixedDeltaTime
            );
            RbVelocity = new Vector3(newVel.x, RbVelocity.y, newVel.z);
        }
        else
        {
            Vector3 airVel = Vector3.MoveTowards(
                velocityXZ,
                targetVelocity,
                acceleration * airControl * Time.fixedDeltaTime
            );
            RbVelocity = new Vector3(airVel.x, RbVelocity.y, airVel.z);
        }
    }

    void HandleJump()
    {
        if (jumpRequest)
        {
            RbVelocity = new Vector3(RbVelocity.x, 0, RbVelocity.z);
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        }

        jumpRequest = false;
    }

    void CheckGround()
    {
        float radius   = capsule.radius * 0.95f;
        float castDist = (capsule.height / 2f) - radius + 0.05f;

        grounded = Physics.SphereCast(
            transform.position,
            radius,
            Vector3.down,
            out _,
            castDist + 0.02f,
            Physics.AllLayers,
            QueryTriggerInteraction.Ignore
        );
    }
}