using UnityEngine;
using Cinemachine;

[ExecuteInEditMode]
[SaveDuringPlay]
public class CinemachineGroundLift : CinemachineExtension
{
    [Tooltip("Layers du sol à détecter (ex: Terrain)")]
    public LayerMask m_GroundLayer = 1; // Default
    
    [Tooltip("Hauteur minimale à maintenir au-dessus du sol")]
    public float m_MinHeightFromGround = 0.5f;

    [Tooltip("Rayon de la sphère de détection (Lisse les aspérités du Low Poly)")]
    public float m_DetectionRadius = 0.5f;

    protected override void PostPipelineStageCallback(
        CinemachineVirtualCameraBase vcam,
        CinemachineCore.Stage stage, ref CameraState state, float deltaTime)
    {
        if (stage == CinemachineCore.Stage.Body)
        {
            Vector3 cameraPos = state.RawPosition;
            
            // SphereCast depuis le ciel pour trouver le "Toit" du terrain sous la camera
            Vector3 rayStart = cameraPos;
            rayStart.y += 200f; 

            // SphereCast lisse mieux le terrain LowPoly qu'un simple Raycast
            if (Physics.SphereCast(rayStart, m_DetectionRadius, Vector3.down, out RaycastHit hit, 500f, m_GroundLayer))
            {
                // La hit.point d'un SphereCast est le point de contact sur la surface, 
                // mais attention le centre de la sphere est plus haut (hit.point + radius * normal).
                // Pour une hauteur sol simple, on peut utiliser hit.point.y direct si c'est assez plat,
                // ou ajuster selon le rayon.
                
                // On veut que la camera soit au moins à (Sol + MinHeight)
                float groundHeight = hit.point.y;
                float targetY = groundHeight + m_MinHeightFromGround;

                // Si la caméra est en dessous de ce plancher
                if (cameraPos.y < targetY)
                {
                    // Correction Hard : On remonte la caméra
                    cameraPos.y = targetY;
                    state.RawPosition = cameraPos;
                }
            }
        }
    }
}
