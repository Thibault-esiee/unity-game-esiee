using UnityEngine;

[ExecuteAlways]
public class PointOfInterestController : MonoBehaviour
{
    public Transform player;
    public Transform gate;
    public float distance = 2000f;
    public float height = 0f;
    public Vector3 direction = new Vector3(0, 0, 1); // direction "nord" du désert

    void LateUpdate()
    {
        if (player == null || gate == null) return;

        Vector3 targetPos = player.position + direction.normalized * distance;
        targetPos.y = height;
        gate.position = targetPos;

        // Toujours orientée vers le joueur
        gate.LookAt(player.position);
    }
}