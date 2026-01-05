using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class DrugTripSequence : MonoBehaviour
{
    [Header("Audio Settings")]
    public AudioClip swallowSound;
    public AudioSource audioSource;
    public float soundDelay = 2.0f; 

    [Header("Visual Settings")]
    public Image glowImage; 
    public float glowDuration = 1.5f; 
    public AnimationCurve glowExpansionCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Scene Settings")]
    public string nextSceneName = "desert";

    private void Start()
    {
        
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
        }

        
        if (glowImage == null)
        {
            CreateProceduralGlowUI();
        }
        else
        {
            
            if (glowImage.sprite == null)
            {
                glowImage.sprite = GenerateGlowSprite();
            }
        }
            
        
        if (glowImage != null)
        {
            glowImage.gameObject.SetActive(false);
            glowImage.transform.localScale = Vector3.zero;
        }
    }

    private void CreateProceduralGlowUI()
    {
        
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("DrugTripCanvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
        }

        
        GameObject imageObj = new GameObject("ProceduralGlowImage");
        imageObj.transform.SetParent(canvas.transform, false);
        
        glowImage = imageObj.AddComponent<Image>();
        glowImage.sprite = GenerateGlowSprite();
        glowImage.preserveAspect = true;
        
        
        RectTransform rt = imageObj.GetComponent<RectTransform>();
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(500, 500); 
    }

    private Sprite GenerateGlowSprite()
    {
        int size = 512;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color[] colors = new Color[size * size];
        Vector2 center = new Vector2(size / 2f, size / 2f);
        float maxDist = size / 2f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center);
                float t = Mathf.Clamp01(1f - (dist / maxDist));
                
                
                float alpha = Mathf.Pow(t, 2); 

                
                
                colors[y * size + x] = new Color(1f, 0.9f, 0.2f, alpha);
            }
        }

        texture.SetPixels(colors);
        texture.Apply();
        
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }

    public void StartDrugSequence()
    {
        StartCoroutine(SequenceCoroutine());
    }

    private IEnumerator SequenceCoroutine()
    {
        Debug.Log("Drug Trip Sequence Started");

        
        if (audioSource != null && swallowSound != null)
        {
            audioSource.PlayOneShot(swallowSound);
        }

        yield return new WaitForSeconds(soundDelay);

        
        if (glowImage != null)
        {
            glowImage.gameObject.SetActive(true);
            glowImage.transform.localScale = Vector3.zero;
            
            
            Color color = glowImage.color;
            color.a = 1f;
            glowImage.color = color;

            float timer = 0f;
            while (timer < glowDuration)
            {
                timer += Time.deltaTime;
                float progress = timer / glowDuration;
                
                
                float scaleValue = glowExpansionCurve.Evaluate(progress) * 50f; 
                glowImage.transform.localScale = Vector3.one * scaleValue;
                
                yield return null;
            }
        }
        else
        {
             
             Debug.LogWarning("Glow Image is missing even after procedural generation attempt.");
             yield return new WaitForSeconds(glowDuration);
        }

        
        if (!string.IsNullOrEmpty(nextSceneName))
        {
            Debug.Log($"Loading next scene: {nextSceneName}");
            SceneManager.LoadScene(nextSceneName);
        }
    }
}
