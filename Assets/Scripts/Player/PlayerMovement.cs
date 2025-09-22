using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{

    private Rigidbody rb;
    private float movementX;
    private float movementY;
    public float speed = 0;
    public float jumpForce = 0;
    public float jumpCooldown = 0.5f;
    private float lastJumpTime;
    private bool isGrounded = true;
    private int count;



    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }



    void OnMove(InputValue movementValue)
    {
        Vector2 movementVector = movementValue.Get<Vector2>();

        movementX = movementVector.x;
        movementY = movementVector.y;

    }

    void FixedUpdate()
    {
        Vector3 movement = new Vector3(movementX, 0.0f, movementY);
        rb.AddForce(movement * speed);

    }

    void OnJump()
    {
        if (isGrounded && Time.time - lastJumpTime >= jumpCooldown)
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            lastJumpTime = Time.time;
            isGrounded = false;
        }

    }
    void OnCollisionEnter(Collision other)
    {


        if (other.gameObject.CompareTag("ground"))
        {
            isGrounded = true;
        }
    }




}
