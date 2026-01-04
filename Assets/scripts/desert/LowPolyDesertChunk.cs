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
    public float mapWidth = 1000f;      // Default large value

    public Vector2Int coord;

    [Header("Visuals")]
    [Range(0f, 0.2f)] public float colorVariation = 0.05f;
    public Gradient groundGradient; // Gradient based on height
    
    // Default gradient setup helper
    private void Reset()
    {
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
        // 🔹 Déterminer le biome
        float biomeNoise = Mathf.PerlinNoise(coord.x * 0.1f, coord.y * 0.1f);
        
        // 🌍 Check Boundaries (Mountains on sides)
        // We use absolute world position math relative to initial origin
        double absX = (double)coord.x * chunkSize;
        // If floating origin is used, we need to be careful. 
        // Simplest is to assume mapWidth is relative to "Game Center" (Absolute 0)
        
        // Let's use the FloatingOrigin accumulator to find "True 0"
        if (FloatingOrigin.Instance != null)
             absX += FloatingOrigin.Instance.accumulatedX;

        // Force Mountain if too far East/West
        if (Mathf.Abs((float)absX) > mapWidth)
        {
            biomeType = BiomeType.Mountain;
        }
        else
        {
            // Standard Biome Logic
            if (biomeNoise > 0.65f) // Was 0.75f
                biomeType = BiomeType.Desert; 
            else if (biomeNoise > 0.35f) // Was 0.55f (Much bigger range for Craters)
                biomeType = BiomeType.Crater;
            else if (biomeNoise > 0.25f) // Smaller Oasis chance
                biomeType = BiomeType.Oasis;
            else
                biomeType = BiomeType.Desert;
        }

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
        // -----------------------
        if (biomeType != BiomeType.Oasis && biomeType != BiomeType.Mountain)
        {
            // GenerateRocks(); // DISABLED FOR DEBUGGING
            GenerateBuildings();
        }
    }

    [Header("Building Settings")]
    public GameObject[] buildingPrefabs;
    [Range(0, 5)] public int buildingCount = 0; // Low density default
    public float buildingSinkAmount = 1.0f; // Height to sink into sand
    public float minBuildingScale = 0.8f;
    public float maxBuildingScale = 1.2f;
    [Range(0.01f, 1f)] public float globalParticleScale = 0.2f; // Default much lower, allow very small

    void GenerateBuildings()
    {
        if (buildingPrefabs == null || buildingPrefabs.Length == 0) return;
        if (buildingCount <= 0) return;

        // Simple safety loop count
        int attempts = 0;
        int placed = 0;
        List<Vector3> placedPositions = new List<Vector3>(); // 🛡️ Keep track of positions
        float minSpacing = 15f; // Minimum distance between buildings

        while (placed < buildingCount && attempts < buildingCount * 10) // More attempts allowed
        {
            attempts++;
            float x = Random.Range(5f, chunkSize - 5f); // Padding from edge
            float z = Random.Range(5f, chunkSize - 5f);
            
            // Overlap Check 🛡️
            bool tooClose = false;
            foreach (Vector3 p in placedPositions)
            {
                if (Vector3.Distance(new Vector3(x, 0, z), new Vector3(p.x, 0, p.z)) < minSpacing)
                {
                    tooClose = true;
                    break;
                }
            }
            if (tooClose) continue; // Try another spot

            // Check Biome - strictly avoid water/craters
            // (Re-using biome logic check implicitly via biomeType check above, 
            // but we could double check height if needed)

            float y = GetSmoothHeight(x, z);

            // Spawn
            GameObject prefab = buildingPrefabs[Random.Range(0, buildingPrefabs.Length)];
            if (prefab == null) continue;

            GameObject instance = Instantiate(prefab, transform);
            instance.name = "Building_" + placed;
            
            // Position: Apply sink amount
            instance.transform.localPosition = new Vector3(x, y - buildingSinkAmount, z);
            
            // Add to list 🛡️
            placedPositions.Add(instance.transform.localPosition);

            // Rotation: Random Y
            instance.transform.localRotation = Quaternion.Euler(0, Random.Range(0, 360f), 0);

            // Scale variation
            float s = Random.Range(minBuildingScale, maxBuildingScale);
            instance.transform.localScale = Vector3.one * s;

            // ⚠️ FIX v5: Comprehensive Scaling (Size, Lifetime, Speed, Shape)
            foreach (var ps in instance.GetComponentsInChildren<ParticleSystem>())
            {
                var main = ps.main;
                var shape = ps.shape;
                
                float factor = s * globalParticleScale;

                // 1. Taille des sprites
                main.startSizeMultiplier *= factor;
                // 2. Hauteur (Durée de vie)
                main.startLifetimeMultiplier *= factor;
                // 3. Vitesse d'ascension (Si elles montent vite, la flamme est haute)
                main.startSpeedMultiplier *= factor;
                
                // 4. Volume d'émission (Réduire la base du feu)
                // Note: Shape scale is a Vector3
                shape.scale *= factor;
            }

            // ⚠️ FIX: Auto-Add MeshColliders
            // ⚠️ FIX: Auto-Add MeshColliders
            // Parcours tous les meshs du building (murs, toits...) et ajoute un collider si absent
            foreach (var mf in instance.GetComponentsInChildren<MeshFilter>())
            {
                // 1. Filter by Name (Aggressive)
                string n = mf.name.ToLower();
                if (n.Contains("particle") || n.Contains("effect") || n.Contains("fire") || n.Contains("smoke") || n.Contains("glow")) 
                    continue;

                // 2. Filter by Component
                if (mf.GetComponent<ParticleSystem>() != null) continue;
                if (mf.GetComponent<ParticleSystemRenderer>() != null) continue;
                if (mf.GetComponent<LineRenderer>() != null) continue;

                // 3. Mesh Validity
                Mesh m = mf.sharedMesh;
                if (m == null) continue;
                if (m.vertexCount < 3) continue;
                if (m.bounds.size.sqrMagnitude < 0.01f) continue;
                
                // 4. Force Topology Check (PhysX needs Triangles)
                // Note: GetTopology(0) is usually safe for standard meshes
                try {
                    if (m.GetTopology(0) != MeshTopology.Triangles) continue;
                } catch { continue; }

                // 5. Global Scale check (PhysX fails on zero/tiny scale)
                if (mf.transform.lossyScale.sqrMagnitude < 0.0001f) continue;

                if (mf.GetComponent<Collider>() == null)
                {
                    try 
                    {
                        // ⚠️ Strategy updated per user request:
                        // 1. If mesh is absurdly heavy (> 60k verts), DO NOTHING (Skip).
                        //    User will handle these manually on Prefabs.
                        // 2. Otherwise, TRY MeshCollider. If it fails, DO NOTHING (Skip).
                        
                        if (m.vertexCount > 60000)
                        {
                             // Too heavy, skip entirely
                             // Debug.LogWarning($"[ColliderFix] Mesh '{mf.name}' skipped (>60k verts).");
                             continue;
                        }
                        else
                        {
                            // Try the preferred MeshCollider
                            mf.gameObject.AddComponent<MeshCollider>();
                        }
                    }
                    catch (System.Exception) 
                    { 
                        // Fallback if MeshCollider failed -> DO NOTHING
                        // Debug.LogError($"[ColliderDebug] Failed to add MeshCollider to '{mf.name}'. Skipped.");
                    }
                }
            }

            // Variation Script Support: If building has variation script, randomize it
            BuildingVariation varScript = instance.GetComponent<BuildingVariation>();
            if (varScript != null)
            {
                varScript.randomSeed = Random.Range(0, 10000);
                varScript.ApplyVariation();
            }

            // ⚠️ WAR MODE: DEBRIS GENERATION
            // Generate small rubble around the building base
            GenerateDebris(instance.transform, x, y, z);

            // 🌪️ WAR MODE: SMOKE GENERATION (30% chance)
            if (Random.value < 0.3f)
            {
                GenerateSmoke(instance.transform, x, y, z);
            }

            placed++;
        }
    }

    // 🌪️ WAR MODE: GENERATE SMOKE
    void GenerateSmoke(Transform building, float bx, float by, float bz)
    {
        GameObject smokeObj = new GameObject("Smoke_Auto");
        smokeObj.transform.parent = building;
        // Position: Slightly above ground, randomized
        smokeObj.transform.localPosition = new Vector3(Random.Range(-2f, 2f), Random.Range(1f, 4f), Random.Range(-2f, 2f));
        
        // Add Particle System
        ParticleSystem ps = smokeObj.AddComponent<ParticleSystem>();
        ParticleSystemRenderer psr = smokeObj.GetComponent<ParticleSystemRenderer>();
        
        // 🎨 Material (Dark Smoke) - FIX for "Black Bubbles"
        // 1. Use Cubes for Low Poly style (instead of Spheres)
        psr.renderMode = ParticleSystemRenderMode.Mesh;
        GameObject meshRef = GameObject.CreatePrimitive(PrimitiveType.Cube);
        psr.mesh = meshRef.GetComponent<MeshFilter>().sharedMesh;
        DestroyImmediate(meshRef); 

        // 2. Matte Material (No Shine)
        Material particleMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        particleMat.color = new Color(0.2f, 0.2f, 0.2f, 0.3f); // Lighten up, more transparent
        
        // Transparency Setup
        particleMat.SetFloat("_Surface", 1); // Transparent
        particleMat.SetFloat("_Blend", 0);   // Alpha
        particleMat.SetInt("_ZWrite", 0);
        particleMat.SetFloat("_Smoothness", 0.0f); // Matte
        particleMat.SetFloat("_SpecularHighlights", 0.0f);
        particleMat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        particleMat.EnableKeyword("_ALPHAPREMULTIPLY_ON");
        particleMat.renderQueue = 3000;
        
        psr.material = particleMat;

        // ⚙️ Main Settings (Finer & Denser)
        var main = ps.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(4f, 7f); 
        main.startSpeed = new ParticleSystem.MinMaxCurve(1.5f, 3.5f); // Rise faster
        main.startSize = new ParticleSystem.MinMaxCurve(0.2f, 0.5f); // 🧱 TINY CUBES (Voxel dust)
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.4f, 0.4f, 0.4f), new Color(0.1f, 0.1f, 0.1f)); 
        main.gravityModifier = -0.05f;
        main.maxParticles = 500; // MASSIVE AMOUNT OF CUBES
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        // 🎇 Emission (High Density)
        var emission = ps.emission;
        emission.rateOverTime = new ParticleSystem.MinMaxCurve(30f, 50f); // Flow

        // 📐 Shape
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 18f; 
        shape.radius = 0.6f; // Slightly wider base

        // 🌈 Color Over Lifetime
        var col = ps.colorOverLifetime;
        col.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] { new GradientColorKey(Color.grey, 0.0f), new GradientColorKey(Color.black, 0.7f) },
            new GradientAlphaKey[] { new GradientAlphaKey(0.4f, 0.0f), new GradientAlphaKey(0.0f, 1.0f) }
        );
        col.color = grad;

        // 📈 Size Over Lifetime (Grow)
        var sz = ps.sizeOverLifetime;
        sz.enabled = true;
        AnimationCurve curve = new AnimationCurve();
        curve.AddKey(0.0f, 0.8f); 
        curve.AddKey(1.0f, 2.0f); 
        sz.size = new ParticleSystem.MinMaxCurve(1.0f, curve);

        // 🔄 Rotation (Chaos)
        var rot = ps.rotationOverLifetime;
        rot.enabled = true;
        rot.x = new ParticleSystem.MinMaxCurve(-180f, 180f);
        rot.y = new ParticleSystem.MinMaxCurve(-180f, 180f);
        rot.z = new ParticleSystem.MinMaxCurve(-180f, 180f);
    }

    // 🧱 WAR MODE: GENERATE DEBRIS (Improved)
    void GenerateDebris(Transform building, float bx, float by, float bz)
    {
        int debrisCount = Random.Range(5, 12); // Plus de débris
        
        // Couleur de base (Sable/Pierre) pour ressembler aux murs
        Color debrisColor = new Color(0.8f, 0.6f, 0.4f); 

        for (int i = 0; i < debrisCount; i++)
        {
            // Position aléatoire autour
            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            float dist = Random.Range(2.0f, 6.0f); // Plus étendu
            float dx = Mathf.Cos(angle) * dist;
            float dz = Mathf.Sin(angle) * dist;

            float yPos = GetSmoothHeight(bx + dx, bz + dz);
            
            GameObject rubble = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rubble.name = "Rubble_Auto";
            rubble.transform.parent = building; 
            
            // Enfoncer un peu dans le sol pour le réalisme + Rotation forte
            rubble.transform.position = new Vector3(transform.position.x + bx + dx, yPos - 0.1f, transform.position.z + bz + dz);
            rubble.transform.rotation = Random.rotation; 
            
            // Variété de taille (Parfois gros blocs)
            Vector3 scale = Vector3.one * Random.Range(0.3f, 0.8f);
            if (Random.value < 0.2f) scale *= 1.5f; // 20% de chance d'un gros bloc
            rubble.transform.localScale = scale;

            DestroyImmediate(rubble.GetComponent<BoxCollider>());

            MeshRenderer mr = rubble.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                // Variation légère de la teinte sable/brique
                float darken = Random.Range(0.8f, 1.1f);
                mat.color = new Color(debrisColor.r * darken, debrisColor.g * darken, debrisColor.b * darken);
                mr.sharedMaterial = mat;
            }
        }
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
        // 1. Calcul de la position ABSOLUE "Univers" (Source de vérité)
        // On n'utilise plus 'coord' qui peut être désynchronisé.
        // On prend la position réelle Unity + le décalage accumulé par le Floating Origin.
        double absGlobalX = transform.position.x + x;
        double absGlobalZ = transform.position.z + z;
        
        if (FloatingOrigin.Instance != null)
        {
            absGlobalX += FloatingOrigin.Instance.accumulatedX;
            absGlobalZ += FloatingOrigin.Instance.accumulatedZ;
        }

        // 2. Wrapping sécurisé (PingPong)
        float safeX = (float)PingPongDouble(absGlobalX, 50000.0);
        float safeZ = (float)PingPongDouble(absGlobalZ, 50000.0);

        // 🛡️ SafeX et SafeZ sont maintenant garantis d'être entre 0 et 50000
        // et parfaitement continus (pas de coupure).

        // 1. Large Scale Undulation (The "Base" Hills)
        float baseH = Mathf.PerlinNoise(safeX / 800f, safeZ / 800f);
        
        // 2. Medium Warp (Wind directionality)
        float wx = safeX + Mathf.PerlinNoise(safeX / 300f, safeZ / 300f) * warpStrength;
        float wz = safeZ + Mathf.PerlinNoise(safeZ / 300f + 55f, safeX / 300f + 55f) * warpStrength;

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
        // Securité : Clamp 01 avant le Pow pour éviter NaN (Source des pics infinis)
        finalH = Mathf.Pow(Mathf.Clamp01(finalH), 1.2f); 

        float finalHeight = finalH * heightMultiplier;

        // Appliquer la forme du biome (cratères/oasis)
        Vector2 center = new Vector2(chunkSize / 2f, chunkSize / 2f);
        finalHeight = GetHeightForBiome(x, z, center, finalHeight);
        
        // 🛡️ SANITY CHECK: Empêcher les pics infinis (NaN)
        if (float.IsNaN(finalHeight) || float.IsInfinity(finalHeight))
        {
            finalHeight = 0f;
        }
        
        // 🛡️ HARD LIMIT: Empêcher physiquement tout pic supérieur à 2x la hauteur prévue
        finalHeight = Mathf.Clamp(finalHeight, 0, heightMultiplier * 2.5f);

        return finalHeight;
    }

    // Helper pour gérer le PingPong avec une précision mathématique infinie (double)
    double PingPongDouble(double t, double length)
    {
        t = System.Math.Abs(t); // Gérer les négatifs
        double cycle = t % (length * 2.0);
        if (cycle > length)
        {
            return (length * 2.0) - cycle;
        }
        return cycle;
    }

    // -----------------------
    // Biomes et variations
    // -----------------------
    float GetHeightForBiome(float x, float z, Vector2 center, float baseHeight)
    {
        // 🌍 1. CALCULATE GLOBAL POSITION ONCE
        double absX = transform.position.x + x;
        double absZ = transform.position.z + z;
        if (FloatingOrigin.Instance != null) { absX += FloatingOrigin.Instance.accumulatedX; absZ += FloatingOrigin.Instance.accumulatedZ; }

        float safeX = (float)PingPongDouble(absX, 50000.0);
        float safeZ = (float)PingPongDouble(absZ, 50000.0);

        // 🌍 2. CALCULATE MAP BOUNDARY BLEND (0 = Desert, 1 = Full Mountain)
        // We look at how far we are from the map center (0,0) in X
        // We use mapWidth as the "start" of the transition
        float distFromCenter = Mathf.Abs((float)absX);
        float transitionDist = 150f; // Mountains rise over 150 meters
        
        // 0 if inside map, 0..1 in transition zone, 1 outside
        float mountainBlend = Mathf.Clamp01((distFromCenter - mapWidth) / transitionDist);

        // 🏜️ 3. DESERT HEIGHT (Base)
        float finalHeight = baseHeight;

        // 🏔️ 4. MOUNTAIN OVERLAY (Only if needed)
        if (mountainBlend > 0.001f)
        {
            // RIDGE NOISE (Sharp tops, wide bases)
            // Scale should be LARGE (e.g., 300) to look like massive mountains, not spikes
            float mScale = 250f; 
            
            float p1 = Mathf.PerlinNoise(safeX / mScale, safeZ / mScale);
            float ridge = 1f - Mathf.Abs(p1 * 2f - 1f); // 0..1 Triangle wave
            ridge = Mathf.Pow(ridge, 2.5f); // Sharpen peaks
            
            float mHeight = ridge * 80f; // 80m tall mountains

            // Secondary detail noise
            float p2 = Mathf.PerlinNoise(safeX / 60f, safeZ / 60f);
            mHeight += p2 * 10f;

            // BLEND: Lerp from Desert Height to Mountain Height
            // We keep the desert base but add the mountain mass on top, smoother
            float targetHeight = Mathf.Max(baseHeight, mHeight);
            finalHeight = Mathf.Lerp(baseHeight, targetHeight, mountainBlend);
        }

        // 💧 5. LOCAL BIOMES (Oasis / Crater) - Only modify if NOT fully mountain
        // (Prevents craters from digging holes in mountain peaks)
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
            // Apply local biome, fading out as we enter mountains
            finalHeight += localBiomeEffect * (1f - mountainBlend);
        }

        return Mathf.Max(0f, finalHeight);
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
            
            // ⚠️ FIX: Use True World Position
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
            
            // 🛡️ Rock Safety
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