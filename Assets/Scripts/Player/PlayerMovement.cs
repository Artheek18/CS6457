using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{

    public float speed = 6f;
    public float jumpForce = 10f;
    public float jumpCooldown = 0.5f;  

    private Rigidbody rb;
    private float movementX;
    private float movementY;
    private float lastJumpTime = -Mathf.Infinity;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    void OnMove(InputValue movementValue)
    {
        Vector2 v = movementValue.Get<Vector2>();
        movementX = v.x;
        movementY = v.y;
    }

    void FixedUpdate()
    {

        Vector3 input = new Vector3(movementX, 0f, movementY);
        Vector3 vel = rb.linearVelocity;
        rb.linearVelocity = new Vector3(input.normalized.x * speed, vel.y, input.normalized.z * speed);
    }

    void OnJump(InputValue _)
    {
        if (Time.time - lastJumpTime < jumpCooldown)
            return; 
        Debug.Log("Jump pressed!");
        rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        lastJumpTime = Time.time;
    }
}
