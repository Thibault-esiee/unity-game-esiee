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
    public Gradient groundGradient;

    private void Reset()
    {
        groundGradient = new Gradient();
        var colorKeys = new GradientColorKey[2];
        colorKeys[0] = new GradientColorKey(new Color(0.8f, 0.5f, 0.3f), 0.0f); // Darker valley
        colorKeys[1] = new GradientColorKey(new Color(0.9f, 0.8f, 0.6f), 1.0f); // Lighter peak
        var alphaKeys = new GradientAlphaKey[2];
        alphaKeys[0] = new GradientAlphaKey(1.0f, 0.0f);
        alphaKeys[1] = new GradientAlphaKey(1.0f, 1.0f);
        groundGradient.SetKeys(colorKeys, alphaKeys);
    }

    [Header("Decoration")]
    public GameObject[] decorations; // Prefabs de roches, ruines
    public int decorationCount = 5;  // Nombre d’objets par chunk

    private Dictionary<Vector2Int, GameObject> chunkDict = new Dictionary<Vector2Int, GameObject>();
    private Vector2Int lastPlayerChunk;

    void Start()
    {
        // 🔹 Détruire tous les anciens chunks avant de régénérer
        ClearAllChunks();

        // 🔹 Forcer la position initiale du joueur comme point de départ
        lastPlayerChunk = new Vector2Int(
            Mathf.FloorToInt(player.position.x / chunkSize),
            Mathf.FloorToInt(player.position.z / chunkSize)
        );

        UpdateVisibleChunks();
    }

    void Update()
    {
        Vector2Int currentChunk = new Vector2Int(
            Mathf.FloorToInt(player.position.x / chunkSize),
            Mathf.FloorToInt(player.position.z / chunkSize)
        );

        if (currentChunk != lastPlayerChunk)
        {
            lastPlayerChunk = currentChunk;
            UpdateVisibleChunks();
        }
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
            Destroy(chunkDict[coord]);
            chunkDict.Remove(coord);
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

    GameObject GenerateChunk(Vector2Int coord)
    {
        GameObject chunk = new GameObject($"Chunk_{coord.x}_{coord.y}");
        chunk.transform.position = new Vector3(coord.x * chunkSize, 0, coord.y * chunkSize);
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
        
        // Pass the gradient
        if (groundGradient == null || groundGradient.colorKeys.Length == 0) Reset();
        desertChunk.groundGradient = groundGradient;
        
        desertChunk.GenerateChunk();

        return chunk;
    }

    // 🔹 Supprime tous les chunks existants dans la scène
    void ClearAllChunks()
    {
        foreach (Transform child in transform)
        {
            DestroyImmediate(child.gameObject);
        }
        chunkDict.Clear();
    }
}