using System.Collections;
using System.Drawing;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class TransporterScriptLifts : MonoBehaviour
{
    public string sceneName;
    public string playerTag = "Player";
    public bool isTransporting = false;
    public UnityEngine.UI.Image fadeImage;
    public AudioSource bgFX;

    private BoxCollider boxCollider;

    void Start()
    {
        boxCollider = GetComponent<BoxCollider>();
        
        if (fadeImage != null)
        {
            fadeImage.color = new(0, 0, 0, 0);
        }
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
        bgFX.Play();

        isTransporting = true;

        yield return new WaitForSeconds(0.5f);

        if (boxCollider != null)
        {
            boxCollider.isTrigger = false;
            Debug.Log("Lift entrance locked.");
        }

        yield return StartCoroutine(FadeOut());

        SceneManager.LoadScene(sceneName);
    }

    private IEnumerator FadeOut()
    {
        float elTime = 0f;
        
        UnityEngine.Color oColor = new(0,0,0,0);
        UnityEngine.Color nColor = new(0, 0, 0, 1);

        float sVolume = bgFX != null ? bgFX.volume : 0f;

        while (elTime < 2f)
        {
            float t = elTime / 2f;

            if (fadeImage != null)
            {
                fadeImage.color = UnityEngine.Color.Lerp(oColor, nColor, t);
            }

            if (bgFX != null)
            {
                bgFX.volume = Mathf.Lerp(sVolume, 0f, t);
            }

            elTime += Time.deltaTime;
            yield return null;
        }

        if (fadeImage != null)
        {
            fadeImage.color = nColor;
        }

        if (bgFX != null)
        {
            bgFX.volume = 0f;
        }
    }
}
