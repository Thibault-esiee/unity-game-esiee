using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways] // Permet aussi de générer dans l’éditeur
public class TerrainGenerator : MonoBehaviour
{
    [Header("Player & Chunk Settings")]
    public Transform player;
    public int chunkSize = 50;       // Taille d’un chunk
    public int chunksVisible = 3;    // Nombre de chunks visibles autour du joueur

    [Header("Noise Settings")]
    public float noiseScale = 50f;     // Turbulence scale (smaller = more sand grain detail)
    public float heightMultiplier = 15f; // Dune height
    
    [Header("Dune Shape")]
    [Header("Dune Shape")]
    public float dunePeriod = 100f;
    [Range(0f, 1f)] public float duneSharpness = 0.8f;
    public float warpStrength = 100f;

    [Header("Visuals")]
    public Material terrainMaterial; // Material avec le Shader "DesertDistanceFade"
    public Gradient groundGradient;

    private void Reset()
    {
        // ⚠️ WAR MODE DEFAULT COLORS
        groundGradient = new Gradient();
        var colorKeys = new GradientColorKey[3];
        colorKeys[0] = new GradientColorKey(new Color(0.35f, 0.25f, 0.2f), 0.0f); // Burnt/Dark Valley
        colorKeys[1] = new GradientColorKey(new Color(0.7f, 0.5f, 0.3f), 0.4f); // Mid-tone Dust
        colorKeys[2] = new GradientColorKey(new Color(0.85f, 0.7f, 0.5f), 1.0f); // Light Peak
        var alphaKeys = new GradientAlphaKey[2];
        alphaKeys[0] = new GradientAlphaKey(1.0f, 0.0f);
        alphaKeys[1] = new GradientAlphaKey(1.0f, 1.0f);
        groundGradient.SetKeys(colorKeys, alphaKeys);
    }

    [Header("Decoration")]
    public GameObject[] decorations; // Prefabs de roches, ruines
    public int decorationCount = 5;  // Nombre d’objets par chunk

    [Header("Building Generation")]
    public GameObject[] buildingPrefabs;
    public int buildingsPerChunk = 3; // Was 1. Increased for War Zone density.
    public float buildingSinkAmount = 1.0f;

    [Header("World Boundaries")]
    public float mapWidth = 600f; // Distance from center where world ends (Mountains start)
    [Range(0.1f, 2f)] public float minBuildingScale = 0.5f;
    [Range(0.1f, 2f)] public float maxBuildingScale = 0.8f;
    [Range(0.01f, 1f)] public float particleScale = 0.2f; // Contrôle global des particules (Allow smaller values)

    [Header("Layers")]
    public string chunkLayerName = "Terrain"; // Le nom du layer pour le sol
    
    GameObject GenerateChunk(Vector2Int coord)
    {
        GameObject chunk = new GameObject($"Chunk_{coord.x}_{coord.y}");
        
        // 🔹 ASSIGNATION AUTOMATIQUE DU LAYER "Terrain"
        int terrainLayerIndex = LayerMask.NameToLayer(chunkLayerName);
        if (terrainLayerIndex != -1)
        {
            chunk.layer = terrainLayerIndex;
        }

        // ⚠️ FLOATING ORIGIN ADJUSTMENT (DOUBLE PRECISION)
        double chunkTrueX = (double)coord.x * chunkSize;
        double chunkTrueZ = (double)coord.y * chunkSize;
        
        double offsetX = 0;
        double offsetZ = 0;
        if (floatingOrigin != null) 
        { 
            offsetX = floatingOrigin.accumulatedX; 
            offsetZ = floatingOrigin.accumulatedZ; 
        }

        float finalX = (float)(chunkTrueX - offsetX);
        float finalZ = (float)(chunkTrueZ - offsetZ);

        chunk.transform.position = new Vector3(finalX, 0, finalZ);
        chunk.transform.parent = this.transform;

        // Génération du mesh low-poly
        LowPolyDesertChunk desertChunk = chunk.AddComponent<LowPolyDesertChunk>();
        desertChunk.coord = coord;
        desertChunk.chunkSize = chunkSize;
        desertChunk.noiseScale = noiseScale;
        desertChunk.heightMultiplier = heightMultiplier;
        
        // New parameters
        desertChunk.dunePeriod = dunePeriod;
        desertChunk.duneSharpness = duneSharpness;
        desertChunk.warpStrength = warpStrength;
        desertChunk.mapWidth = mapWidth; // 🌍 Pass the boundary limit

        // Settings Building
        desertChunk.buildingPrefabs = buildingPrefabs;
        desertChunk.buildingCount = buildingsPerChunk;
        desertChunk.buildingSinkAmount = buildingSinkAmount;
        desertChunk.minBuildingScale = minBuildingScale;
        desertChunk.maxBuildingScale = maxBuildingScale;
        desertChunk.globalParticleScale = particleScale; // Pass the value

        // Pass the gradient
        if (groundGradient == null || groundGradient.colorKeys.Length == 0) Reset();
        desertChunk.groundGradient = groundGradient;
        
        desertChunk.GenerateChunk();

        // 🔹 ASSIGNER LE MATERIAU SPECIAL (SI LE USER L'A MIS)
        if (terrainMaterial != null)
        {
            var mr = chunk.GetComponent<MeshRenderer>();
            // if (mr != null) mr.sharedMaterial = terrainMaterial; // DISABLED FOR DEBUGGING
        }

        return chunk;
    }

    private Dictionary<Vector2Int, GameObject> chunkDict = new Dictionary<Vector2Int, GameObject>();
    private Vector2Int lastPlayerChunk;
    
    // --- INTEGRATION FLOATING ORIGIN ---
    private FloatingOrigin floatingOrigin;
    
    Vector3 GetAbsolutePlayerPos()
    {
        Vector3 pos = player.position;
        if (floatingOrigin != null)
        {
            pos += floatingOrigin.accumulatedOffset;
        }
        return pos;
    }

    void Start()
    {
        floatingOrigin = FloatingOrigin.Instance;
        if (floatingOrigin == null) floatingOrigin = FindFirstObjectByType<FloatingOrigin>();

        // ⚠️ AUTO-FIX: Apply War Gradient if user has the old default (2 keys)
        if (groundGradient == null || groundGradient.colorKeys.Length <= 2)
        {
            Reset(); // Force the War Gradient
        }

        // 🔹 Détruire tous les anciens chunks avant de régénérer
        ClearAllChunks();

        Vector3 realPlayerPos = GetAbsolutePlayerPos();

        // 🔹 Forcer la position initiale du joueur comme point de départ
        lastPlayerChunk = new Vector2Int(
            Mathf.FloorToInt(realPlayerPos.x / chunkSize),
            Mathf.FloorToInt(realPlayerPos.z / chunkSize)
        );

        UpdateVisibleChunks();
    }

    void Update()
    {
        Vector3 realPlayerPos = GetAbsolutePlayerPos();

        Vector2Int currentChunk = new Vector2Int(
            Mathf.FloorToInt(realPlayerPos.x / chunkSize),
            Mathf.FloorToInt(realPlayerPos.z / chunkSize)
        );

        if (currentChunk != lastPlayerChunk)
        {
            lastPlayerChunk = currentChunk;
            UpdateVisibleChunks();
        }
    }

    void SafeDestroy(UnityEngine.Object obj)
    {
        if (Application.isPlaying)
            Destroy(obj);
        else
            DestroyImmediate(obj);
    }

    void UpdateVisibleChunks()
    {
        // Supprimer les chunks trop loin
        List<Vector2Int> chunksToRemove = new List<Vector2Int>();
        foreach (var chunkCoord in chunkDict.Keys)
        {
            if (Vector2Int.Distance(chunkCoord, lastPlayerChunk) > chunksVisible)
                chunksToRemove.Add(chunkCoord);
        }

        foreach (var coord in chunksToRemove)
        {
            if (chunkDict.ContainsKey(coord))
            {
                SafeDestroy(chunkDict[coord]);
                chunkDict.Remove(coord);
            }
        }

        // Générer les nouveaux chunks autour du joueur
        for (int x = -chunksVisible; x <= chunksVisible; x++)
        {
            for (int z = -chunksVisible; z <= chunksVisible; z++)
            {
                Vector2Int chunkCoord = new Vector2Int(lastPlayerChunk.x + x, lastPlayerChunk.y + z);
                if (!chunkDict.ContainsKey(chunkCoord))
                {
                    GameObject chunk = GenerateChunk(chunkCoord);
                    chunkDict.Add(chunkCoord, chunk);
                }
            }
        }
    }



    // 🔹 Supprime tous les chunks existants dans la scène
    // 🔹 Supprime tous les chunks existants dans la scène
    void ClearAllChunks()
    {
        // Utiliser une boucle while est plus sûr que foreach quand on détruit des objets
        // car la collection est modifiée pendant l'itération.
        while (transform.childCount > 0)
        {
             DestroyImmediate(transform.GetChild(0).gameObject);
        }
        chunkDict.Clear();
    }
}