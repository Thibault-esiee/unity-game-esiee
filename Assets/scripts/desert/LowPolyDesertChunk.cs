using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class LowPolyDesertChunk : MonoBehaviour
{
    [Header("Terrain Settings")]
    public int chunkSize = 50;
    [Header("Noise Settings")]
    public float noiseScale = 50f;
    public float heightMultiplier = 15f;
    
    [Header("Dune Shape")]
    public float dunePeriod = 100f;
    [Range(0f, 1f)] public float duneSharpness = 0.8f;
    public float warpStrength = 100f;
    public float mapWidth = 1000f;

    public Vector2Int coord;

    [Header("Visuals")]
    [Range(0f, 0.2f)] public float colorVariation = 0.05f;
    public Gradient groundGradient;
    
    
    [HideInInspector] public Vector2 exclusionCenter;
    [HideInInspector] public float exclusionRadius;

    
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

    [Header("Rock Settings")]
    public int rockCount = 5;

    private enum BiomeType { Desert, Oasis, Crater, Mountain }
    private BiomeType biomeType = BiomeType.Desert;

    
    private static List<Transform> allBuildings = new List<Transform>();

    public void GenerateChunk()
    {
        
        foreach (Transform child in transform)
        {
            if (Application.isPlaying)
                Destroy(child.gameObject);
            else
                DestroyImmediate(child.gameObject);
        }

        
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

        
        if (mr.sharedMaterial == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader != null)
            {
            if (shader != null)
            {
                Material mat = new Material(shader);
                
                mat.color = Color.white; 
                mr.sharedMaterial = mat;
            }
            }
            else
            {
                Debug.LogError("Failed to find URP/Lit shader. Make sure URP is properly set up in your project.");
            }
        }

        
        
        float biomeNoise = Mathf.PerlinNoise(coord.x * 0.1f, coord.y * 0.1f);
        
        
        
        double absX = (double)coord.x * chunkSize;
        
        
        
        
        if (FloatingOrigin.Instance != null)
             absX += FloatingOrigin.Instance.accumulatedX;

        
        if (Mathf.Abs((float)absX) > mapWidth)
        {
            biomeType = BiomeType.Mountain;
        }
        else
        {
            
            if (biomeNoise > 0.65f) 
                biomeType = BiomeType.Desert; 
            else if (biomeNoise > 0.35f) 
                biomeType = BiomeType.Crater;
            else if (biomeNoise > 0.25f) 
                biomeType = BiomeType.Oasis;
            else
                biomeType = BiomeType.Desert;
        }

        
        
        
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

                
                float avgHeight = (v00.y + v01.y + v10.y + v11.y) / 4f;
                float normalizedHeight = Mathf.Clamp01(avgHeight / heightMultiplier);
                
                
                if (groundGradient == null || groundGradient.colorKeys.Length == 0) Reset();
                Color quadColor = groundGradient.Evaluate(normalizedHeight);

                
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

        
        
        if (biomeType != BiomeType.Oasis && biomeType != BiomeType.Mountain)
        {
            
            GenerateBuildings();
        }
    }

    [Header("Building Settings")]
    public GameObject[] buildingPrefabs;
    [Range(0, 5)] public int buildingCount = 0; 
    public float buildingSinkAmount = 1.0f; 
    public float minBuildingScale = 0.8f;
    public float maxBuildingScale = 1.2f;
    [Range(0.01f, 1f)] public float globalParticleScale = 0.2f; 


    

    
    [HideInInspector] public AudioClip fireSound;
    [HideInInspector] public float fireVolume;
    [HideInInspector] public float fireMinDistance;
    [HideInInspector] public float fireMaxDistance;

    void GenerateBuildings()
    {
        if (buildingPrefabs == null || buildingPrefabs.Length == 0) return;
        if (buildingCount <= 0) return;
        
        
        HashSet<string> audioExclusions = new HashSet<string> { 
            "Building_1", "Building_2", "Building_8", "Building_9",
            "Building_1 Variant", "Building_2 Variant", "Building_8 Variant", "Building_9 Variant" 
        };

        
        int attempts = 0;
        int placed = 0;
        
        float minSpacing = 40f; 

        
        allBuildings.RemoveAll(item => item == null);

        while (placed < buildingCount && attempts < buildingCount * 15) 
        {
            attempts++;
            float x = Random.Range(5f, chunkSize - 5f); 
            float z = Random.Range(5f, chunkSize - 5f);
            
            
            

            
            
            
            
            double absBuildingX = (double)coord.x * chunkSize + x;
            double absBuildingZ = (double)coord.y * chunkSize + z;
            
            if (FloatingOrigin.Instance != null)
            {
               absBuildingX += FloatingOrigin.Instance.accumulatedX;
               absBuildingZ += FloatingOrigin.Instance.accumulatedZ;
            }
            
            float distToStart = Vector2.Distance(new Vector2((float)absBuildingX, (float)absBuildingZ), exclusionCenter);
            if (distToStart < exclusionRadius)
            {
                continue; 
            }



            
            
            Vector3 candidatePos = new Vector3((float)absBuildingX, 0, (float)absBuildingZ);
            bool tooClose = false;
            
            foreach (Transform t in allBuildings)
            {
                if (t == null) continue;
                
                
                
                
                
                
                
                
                
                Vector3 candidateUnityPos = transform.position + new Vector3(x, 0, z);
                
                float dist = Vector3.Distance(candidateUnityPos, t.position);
                
                dist = Vector2.Distance(new Vector2(candidateUnityPos.x, candidateUnityPos.z), new Vector2(t.position.x, t.position.z));
                
                if (dist < minSpacing)
                {
                    tooClose = true;
                    break;
                }
            }
            if (tooClose) continue; 

            

            
            
            

            float y = GetSmoothHeight(x, z);

            
            GameObject prefab = buildingPrefabs[Random.Range(0, buildingPrefabs.Length)];
            if (prefab == null) continue;

            GameObject instance = Instantiate(prefab, transform);
            instance.name = "Building_" + placed;
            
            
            instance.transform.localPosition = new Vector3(x, y - buildingSinkAmount, z);
            
            
            allBuildings.Add(instance.transform);

            
            instance.transform.localRotation = Quaternion.Euler(0, Random.Range(0, 360f), 0);

            
            float s = Random.Range(minBuildingScale, maxBuildingScale);
            instance.transform.localScale = Vector3.one * s;

            
            foreach (var ps in instance.GetComponentsInChildren<ParticleSystem>())
            {
                var main = ps.main;
                var shape = ps.shape;
                
                float factor = s * globalParticleScale;

                
                main.startSizeMultiplier *= factor;
                
                main.startLifetimeMultiplier *= factor;
                
                main.startSpeedMultiplier *= factor;
                
                
                
                shape.scale *= factor;
            }
            
            
            
            
            
            
            bool excludeAudio = false;
            foreach (string ex in audioExclusions)
            {
                if (prefab.name.Contains(ex)) 
                {
                    excludeAudio = true;
                    break;
                }
            }

            if (!excludeAudio && fireSound != null && Application.isPlaying)
            {
                AudioSource audio = instance.AddComponent<AudioSource>();
                audio.clip = fireSound;
                audio.volume = fireVolume;
                audio.loop = true;
                audio.spatialBlend = 1.0f; 
                audio.rolloffMode = AudioRolloffMode.Linear; 
                audio.dopplerLevel = 0f; 
                audio.reverbZoneMix = 0f; 
                audio.pitch = Random.Range(0.8f, 1.2f); 
                
                audio.minDistance = fireMinDistance;
                audio.maxDistance = fireMaxDistance;
                audio.Play();
            }

            
            
            
            foreach (var mf in instance.GetComponentsInChildren<MeshFilter>())
            {
                
                string n = mf.name.ToLower();
                if (n.Contains("particle") || n.Contains("effect") || n.Contains("fire") || n.Contains("smoke") || n.Contains("glow")) 
                    continue;

                
                if (mf.GetComponent<ParticleSystem>() != null) continue;
                if (mf.GetComponent<ParticleSystemRenderer>() != null) continue;
                if (mf.GetComponent<LineRenderer>() != null) continue;

                
                Mesh m = mf.sharedMesh;
                if (m == null) continue;
                if (m.vertexCount < 3) continue;
                if (m.bounds.size.sqrMagnitude < 0.01f) continue;
                
                
                
                try {
                    if (m.GetTopology(0) != MeshTopology.Triangles) continue;
                } catch { continue; }

                
                if (mf.transform.lossyScale.sqrMagnitude < 0.0001f) continue;

                if (mf.GetComponent<Collider>() == null)
                {
                    try 
                    {
                        
                        
                        
                        
                        
                        if (m.vertexCount > 60000)
                        {
                             
                             
                             continue;
                        }
                        else
                        {
                            
                            mf.gameObject.AddComponent<MeshCollider>();
                        }
                    }
                    catch (System.Exception) 
                    { 
                        
                        
                    }
                }
            }

            
            BuildingVariation varScript = instance.GetComponent<BuildingVariation>();
            if (varScript != null)
            {
                varScript.randomSeed = Random.Range(0, 10000);
                varScript.ApplyVariation();
            }

            
            
            GenerateDebris(instance.transform, x, y, z);

            
            if (Random.value < 0.3f)
            {
                GenerateSmoke(instance.transform, x, y, z);
            }

            placed++;
        }
    }

    
    void GenerateSmoke(Transform building, float bx, float by, float bz)
    {
        GameObject smokeObj = new GameObject("Smoke_Auto");
        smokeObj.transform.parent = building;
        
        smokeObj.transform.localPosition = new Vector3(Random.Range(-2f, 2f), Random.Range(1f, 4f), Random.Range(-2f, 2f));
        
        
        ParticleSystem ps = smokeObj.AddComponent<ParticleSystem>();
        ParticleSystemRenderer psr = smokeObj.GetComponent<ParticleSystemRenderer>();
        
        
        
        psr.renderMode = ParticleSystemRenderMode.Mesh;
        GameObject meshRef = GameObject.CreatePrimitive(PrimitiveType.Cube);
        psr.mesh = meshRef.GetComponent<MeshFilter>().sharedMesh;
        
        Shader particleShader = Shader.Find("Universal Render Pipeline/Unlit");
        Material particleMat = new Material(particleShader);
        
        
        
        Color baseSmokeColor = new Color(0.3f, 0.3f, 0.3f, 0.45f); 
        
        if (particleMat.HasProperty("_BaseColor"))
            particleMat.SetColor("_BaseColor", baseSmokeColor);
        else 
             particleMat.SetColor("_Color", baseSmokeColor);

        
        
        particleMat.SetFloat("_Surface", 1); 
        
        particleMat.SetFloat("_Blend", 0); 
        
        
        particleMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        particleMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        particleMat.SetInt("_ZWrite", 0);
        
        particleMat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        particleMat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        particleMat.DisableKeyword("_ALPHATEST_ON"); 
        particleMat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        
        psr.material = particleMat;

        
        var main = ps.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(4f, 7f); 
        main.startSpeed = new ParticleSystem.MinMaxCurve(1.5f, 3.5f); 
        main.startSize = new ParticleSystem.MinMaxCurve(0.2f, 0.5f); 
        
        
        main.startColor = new ParticleSystem.MinMaxGradient(Color.white); 
        main.gravityModifier = -0.05f;
        main.maxParticles = 500; 
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        
        var emission = ps.emission;
        emission.rateOverTime = new ParticleSystem.MinMaxCurve(15f, 25f); 

        
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 18f; 
        shape.radius = 0.6f; 

        
        var col = ps.colorOverLifetime;
        col.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            
            new GradientColorKey[] { new GradientColorKey(new Color(0.7f, 0.7f, 0.7f), 0.0f), new GradientColorKey(new Color(0.3f, 0.3f, 0.3f), 0.8f) },
            new GradientAlphaKey[] { new GradientAlphaKey(0.3f, 0.0f), new GradientAlphaKey(0.0f, 1.0f) }
        );
        col.color = grad;

        
        var sz = ps.sizeOverLifetime;
        sz.enabled = true;
        AnimationCurve curve = new AnimationCurve();
        curve.AddKey(0.0f, 0.8f); 
        curve.AddKey(1.0f, 2.0f); 
        sz.size = new ParticleSystem.MinMaxCurve(1.0f, curve);

        
        var rot = ps.rotationOverLifetime;
        rot.enabled = true;
        rot.x = new ParticleSystem.MinMaxCurve(-45f, 45f);
        rot.y = new ParticleSystem.MinMaxCurve(-45f, 45f);
        rot.z = new ParticleSystem.MinMaxCurve(-45f, 45f);
    }

    
    void GenerateDebris(Transform building, float bx, float by, float bz)
    {
        int debrisCount = Random.Range(5, 12); 
        
        
        Color debrisColor = new Color(0.8f, 0.6f, 0.4f); 

        for (int i = 0; i < debrisCount; i++)
        {
            
            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            float dist = Random.Range(2.0f, 6.0f); 
            float dx = Mathf.Cos(angle) * dist;
            float dz = Mathf.Sin(angle) * dist;

            float yPos = GetSmoothHeight(bx + dx, bz + dz);
            
            GameObject rubble = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rubble.name = "Rubble_Auto";
            rubble.transform.parent = building; 
            
            
            rubble.transform.position = new Vector3(transform.position.x + bx + dx, yPos - 0.1f, transform.position.z + bz + dz);
            rubble.transform.rotation = Random.rotation; 
            
            
            Vector3 scale = Vector3.one * Random.Range(0.3f, 0.8f);
            if (Random.value < 0.2f) scale *= 1.5f; 
            rubble.transform.localScale = scale;

            DestroyImmediate(rubble.GetComponent<BoxCollider>());

            MeshRenderer mr = rubble.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                
                float darken = Random.Range(0.8f, 1.1f);
                mat.color = new Color(debrisColor.r * darken, debrisColor.g * darken, debrisColor.b * darken);
                mr.sharedMaterial = mat;
            }
        }
    }

    
    
    
    
    
    
    
    float GetSmoothHeight(float x, float z)
    {
        
        
        
        double absGlobalX = transform.position.x + x;
        double absGlobalZ = transform.position.z + z;
        
        if (FloatingOrigin.Instance != null)
        {
            absGlobalX += FloatingOrigin.Instance.accumulatedX;
            absGlobalZ += FloatingOrigin.Instance.accumulatedZ;
        }

        
        float safeX = (float)PingPongDouble(absGlobalX, 50000.0);
        float safeZ = (float)PingPongDouble(absGlobalZ, 50000.0);

        
        

        
        float baseH = Mathf.PerlinNoise(safeX / 800f, safeZ / 800f);
        
        
        float wx = safeX + Mathf.PerlinNoise(safeX / 300f, safeZ / 300f) * warpStrength;
        float wz = safeZ + Mathf.PerlinNoise(safeZ / 300f + 55f, safeX / 300f + 55f) * warpStrength;

        
        
        float h1 = Mathf.PerlinNoise(wx / 150f, wz / 150f);
        
        
        float h2 = Mathf.PerlinNoise(wx / 60f, wz / 60f) * 0.5f;
        
        
        float h3 = Mathf.PerlinNoise(wx / 20f, wz / 20f) * 0.1f;

        
        float finalH = baseH * 0.5f + h1 * 0.4f + h2 * 0.1f + h3;
        
        
        
        finalH = Mathf.Pow(Mathf.Clamp01(finalH), 1.2f); 

        float finalHeight = finalH * heightMultiplier;

        
        Vector2 center = new Vector2(chunkSize / 2f, chunkSize / 2f);
        finalHeight = GetHeightForBiome(x, z, center, finalHeight);
        
        
        if (float.IsNaN(finalHeight) || float.IsInfinity(finalHeight))
        {
            finalHeight = 0f;
        }
        
        
        finalHeight = Mathf.Clamp(finalHeight, 0, heightMultiplier * 2.5f);

        return finalHeight;
    }

    
    double PingPongDouble(double t, double length)
    {
        t = System.Math.Abs(t); 
        double cycle = t % (length * 2.0);
        if (cycle > length)
        {
            return (length * 2.0) - cycle;
        }
        return cycle;
    }

    
    
    
    float GetHeightForBiome(float x, float z, Vector2 center, float baseHeight)
    {
        
        double absX = transform.position.x + x;
        double absZ = transform.position.z + z;
        if (FloatingOrigin.Instance != null) { absX += FloatingOrigin.Instance.accumulatedX; absZ += FloatingOrigin.Instance.accumulatedZ; }

        float safeX = (float)PingPongDouble(absX, 50000.0);
        float safeZ = (float)PingPongDouble(absZ, 50000.0);

        
        
        
        float distFromCenter = Mathf.Abs((float)absX);
        float transitionDist = 150f; 
        
        
        float mountainBlend = Mathf.Clamp01((distFromCenter - mapWidth) / transitionDist);

        
        float finalHeight = baseHeight;

        
        if (mountainBlend > 0.001f)
        {
            
            
            float mScale = 250f; 
            
            float p1 = Mathf.PerlinNoise(safeX / mScale, safeZ / mScale);
            float ridge = 1f - Mathf.Abs(p1 * 2f - 1f); 
            ridge = Mathf.Pow(ridge, 2.5f); 
            
            float mHeight = ridge * 80f; 

            
            float p2 = Mathf.PerlinNoise(safeX / 60f, safeZ / 60f);
            mHeight += p2 * 10f;

            
            
            float targetHeight = Mathf.Max(baseHeight, mHeight);
            finalHeight = Mathf.Lerp(baseHeight, targetHeight, mountainBlend);
        }

        
        
        if (mountainBlend < 0.5f)
        {
            float localBiomeEffect = 0f;
            if (biomeType == BiomeType.Crater || biomeType == BiomeType.Oasis)
            {
                float dist = Vector2.Distance(new Vector2(x, z), center) / (chunkSize / 2f);
                dist = Mathf.Clamp01(dist);
                float craterFactor = Mathf.SmoothStep(1f, 0f, dist);
                
                if (biomeType == BiomeType.Crater)
                    localBiomeEffect -= craterFactor * 5f;
                else
                    localBiomeEffect -= craterFactor * 2f;
            }
            
            finalHeight += localBiomeEffect * (1f - mountainBlend);
        }

        return Mathf.Max(0f, finalHeight);
    }

    
    
    
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

    
    
    
    void GenerateRocks()
    {
        for (int i = 0; i < rockCount; i++)
        {
            float x = Random.Range(0f, chunkSize);
            float z = Random.Range(0f, chunkSize);
            
            
            double absX = transform.position.x + x;
            double absZ = transform.position.z + z;
            if (FloatingOrigin.Instance != null) { absX += FloatingOrigin.Instance.accumulatedX; absZ += FloatingOrigin.Instance.accumulatedZ; }

            float safeX = (float)PingPongDouble(absX, 50000.0);
            float safeZ = (float)PingPongDouble(absZ, 50000.0);

            float safeScale = Mathf.Max(noiseScale, 0.1f);

            float y = Mathf.PerlinNoise(
                safeX / safeScale,
                safeZ / safeScale
            ) * heightMultiplier;
            
            
            if (float.IsNaN(y) || float.IsInfinity(y)) y = 0;
            y = Mathf.Clamp(y, 0, heightMultiplier * 2.0f);

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