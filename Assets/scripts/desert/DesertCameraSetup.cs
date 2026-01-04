using UnityEngine;
using Cinemachine;

public class DesertCameraSetup : MonoBehaviour
{
    [Header("Settings")]
    public Transform player;
    public Transform giantGate; // La Grande Porte
    public float distanceBehind = 15f;
    public float height = 8f;
    
    [Header("Damping")]
    public float xDamping = 1f;
    public float yDamping = 1f;
    public float zDamping = 1f;

    [Header("Collision")]
    public LayerMask collisionLayers = 1; // Default layer only by default
    public float minDistanceFromTarget = 2.0f;

    private CinemachineVirtualCamera vcam;

    void Start()
    {
        InitializeCamera();
    }

    // Helper pour trouver le VRAI centre visuel d'un objet (pas juste son pivot)
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
        // 0. Désactiver les autres caméras virtuelles gênantes
        var allVcams = FindObjectsByType<CinemachineVirtualCamera>(FindObjectsSortMode.None);
        foreach (var c in allVcams)
        {
            if (c.gameObject != gameObject && c.transform.parent != transform)
            {
                c.gameObject.SetActive(false);
            }
        }

        // 0. S'assurer qu'il y a une Main Camera avec un CinemachineBrain
        if (Camera.main != null)
        {
            var brain = Camera.main.gameObject.GetComponent<CinemachineBrain>();
            if (brain == null)
            {
                brain = Camera.main.gameObject.AddComponent<CinemachineBrain>();
            }
            // FORCE INSTANT BLEND
            brain.m_DefaultBlend.m_Style = CinemachineBlendDefinition.Style.Cut;
        }
        else
        {
            Debug.LogError("DesertCameraSetup: Aucune 'MainCamera' trouvée dans la scène !");
        }

        // 1. Trouver les références
        if (player == null) player = GameObject.FindWithTag("Player")?.transform;
        if (giantGate == null) giantGate = FindFirstObjectByType<GiantGate>()?.transform;

        if (player == null || giantGate == null)
        {
             Debug.LogError($"MANQUE DE REFERENCES: Player={player}, Gate={giantGate}");
             return;
        }

        // CALCUL DU VRAI CENTRE
        Vector3 gatePos = GetVisualCenter(giantGate);

        // 2. Créer ou récupérer la Virtual Camera
        var existingVcam = GetComponentInChildren<CinemachineVirtualCamera>();
        if (existingVcam != null) vcam = existingVcam;
        else
        {
            var vcamObj = new GameObject("Desert Cinematic Camera");
            vcamObj.transform.parent = transform;
            vcam = vcamObj.AddComponent<CinemachineVirtualCamera>();
        }
        
        vcam.m_Priority = 1000;
        
        // Pour le LookAt, on ne peut pas passer un Vector3. 
        // On va créer un petit objet temporaire invisible au centre de la porte
        var gateTargetObj = GameObject.Find("GATE_LOOK_TARGET");
        if (gateTargetObj == null) gateTargetObj = new GameObject("GATE_LOOK_TARGET");
        gateTargetObj.transform.position = gatePos;
        vcam.LookAt = gateTargetObj.transform;

        vcam.Follow = player;

        // 5. Setup Body & Aim
        var transposer = vcam.GetCinemachineComponent<CinemachineTransposer>();
        if (transposer == null) transposer = vcam.AddCinemachineComponent<CinemachineTransposer>();
        transposer.m_BindingMode = CinemachineTransposer.BindingMode.WorldSpace;
        
        var composer = vcam.GetCinemachineComponent<CinemachineComposer>();
        if (composer == null) composer = vcam.AddCinemachineComponent<CinemachineComposer>();
        composer.m_TrackedObjectOffset = new Vector3(0, 5f, 0); 

        // 6. ANTI-CLIPPING: REMOVE Collider, ADD Custom Lift
        // On supprime l'ancien collider s'il existe car il fait sauter la camera
        var oldCollider = vcam.GetComponent<CinemachineCollider>();
        if (oldCollider != null) DestroyImmediate(oldCollider);

        // On ajoute notre script de levée
        var lift = vcam.GetComponent<CinemachineGroundLift>();
        if (lift == null) lift = vcam.gameObject.AddComponent<CinemachineGroundLift>();
        
        lift.m_GroundLayer = collisionLayers; // On passe le layer configuré ici
        lift.m_MinHeightFromGround = 2.0f;

        UpdateCameraPosition(gatePos);
    }
    
    // Surcharge pour utiliser la position calculée
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
}
