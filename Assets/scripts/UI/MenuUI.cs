using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

public class MenuUI: MonoBehaviour
{
    private Canvas fadeCanvas;
    private Image fadeImage;
    [SerializeField] private float fadeDuration = 2.0f;

    private void Start()
    {
        SetupFadeCanvas();
        StartCoroutine(FadeInSequence());
    }

    private void SetupFadeCanvas()
    {
        GameObject canvasObj = new GameObject("MenuFadeCanvas");
        fadeCanvas = canvasObj.AddComponent<Canvas>();
        fadeCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        fadeCanvas.sortingOrder = 2000; 
        
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        
        canvasObj.AddComponent<GraphicRaycaster>();
        
        GameObject imgObj = new GameObject("FadeImage");
        imgObj.transform.SetParent(fadeCanvas.transform, false);
        
        fadeImage = imgObj.AddComponent<Image>();
        fadeImage.color = Color.black;
        
        RectTransform rt = fadeImage.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero;
        rt.anchoredPosition = Vector2.zero;
    }

    private IEnumerator FadeInSequence()
    {
        
        AudioSource music = GetComponent<AudioSource>();
        if (music == null) music = FindFirstObjectByType<AudioSource>();
        
        float targetVolume = 1f;
        if (music != null)
        {
            targetVolume = music.volume > 0 ? music.volume : 1f;
            music.volume = 0f;
        }

        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            float normalized = t / fadeDuration;
            
            
            if (fadeImage != null)
            {
                fadeImage.color = new Color(0, 0, 0, 1f - normalized);
            }
            
            
            if (music != null)
            {
                music.volume = Mathf.Lerp(0f, targetVolume, normalized);
            }
            
            yield return null;
        }
        
        
        if (fadeImage != null) fadeImage.gameObject.SetActive(false);
        if (music != null) music.volume = targetVolume;
    }

    public void Play()
    {
        SceneManager.LoadScene("Floor1 1");
    }

    public void Quit()
    {
        Application.Quit();
    }
}