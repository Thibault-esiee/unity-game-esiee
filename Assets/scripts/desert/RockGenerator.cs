using UnityEngine;

public class RockGenerator : MonoBehaviour
{
    [Header("Paramètres du rocher")]
    public int cubeCount = 10;            // Nombre de cubes par rocher
    public Vector3 baseScale = new Vector3(1f, 1f, 1f);
    public float randomScaleFactor = 0.5f;
    public float randomOffset = 0.5f;
    public float randomRotation = 25f;

    [Header("Placement sur le terrain")]
    public int rockCount = 50;            // Nombre total de rochers
    public Vector2 terrainSize = new Vector2(100f, 100f); // Taille du terrain X,Z

    [Header("Apparence")]
    public Material rockMaterial;

    [Header("Performance")]
    public bool combineMeshes = true;     // Fusionner les cubes pour réduire le nombre de draw calls

    void Start()
    {
        GenerateRocks();
    }

    void GenerateRocks()
    {
        for (int i = 0; i < rockCount; i++)
        {
            Vector3 pos = new Vector3(
                Random.Range(-terrainSize.x / 2f, terrainSize.x / 2f),
                0f,
                Random.Range(-terrainSize.y / 2f, terrainSize.y / 2f)
            );

            GameObject newRock = GenerateRock();
            newRock.transform.position = pos;
        }
    }

    GameObject GenerateRock()
    {
        GameObject rock = new GameObject("Rock");
        rock.transform.parent = transform;

        for (int i = 0; i < cubeCount; i++)
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.transform.parent = rock.transform;

            // Taille aléatoire
            Vector3 scale = baseScale + Random.insideUnitSphere * randomScaleFactor;
            cube.transform.localScale = scale;

            // Position aléatoire
            cube.transform.localPosition = Random.insideUnitSphere * randomOffset;

            // Rotation aléatoire
            cube.transform.localRotation = Random.rotation;

            // Matériau
            if (rockMaterial != null)
                cube.GetComponent<MeshRenderer>().material = rockMaterial;
        }

        if (combineMeshes)
        {
            MeshCombiner.CombineChildren(rock);
        }

        // Ajouter un collider
        rock.AddComponent<MeshCollider>();

        return rock;
    }
}

public static class MeshCombiner
{
    public static void CombineChildren(GameObject parent)
    {
        MeshFilter[] meshFilters = parent.GetComponentsInChildren<MeshFilter>();
        CombineInstance[] combine = new CombineInstance[meshFilters.Length];

        for (int i = 0; i < meshFilters.Length; i++)
        {
            combine[i].mesh = meshFilters[i].sharedMesh;
            combine[i].transform = meshFilters[i].transform.localToWorldMatrix;
            meshFilters[i].gameObject.SetActive(false);
        }

        Mesh combinedMesh = new Mesh();
        combinedMesh.CombineMeshes(combine);

        MeshFilter mf = parent.AddComponent<MeshFilter>();
        mf.mesh = combinedMesh;

        MeshRenderer mr = parent.AddComponent<MeshRenderer>();
        mr.material = meshFilters[0].GetComponent<Renderer>().sharedMaterial;

        parent.SetActive(true);
    }
}