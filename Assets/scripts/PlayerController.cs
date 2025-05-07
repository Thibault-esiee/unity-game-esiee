using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private float walkSpeed = 1f;
    [SerializeField] private float runSpeed = 3f;
    [SerializeField] private float lookSenesitivity = 5f;

    private float currentSpeed;

    private Vector2 moveVector;
    private Vector2 LookVector;
    private Vector3 rotation;

    private CharacterController characterController;
    private Animator animator;

    private bool isRunning = false;

    void Start()
    {
        characterController = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
        currentSpeed = walkSpeed;
    }

    void Update()
    {
        Move();
        Rotate();
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        moveVector = context.ReadValue<Vector2>();
        bool isWalking = moveVector.magnitude > 0;
        animator.SetBool("IsWalking", isWalking);

        // Met à jour IsRunning seulement si on bouge
        if (!isWalking && isRunning)
        {
            isRunning = false;
            currentSpeed = walkSpeed;
            animator.SetBool("IsRunning", false);
        }
    }

    public void OnRun(InputAction.CallbackContext context)
    {
        if (context.performed && moveVector.magnitude > 0) // On ne toggle que si on marche
        {
            isRunning = !isRunning;
            currentSpeed = isRunning ? runSpeed : walkSpeed;
            animator.SetBool("IsRunning", isRunning);

            Debug.Log("Shift pressé. isRunning = " + isRunning);
        }
    }

    private void Move()
    {
        Vector3 move = transform.right * moveVector.x + transform.forward * moveVector.y;
        characterController.Move(move * currentSpeed * Time.deltaTime);
    }

    public void OnLook(InputAction.CallbackContext context)
    {
        LookVector = context.ReadValue<Vector2>();
    }

    private void Rotate()
    {
        rotation.y += LookVector.x * lookSenesitivity * Time.deltaTime;
        transform.localEulerAngles = rotation;
    }
}