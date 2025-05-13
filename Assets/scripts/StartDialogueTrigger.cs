using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Yarn.Unity;
using UnityEngine.InputSystem;

public class StartDialogueTrigger : MonoBehaviour
{
    public DialogueRunner dRunner;
    public string playerTag = "Player";
    public string dialogName = "Start";
    public MonoBehaviour playerControllerScript;
    public PlayerInput playerInput;

    private bool ran = false;

    void Start()
    {
        if (dRunner == null)
        {
            Debug.LogError("No DialogueRunner assigned to StartDialogueTrigger.");
            return;
        }

        dRunner.onDialogueStart.AddListener(OnDialogueStart);
        dRunner.onDialogueComplete.AddListener(OnDialogueEnd);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (ran) return;

        if (other.CompareTag(playerTag))
        {
            if (dRunner != null && !string.IsNullOrEmpty(dialogName))
            {
                dRunner.StartDialogue(dialogName);
                ran = true;
            }
            else
            {
                Debug.LogWarning("DialogueRunner or dialogName not set.");
            }
        }
    }

    private void OnDialogueStart()
    {
        if (playerControllerScript != null)
            playerControllerScript.enabled = false;

        if (playerInput != null)
            playerInput.enabled = false;
    }

    private void OnDialogueEnd()
    {
        if (playerControllerScript != null)
            playerControllerScript.enabled = true;

        if (playerInput != null)
            playerInput.enabled = true;
    }
}