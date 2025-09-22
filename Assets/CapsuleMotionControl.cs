using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
[RequireComponent(typeof(CharacterInputController))]
public class CapsuleMotionControl : MonoBehaviour
{
    // --- Refs ---
    private Rigidbody rbody;
    private CharacterInputController cinput;

    // --- Movement tuning ---
    [Header("Movement")]
    [Tooltip("Forward speed on ground (m/s).")]
    public float moveSpeed = 6f;

    [Tooltip("How fast we reach target horizontal speed on ground.")]
    public float acceleration = 25f;

    [Tooltip("0..1, how much horizontal control in air.")]
    [Range(0f, 1f)] public float airControl = 0.35f;

    [Tooltip("Degrees per second when turning with Turn input.")]
    public float turnSpeed = 540f;

    [Header("Jump")]
    public float jumpForce = 6.5f;
    public float jumpCooldown = 0.5f;
    private float lastJumpTime;

    [Header("Physics")]
    [Tooltip("Drag applied when grounded (helps reduce 'ice skating').")]
    public float groundedDrag = 5f;

    [Tooltip("Drag applied in air.")]
    public float airDrag = 0.1f;

    [Tooltip("Max ground normal angle considered jumpable.")]
    public float jumpableGroundNormalMaxAngle = 45f;

    // --- Grounding ---
    private int groundContactCount = 0;
    public bool closeToJumpableGround;
    public bool IsGrounded => groundContactCount > 0;

    // --- Cached inputs (from CharacterInputController) ---
    private float _inputForward;  // -1..1 (already filtered & clamped by CIC)
    private float _inputTurn;     // -1..1
    private bool _jumpPressed;    // read in Update, consumed in FixedUpdate

    void Awake()
    {
        rbody = GetComponent<Rigidbody>();
        cinput = GetComponent<CharacterInputController>();

        if (!rbody) Debug.LogWarning("Rigidbody not found");
        if (!cinput) Debug.LogWarning("CharacterInputController not found");

        // Keep the capsule upright & stable
        rbody.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        rbody.interpolation = RigidbodyInterpolation.Interpolate;
        rbody.collisionDetectionMode = CollisionDetectionMode.Continuous;
    }

    void Update()
    {
        // Pull filtered inputs from your CharacterInputController
        if (cinput && cinput.enabled)
        {
            _inputForward = cinput.Forward; // already speed-limited inside CIC
            _inputTurn = cinput.Turn;

            // GetButtonDown("Jump") → CIC.Jump becomes true for one Update frame
            if (cinput.Jump)
                _jumpPressed = true; // latch until FixedUpdate consumes it
        }
        else
        {
            _inputForward = 0f;
            _inputTurn = 0f;
        }
    }

    void FixedUpdate()
    {
        // Robust ground check: contacts OR ray proximity (matches your previous pattern)
        bool grounded = IsGrounded || CharacterCommon.CheckGroundNear(
            transform.position,
            jumpableGroundNormalMaxAngle,
            0.85f,
            0f,
            out closeToJumpableGround
        );

        // --- Rotation (tank-style: Turn rotates Y) ---
        if (Mathf.Abs(_inputTurn) > 0.0001f)
        {
            float yaw = _inputTurn * turnSpeed * Time.fixedDeltaTime;
            Quaternion delta = Quaternion.Euler(0f, yaw, 0f);
            rbody.MoveRotation(rbody.rotation * delta);
        }

        // --- Horizontal movement ---
        // Desired horizontal velocity along *current forward*
        Vector3 currentVel = rbody.linearVelocity;
        Vector3 fwd = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
        Vector3 targetHorizVel = fwd * (_inputForward * moveSpeed);

        float accel = grounded ? acceleration : acceleration * airControl;

        // Smoothly approach target horizontal speed
        Vector3 newHoriz = Vector3.MoveTowards(
            new Vector3(currentVel.x, 0f, currentVel.z),
            targetHorizVel,
            accel * Time.fixedDeltaTime
        );

        // Keep existing vertical velocity
        Vector3 newVel = new Vector3(newHoriz.x, currentVel.y, newHoriz.z);

        // --- Jump (consume latched press here to avoid missed FixedUpdate) ---
        if (_jumpPressed && grounded && (Time.time - lastJumpTime) >= jumpCooldown)
        {
            newVel.y = 0f; // optional: clear small downward velocity before jump
            rbody.linearVelocity = newVel;
            rbody.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            lastJumpTime = Time.time;
        }
        else
        {
            rbody.linearVelocity = newVel;
        }

        // we consumed the jump press this physics step
        _jumpPressed = false;

        // --- Drag ---
        rbody.linearDamping = grounded ? groundedDrag : airDrag;
    }

    // --- Grounding via tag "ground" (matches your scripts) ---
    void OnCollisionEnter(Collision collision)
    {
        if (collision.transform.CompareTag("ground"))
        {
            ++groundContactCount;

            // Optional landing event (comment out if you don’t use it)
            // EventManager.TriggerEvent<MinionLandsEvent, Vector3, float>(
            //     collision.contacts[0].point,
            //     collision.impulse.magnitude
            // );
        }
    }

    void OnCollisionExit(Collision collision)
    {
        if (collision.transform.CompareTag("ground"))
        {
            --groundContactCount;
            if (groundContactCount < 0) groundContactCount = 0;
        }
    }
}
