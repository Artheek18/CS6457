using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class GrappleHook : MonoBehaviour
{
    [Header("Aim / Fire")]
    [Tooltip("Camera used to aim the grapple (usually your player camera).")]
    public Camera aimCamera;
    [Tooltip("Where the rope appears to come from (gun muzzle / hand). If null, uses this transform.")]
    public Transform ropeOrigin;
    [Tooltip("Layers that can be grappled.")]
    public LayerMask grappleMask = ~0;
    [Tooltip("Max distance the grapple can reach.")]
    public float maxGrappleDistance = 40f;

    [Header("Rope Physics (SpringJoint)")]
    [Tooltip("Spring stiffness. Higher = stronger pull.")]
    public float spring = 60f;
    [Tooltip("Damping. Higher = less oscillation.")]
    public float damper = 7f;
    [Tooltip("How much the joint resists mass. 1 is fine.")]
    public float massScale = 1f;
    [Tooltip("Slack at latch time (multiplies hit distance). <1 = immediate pull, >1 = slacky rope.")]
    [Range(0.5f, 1.25f)] public float latchSlackFactor = 0.95f;

    [Header("Reel Controls")]
    [Tooltip("Hold to reel in (shrinks rope length).")]
    public KeyCode reelInKey = KeyCode.LeftShift;
    [Tooltip("Hold to reel out (extends rope length).")]
    public KeyCode reelOutKey = KeyCode.LeftControl;
    [Tooltip("Meters per second to change max rope length.")]
    public float reelSpeed = 12f;

    [Header("Input")]
    [Tooltip("Press to fire/attach, release to detach.")]
    public KeyCode fireKey = KeyCode.Mouse1; // Right mouse

    [Header("Rope Visuals (optional)")]
    [Tooltip("Assign a LineRenderer to draw the rope.")]
    public LineRenderer line;
    [Tooltip("Smooth rope end motion.")]
    public float ropeSmoothing = 25f;

    // runtime
    private Rigidbody rb;
    private SpringJoint joint;
    private Vector3 grapplePoint;
    private float targetRopeLen;
    private Vector3 smoothedRopeEnd;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (!aimCamera)
        {
            // Try to find a camera on self or in children
            aimCamera = GetComponentInChildren<Camera>();
            if (!aimCamera) aimCamera = Camera.main;
        }
        if (!ropeOrigin) ropeOrigin = transform;
        if (line)
        {
            line.positionCount = 0;
            line.enabled = false;
        }
    }

    void Update()
    {
        // Fire / Attach
        if (Input.GetKeyDown(fireKey))
            TryStartGrapple();

        // Detach on release
        if (Input.GetKeyUp(fireKey))
            StopGrapple();

        // Reel while attached
        if (joint)
        {
            float delta = 0f;
            if (Input.GetKey(reelInKey)) delta -= reelSpeed * Time.deltaTime;
            if (Input.GetKey(reelOutKey)) delta += reelSpeed * Time.deltaTime;

            if (Mathf.Abs(delta) > 0f)
            {
                targetRopeLen = Mathf.Clamp(targetRopeLen + delta, 1f, maxGrappleDistance);
                joint.maxDistance = targetRopeLen;
                // minDistance keeps a little slack so it doesn't jitter
                joint.minDistance = Mathf.Min(joint.maxDistance * 0.5f, joint.maxDistance - 0.1f);
            }
        }
    }

    void LateUpdate()
    {
        // Rope rendering
        if (line && joint)
        {
            if (!line.enabled)
            {
                line.enabled = true;
                line.positionCount = 2;
                smoothedRopeEnd = ropeOrigin.position;
            }

            // Smooth the rope tail for nicer visuals
            smoothedRopeEnd = Vector3.Lerp(smoothedRopeEnd, grapplePoint, 1f - Mathf.Exp(-ropeSmoothing * Time.deltaTime));

            line.SetPosition(0, ropeOrigin.position);
            line.SetPosition(1, smoothedRopeEnd);
        }
        else if (line && !joint && line.enabled)
        {
            line.positionCount = 0;
            line.enabled = false;
        }
    }

    private void TryStartGrapple()
    {
        if (!aimCamera) return;

        Ray ray = new Ray(aimCamera.transform.position, aimCamera.transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, maxGrappleDistance, grappleMask, QueryTriggerInteraction.Ignore))
        {
            grapplePoint = hit.point;

            if (!joint)
                joint = gameObject.AddComponent<SpringJoint>();

            joint.autoConfigureConnectedAnchor = false;
            joint.connectedAnchor = grapplePoint;

            float dist = Vector3.Distance(transform.position, grapplePoint);
            targetRopeLen = dist * latchSlackFactor;

            joint.maxDistance = targetRopeLen;
            joint.minDistance = Mathf.Min(targetRopeLen * 0.5f, targetRopeLen - 0.1f);

            joint.spring = spring;
            joint.damper = damper;
            joint.massScale = massScale;

            // Small initial tug to feel responsive (preserve vertical)
            Vector3 toPoint = (grapplePoint - transform.position);
            Vector3 planar = Vector3.ProjectOnPlane(toPoint, Vector3.up).normalized;
            rb.linearVelocity = new Vector3(planar.x, rb.linearVelocity.y, planar.z) + planar * 2f;
        }
    }

    public void StopGrapple()
    {
        if (joint)
        {
            Destroy(joint);
            joint = null;
        }
        if (line)
        {
            line.positionCount = 0;
            line.enabled = false;
        }
    }

    // Optional: expose status
    public bool IsGrappling => joint != null;
    public Vector3 CurrentGrapplePoint => grapplePoint;
}
