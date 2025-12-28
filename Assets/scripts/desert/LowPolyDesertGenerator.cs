using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class LowPolyDesertGenerator : MonoBehaviour
{
    [Header("Terrain Settings")]
    public int size = 100;          // Nombre de vertices sur un axe
    public float scale = 10f;       // Fréquence du bruit
    public float height = 3f;       // Amplitude des dunes

    [Header("Material Settings")]
    public Color sandColor = new Color(0.91f, 0.79f, 0.49f); // couleur sable

    private Mesh mesh;

    void Start()
    {
        Generate();
    }

    void Generate()
    {
        mesh = new Mesh();
        mesh.name = "Procedural Desert";
        GetComponent<MeshFilter>().mesh = mesh;

        Vector3[] vertices = new Vector3[(size + 1) * (size + 1)];
        int[] triangles = new int[size * size * 6];

        // --- Génération des vertices ---
        for (int z = 0, i = 0; z <= size; z++)
        {
            for (int x = 0; x <= size; x++, i++)
            {
                float y = Mathf.PerlinNoise(x / scale, z / scale) * height;
                vertices[i] = new Vector3(x, y, z);
            }
        }

        // --- Génération des triangles ---
        for (int z = 0, vert = 0, tris = 0; z < size; z++, vert++)
        {
            for (int x = 0; x < size; x++, vert++, tris += 6)
            {
                triangles[tris + 0] = vert;
                triangles[tris + 1] = vert + size + 1;
                triangles[tris + 2] = vert + 1;
                triangles[tris + 3] = vert + 1;
                triangles[tris + 4] = vert + size + 1;
                triangles[tris + 5] = vert + size + 2;
            }
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;

        // --- Flat shading optionnel ---
        //Vector3[] flatVerts = new Vector3[mesh.triangles.Length];
        //int[] flatTris = new int[mesh.triangles.Length];

        //for (int i = 0; i < mesh.triangles.Length; i++)
        //{
        //    flatVerts[i] = mesh.vertices[mesh.triangles[i]];
        //    flatTris[i] = i;
        //}

        //mesh.vertices = flatVerts;
        //mesh.triangles = flatTris;
        //mesh.RecalculateNormals();

        // --- Matériau ---
        var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.color = sandColor;
        GetComponent<MeshRenderer>().material = mat;

        // --- Collision (le plus important ici !) ---
        MeshCollider collider = GetComponent<MeshCollider>();
        if (collider == null)
            collider = gameObject.AddComponent<MeshCollider>();
        collider.sharedMesh = mesh;

        // --- Centrage ---
        transform.position = new Vector3(-size / 2f, 0, -size / 2f);
    }
}