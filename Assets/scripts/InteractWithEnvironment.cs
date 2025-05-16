using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class InteractWithEnvironment : MonoBehaviour
{
    [Header("Object to set isTrigger after interaction")]
    [SerializeField]
    private GameObject objectToSetTrigger;
    [Header("Choose the way you want to trigger the event")]
    [Tooltip("By using a tag, default: Player")]
    [SerializeField]
    private string otherTag = "Player";
    [Tooltip("By using a GameObject, default: null")]
    [SerializeField]
    private GameObject GameObject;

    [Header("Choose the image that will be used to display the indicator")]
    [SerializeField]
    private Image interactImage;

    [Header("Canvas to display when interacting")]
    [SerializeField]
    private Canvas interactionCanvas;

    [Header("Enemies to activate")]
    [SerializeField]
    private GameObject enemy1;

    [SerializeField]
    private GameObject enemy2;

    [Header("Player to disactivate")]
    public MonoBehaviour playerControllerScript;
    public PlayerInput playerInput;

    private bool interacting = false;
    private bool insideRange = false;


    void Start()
    {
        if (interactImage != null)
        {
            if (interactImage.enabled)
            {
                interactImage.enabled = false;
            }
        }

        if (interactionCanvas != null)
        {
            interactionCanvas.gameObject.SetActive(false);
        }
    }

    void Update()
    {
        if (insideRange && !interacting)
        {
            if (Input.GetKeyDown(KeyCode.F))
            {
                interacting = true;
                interactImage.enabled = false;

                if (interactionCanvas != null)
                {

                    OnInteractStart();

                    interactionCanvas.gameObject.SetActive(true);

                    if (enemy1 != null)
                    {
                        var agent1 = enemy1.GetComponent<UnityEngine.AI.NavMeshAgent>();
                        if (agent1 != null) agent1.enabled = true;
                    }

                    if (enemy2 != null)
                    {
                        var agent2 = enemy2.GetComponent<UnityEngine.AI.NavMeshAgent>();
                        if (agent2 != null) agent2.enabled = true;
                    }

                    StartCoroutine(HideCanvasAfterDelay(3f));
                }
                Collider collider = GetComponent<Collider>();
                if (collider != null) collider.enabled = false;
            }
        }
    }

    private IEnumerator HideCanvasAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        interacting = false;

        if (interactionCanvas != null)
        {
            interactionCanvas.gameObject.SetActive(false);
            OnInteractEnd();
        }

        if (objectToSetTrigger != null)
        {
            var collider = objectToSetTrigger.GetComponent<BoxCollider>();
            if (collider != null)
            {
                collider.enabled = true;
            }
        }

    }

    private void OnTriggerEnter(Collider other)
    {
        if (other != null)
        {
            bool f = GameObject ? GameObject.Equals(other.gameObject) : false;
            if (other.CompareTag(otherTag) || f)
            {
                GameObject = other.gameObject;
                interactImage.enabled = true;
                insideRange = true;
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        insideRange = false;
        interactImage.enabled = false;
    }
    
    private void OnInteractStart()
    {
        if (playerControllerScript != null)
            playerControllerScript.enabled = false;

        if (playerInput != null)
            playerInput.enabled = false;
    }

    private void OnInteractEnd()
    {
        if (playerControllerScript != null)
            playerControllerScript.enabled = true;

        if (playerInput != null)
            playerInput.enabled = true;
    }
}