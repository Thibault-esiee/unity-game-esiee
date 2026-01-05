using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

[ExecuteAlways]
public class DesertHeatSystem : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float maxDistanceBeforeFaint = 100f;
    [SerializeField] private float minFogDensity = 0.0f;
    [SerializeField] private float maxFogDensity = 0.5f;
    [SerializeField] private bool enableSandstorm = true;

    [Header("Visuals & Wind")]
    [SerializeField] private Color sandFogColor = new Color(0.82f, 0.72f, 0.55f, 1f);
    [SerializeField] private float windSpeed = 1.0f;
    [SerializeField] private float windTurbulence = 0.02f;
    [SerializeField] private ParticleSystem sandParticles;

    [Header("Post Processing")]
    [SerializeField] private Volume globalVolume;
    [SerializeField] private bool enableBlur = true;
    [SerializeField] private float minBlurFocusDistance = 0.1f;

    private DepthOfField dof;
    private float initialFocusDistance = 10.0f;

    [Header("Audio")]
    [SerializeField] private AudioClip lightWindClip;
    [SerializeField] private AudioClip strongWindClip;
    [SerializeField] private float maxWindVolume = 1.0f;
    
    private AudioSource lightWindSource;
    private AudioSource strongWindSource;
    
    [Header("Volumetric Sand")]
    [SerializeField] private Material volumetricSandMaterial;
    [SerializeField] private int noiseTextureSize = 256;
    [SerializeField] private float minVolumetricDensity = 0.0f;
    [SerializeField] private float maxVolumetricDensity = 5.0f;
    [SerializeField] private float noiseScale = 0.1f;
    
    private Texture2D cachedNoiseTexture;
    private Vector3 currentWindOffset;
    
    [Header("References")]
    [SerializeField] private PlayerController playerController;
    [SerializeField] private Transform sandVolume;

    [Header("Dialogue")]
    [SerializeField] private Yarn.Unity.DialogueRunner dialogueRunner;
    [SerializeField] private System.Collections.Generic.List<DialogueTriggerEvent> dialogueEvents = new System.Collections.Generic.List<DialogueTriggerEvent>();

    private Vector3 startPosition;
    private float distanceTraveled = 0f;
    private bool hasFainted = false;

    private bool lastDialogueCompleted = false;
    private string currentRunningNode = "";
    private string lastDialogueNodeName = "";

    private void OnEnable()
    {
        if (volumetricSandMaterial != null)
        {
            GenerateNoiseTexture();
        }
    }

    private void Start()
    {
        if (playerController == null)
        {
            playerController = GetComponent<PlayerController>();
            if (playerController == null) playerController = FindFirstObjectByType<PlayerController>();
            if (playerController == null) Debug.LogWarning("DesertHeatSystem: PlayerController not found on this GameObject or in Scene!");
        }

        if (dialogueRunner == null)
        {
            dialogueRunner = FindFirstObjectByType<Yarn.Unity.DialogueRunner>();
        }

        if (dialogueEvents.Count == 0)
        {
            dialogueEvents.Add(new DialogueTriggerEvent { triggerPercentage = 0.05f, nodeName = "Desert_1" });
            dialogueEvents.Add(new DialogueTriggerEvent { triggerPercentage = 0.25f, nodeName = "Desert_2" });
            dialogueEvents.Add(new DialogueTriggerEvent { triggerPercentage = 0.50f, nodeName = "Desert_3" });
            dialogueEvents.Add(new DialogueTriggerEvent { triggerPercentage = 0.75f, nodeName = "Desert_4" });
            dialogueEvents.Add(new DialogueTriggerEvent { triggerPercentage = 0.90f, nodeName = "Desert_5" });
        }

        if (Application.isPlaying && playerController != null)
        {
            startPosition = playerController.transform.position;
        }
        
        if (enableSandstorm)
        {
            RenderSettings.fog = true;
            RenderSettings.fogDensity = minFogDensity;
            RenderSettings.fogColor = sandFogColor;
        }
        
        if (Camera.main != null)
        {
            Camera.main.depthTextureMode |= DepthTextureMode.Depth;
        }

        if (playerController == null)
        {
            playerController = GetComponent<PlayerController>();
            if (playerController == null) Debug.LogWarning("DesertHeatSystem: PlayerController not found on this GameObject!");
        }

        if (volumetricSandMaterial != null)
        {
            GenerateNoiseTexture();
        }
        else
        {
            Debug.LogError("DesertHeatSystem: Volumetric Sand Material is NOT assigned!");
        }
        
        if (sandVolume == null)
        {
            Debug.LogWarning("DesertHeatSystem: Sand Volume is NOT assigned!");
        }

        if (dialogueRunner != null)
        {
            dialogueRunner.onDialogueComplete.AddListener(OnDialogueComplete);
            
            if (dialogueEvents.Count > 0)
            {
                lastDialogueNodeName = dialogueEvents[dialogueEvents.Count - 1].nodeName;
            }
        }

        if (Application.isPlaying)
        {
            lightWindSource = SetupAudioSource("LightWindSource", lightWindClip);
            strongWindSource = SetupAudioSource("StrongWindSource", strongWindClip);
        }

        if (globalVolume != null && globalVolume.profile != null)
        {
            if (globalVolume.profile.TryGet(out dof))
            {
                dof.active = true;
                initialFocusDistance = dof.focusDistance.value;
                Debug.Log($"[DesertHeatSystem] DepthOfField found. Initial Focus Distance: {initialFocusDistance}");
            }
            else
            {
                Debug.LogWarning("[DesertHeatSystem] DepthOfField NOT found in Global Volume Profile!");
            }
        }
    }

    private AudioSource SetupAudioSource(string name, AudioClip clip)
    {
        if (clip == null) return null;

        GameObject audioObj = new GameObject(name);
        audioObj.transform.parent = transform;
        audioObj.transform.localPosition = Vector3.zero;

        AudioSource source = audioObj.AddComponent<AudioSource>();
        source.clip = clip;
        source.loop = true;
        source.playOnAwake = true;
        source.volume = 0f;
        source.spatialBlend = 0f;
        source.Play();

        return source;
    }

    private void Update()
    {
        if (playerController == null) return;

        distanceTraveled = Vector3.Distance(playerController.transform.position, startPosition);

        if (enableSandstorm)
        {
            float t = Mathf.Clamp01(distanceTraveled / maxDistanceBeforeFaint);

            if (Application.isPlaying)
            {
                if (lightWindSource != null)
                {
                    lightWindSource.volume = Mathf.Lerp(maxWindVolume, 0f, t);
                }

                if (strongWindSource != null)
                {
                    strongWindSource.volume = Mathf.Lerp(0f, maxWindVolume, t);
                }
            }
            
            float noise = Mathf.PerlinNoise(Time.time * windSpeed, 0f) * windTurbulence;
            
            RenderSettings.fogMode = FogMode.Exponential; 
            
            float fogCurve = t * t * t; 
            float baseFogDensity = Mathf.Lerp(minFogDensity, maxFogDensity, fogCurve);
            
            RenderSettings.fogDensity = Mathf.Max(0f, baseFogDensity + noise * 0.1f);
            RenderSettings.fogColor = sandFogColor;

            dof.active = true;
            dof.focusDistance.overrideState = true;
            dof.aperture.overrideState = true;
            dof.aperture.value = 1.4f; 

            float blurCurve = t * t;
            float focusDist = Mathf.Lerp(initialFocusDistance, minBlurFocusDistance, blurCurve);
            dof.focusDistance.value = focusDist;
                
            if (Time.frameCount % 60 == 0)
            {
                Debug.Log($"[DesertHeatSystem] Blur Status: t={t:F2}, FocusDist={focusDist:F2}");
            }

            if (sandParticles != null)
            {
                var emission = sandParticles.emission;
                emission.rateOverTime = Mathf.Lerp(10f, 100f, t);
                sandParticles.transform.position = playerController.transform.position + Vector3.up * 5f;
            }

            if (volumetricSandMaterial != null)
            {
                float densityCurve = t * t * t;
                float volDensity = Mathf.Lerp(minVolumetricDensity, maxVolumetricDensity, densityCurve);
                
                volDensity += noise * 10f; 
                
                volumetricSandMaterial.SetFloat("_Density", Mathf.Max(0f, volDensity));
                
                volumetricSandMaterial.SetColor("_Color", sandFogColor);
                
                volumetricSandMaterial.SetFloat("_NoiseScale", noiseScale);

                currentWindOffset += new Vector3(windSpeed, windSpeed * 0.1f, 0) * Time.deltaTime * 0.5f;
                
                Vector3 finalOffset = currentWindOffset;
                if (sandVolume != null)
                {
                    sandVolume.position = playerController.transform.position;
                    finalOffset += sandVolume.position * noiseScale;
                }

                volumetricSandMaterial.SetVector("_WindOffset", new Vector4(finalOffset.x, finalOffset.y, finalOffset.z, 0));
            }

            if (Application.isPlaying && !hasFainted)
            {
                if (dialogueRunner != null)
                {
                    foreach (var evt in dialogueEvents)
                    {
                        if (!evt.hasTriggered && t >= evt.triggerPercentage)
                        {
                            evt.hasTriggered = true;
                            Debug.Log($"[DesertHeatSystem] Triggering Dialogue: {evt.nodeName}");
                            currentRunningNode = evt.nodeName;
                            dialogueRunner.StartDialogue(evt.nodeName);
                        }
                    }
                }

                bool dialoguesFinished = (dialogueEvents.Count == 0) || lastDialogueCompleted;
                
                if (distanceTraveled >= maxDistanceBeforeFaint && dialoguesFinished)
                {
                    TriggerFaint();
                }
            }
        }
    }

    private void OnDialogueComplete()
    {
        Debug.Log($"[DesertHeatSystem] Dialogue Complete. Current Node: {currentRunningNode}");

        if (!string.IsNullOrEmpty(currentRunningNode)) 
        {
            if (currentRunningNode == lastDialogueNodeName)
            {
                StartCoroutine(WaitAndFinishDialogue());
            }
            else if (currentRunningNode == "Desert_Ending")
            {
                Debug.Log("[DesertHeatSystem] Desert_Ending finished. Starting Light Sequence.");
                StartEndingLightSequence();
            }
        }
    }

    private System.Collections.IEnumerator WaitAndFinishDialogue()
    {
        Debug.Log("[DesertHeatSystem] Last dialogue finished. Waiting 2 seconds before allowing faint...");
        yield return new WaitForSeconds(2.0f);
        lastDialogueCompleted = true;
        Debug.Log("[DesertHeatSystem] 2 seconds passed. Player will faint if distance is reached.");
    }

    [System.Serializable]
    public class DialogueTriggerEvent
    {
        [Range(0f, 1f)] public float triggerPercentage;
        public string nodeName;
        [HideInInspector] public bool hasTriggered;
    }

    private void TriggerFaint()
    {
        hasFainted = true;
        Debug.Log("Player fainted from heat/sand!");
        
        if (playerController != null)
        {
            playerController.Die(false);
            StartCoroutine(PlayEndingDialogueSequence());
        }
    }

    [Header("Ending Audio")]
    [SerializeField] private AudioClip heartBeatClip;
    private AudioSource heartBeatSource;

    private System.Collections.IEnumerator PlayEndingDialogueSequence()
    {
        yield return new WaitForSeconds(3.5f);
        
        yield return StartCoroutine(FadeAllAudio(2.0f));
        
        if (heartBeatClip != null)
        {
            heartBeatSource = SetupAudioSource("HeartBeatSource", heartBeatClip);
            heartBeatSource.volume = 0.5f;
            heartBeatSource.pitch = 1.0f;
        }

        if (dialogueRunner != null)
        {
            Debug.Log("[DesertHeatSystem] Starting Ending Dialogue");
            currentRunningNode = "Desert_Ending";
            dialogueRunner.StartDialogue("Desert_Ending");
        }
    }

    private System.Collections.IEnumerator PlayLightSequence()
    {
        Debug.Log("[DesertHeatSystem] Starting Light Sequence (Polished)...");
        
        if (endLightImage == null) yield break;

        int pulses = 5;
        float currentSize = 100f;
        float sizeMultiplier = 2.5f;
        
        float startVol = 0.5f;
        float maxVol = 1.0f;

        for (int i = 0; i < pulses; i++)
        {
            if (heartBeatSource != null)
            {
                float progress = (float)i / (pulses - 1);
                heartBeatSource.volume = Mathf.Lerp(startVol, maxVol, progress);
                heartBeatSource.pitch = Mathf.Lerp(1.0f, 1.4f, progress);
            }

            endLightImage.rectTransform.sizeDelta = new Vector2(currentSize, currentSize);
            
            float t = 0;
            float flashInDur = 0.1f;
            while (t < flashInDur)
            {
                t += Time.deltaTime;
                float a = t / flashInDur;
                endLightImage.color = new Color(1, 1, 1, a);
                yield return null;
            }
            endLightImage.color = Color.white;

            yield return new WaitForSeconds(0.15f);

            t = 0;
            float fadeOutDur = 0.3f;
            while (t < fadeOutDur)
            {
                t += Time.deltaTime;
                float a = 1f - (t / fadeOutDur);
                endLightImage.color = new Color(1, 1, 1, a);
                yield return null;
            }
            endLightImage.color = new Color(1, 1, 1, 0);

            yield return new WaitForSeconds(0.2f);
            
            currentSize *= sizeMultiplier;
        }

        yield return new WaitForSeconds(1.0f);
        
        if (heartBeatSource != null) heartBeatSource.Stop();

        Debug.Log("[DesertHeatSystem] Loading Menu Scene");
        UnityEngine.SceneManagement.SceneManager.LoadScene("Menu Scene");
    }

    private System.Collections.IEnumerator FadeAllAudio(float duration)
    {
        AudioSource[] allAudio = FindObjectsByType<AudioSource>(FindObjectsSortMode.None);
        float[] startVolumes = new float[allAudio.Length];
        for(int i=0; i<allAudio.Length; i++) 
        {
            if (allAudio[i] != null) startVolumes[i] = allAudio[i].volume;
        }
        
        float t = 0;
        while(t < duration)
        {
            t += Time.deltaTime;
            float fraction = 1f - (t / duration);
            
            for(int i=0; i<allAudio.Length; i++)
            {
                if(allAudio[i] != null)
                    allAudio[i].volume = startVolumes[i] * fraction;
            }
            yield return null;
        }
        
        foreach(var a in allAudio) 
        {
            if(a != null) a.Stop();
        }
    }

    private void GenerateNoiseTexture()
    {
        if (cachedNoiseTexture != null) return;

        cachedNoiseTexture = new Texture2D(noiseTextureSize, noiseTextureSize);
        Color[] pixels = new Color[noiseTextureSize * noiseTextureSize];
        
        float scale = 10.0f;
        for (int y = 0; y < noiseTextureSize; y++)
        {
            for (int x = 0; x < noiseTextureSize; x++)
            {
                float xCoord = (float)x / noiseTextureSize * scale;
                float yCoord = (float)y / noiseTextureSize * scale;
                float sample = Mathf.PerlinNoise(xCoord, yCoord);
                pixels[y * noiseTextureSize + x] = new Color(sample, sample, sample);
            }
        }
        
        cachedNoiseTexture.SetPixels(pixels);
        cachedNoiseTexture.Apply();
        
        if (volumetricSandMaterial != null)
        {
            volumetricSandMaterial.SetTexture("_MainTex", cachedNoiseTexture);
        }
    }

    // ---------------------------------------------------------
    // ENDING SEQUENCE LOGIC
    // ---------------------------------------------------------

    private Canvas endLightCanvas;
    private Image endLightImage;

    private void StartEndingLightSequence()
    {
        InitializeEndLightCanvas();
        StartCoroutine(PlayLightSequence());
    }

    private void InitializeEndLightCanvas()
    {
        if (endLightCanvas != null) return;

        GameObject canvasObj = new GameObject("EndLightCanvas");
        endLightCanvas = canvasObj.AddComponent<Canvas>();
        endLightCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        endLightCanvas.sortingOrder = 1000;
        
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        
        canvasObj.AddComponent<GraphicRaycaster>();
        
        GameObject imgObj = new GameObject("EndLight");
        imgObj.transform.SetParent(endLightCanvas.transform, false);
        
        endLightImage = imgObj.AddComponent<Image>();
        endLightImage.sprite = CreateSoftCircleSprite();
        endLightImage.color = new Color(1, 1, 1, 0);
        
        RectTransform rt = endLightImage.rectTransform;
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(50f, 50f); 
        rt.anchoredPosition = Vector2.zero;
    }

    private Sprite CreateSoftCircleSprite()
    {
        int size = 128;
        Texture2D tex = new Texture2D(size, size);
        Color[] pixels = new Color[size * size];
        Vector2 center = new Vector2(size / 2f, size / 2f);
        float radius = size / 2f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center);
                float alpha = Mathf.Clamp01(1f - (dist / radius));
                alpha = Mathf.Pow(alpha, 3f); 
                pixels[y * size + x] = new Color(1, 1, 1, alpha);
            }
        }
        
        tex.SetPixels(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }

}
