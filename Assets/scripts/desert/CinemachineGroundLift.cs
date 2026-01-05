using UnityEngine;
using Cinemachine;

[ExecuteInEditMode]
[SaveDuringPlay]
public class CinemachineGroundLift : CinemachineExtension
{
    [Tooltip("Layers du sol à détecter (ex: Terrain)")]
    public LayerMask m_GroundLayer = 1;
    
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
            
            Vector3 rayStart = cameraPos;
            rayStart.y += 200f; 

            if (Physics.SphereCast(rayStart, m_DetectionRadius, Vector3.down, out RaycastHit hit, 500f, m_GroundLayer))
            {
                
                
                
                float groundHeight = hit.point.y;
                float targetY = groundHeight + m_MinHeightFromGround;

                if (cameraPos.y < targetY)
                {
                    cameraPos.y = targetY;
                    state.RawPosition = cameraPos;
                }
            }
        }
    }
}
