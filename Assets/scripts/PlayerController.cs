using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private float walkSpeed = 1f;
    [SerializeField] private float runSpeed = 3f;
    [SerializeField] private float lookSensitivity = 5f;

    private bool isDead = false;
    [SerializeField] private GameObject deathBody;
    [SerializeField] private GameObject playerModel;
    private float currentSpeed;

    private Vector2 moveVector;
    private Vector2 LookVector;
    private Vector3 rotation;

    private CharacterController characterController;
    private Animator animator;

    private Vector2 moveInput;
    private bool isRunning = false;

    [Header("Audio")]
    [SerializeField] private AudioSource footstepsAudioSource;
    [SerializeField] private AudioClip[] walkFootstepSounds;
    [SerializeField] private AudioClip[] runFootstepSounds;
    [SerializeField] private float walkStepInterval = 0.5f;
    [SerializeField] private float runStepInterval = 0.3f;
    private float footstepTimer = 0f;
    private float currentStepInterval;

    public GameObject DeathBody { get => deathBody; }

    private void Start()
    {
        characterController = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
        currentSpeed = walkSpeed;

        if (footstepsAudioSource == null)
        {
            footstepsAudioSource = gameObject.AddComponent<AudioSource>();
            footstepsAudioSource.playOnAwake = false;
            footstepsAudioSource.spatialBlend = 1.0f;
            footstepsAudioSource.volume = 1.5f;
        }

        currentStepInterval = walkStepInterval;
    }

    private void Update()
    {
        if (isDead) return;
        Move();
        HandleMovement();
        HandleFootsteps();
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

    private void HandleFootsteps()
    {
        if (moveInput.sqrMagnitude > 0.01f)
        {
            
            footstepTimer += Time.deltaTime;

            
            if (footstepTimer >= currentStepInterval)
            {
                PlayFootstepSound();
                footstepTimer = 0f;
            }
        }
        else
        {
            
            footstepTimer = 0f;
        }
    }

    private void PlayFootstepSound()
    {
        if (footstepsAudioSource == null) return;

        AudioClip[] currentFootstepSounds = isRunning ? runFootstepSounds : walkFootstepSounds;
        
        if (currentFootstepSounds == null || currentFootstepSounds.Length == 0) return;

        
        int randomIndex = Random.Range(0, currentFootstepSounds.Length);
        AudioClip footstepSound = currentFootstepSounds[randomIndex];

        if (footstepSound != null)
        {
            footstepsAudioSource.clip = footstepSound;
            footstepsAudioSource.Play();
        }
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
        bool isWalking = moveInput.magnitude > 0;

        animator.SetBool("IsWalking", isWalking);

        if (!isWalking && isRunning)
        {
            isRunning = false;
            currentSpeed = walkSpeed;
            currentStepInterval = walkStepInterval;
            animator.SetBool("IsRunning", false);
        }
    }

    public void OnRun(InputAction.CallbackContext context)
    {
        if (context.performed && moveVector.magnitude > 0)
        {
            isRunning = !isRunning;
            currentSpeed = isRunning ? runSpeed : walkSpeed;
            currentStepInterval = isRunning ? runStepInterval : walkStepInterval;
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
    
    public void OnRespawn(InputAction.CallbackContext context)
    {
        PlayerRespawnManager respawnManager = FindObjectOfType<PlayerRespawnManager>();
        if (respawnManager != null)
        {
            respawnManager.TryRespawn();
        }
    }

    public bool IsPlayerAlive()
    {
        return !isDead;
    }

    public void Respawn()
    {
        
        if (playerModel != null)
            playerModel.SetActive(true);

        
        if (deathBody != null)
            deathBody.SetActive(false);

        
        this.enabled = true;

        
        if (TryGetComponent<CharacterController>(out var cc))
            cc.enabled = true;

        
        isDead = false;

        
        if (animator != null)
        {
            animator.SetBool("IsWalking", false);
            animator.SetBool("IsRunning", false);
            animator.ResetTrigger("Die_first");
        }
    }

    public void Die()
    {
        Debug.Log("Player touché");
        if (isDead) return;

        isDead = true;

        animator.SetTrigger("Die_first");

        if (playerModel != null)
            playerModel.SetActive(false);

        if (deathBody != null)
            deathBody.SetActive(true);

        this.enabled = false;

        if (TryGetComponent<CharacterController>(out var cc))
            cc.enabled = false;

        NPC_Enemy[] enemies = FindObjectsOfType<NPC_Enemy>();
        foreach (NPC_Enemy enemy in enemies)
        {
            enemy.OnPlayerDeath();
        }

        PlayerRespawnManager respawnManager = FindObjectOfType<PlayerRespawnManager>();
        if (respawnManager != null && respawnManager.gameObject.activeInHierarchy)
        {
            GameObject respawnUI = GameObject.FindGameObjectWithTag("RespawnUI");
            if (respawnUI != null)
                respawnUI.SetActive(true);
        }
    }
}