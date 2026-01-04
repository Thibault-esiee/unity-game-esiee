using UnityEngine;

[ExecuteAlways]
public class DesertFogController : MonoBehaviour
{
    [Header("Fog Settings")]
    public Color fogColor = new Color(0.85f, 0.55f, 0.35f); // Couleur Ocre/Poussière de guerre
    public bool enableFog = true;
    public float densityMultiplier = 1.0f;
    
    [Header("Sky Blending")]
    public bool matchCameraBackground = false; // Mettre à FALSE pour voir le Ciel (Skybox)
    
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

        // 1. Configurer le brouillard
        RenderSettings.fog = true;
        RenderSettings.fogColor = fogColor;
        RenderSettings.fogMode = FogMode.Linear;

        // 2. Calculer la distance
        float viewDistance = 150f;
        
        if (useManualDistance)
        {
            viewDistance = manualDistance;
        }
        else if (terrainGenerator != null)
        {
            viewDistance = (terrainGenerator.chunksVisible * terrainGenerator.chunkSize);
        }

        RenderSettings.fogStartDistance = viewDistance * 0.2f; // Le brouillard commence à 20%
        RenderSettings.fogEndDistance = viewDistance * 0.95f * densityMultiplier; // Il finit à 95%

        // 3. Gestion du Ciel
        if (Camera.main != null)
        {
            if (matchCameraBackground)
            {
                // Mode "Mur de brume" : On cache le ciel avec la couleur unie
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
