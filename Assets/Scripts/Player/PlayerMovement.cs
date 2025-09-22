using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Move")]
    [SerializeField] float moveSpeed = 6f;

    [Header("Jump")]
    [SerializeField] float jumpForce = 10f;
    [SerializeField] float jumpCooldown = 0.35f;
    [SerializeField] KeyCode jumpKey = KeyCode.Space;

    [Header("Ledge Grab")]
    [SerializeField] KeyCode climbKey = KeyCode.Q;
    [SerializeField] KeyCode dropKey  = KeyCode.S;
    [SerializeField] float topProbeStartY = 1.5f;      // higher point (from center)
    [SerializeField] float topProbeEndY   = 0.7f;      // lower point (from center)
    [SerializeField] float forwardProbeYOffset = 0.1f; // small gap below top for forward probe
    [SerializeField] float hangBack = 0.10f;           // back from edge when hanging
    [SerializeField] float hangDown = 0.35f;           // how far below the lip to hang
    [SerializeField] float climbForward = 0.55f;
    [SerializeField] float climbUp = 0.25f;
    [SerializeField] float climbTime = 0.25f;
    [SerializeField] LayerMask platformMask = ~0;      // set in Inspector; auto-set to "Platform" in Awake if present

    Rigidbody rb;
    CapsuleCollider capsule;
    Dashing dash; // to detect active dash

    float lastJumpTime = -Mathf.Infinity;

    bool hanging = false;
    bool climbing = false;
    Vector3 ledgeTopPoint;
    Vector3 ledgeNormal;
    Vector3 hangPoint;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        capsule = GetComponent<CapsuleCollider>();
        dash = GetComponent<Dashing>();

        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

    }

    void Update()
    {
        // Skip writing horizontal velocity while dashing / hanging / climbing
        bool dashActive = (dash != null && dash.IsDashing);
        if (!(hanging || climbing || dashActive))
        {
            float ix = Input.GetAxisRaw("Horizontal");
            float iz = Input.GetAxisRaw("Vertical");

            Vector3 wish = (transform.forward * iz + transform.right * ix);
            if (wish.sqrMagnitude > 1f) wish.Normalize();

            Vector3 v = rb.linearVelocity;
            rb.linearVelocity = new Vector3(wish.x * moveSpeed, v.y, wish.z * moveSpeed);
        }

        // Jump (allowed when not hanging/climbing; dashing behavior is up to you)
        if (!hanging && !climbing && Input.GetKeyDown(jumpKey) && Time.time - lastJumpTime >= jumpCooldown && IsGrounded())
        {
            Vector3 vv = rb.linearVelocity; vv.y = 0f; rb.linearVelocity = vv;
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            lastJumpTime = Time.time;
        }

        // Ledge inputs while hanging
        if (hanging && !climbing)
        {
            if (Input.GetKeyDown(climbKey)) StartCoroutine(ClimbUp());
            if (Input.GetKeyDown(dropKey))  ExitHang();
        }
    }

    void FixedUpdate()
    {
        if (!hanging && !climbing)
            TryLedgeGrab();
    }

    // ---------- Ledge grab (tutorial-style, but corrected) ----------
    void TryLedgeGrab()
    {
        // Only when falling
        if (rb.linearVelocity.y >= 0f) return;

        // (1) Top linecast to find the ledge lip (slightly in front to catch edge)
        Vector3 center = transform.position;
        Vector3 start  = center + Vector3.up * topProbeStartY + transform.forward * 0.15f;
        Vector3 end    = center + Vector3.up * topProbeEndY   + transform.forward * 0.15f;

        if (!Physics.Linecast(start, end, out RaycastHit downHit, platformMask)) return;

        // (2) Forward linecast at the ledge height to get the wall face/normal
        float y = downHit.point.y - forwardProbeYOffset;
        Vector3 fwdStart = new Vector3(center.x, y, center.z);
        Vector3 fwdEnd   = fwdStart + transform.forward * 1.0f;

        if (!Physics.Linecast(fwdStart, fwdEnd, out RaycastHit fwdHit, platformMask)) return;

        // Save data for hang/climb
        ledgeTopPoint = downHit.point;
        ledgeNormal   = fwdHit.normal;

        // Hang point: use ledge normal (not transform.forward)
        Vector3 back = -ledgeNormal.normalized * hangBack;
        Vector3 down = Vector3.down * hangDown;
        hangPoint = new Vector3(fwdHit.point.x, ledgeTopPoint.y, fwdHit.point.z) + back + down;

        EnterHang();
    }

    void EnterHang()
    {
        hanging = true;

        rb.linearVelocity = Vector3.zero;
        rb.useGravity = false;
        rb.isKinematic = true;

        transform.position = hangPoint;

        // Face the ledge
        Vector3 f = -ledgeNormal; f.y = 0f;
        if (f.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.LookRotation(f.normalized, Vector3.up);
    }

    void ExitHang()
    {
        hanging = false;
        rb.isKinematic = false;
        rb.useGravity = true;
    }

    System.Collections.IEnumerator ClimbUp()
    {
        climbing = true;

        // Move onto the surface: forward (via ledge normal) + up
        Vector3 flatFwd = new Vector3(-ledgeNormal.x, 0f, -ledgeNormal.z).normalized;
        Vector3 target  = new Vector3(ledgeTopPoint.x, ledgeTopPoint.y, ledgeTopPoint.z)
                        + flatFwd * climbForward
                        + Vector3.up * climbUp;

        Vector3 start = transform.position;
        Quaternion r0 = transform.rotation;
        Quaternion r1 = Quaternion.LookRotation(flatFwd, Vector3.up);

        bool prevDetect = rb.detectCollisions;
        rb.detectCollisions = false;

        float t = 0f, dur = Mathf.Max(0.01f, climbTime);
        while (t < 1f)
        {
            t += Time.fixedDeltaTime / dur;
            float e = Mathf.SmoothStep(0f, 1f, t);
            rb.MovePosition(Vector3.Lerp(start, target, e));
            rb.MoveRotation(Quaternion.Slerp(r0, r1, e));
            yield return new WaitForFixedUpdate();
        }

        rb.detectCollisions = prevDetect;
        ExitHang();
        climbing = false;
    }

    // ---------- simple ground check ----------
    bool IsGrounded()
    {
        Bounds b = capsule.bounds;
        return Physics.Raycast(b.center, Vector3.down, b.extents.y + 0.1f, ~0, QueryTriggerInteraction.Ignore);
    }

    // Expose states if other scripts care
    public bool IsHanging => hanging;
    public bool IsClimbing => climbing;
}
