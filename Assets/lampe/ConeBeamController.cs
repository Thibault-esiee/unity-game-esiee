using UnityEngine;

public class ConeBeamController : MonoBehaviour
{
    public Transform lightSource;  // Position de la lampe
    public float maxDistance = 10f;  // Longueur maximale du faisceau
    public float coneAngle = 45f;   // Angle du cône
    public LayerMask collisionLayer;  // Couches d'objets que le Raycast peut toucher

    private void Update()
    {
        AdjustConeLength();
    }

    void AdjustConeLength()
    {
        // Calculer la direction du faisceau lumineux
        Vector3 direction = lightSource.forward;
        RaycastHit hit;

        // Effectuer un Raycast depuis la position de la lampe
        if (Physics.Raycast(lightSource.position, direction, out hit, maxDistance, collisionLayer))
        {
            // Si le Raycast touche un objet, ajuster la longueur du cône à la distance du hit
            float distance = hit.distance;
            AdjustConeSize(distance);
        }
        else
        {
            // Sinon, utiliser la distance maximale
            AdjustConeSize(maxDistance);
        }
    }

    void AdjustConeSize(float distance)
    {
        // Modifier la taille du cône selon la distance du Raycast
        // Tu peux ajuster l'échelle du cône ou la taille de la texture/dissipation en fonction de la distance.
        transform.localScale = new Vector3(1f, 1f, distance);
    }

    private void OnDrawGizmos()
    {
        // Visualisation du Raycast dans l'éditeur
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(lightSource.position, lightSource.position + lightSource.forward * maxDistance);
    }
}