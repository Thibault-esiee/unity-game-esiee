using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class LowPolyDesertChunk : MonoBehaviour
{
    [Header("Terrain Settings")]
    public int chunkSize = 50;
    [Header("Noise Settings")]
    public float noiseScale = 50f;     // Scale of the turbulence
    public float heightMultiplier = 15f; // Max height
    
    [Header("Dune Shape")]
    public float dunePeriod = 100f;    // Width of one dune wave
    [Range(0f, 1f)] public float duneSharpness = 0.8f; // How "pinched" the top is
    public float warpStrength = 100f;   // Distortion amount (Increased for curvy dunes)

    public Vector2Int coord;

    [Header("Visuals")]
    [Range(0f, 0.2f)] public float colorVariation = 0.05f;
    public Gradient groundGradient; // Gradient based on height
    
    // Default gradient setup helper
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

    [Header("Rock Settings")]
    public int rockCount = 5;

    private enum BiomeType { Desert, Oasis, Crater, Mountain }
    private BiomeType biomeType = BiomeType.Desert;

    public void GenerateChunk()
    {
        // 🔹 Nettoyer le chunk avant de régénérer
        foreach (Transform child in transform)
        {
            if (Application.isPlaying)
                Destroy(child.gameObject);
            else
                DestroyImmediate(child.gameObject);
        }

        // 🔹 MeshFilter & Renderer
        MeshFilter mf = GetComponent<MeshFilter>();
        if (mf == null)
        {
            mf = gameObject.AddComponent<MeshFilter>();
            if (mf == null)
            {
                Debug.LogError("Failed to add MeshFilter component to " + gameObject.name);
                return;
            }
        }

        MeshRenderer mr = GetComponent<MeshRenderer>();
        if (mr == null)
        {
            mr = gameObject.AddComponent<MeshRenderer>();
            if (mr == null)
            {
                Debug.LogError("Failed to add MeshRenderer component to " + gameObject.name);
                return;
            }
        }

        // Matériau partagé
        if (mr.sharedMaterial == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader != null)
            {
            if (shader != null)
            {
                Material mat = new Material(shader);
                // Mat color is white so vertex colors show up correctly
                mat.color = Color.white; 
                mr.sharedMaterial = mat;
            }
            }
            else
            {
                Debug.LogError("Failed to find URP/Lit shader. Make sure URP is properly set up in your project.");
            }
        }

        // 🔹 Déterminer le biome
        float biomeNoise = Mathf.PerlinNoise(coord.x * 0.1f, coord.y * 0.1f);
        if (biomeNoise > 0.75f)
            biomeType = BiomeType.Mountain;
        else if (biomeNoise > 0.55f)
            biomeType = BiomeType.Crater;
        else if (biomeNoise > 0.35f)
            biomeType = BiomeType.Oasis;
        else
            biomeType = BiomeType.Desert;

        // -----------------------
        // 1️⃣ Génération du mesh
        // -----------------------
        Vector3[] baseVertices = new Vector3[(chunkSize + 1) * (chunkSize + 1)];
        Vector2 center = new Vector2(chunkSize / 2f, chunkSize / 2f);

        for (int i = 0, z = 0; z <= chunkSize; z++)
        {
            for (int x = 0; x <= chunkSize; x++, i++)
            {
                float y = GetSmoothHeight(x, z);
                baseVertices[i] = new Vector3(x, y, z);
            }
        }

        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Color> colors = new List<Color>();

        for (int z = 0; z < chunkSize; z++)
        {
            for (int x = 0; x < chunkSize; x++)
            {
                int i = z * (chunkSize + 1) + x;

                Vector3 v00 = baseVertices[i];
                Vector3 v01 = baseVertices[i + 1];
                Vector3 v10 = baseVertices[i + chunkSize + 1];
                Vector3 v11 = baseVertices[i + chunkSize + 2];

                // Calculate average height for color
                float avgHeight = (v00.y + v01.y + v10.y + v11.y) / 4f;
                float normalizedHeight = Mathf.Clamp01(avgHeight / heightMultiplier);
                
                // Sample gradient (verify it's initialized)
                if (groundGradient == null || groundGradient.colorKeys.Length == 0) Reset();
                Color quadColor = groundGradient.Evaluate(normalizedHeight);

                // Add random variation
                float offset = Random.Range(-colorVariation, colorVariation);
                quadColor.r += offset;
                quadColor.g += offset;
                quadColor.b += offset;

                AddFace(vertices, triangles, colors, v00, v10, v01, quadColor);
                AddFace(vertices, triangles, colors, v01, v10, v11, quadColor);
            }
        }

        Mesh mesh = new Mesh();
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.colors = colors.ToArray();
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        mf.sharedMesh = mesh;

        MeshCollider mc = GetComponent<MeshCollider>();
        if (mc == null)
        {
            mc = gameObject.AddComponent<MeshCollider>();
            if (mc == null)
            {
                Debug.LogError("Failed to add MeshCollider component to " + gameObject.name);
                return;
            }
        }
        
        if (mesh != null)
        {
            mc.sharedMesh = mesh;
        }
        else
        {
            Debug.LogError("Cannot set sharedMesh: mesh is null");
            return;
        }

        // -----------------------
        // 2️⃣ Rochers (pas dans oasis/cratère)
        // -----------------------
        if (biomeType != BiomeType.Oasis && biomeType != BiomeType.Crater)
            GenerateRocks();
    }

    // -----------------------
    // Hauteur lissée
    // -----------------------
    // -----------------------
    // -----------------------
    // Rolling Organic Dunes (FBM)
    // -----------------------
    float GetSmoothHeight(float x, float z)
    {
        float globalX = (float)coord.x * chunkSize + x;
        float globalZ = (float)coord.y * chunkSize + z;

        // 1. Large Scale Undulation (The "Base" Hills)
        float baseH = Mathf.PerlinNoise(globalX / 800f, globalZ / 800f);
        
        // 2. Medium Warp (Wind directionality)
        float wx = globalX + Mathf.PerlinNoise(globalX / 300f, globalZ / 300f) * warpStrength;
        float wz = globalZ + Mathf.PerlinNoise(globalZ / 300f + 55f, globalX / 300f + 55f) * warpStrength;

        // 3. Layered Noise (FBM) for organic texture
        // Layer 1: Main Shape (Soft)
        float h1 = Mathf.PerlinNoise(wx / 150f, wz / 150f);
        
        // Layer 2: Detail (Smaller)
        float h2 = Mathf.PerlinNoise(wx / 60f, wz / 60f) * 0.5f;
        
        // Layer 3: Micro grain
        float h3 = Mathf.PerlinNoise(wx / 20f, wz / 20f) * 0.1f;

        // Combine:
        float finalH = baseH * 0.5f + h1 * 0.4f + h2 * 0.1f + h3;
        
        // Power for slight "valley" flattening without sharp peaks
        finalH = Mathf.Pow(finalH, 1.2f); // Very gentle curve

        float finalHeight = finalH * heightMultiplier;

        // Appliquer la forme du biome (cratères/oasis)
        Vector2 center = new Vector2(chunkSize / 2f, chunkSize / 2f);
        finalHeight = GetHeightForBiome(x, z, center, finalHeight);
        
        return finalHeight;
    }

    // -----------------------
    // Biomes et variations
    // -----------------------
    float GetHeightForBiome(float x, float z, Vector2 center, float baseHeight)
    {
        float height = baseHeight;

        switch (biomeType)
        {
            case BiomeType.Crater:
            case BiomeType.Oasis:
                {
                    float dist = Vector2.Distance(new Vector2(x, z), center) / (chunkSize / 2f);
                    // Ensure we strictly clamp to 0..1
                    dist = Mathf.Clamp01(dist);
                    
                    // Old math: Cos(dist * PI) which is -1 at dist=1. Abs(-1)=1. 
                    // This creates a hole at the edge.
                    // New math: SmoothStep (1 -> 0)
                    // At dist 0 (center) -> 1. At dist 1 (edge) -> 0.
                    float craterFactor = Mathf.SmoothStep(1f, 0f, dist);
                    
                    if (biomeType == BiomeType.Crater)
                        height -= craterFactor * 5f; // Deep crater at center, flat at edge
                    else // Oasis
                        height -= craterFactor * 2f; // Shallow depression
                    
                    break;
                }

            case BiomeType.Mountain:
                {
                    float mountainNoise = Mathf.PerlinNoise(
                        (x + coord.x * chunkSize) / (noiseScale * 0.5f),
                        (z + coord.y * chunkSize) / (noiseScale * 0.5f)
                    );
                    height += mountainNoise * heightMultiplier * 3f;
                    break;
                }
        }

        return Mathf.Max(0f, height);
    }

    // -----------------------
    // Face low-poly
    // -----------------------
    private void AddFace(List<Vector3> verts, List<int> tris, List<Color> cols,
                         Vector3 a, Vector3 b, Vector3 c, Color faceColor)
    {
        int index = verts.Count;
        verts.Add(a);
        verts.Add(b);
        verts.Add(c);

        tris.Add(index);
        tris.Add(index + 1);
        tris.Add(index + 2);

        cols.Add(faceColor);
        cols.Add(faceColor);
        cols.Add(faceColor);
    }

    // -----------------------
    // 🌵 Rochers procéduraux
    // -----------------------
    void GenerateRocks()
    {
        for (int i = 0; i < rockCount; i++)
        {
            float x = Random.Range(0f, chunkSize);
            float z = Random.Range(0f, chunkSize);

            float y = Mathf.PerlinNoise(
                (x + coord.x * chunkSize) / noiseScale,
                (z + coord.y * chunkSize) / noiseScale
            ) * heightMultiplier;

            Vector3 rockPos = new Vector3(x, y, z);

            GameObject rock = new GameObject("Rock");
            rock.transform.parent = transform;
            rock.transform.localPosition = rockPos;

            int pieces = Random.Range(2, 5);
            for (int j = 0; j < pieces; j++)
            {
                GameObject piece = GameObject.CreatePrimitive(Random.value < 0.6f ? PrimitiveType.Cube : PrimitiveType.Sphere);
                piece.transform.parent = rock.transform;

                float size = Random.Range(0.5f, 1.5f) * (1f / (j + 1));
                Vector3 scale = Vector3.one * size;
                if (piece.GetComponent<MeshFilter>().sharedMesh.name.Contains("Sphere"))
                    scale.y *= 0.6f;

                piece.transform.localScale = scale;
                piece.transform.localPosition = new Vector3(Random.Range(-0.5f, 0.5f), j * 0.3f * size, Random.Range(-0.5f, 0.5f));
                piece.transform.localRotation = Quaternion.Euler(Random.Range(0f, 360f), Random.Range(0f, 360f), Random.Range(0f, 360f));

                MeshRenderer mr = piece.GetComponent<MeshRenderer>();
                mr.sharedMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                float colVar = Random.Range(-0.1f, 0.1f);
                mr.sharedMaterial.color = new Color(0.5f + colVar, 0.45f + colVar, 0.4f + colVar);
            }
        }
    }
}