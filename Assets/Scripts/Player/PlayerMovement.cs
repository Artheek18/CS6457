using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class PlayerMovement : MonoBehaviour
{
    [Header("Move & Jump")]
    public float speed = 6f;
    public float jumpForce = 10f;
    public float jumpCooldown = 0.5f;

    [Header("Ledge Grab (no layers/tags)")]
    public float chestHeight = 1.1f;
    public float forwardCheckRadius = 0.2f;
    public float forwardCheckDistance = 0.6f;
    public float maxLedgeHeight = 1.6f;
    public float headClearance = 0.2f;
    public float downCheckDistance = 1.0f;
    public float hangBackOffset = 0.25f;
    public float hangDownOffset = 0.35f;
    public float climbTime = 0.25f;

    // Optional: prevent instant re-grab after climbing up
    public float regrabBlockTime = 0.25f;
    private float lastClimbEndTime = -999f;

    private Rigidbody rb;
    private CapsuleCollider capsule;
    private float movementX, movementY;
    private float lastJumpTime = -Mathf.Infinity;

    private enum State { Normal, Hanging, Climbing }
    private State state = State.Normal;

    // ledge data
    private Vector3 ledgeTop;
    private Vector3 ledgeNormal;
    private Vector3 hangPoint;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        capsule = GetComponent<CapsuleCollider>();
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
    }

    // -------- Input --------
    void OnMove(InputValue movementValue)
    {
        Vector2 v = movementValue.Get<Vector2>();
        movementX = v.x;
        movementY = v.y;

        if (state == State.Hanging)
        {
            if (v.y > 0.5f && state != State.Climbing) StartCoroutine(ClimbUpRoutine()); // press Up to climb (optional)
            if (v.y < -0.5f) ExitHangRestore(); // press Down to drop
        }
    }

    void OnJump(InputValue _)
    {
        if (state == State.Hanging)
        {
            if (state != State.Climbing) StartCoroutine(ClimbUpRoutine());
            return;
        }

        if (state != State.Normal) return;
        if (Time.time - lastJumpTime < jumpCooldown) return;

        rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        lastJumpTime = Time.time;
    }

    // -------- Simulation --------
    void FixedUpdate()
    {
        if (state == State.Normal)
        {
            // Move on XZ (velocity-based, no vertical tampering)
            Vector3 input = new Vector3(movementX, 0f, movementY);
            Vector3 desired = input.normalized * speed;
            Vector3 vel = rb.linearVelocity;                         // FIX
            rb.linearVelocity = new Vector3(desired.x, vel.y, desired.z); // FIX

            TryStartLedgeGrab(); // only detect when free
        }
        // When Hanging/Climbing, movement is paused by design
    }

    // -------- Ledge Detection (raycasts, no layers/tags) --------
    void TryStartLedgeGrab()
    {
        // block immediate re-grab after climbing
        if (Time.time - lastClimbEndTime < regrabBlockTime) return;

        // only try to catch ledges while moving downward
        if (rb.linearVelocity.y >= 0f) return; // FIX

        Vector3 feet = new Vector3(transform.position.x, capsule.bounds.min.y, transform.position.z);
        Vector3 chest = feet + Vector3.up * chestHeight;

        // 1) Spherecast forward from chest to find a wall
        if (Physics.SphereCast(chest, forwardCheckRadius, transform.forward,
                               out RaycastHit wallHit, forwardCheckDistance,
                               ~0, QueryTriggerInteraction.Ignore))
        {
            // 2) From above that hit, cast down to find the top surface (the ledge lip)
            Vector3 upStart = wallHit.point + Vector3.up * maxLedgeHeight;
            if (Physics.Raycast(upStart, Vector3.down, out RaycastHit topHit,
                                downCheckDistance + maxLedgeHeight, ~0, QueryTriggerInteraction.Ignore))
            {
                // Must be above our feet and reasonably flat
                if (topHit.point.y > feet.y + 0.1f && topHit.normal.y > 0.6f)
                {
                    ledgeNormal = wallHit.normal;
                    ledgeTop = topHit.point;

                    // compute a good hanging point
                    Vector3 towardWall = -ledgeNormal.normalized;
                    hangPoint = new Vector3(wallHit.point.x, ledgeTop.y - hangDownOffset, wallHit.point.z)
                                - towardWall * hangBackOffset;

                    // head clearance check
                    float head = capsule.bounds.extents.y;
                    Vector3 headPos = hangPoint + Vector3.up * (head + headClearance);
                    bool blocked = Physics.CheckCapsule(hangPoint, headPos, capsule.radius * 0.95f, ~0, QueryTriggerInteraction.Ignore);
                    if (!blocked) EnterHang();
                }
            }
        }
    }

    void EnterHang()
    {
        state = State.Hanging;

        rb.linearVelocity = Vector3.zero; // FIX
        rb.useGravity = false;

        transform.position = hangPoint;

        Vector3 fwd = -ledgeNormal; fwd.y = 0f;
        if (fwd.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.LookRotation(fwd.normalized, Vector3.up);
    }

    void ExitHangRestore()
    {
        rb.useGravity = true;
        state = State.Normal;
        lastJumpTime = Time.time;
        lastClimbEndTime = Time.time; // start anti-regrab window
    }

    IEnumerator ClimbUpRoutine()
    {
        state = State.Climbing;

        // target: slightly forward and up onto the surface
        Vector3 flatFwd = new Vector3(-ledgeNormal.x, 0f, -ledgeNormal.z).normalized;
        Vector3 target = new Vector3(ledgeTop.x, ledgeTop.y, ledgeTop.z)
                         + flatFwd * 0.35f
                         + Vector3.up * 0.2f;

        Vector3 start = transform.position;
        Quaternion startRot = transform.rotation;
        Quaternion endRot = Quaternion.LookRotation(flatFwd, Vector3.up);

        float t = 0f;
        float dur = Mathf.Max(0.01f, climbTime);
        while (t < 1f)
        {
            t += Time.fixedDeltaTime / dur;
            float e = Mathf.SmoothStep(0f, 1f, t);
            transform.position = Vector3.Lerp(start, target, e);
            transform.rotation = Quaternion.Slerp(startRot, endRot, e);
            yield return new WaitForFixedUpdate();
        }

        ExitHangRestore();
    }
}
