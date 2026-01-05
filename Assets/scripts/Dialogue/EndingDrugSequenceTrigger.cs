using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Yarn.Unity;
using UnityEngine.InputSystem;

public class EndingDrugSequenceTrigger : MonoBehaviour
{
    [Header("Dialogue Dependencies")]
    public DialogueRunner dRunner;
    public string playerTag = "Player";
    public string dialogName = "Ending"; 
    
    [Header("Player Control")]
    public MonoBehaviour playerControllerScript;
    public PlayerInput playerInput;

    [Header("Sequence")]
    public DrugTripSequence drugTripSequence;

    private bool ran = false;

    void Start()
    {
        if (dRunner == null)
        {
            Debug.LogError("No DialogueRunner assigned to EndingDrugSequenceTrigger.");
            return;
        }

        dRunner.onDialogueStart.AddListener(OnDialogueStart);
        dRunner.onDialogueComplete.AddListener(OnDialogueEnd);
    }

    void Update()
    {
        
        if (dRunner.IsDialogueRunning && Keyboard.current != null && Keyboard.current.kKey.wasPressedThisFrame)
        {
            
            
            
            dRunner.Stop();
        }
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
        
        
        
        
        if (ran) 
        {
             
             
             
             
             
             
             
             
             
             
             
             
             
             
             
             
             
             
             if (drugTripSequence != null)
             {
                 drugTripSequence.StartDrugSequence();
                 
             }
             else
             {
                 
                if (playerControllerScript != null)
                    playerControllerScript.enabled = true;

                if (playerInput != null)
                    playerInput.enabled = true;
             }
        }
    }
}
