using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Dashing : MonoBehaviour
{
    [Header("Dash Settings")]
    [SerializeField] private float dashForce = 20f;
    [SerializeField] private float dashCooldown = 1f;
    [SerializeField] private float dashDuration = 0.2f;

    [Header("Keybind")]
    [SerializeField] private KeyCode dashKey = KeyCode.LeftShift;

    private Rigidbody rb;
    private CharacterInputController input;

    public bool IsDashing { get; private set; }  // <-- expose to PlayerMovement
    private float dashEndTime = 0f;
    private float lastDashTime = -Mathf.Infinity;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        input = GetComponent<CharacterInputController>();
    }

    private void Update()
    {
        if (Input.GetKeyDown(dashKey) && Time.time >= lastDashTime + dashCooldown)
            StartDash();

        if (IsDashing && Time.time >= dashEndTime)
            EndDash();
    }

    private void StartDash()
    {
        Vector3 moveDir = Vector3.zero;
        if (input != null)
            moveDir = transform.forward * input.Forward + transform.right * input.Turn;

        if (moveDir.sqrMagnitude < 0.01f) moveDir = transform.forward;
        moveDir.Normalize();

        IsDashing = true;
        lastDashTime = Time.time;
        dashEndTime = Time.time + dashDuration;

        // zero current velocity, then apply an instantaneous velocity change
        rb.linearVelocity = Vector3.zero;                              // ✅
        rb.AddForce(moveDir * dashForce, ForceMode.VelocityChange);
    }

    private void EndDash()
    {
        IsDashing = false;
        rb.linearVelocity *= 0.5f;                                     // ✅ optional slow-down
    }
}
