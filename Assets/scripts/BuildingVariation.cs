using UnityEngine;

[ExecuteInEditMode] // S'execute meme dans l'editeur pour voir le resultat direct
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

        // On initialise le générateur aléatoire avec la seed
        Random.InitState(randomSeed);

        foreach (var r in renderers)
        {
            // 1. Récupérer la couleur actuelle du materiau (la couleur de base)
            // Note: On suppose que tous utilisent le meme materiau de base
            Material mat = r.sharedMaterial;
            if (mat == null) continue;

            if (!mat.HasProperty("_BaseColor")) continue;
            
            Color baseColor = mat.GetColor("_BaseColor");

            // 2. Calculer une variation
            // On fait varier légèrement la teinte (Hue) et la luminosité (Value)
            float hueShift = Random.Range(-0.05f, 0.05f) * variationIntensity;
            float valShift = Random.Range(-0.2f, 0.2f) * variationIntensity;

            float H, S, V;
            Color.RGBToHSV(baseColor, out H, out S, out V);

            H = (H + hueShift) % 1.0f;
            V = Mathf.Clamp01(V + valShift);

            Color finalColor = Color.HSVToRGB(H, S, V);

            // 3. Appliquer via Property Block (Optimisé, ne crée pas de copie de materiau)
            r.GetPropertyBlock(_propBlock);
            _propBlock.SetColor("_BaseColor", finalColor);
            r.SetPropertyBlock(_propBlock);
        }
    }
}
