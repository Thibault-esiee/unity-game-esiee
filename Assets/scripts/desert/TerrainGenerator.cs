using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways] 
public class TerrainGenerator : MonoBehaviour
{
    [Header("Player & Chunk Settings")]
    public Transform player;
    public int chunkSize = 50;       
    public int chunksVisible = 3;    

    [Header("Noise Settings")]
    public float noiseScale = 50f;     
    public float heightMultiplier = 15f; 
    
    [Header("Dune Shape")]
    [Header("Dune Shape")]
    public float dunePeriod = 100f;
    [Range(0f, 1f)] public float duneSharpness = 0.8f;
    public float warpStrength = 100f;

    [Header("Visuals")]
    public Material terrainMaterial; 
    public Gradient groundGradient;

    private void Reset()
    {
        
        groundGradient = new Gradient();
        var colorKeys = new GradientColorKey[3];
        colorKeys[0] = new GradientColorKey(new Color(0.35f, 0.25f, 0.2f), 0.0f); 
        colorKeys[1] = new GradientColorKey(new Color(0.7f, 0.5f, 0.3f), 0.4f); 
        colorKeys[2] = new GradientColorKey(new Color(0.85f, 0.7f, 0.5f), 1.0f); 
        var alphaKeys = new GradientAlphaKey[2];
        alphaKeys[0] = new GradientAlphaKey(1.0f, 0.0f);
        alphaKeys[1] = new GradientAlphaKey(1.0f, 1.0f);
        groundGradient.SetKeys(colorKeys, alphaKeys);
    }

    [Header("Decoration")]
    public GameObject[] decorations; 
    public int decorationCount = 5;  

    [Header("Building Generation")]
    public GameObject[] buildingPrefabs;
    public int buildingsPerChunk = 3; 

    public float buildingSinkAmount = 1.0f;

    public float safeZoneRadius = 100f; 
    

    [Header("Audio")]
    public AudioClip fireSound;
    [Range(0f, 1f)] public float fireVolume = 1.0f; 
    public float fireMinDistance = 10f; 
    public float fireMaxDistance = 80f; 

    [Header("World Boundaries")]
    public float mapWidth = 600f; 
    [Range(0.1f, 2f)] public float minBuildingScale = 0.5f;
    [Range(0.1f, 2f)] public float maxBuildingScale = 0.8f;
    [Range(0.01f, 1f)] public float particleScale = 0.2f; 

    [Header("Layers")]
    public string chunkLayerName = "Terrain"; 
    
    GameObject GenerateChunk(Vector2Int coord)
    {
        GameObject chunk = new GameObject($"Chunk_{coord.x}_{coord.y}");
        
        
        int terrainLayerIndex = LayerMask.NameToLayer(chunkLayerName);
        if (terrainLayerIndex != -1)
        {
            chunk.layer = terrainLayerIndex;
        }

        
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

        
        LowPolyDesertChunk desertChunk = chunk.AddComponent<LowPolyDesertChunk>();
        desertChunk.coord = coord;
        desertChunk.chunkSize = chunkSize;
        desertChunk.noiseScale = noiseScale;
        desertChunk.heightMultiplier = heightMultiplier;
        
        
        desertChunk.dunePeriod = dunePeriod;
        desertChunk.duneSharpness = duneSharpness;
        desertChunk.warpStrength = warpStrength;
        desertChunk.mapWidth = mapWidth; 

        
        desertChunk.buildingPrefabs = buildingPrefabs;
        desertChunk.buildingCount = buildingsPerChunk;
        desertChunk.buildingSinkAmount = buildingSinkAmount;
        desertChunk.minBuildingScale = minBuildingScale;
        desertChunk.maxBuildingScale = maxBuildingScale;
        desertChunk.globalParticleScale = particleScale; 
        

        
        desertChunk.fireSound = fireSound;
        desertChunk.fireVolume = fireVolume;
        desertChunk.fireMinDistance = fireMinDistance;
        desertChunk.fireMaxDistance = fireMaxDistance;

        
        if (groundGradient == null || groundGradient.colorKeys.Length == 0) Reset();
        desertChunk.groundGradient = groundGradient;
        

        
        
        desertChunk.exclusionCenter = new Vector2(initialAbsPlayerPos.x, initialAbsPlayerPos.z);
        desertChunk.exclusionRadius = safeZoneRadius;

        desertChunk.GenerateChunk();

        
        if (terrainMaterial != null)
        {
            var mr = chunk.GetComponent<MeshRenderer>();
            
        }

        return chunk;
    }

    private Dictionary<Vector2Int, GameObject> chunkDict = new Dictionary<Vector2Int, GameObject>();
    private Vector2Int lastPlayerChunk;
    


    
    private FloatingOrigin floatingOrigin;
    private Vector3 initialAbsPlayerPos; 
    
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
        
        
        initialAbsPlayerPos = GetAbsolutePlayerPos();

        
        if (groundGradient == null || groundGradient.colorKeys.Length <= 2)
        {
            Reset(); 
        }

        
        ClearAllChunks();

        Vector3 realPlayerPos = GetAbsolutePlayerPos();

        
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



    
    
    void ClearAllChunks()
    {
        
        
        while (transform.childCount > 0)
        {
             DestroyImmediate(transform.GetChild(0).gameObject);
        }
        chunkDict.Clear();
    }
}