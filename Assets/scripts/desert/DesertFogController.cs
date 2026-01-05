using UnityEngine;

[ExecuteAlways]
public class DesertFogController : MonoBehaviour
{
    [Header("Fog Settings")]
    public Color fogColor = new Color(0.85f, 0.55f, 0.35f);
    public bool enableFog = true;
    public float densityMultiplier = 1.0f;
    
    [Header("Sky Blending")]
    public bool matchCameraBackground = false;
    
    [Header("Manual Overrides")]
    public bool useManualDistance = false;
    public float manualDistance = 300f;

    [Header("References")]
    public TerrainGenerator terrainGenerator;

    void Start()
    {
        if (terrainGenerator == null)
            terrainGenerator = FindFirstObjectByType<TerrainGenerator>();
    }

    void Update()
    {
        UpdateFogSettings();
    }

    void UpdateFogSettings()
    {
        if (!enableFog)
        {
            RenderSettings.fog = false;
            return;
        }

        RenderSettings.fog = true;
        RenderSettings.fogColor = fogColor;
        RenderSettings.fogMode = FogMode.Linear;
        float viewDistance = 150f;
        
        if (useManualDistance)
        {
            viewDistance = manualDistance;
        }
        else if (terrainGenerator != null)
        {
            viewDistance = (terrainGenerator.chunksVisible * terrainGenerator.chunkSize);
        }

        RenderSettings.fogStartDistance = viewDistance * 0.2f;
        RenderSettings.fogEndDistance = viewDistance * 0.95f * densityMultiplier;

        if (Camera.main != null)
        {
            if (matchCameraBackground)
            {
                Camera.main.clearFlags = CameraClearFlags.SolidColor;
                Camera.main.backgroundColor = fogColor;
            }
            else
            {
                Camera.main.clearFlags = CameraClearFlags.Skybox;
            }
        }
    }
}
