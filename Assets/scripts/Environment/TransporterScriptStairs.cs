using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class TransporterScript : MonoBehaviour
{
    public string sceneName;
    public string playerTag = "Player";
    public bool isTransporting = false;
    public Image fadeImage;
    private BoxCollider boxCollider;
    public MonoBehaviour playerControllerScript;
    public PlayerInput playerInput;

    void Start()
    {
        boxCollider = GetComponent<BoxCollider>();

        if (fadeImage != null)
        {
            fadeImage.color = new Color(0, 0, 0, 0);
        }

        OnTransportEnd(); 
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!isTransporting && other.CompareTag(playerTag))
        {
            Debug.Log("Player entered lift trigger.");
            StartCoroutine(ActivateTransportBarrier());
        }
    }

    private IEnumerator ActivateTransportBarrier()
    {
        isTransporting = true;

        yield return new WaitForSeconds(0.5f);

        if (boxCollider != null)
        {
            boxCollider.isTrigger = false;
            Debug.Log("Lift entrance locked.");
        }

        OnTransportStart();

        yield return StartCoroutine(FadeOut());

        SceneManager.LoadScene(sceneName);
    }

    private IEnumerator FadeOut()
    {
        float elTime = 0f;

        Color oColor = new Color(0, 0, 0, 0);
        Color nColor = new Color(0, 0, 0, 1);

        while (elTime < 2f)
        {
            float t = elTime / 2f;

            if (fadeImage != null)
            {
                fadeImage.color = Color.Lerp(oColor, nColor, t);
            }

            elTime += Time.deltaTime;
            yield return null;
        }

        if (fadeImage != null)
        {
            fadeImage.color = nColor;
        }
    }

    private void OnTransportStart()
    {
        if (playerControllerScript != null)
            playerControllerScript.enabled = false;

        if (playerInput != null)
            playerInput.enabled = false;

        Debug.Log("Transport started.");
    }

    private void OnTransportEnd()
    {
        if (playerControllerScript != null)
            playerControllerScript.enabled = true;

        if (playerInput != null)
            playerInput.enabled = true;

        Debug.Log("Transport ended.");
    }
}