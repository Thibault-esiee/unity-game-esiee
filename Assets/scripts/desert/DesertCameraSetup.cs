using System.Collections;
using UnityEngine;
using Cinemachine;

public class DesertCameraSetup : MonoBehaviour
{
    [Header("Settings")]
    public Transform player;
    public Transform giantGate;
    public float distanceBehind = 15f;
    public float height = 8f;
    
    [Header("Zoom Sequence")]
    public float zoomDistance = 5f;
    public float zoomHeight = 4f;
    public float zoomDuration = 2f;
    public float unzoomDuration = 2f;

    [Header("Damping")]
    public float xDamping = 1f;
    public float yDamping = 1f;
    public float zDamping = 1f;

    [Header("Collision")]
    public LayerMask collisionLayers = 1;
    public float minDistanceFromTarget = 2.0f;

    private CinemachineVirtualCamera vcam;
    private float originalDistance;
    private float originalHeight;
    private Vector3 cachedGatePos;

    void Start()
    {
        InitializeCamera();
        StartCoroutine(OpeningZoomSequence());
    }

    Vector3 GetVisualCenter(Transform t)
    {
        if (t == null) return Vector3.zero;
        var renderers = t.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return t.position;

        Bounds b = renderers[0].bounds;
        foreach (var r in renderers) b.Encapsulate(r.bounds);
        return b.center;
    }

    void InitializeCamera()
    {
        var allVcams = FindObjectsByType<CinemachineVirtualCamera>(FindObjectsSortMode.None);
        foreach (var c in allVcams)
        {
            if (c.gameObject != gameObject && c.transform.parent != transform)
            {
                c.gameObject.SetActive(false);
            }
        }

        if (Camera.main != null)
        {
            var brain = Camera.main.gameObject.GetComponent<CinemachineBrain>();
            if (brain == null)
            {
                brain = Camera.main.gameObject.AddComponent<CinemachineBrain>();
            }
            brain.m_DefaultBlend.m_Style = CinemachineBlendDefinition.Style.Cut;
        }
        else
        {
            Debug.LogError("DesertCameraSetup: Aucune 'MainCamera' trouvée dans la scène !");
        }

        if (player == null) player = GameObject.FindWithTag("Player")?.transform;
        if (giantGate == null) giantGate = FindFirstObjectByType<GiantGate>()?.transform;

        if (player == null || giantGate == null)
        {
             Debug.LogError($"MANQUE DE REFERENCES: Player={player}, Gate={giantGate}");
             return;
        }

        cachedGatePos = GetVisualCenter(giantGate);

        var existingVcam = GetComponentInChildren<CinemachineVirtualCamera>();
        if (existingVcam != null) vcam = existingVcam;
        else
        {
            var vcamObj = new GameObject("Desert Cinematic Camera");
            vcamObj.transform.parent = transform;
            vcam = vcamObj.AddComponent<CinemachineVirtualCamera>();
        }
        
        vcam.m_Priority = 1000;
        
        var gateTargetObj = GameObject.Find("GATE_LOOK_TARGET");
        if (gateTargetObj == null) gateTargetObj = new GameObject("GATE_LOOK_TARGET");
        gateTargetObj.transform.position = cachedGatePos;
        vcam.LookAt = gateTargetObj.transform;

        vcam.Follow = player;

        var transposer = vcam.GetCinemachineComponent<CinemachineTransposer>();
        if (transposer == null) transposer = vcam.AddCinemachineComponent<CinemachineTransposer>();
        transposer.m_BindingMode = CinemachineTransposer.BindingMode.WorldSpace;
        
        var composer = vcam.GetCinemachineComponent<CinemachineComposer>();
        if (composer == null) composer = vcam.AddCinemachineComponent<CinemachineComposer>();
        composer.m_TrackedObjectOffset = new Vector3(0, 5f, 0); 

        var oldCollider = vcam.GetComponent<CinemachineCollider>();
        if (oldCollider != null) DestroyImmediate(oldCollider);
        var lift = vcam.GetComponent<CinemachineGroundLift>();
        if (lift == null) lift = vcam.gameObject.AddComponent<CinemachineGroundLift>();
        
        lift.m_GroundLayer = collisionLayers;
        lift.m_MinHeightFromGround = 2.0f;

        originalDistance = distanceBehind;
        originalHeight = height;

        UpdateCameraPosition(cachedGatePos);
    }
    
    public void UpdateCameraPosition(Vector3 gatePos)
    {
         if (vcam == null || player == null) return;
         
         var transposer = vcam.GetCinemachineComponent<CinemachineTransposer>();
         Vector3 dirToGate = (gatePos - player.position).normalized;
         
         Vector3 idealPos = player.position - (dirToGate * distanceBehind) + Vector3.up * height;
         transposer.m_FollowOffset = idealPos - player.position;
         transposer.m_XDamping = xDamping;
         transposer.m_YDamping = yDamping;
         transposer.m_ZDamping = zDamping;
    }

    IEnumerator OpeningZoomSequence()
    {
        distanceBehind = zoomDistance;
        height = zoomHeight;
        UpdateCameraPosition(cachedGatePos);
        yield return new WaitForSeconds(zoomDuration);
        float elapsed = 0f;
        while (elapsed < unzoomDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / unzoomDuration;
            
            float smoothT = Mathf.SmoothStep(0, 1, t);

            distanceBehind = Mathf.Lerp(zoomDistance, originalDistance, smoothT);
            height = Mathf.Lerp(zoomHeight, originalHeight, smoothT);
            
            UpdateCameraPosition(cachedGatePos);
            yield return null;
        }

        distanceBehind = originalDistance;
        height = originalHeight;
        UpdateCameraPosition(cachedGatePos);
    }
}
