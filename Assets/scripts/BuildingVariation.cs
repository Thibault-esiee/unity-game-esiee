using UnityEngine;

[ExecuteInEditMode]
public class BuildingVariation : MonoBehaviour
{
    [Header("Réglages")]
    [Tooltip("Intensité de la variation de couleur aléatoire entre les éléments")]
    [Range(0f, 0.5f)]
    public float variationIntensity = 0.1f;

    [Tooltip("Graine aléatoire pour changer les couleurs si elles ne vous plaisent pas")]
    public int randomSeed = 0;

    private MaterialPropertyBlock _propBlock;

    void OnValidate()
    {
        ApplyVariation();
    }

    void Start()
    {
        ApplyVariation();
    }

    [ContextMenu("Appliquer Variation")]
    public void ApplyVariation()
    {
        if (_propBlock == null)
            _propBlock = new MaterialPropertyBlock();

        MeshRenderer[] renderers = GetComponentsInChildren<MeshRenderer>();
        if (renderers.Length == 0) return;

        Random.InitState(randomSeed);

        foreach (var r in renderers)
        {
            Material mat = r.sharedMaterial;
            if (mat == null) continue;

            if (!mat.HasProperty("_BaseColor")) continue;
            
            Color baseColor = mat.GetColor("_BaseColor");

            float hueShift = Random.Range(-0.05f, 0.05f) * variationIntensity;
            float valShift = Random.Range(-0.2f, 0.2f) * variationIntensity;

            float H, S, V;
            Color.RGBToHSV(baseColor, out H, out S, out V);

            H = (H + hueShift) % 1.0f;
            V = Mathf.Clamp01(V + valShift);

            Color finalColor = Color.HSVToRGB(H, S, V);

            r.GetPropertyBlock(_propBlock);
            _propBlock.SetColor("_BaseColor", finalColor);
            r.SetPropertyBlock(_propBlock);
        }
    }
}
