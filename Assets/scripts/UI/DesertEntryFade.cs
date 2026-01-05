using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class DesertEntryFade : MonoBehaviour
{
    [Header("Settings")]
    public float fadeDuration = 2.0f;
    public Color fadeColor = new Color(1f, 0.9f, 0.2f, 1f); 

    private Image fadeImage;

    void Start()
    {
        CreateFadeUI();
        StartCoroutine(FadeInRoutine());
    }

    private void CreateFadeUI()
    {
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("DesertEntryCanvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
        }

        GameObject imageObj = new GameObject("EntryFadeImage");
        imageObj.transform.SetParent(canvas.transform, false);

        fadeImage = imageObj.AddComponent<Image>();
        fadeImage.color = fadeColor;
        
        
        RectTransform rt = imageObj.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero; 
    }

    private IEnumerator FadeInRoutine()
    {
        float timer = 0f;
        Color startColor = fadeColor;
        Color endColor = new Color(startColor.r, startColor.g, startColor.b, 0f);

        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            float t = timer / fadeDuration;
            
            if (fadeImage != null)
            {
                fadeImage.color = Color.Lerp(startColor, endColor, t);
            }
            
            yield return null;
        }

        if (fadeImage != null)
        {
            Destroy(fadeImage.gameObject);
        }
    }
}
