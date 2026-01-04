using UnityEngine;

[ExecuteAlways]
public class GiantGate : MonoBehaviour
{
    [Header("Gate Settings")]
    public float width = 100f;
    public float height = 200f;
    public float thickness = 20f;
    public float archHeight = 50f;
    public Color gateColor = new Color(0.5f, 0.45f, 0.4f);
    
    [Header("Visuals")]
    public Material gateMaterial; // Assign the glowing material here

    public void GenerateGate()
    {
        // Nettoyer
        foreach (Transform child in transform)
            if (Application.isPlaying)
                Destroy(child.gameObject);
            else
                DestroyImmediate(child.gameObject);

        // Montants gauche et droit
        CreateBlock(new Vector3(-width / 2f, height / 2f, 0), new Vector3(thickness, height, thickness));
        CreateBlock(new Vector3(width / 2f, height / 2f, 0), new Vector3(thickness, height, thickness));

        // Linteau supérieur
        CreateBlock(new Vector3(0, height - (archHeight / 2f), 0), new Vector3(width, archHeight, thickness * 0.8f));

        // Base solide
        CreateBlock(new Vector3(0, thickness / 2f, 0), new Vector3(width + 10f, thickness, thickness * 1.2f));
    }

    private void CreateBlock(Vector3 localPos, Vector3 scale)
    {
        GameObject block = GameObject.CreatePrimitive(PrimitiveType.Cube);
        block.transform.parent = transform;
        block.transform.localPosition = localPos;
        block.transform.localScale = scale;

        MeshRenderer mr = block.GetComponent<MeshRenderer>();
        
        if (gateMaterial != null)
        {
            mr.sharedMaterial = gateMaterial;
        }
        else
        {
            mr.sharedMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mr.sharedMaterial.color = gateColor;
        }
    }
}