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
    private bool isDashing = false;
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
        {
            StartDash();
        }

        if (isDashing && Time.time >= dashEndTime)
        {
            EndDash();
        }
    }

    private void StartDash()
    {
        // Get input-based movement vector in world space
        Vector3 moveDir = transform.forward * input.Forward + transform.right * input.Turn;

        if (moveDir.sqrMagnitude < 0.01f)
        {
            // If no input, default to forward dash
            moveDir = transform.forward;
        }

        moveDir.Normalize();

        isDashing = true;
        lastDashTime = Time.time;
        dashEndTime = Time.time + dashDuration;

        // Cancel current velocity, then dash
        rb.linearVelocity = Vector3.zero;
        rb.AddForce(moveDir * dashForce, ForceMode.VelocityChange);
    }

    private void EndDash()
    {
        isDashing = false;
        rb.linearVelocity *= 0.5f; // optional slowdown after dash
    }
}
