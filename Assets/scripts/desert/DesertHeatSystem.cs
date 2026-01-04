using UnityEngine;

[ExecuteAlways]
public class DesertHeatSystem : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float maxDistanceBeforeFaint = 100f;
    [SerializeField] private float minFogDensity = 0.0f;
    [SerializeField] private float maxFogDensity = 0.05f; // Adjust based on your scene lighting/skybox
    [SerializeField] private bool enableSandstorm = true;

    [Header("Visuals & Wind")]
    [SerializeField] private Color sandFogColor = new Color(0.82f, 0.72f, 0.55f, 1f); // Sand color
    [SerializeField] private float windSpeed = 1.0f;
    [SerializeField] private float windTurbulence = 0.02f;
    [SerializeField] private ParticleSystem sandParticles;
    
    [Header("Volumetric Sand")]
    [Header("Volumetric Sand")]
    [SerializeField] private Material volumetricSandMaterial;
    [SerializeField] private int noiseTextureSize = 256;
    [SerializeField] private float minVolumetricDensity = 0.0f;
    [SerializeField] private float maxVolumetricDensity = 5.0f;
    [SerializeField] private float noiseScale = 0.1f; // Must match or drive shader
    
    private Texture2D cachedNoiseTexture;
    private Vector3 currentWindOffset;
    
    [Header("References")]
    [SerializeField] private PlayerController playerController;
    [SerializeField] private Transform sandVolume;

    private Vector3 startPosition;
    private float distanceTraveled = 0f;
    private bool hasFainted = false;

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
            // If still null, try finding it in scene
            if (playerController == null) playerController = FindFirstObjectByType<PlayerController>();
            if (playerController == null) Debug.LogWarning("DesertHeatSystem: PlayerController not found on this GameObject or in Scene!");
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
        
        // Enable Depth Texture for Shader
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
    }

    private void Update()
    {
        if (playerController == null) return;

        // Visuals should ALWAYS update, even if fainted
        distanceTraveled = Vector3.Distance(playerController.transform.position, startPosition);

        if (enableSandstorm)
        {
            float t = Mathf.Clamp01(distanceTraveled / maxDistanceBeforeFaint);
            
            // Add wind turbulence
            float noise = Mathf.PerlinNoise(Time.time * windSpeed, 0f) * windTurbulence;
            
            // Standard Fog Calculation: Linear or Exponential for smoother start
            RenderSettings.fogMode = FogMode.Exponential; 
            // Exponential fog: Density 0.01 is light, 0.1 is thick. 
            float baseFogDensity = Mathf.Lerp(minFogDensity, maxFogDensity, t * t);
            RenderSettings.fogDensity = Mathf.Max(0f, baseFogDensity + noise * 0.1f);
            RenderSettings.fogColor = sandFogColor;

            // Update particles
            if (sandParticles != null)
            {
                var emission = sandParticles.emission;
                emission.rateOverTime = Mathf.Lerp(10f, 100f, t);
                sandParticles.transform.position = playerController.transform.position + Vector3.up * 5f; // Keep particles with player
            }

            // Update Volumetric Material
            if (volumetricSandMaterial != null)
            {
                // Map density using a cubic curve for exponential growth (slow start, heavy finish)
                float densityCurve = t * t * t;
                float volDensity = Mathf.Lerp(minVolumetricDensity, maxVolumetricDensity, densityCurve);
                
                // Add noise
                volDensity += noise * 10f; 
                
                volumetricSandMaterial.SetFloat("_Density", Mathf.Max(0f, volDensity));
                
                // Update color
                volumetricSandMaterial.SetColor("_Color", sandFogColor);
                
                // Set Noise Scale to ensure sync
                volumetricSandMaterial.SetFloat("_NoiseScale", noiseScale);

                // Manual Wind Accumulation
                currentWindOffset += new Vector3(windSpeed, windSpeed * 0.1f, 0) * Time.deltaTime * 0.5f;
                
                // Follow Player Logic
                Vector3 finalOffset = currentWindOffset;
                if (sandVolume != null)
                {
                    // Move the Volume to the Player, not this script object
                    sandVolume.position = playerController.transform.position;
                    
                    // Add world position to offset so texture doesn't stick to the cube
                    // Offset = WorldPos * Scale
                    finalOffset += sandVolume.position * noiseScale;
                }

                volumetricSandMaterial.SetVector("_WindOffset", new Vector4(finalOffset.x, finalOffset.y, finalOffset.z, 0));
            }

            // Faint Logic - Only if Playing and not already fainted
            if (Application.isPlaying && !hasFainted && distanceTraveled >= maxDistanceBeforeFaint)
            {
                TriggerFaint();
            }
        }
    }

    private void TriggerFaint()
    {
        hasFainted = true;
        Debug.Log("Player fainted from heat/sand!");
        
        if (playerController != null)
        {
            playerController.Die();
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
}
