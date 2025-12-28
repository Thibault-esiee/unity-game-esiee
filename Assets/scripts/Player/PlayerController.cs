using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private float walkSpeed = 1f;
    [SerializeField] private float runSpeed = 3f;
    [SerializeField] private float lookSensitivity = 5f;

    private bool isDead = false;
    [SerializeField] private GameObject deathBody;
    [SerializeField] private GameObject playerModel;
    private float currentSpeed;

    [SerializeField] private float fadeInDelay = 1.5f;
    [SerializeField] private float sceneReloadDelay = 5f;
    [SerializeField] private float fadeDuration = 3.5f;
    
    [Header("Death Fade Effect")]
    [SerializeField] private Canvas deathFadeCanvas;
    [SerializeField] private Image fadeImage;
    
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

    private bool wasMovingLastFrame = false;
    
    private float verticalVelocity = 0f;
    [SerializeField] private float gravity = -9.81f;
    [SerializeField] private float groundedGravity = -2f;
        
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
        
        InitializeDeathFadeCanvas();

        currentStepInterval = walkStepInterval;
    }
    
    private void InitializeDeathFadeCanvas()
    {
        if (deathFadeCanvas == null)
        {
            GameObject canvasObject = new GameObject("DeathFadeCanvas");
            deathFadeCanvas = canvasObject.AddComponent<Canvas>();
            deathFadeCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            deathFadeCanvas.sortingOrder = 999;
            
            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            canvasObject.AddComponent<GraphicRaycaster>();
            
            GameObject imageObject = new GameObject("FadeImage");
            imageObject.transform.SetParent(canvasObject.transform, false);
            
            fadeImage = imageObject.AddComponent<Image>();
            fadeImage.color = new Color(0, 0, 0, 0);
            
            RectTransform rectTransform = imageObject.GetComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.sizeDelta = Vector2.zero;
            rectTransform.anchoredPosition = Vector2.zero;
        }
        else if (fadeImage == null && deathFadeCanvas != null)
        {
            GameObject imageObject = new GameObject("FadeImage");
            imageObject.transform.SetParent(deathFadeCanvas.transform, false);
            
            fadeImage = imageObject.AddComponent<Image>();
            fadeImage.color = new Color(0, 0, 0, 0);
            
            RectTransform rectTransform = imageObject.GetComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.sizeDelta = Vector2.zero;
            rectTransform.anchoredPosition = Vector2.zero;
        }
        
        if (deathFadeCanvas != null)
        {
            deathFadeCanvas.gameObject.SetActive(true);
            if (fadeImage != null)
            {
                fadeImage.color = new Color(0, 0, 0, 0);
            }
        }
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
        bool isMoving = moveInput.sqrMagnitude > 0.01f;

        if (isMoving)
        {
            footstepTimer += Time.deltaTime;

            if (!wasMovingLastFrame)
            {
                PlayFootstepSound();
                footstepTimer = 0f;
            }
            else if (footstepTimer >= currentStepInterval)
            {
                PlayFootstepSound();
                footstepTimer = 0f;
            }
        }
        else
        {
            footstepTimer = 0f;
        }

        wasMovingLastFrame = isMoving;
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

        if (characterController.isGrounded)
        {
            verticalVelocity = groundedGravity;
        }
        else
        {
            verticalVelocity += gravity * Time.deltaTime;
        }

        move.y = verticalVelocity;

        characterController.Move(move * currentSpeed * Time.deltaTime);
    }

    public void OnLook(InputAction.CallbackContext context)
    {
        LookVector = context.ReadValue<Vector2>();
    }

    public bool IsPlayerAlive()
    {
        return !isDead;
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
        
        StartCoroutine(ReloadSceneAfterDelay());
    }
    
    private IEnumerator ReloadSceneAfterDelay()
    {
        Debug.Log("La scène sera rechargée dans " + sceneReloadDelay + " secondes");
        
        yield return new WaitForSeconds(fadeInDelay);
        
        StartCoroutine(FadeToBlack());
        
        yield return new WaitForSeconds(sceneReloadDelay - fadeInDelay);
        
        Scene currentScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(currentScene.name);
        
        Debug.Log("Scène rechargée");
    }
    
    private IEnumerator FadeToBlack()
    {
        if (fadeImage == null)
        {
            Debug.LogError("FadeImage n'est pas initialisée!");
            yield break;
        }
        
        float elapsedTime = 0;
        Color startColor = fadeImage.color;
        Color targetColor = new Color(0, 0, 0, 1);
        
        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            float normalizedTime = Mathf.Clamp01(elapsedTime / fadeDuration);
            
            fadeImage.color = Color.Lerp(startColor, targetColor, normalizedTime);
            
            yield return null;
        }
        
        fadeImage.color = targetColor;
    }
}