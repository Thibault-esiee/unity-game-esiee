using UnityEngine;
using System.Collections.Generic;
using Cinemachine;

public class FloatingOrigin : MonoBehaviour
{
    [Header("Settings")]
    public float threshold = 5000f;

    [Header("Debug Info")]
    public Vector3 accumulatedOffset;
    
    public double accumulatedX;
    public double accumulatedZ;
    
    public static FloatingOrigin Instance;

    void Awake()
    {
        Instance = this;
    }

    void LateUpdate()
    {
        Vector3 cameraPos = Camera.main.transform.position;
        cameraPos.y = 0;

        if (cameraPos.magnitude > threshold)
        {
            ShiftWorld(-cameraPos);
        }
    }

    void ShiftWorld(Vector3 offset)
    {
        Debug.Log($"[FloatingOrigin] Shifting world by {offset}");
        
        accumulatedX -= (double)offset.x;
        accumulatedZ -= (double)offset.z;
        accumulatedOffset -= offset;
        var rootObjects = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
        foreach (var go in rootObjects)
        {
            go.transform.position += offset;
        }

        var vcams = FindObjectsByType<CinemachineVirtualCamera>(FindObjectsSortMode.None);
        foreach (var vcam in vcams)
        {
            vcam.OnTargetObjectWarped(vcam.Follow, offset);
        }
        
        ParticleSystem[] particles = FindObjectsByType<ParticleSystem>(FindObjectsSortMode.None);
        foreach (ParticleSystem sys in particles)
        {
            if (sys.main.simulationSpace == ParticleSystemSimulationSpace.World)
            {
                sys.Clear(); 
            }
        }
    }

    public Vector3 GetAbsolutePosition(Vector3 unityPosition)
    {
        return unityPosition + accumulatedOffset;
    }
}
