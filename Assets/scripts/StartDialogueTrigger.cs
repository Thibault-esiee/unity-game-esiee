using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Yarn.Unity;

public class StartDialogueTrigger : MonoBehaviour
{
    public DialogueRunner dRunner;
    public string playerTag = "Player";
    public string dialogName = "Start";

    private bool ran = false;

    void Start()
    {
        if (dRunner == null)
        {
            Debug.LogError("No DialogueRunner assigned to StartDialogueTrigger.");
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
}
