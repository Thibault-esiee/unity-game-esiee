using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using Yarn.Unity;

public class InteractWithMiror : MonoBehaviour
{
    public DialogueRunner dRunner;
    public string playerTag = "Player";
    public string firstDialogueName = "Start";
    public string secondDialogueName = "Start";
    public MonoBehaviour playerControllerScript;
    public string sceneName;
    public bool isTransporting = false;
    public Image fadeImage;
    public PlayerInput playerInput;

    private BoxCollider boxCollider;
    private bool ran = false;
    private GameObject playerInTrigger;
    private bool isFirstDialogueComplete = false;
    private bool isSecondDialogueComplete = false;

    void Start()
    {
        if (dRunner == null)
        {
            Debug.LogError("No DialogueRunner assigned to DialogueTransitionManager.");
            return;
        }

        dRunner.onDialogueStart.AddListener(OnDialogueStart);
        dRunner.onDialogueComplete.AddListener(OnDialogueEnd);

        boxCollider = GetComponent<BoxCollider>();
        
        if (fadeImage != null)
        {
            fadeImage.color = new Color(0, 0, 0, 0);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (ran || dRunner == null || string.IsNullOrEmpty(firstDialogueName)) return;

        if (other.CompareTag(playerTag))
        {
            playerInTrigger = other.gameObject;
            dRunner.StartDialogue(firstDialogueName);
            ran = true;
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
        if (!isFirstDialogueComplete)
        {
            isFirstDialogueComplete = true;
            StartCoroutine(FadeOutAndStartSecondDialogue());
        }
        else if (!isSecondDialogueComplete)
        {
            isSecondDialogueComplete = true;
            StartCoroutine(TransitionToNewScene());
        }
    }

    private IEnumerator FadeOutAndStartSecondDialogue()
    {
        yield return StartCoroutine(FadeOut());
        
        if (dRunner != null && !string.IsNullOrEmpty(secondDialogueName))
        {
            dRunner.StartDialogue(secondDialogueName);
        }
        else
        {
            StartCoroutine(TransitionToNewScene());
        }
    }

    private IEnumerator TransitionToNewScene()
    {
        isTransporting = true;

        yield return new WaitForSeconds(0.5f);

        if (boxCollider != null)
        {
            boxCollider.isTrigger = false;
            Debug.Log("Entrance locked.");
        }

        SceneManager.LoadScene(sceneName);
    }

    private IEnumerator FadeOut()
    {
        float elTime = 0f;
        float fadeDuration = 2f;
        Color startColor = new Color(0, 0, 0, 0);
        Color endColor = new Color(0, 0, 0, 1);

        while (elTime < fadeDuration)
        {
            float t = elTime / fadeDuration;

            if (fadeImage != null)
                fadeImage.color = Color.Lerp(startColor, endColor, t);

            elTime += Time.deltaTime;
            yield return null;
        }

        if (fadeImage != null)
            fadeImage.color = endColor;
    }
}