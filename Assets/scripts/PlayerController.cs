using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private float walkSpeed = 1f;
    [SerializeField] private float runSpeed = 3f;
    [SerializeField] private float lookSensitivity = 5f;
    
    private CharacterController characterController;
    private Animator animator;

    private Vector2 moveInput;
    private float currentSpeed;

    private void Start()
    {
        characterController = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
        currentSpeed = walkSpeed;
    }

    private void Update()
    {
        HandleMovement();        
    }

    private void HandleMovement()
    {
        Vector3 moveDirection = new Vector3(moveInput.x, 0f, moveInput.y);

        if (moveDirection.sqrMagnitude > 0.01f)
        {
            Quaternion r = Quaternion.LookRotation(moveDirection, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, r, lookSensitivity);
        }
        characterController.Move(currentSpeed * Time.deltaTime * moveDirection.normalized);
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
        bool isWalking = moveInput.magnitude > 0;

        animator.SetBool("IsWalking", isWalking);
    }
}