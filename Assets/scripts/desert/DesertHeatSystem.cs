using UnityEngine;

public class DesertHeatSystem : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float maxDistanceBeforeFaint = 100f;
    [SerializeField] private float minFogDensity = 0.0f;
    [SerializeField] private float maxFogDensity = 0.05f; // Adjust based on your scene lighting/skybox
    [SerializeField] private bool enableFogControl = true;

    [Header("Visuals & Wind")]
    [SerializeField] private Color sandFogColor = new Color(0.82f, 0.72f, 0.55f, 1f); // Sand color
    [SerializeField] private float windSpeed = 1.0f;
    [SerializeField] private float windTurbulence = 0.02f;
    [SerializeField] private ParticleSystem sandParticles;

    [Header("References")]
    [SerializeField] private DesertPlayerController playerController;

    private Vector3 startPosition;
    private float distanceTraveled = 0f;
    private bool hasFainted = false;

    private void Start()
    {
        startPosition = transform.position;
        
        if (enableFogControl)
        {
            RenderSettings.fog = true;
            RenderSettings.fogDensity = minFogDensity;
            RenderSettings.fogColor = sandFogColor;
        }

        if (playerController == null)
        {
            playerController = GetComponent<DesertPlayerController>();
        }
    }

    private void Update()
    {
        if (hasFainted) return;

        // Calculate distance from start (or cumulative distance if preferred, but "further you go" usually means from start)
        // If the user meant "total steps taken", we would accumulate distance. 
        // "Plus le joueur bouge" -> "The more the player moves". This could be cumulative.
        // Let's assume cumulative distance is what is requested "activity based", 
        // but often in games it's about going deep into the desert.
        // Let's stick to Distance from Start for "venturing into the deep", matches "getting lost".
        // HOWEVER, "The more the player moves" might literally mean movement.
        // Let's implement cumulative distance as it fits "exhaustion" better.
        
        float moveFrame = Vector3.Distance(transform.position, startPosition); // This is displacement.
        
        // Let's track actual movement for exhaustion.
        // But for "Fog of sand", usually it gets thicker as you go further AWAY.
        // If I walk in circles, does the fog get thick? Maybe not.
        // "Il y a un brouillard de sable sur la map qui est plutôt légé au début, plus le joueur bouge, plus le brouillard s'aipaissi"
        // This suggests time/effort or distance.
        // Let's go with Displacement from some "Safety Zone" (Start Position).
        // It makes more sense for a level design (you can't leave the area).
        
        distanceTraveled = Vector3.Distance(transform.position, startPosition);


        if (enableFogControl)
        {
            float t = Mathf.Clamp01(distanceTraveled / maxDistanceBeforeFaint);
            
            // Add wind turbulence
            float noise = Mathf.PerlinNoise(Time.time * windSpeed, 0f) * windTurbulence;
            
            float currentDensity = Mathf.Lerp(minFogDensity, maxFogDensity, t) + noise;
            RenderSettings.fogDensity = Mathf.Max(0f, currentDensity);
            RenderSettings.fogColor = sandFogColor;

            // Update particles
            if (sandParticles != null)
            {
                var emission = sandParticles.emission;
                emission.rateOverTime = Mathf.Lerp(10f, 100f, t); // Increase particles with distance
            }

            if (distanceTraveled >= maxDistanceBeforeFaint)
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
            playerController.Faint();
        }
    }
}
