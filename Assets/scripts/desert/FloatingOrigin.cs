using UnityEngine;
using System.Collections.Generic;
using Cinemachine;

public class FloatingOrigin : MonoBehaviour
{
    [Header("Settings")]
    public float threshold = 5000f; // Distance before reset

    [Header("Debug Info")]
    public Vector3 accumulatedOffset; // Keep for Inspector visibility (Float)
    
    // Double precision counters for logic
    public double accumulatedX;
    public double accumulatedZ;
    
    // Singleton simple for access
    public static FloatingOrigin Instance;

    void Awake()
    {
        Instance = this;
    }

    void LateUpdate()
    {
        Vector3 cameraPos = Camera.main.transform.position;
        // Ignore Y (Height), only check X and Z distance
        cameraPos.y = 0;

        if (cameraPos.magnitude > threshold)
        {
            ShiftWorld(-cameraPos);
        }
    }

    void ShiftWorld(Vector3 offset)
    {
        Debug.Log($"[FloatingOrigin] Shifting world by {offset}");
        
        // 1. Mise à jour de l'offset global (Double Precision)
        accumulatedX -= (double)offset.x;
        accumulatedZ -= (double)offset.z;
        accumulatedOffset -= offset; // Keep float sync for debug

        // 2. Déplacer tous les objets racine de la scène
        var rootObjects = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
        foreach (var go in rootObjects)
        {
            go.transform.position += offset;
        }

        // 3. Informer Cinemachine
        var vcams = FindObjectsByType<CinemachineVirtualCamera>(FindObjectsSortMode.None);
        foreach (var vcam in vcams)
        {
            vcam.OnTargetObjectWarped(vcam.Follow, offset);
        }
        
        // 4. Particles
        ParticleSystem[] particles = FindObjectsByType<ParticleSystem>(FindObjectsSortMode.None);
        foreach (ParticleSystem sys in particles)
        {
            if (sys.main.simulationSpace == ParticleSystemSimulationSpace.World)
            {
                sys.Clear(); 
            }
        }
    }

    // Helper pour convertir une position "Monde Unity" en "Vraie Position Absolue"
    public Vector3 GetAbsolutePosition(Vector3 unityPosition)
    {
        return unityPosition + accumulatedOffset;
    }
}
