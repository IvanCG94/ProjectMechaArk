using UnityEngine;

public class SimpleLegIK : MonoBehaviour
{
    [Header("Referencias")]
    public Transform boneThigh; 
    public Transform boneShin;  
    public Transform boneFoot;  

    // TUS VALORES (Se mantienen)
    [Header("Configuración PIERNA DERECHA")]
    public Vector3 rightThighOffset = new Vector3(0, 90, 0);
    public Vector3 rightShinOffset = new Vector3(0, 90, 0);
    public Vector3 rightFootOffset = new Vector3(0, 0, 0);
    public float rightKneeBend = 1f;

    [Header("Configuración PIERNA IZQUIERDA")]
    public Vector3 leftThighOffset = new Vector3(0, -90, 0);
    public Vector3 leftShinOffset = new Vector3(0, -90, 0);
    public Vector3 leftFootOffset = new Vector3(0, 0, 0);
    public float leftKneeBend = -1f;

    [Header("Detección de Suelo (DEBUG MODE)")]
    public bool enableGrounding = true; 
    public LayerMask groundLayer;       
    public float raycastOffsetHeight = 0.5f; // Bajé esto un poco para probar
    
    [Header("Estado")]
    public Transform currentTarget; 
    public Transform currentPole;   

    private float lengthThigh;
    private float lengthShin;
    private float totalLegLength;

    void Start()
    {
        if (boneThigh == null || boneShin == null || boneFoot == null) { this.enabled = false; return; }

        lengthThigh = Vector3.Distance(boneThigh.position, boneShin.position);
        lengthShin = Vector3.Distance(boneShin.position, boneFoot.position);
        totalLegLength = lengthThigh + lengthShin;

        if (currentTarget == null)
        {
            GameObject targetObj = new GameObject($"{gameObject.name}_IK_Target");
            targetObj.transform.position = boneFoot.position;
            targetObj.transform.rotation = boneFoot.rotation;
            currentTarget = targetObj.transform;
        }

        if (currentPole == null)
        {
            GameObject poleObj = new GameObject($"{gameObject.name}_IK_Pole");
            Vector3 forwardRef = (transform.root != null) ? transform.root.forward : transform.forward; 
            poleObj.transform.position = boneShin.position + (forwardRef * 1.0f);
            currentPole = poleObj.transform;
            if (transform.root != null) currentPole.SetParent(transform.root);
        }
    }

    void LateUpdate()
    {
        if (currentTarget == null) return;

        if (enableGrounding)
        {
            DetectGround();
        }

        ResolveIK();
    }

    void DetectGround()
    {
        // Origen del rayo: Un poco arriba del muslo
        Vector3 rayOrigin = boneThigh.position + (Vector3.up * raycastOffsetHeight);
        RaycastHit hit;
        
        // El rayo debe ser largo: La pierna + un margen extra (2.0 metros extra)
        float rayDistance = totalLegLength + 2.0f;

        // DIBUJO DEL RAYO (VISUAL)
        // Esto dibujará una línea en la ventana SCENE mientras juegas
        Debug.DrawLine(rayOrigin, rayOrigin + (Vector3.down * rayDistance), Color.yellow);

        if (Physics.Raycast(rayOrigin, Vector3.down, out hit, rayDistance, groundLayer))
        {
            // --- CASO: IMPACTO (SUELO DETECTADO) ---
            currentTarget.position = hit.point;
            
            // Rotación del pie según el suelo
            Vector3 robotForward = transform.root != null ? transform.root.forward : transform.forward;
            Vector3 projectedForward = Vector3.ProjectOnPlane(robotForward, hit.normal).normalized;
            currentTarget.rotation = Quaternion.LookRotation(projectedForward, hit.normal);

            // DEBUG VISUAL: Línea VERDE si toca suelo
            Debug.DrawLine(rayOrigin, hit.point, Color.green);
            
            // LOG DE CONSOLA (Solo si toca algo que NO sea Ground para avisarte)
            // Si la capa no se llama "Ground", te avisará.
            string layerName = LayerMask.LayerToName(hit.collider.gameObject.layer);
            if (layerName != "Ground")
            {
                Debug.LogWarning($"⚠️ ALERTA: El raycast de {gameObject.name} chocó con '{hit.collider.gameObject.name}' en la capa '{layerName}'. ¡Debería ser Ground!");
            }
        }
        else
        {
            // --- CASO: AIRE (NO TOCÓ NADA) ---
            // Estiramos la pierna totalmente hacia abajo
            Vector3 footHangingPos = rayOrigin + (Vector3.down * totalLegLength);
            currentTarget.position = footHangingPos;
            
            // Rotación neutral
            currentTarget.rotation = (transform.root != null) ? transform.root.rotation : transform.rotation;

            // DEBUG VISUAL: Línea ROJA si falla
            Debug.DrawLine(rayOrigin, rayOrigin + (Vector3.down * rayDistance), Color.red);
        }
    }

    void ResolveIK()
    {
        bool isMirrored = transform.lossyScale.x < 0;
        if (transform.parent != null) isMirrored = isMirrored || transform.parent.lossyScale.x < 0;

        Vector3 useThighOffset = isMirrored ? leftThighOffset : rightThighOffset;
        Vector3 useShinOffset  = isMirrored ? leftShinOffset : rightShinOffset;
        Vector3 useFootOffset  = isMirrored ? leftFootOffset : rightFootOffset;
        float useKneeBend      = isMirrored ? leftKneeBend : rightKneeBend;

        Vector3 rootPos = boneThigh.position;
        Vector3 targetPos = currentTarget.position;
        Vector3 polePos = (currentPole != null) ? currentPole.position : (rootPos + transform.forward);

        Vector3 thighToTarget = targetPos - rootPos;
        float distToTarget = thighToTarget.magnitude;
        float totalLen = lengthThigh + lengthShin;

        if (distToTarget >= totalLen) 
        {
            targetPos = rootPos + (thighToTarget.normalized * (totalLen - 0.001f));
            thighToTarget = targetPos - rootPos;
            distToTarget = thighToTarget.magnitude;
        }

        float cosAngleThigh = ((distToTarget * distToTarget) + (lengthThigh * lengthThigh) - (lengthShin * lengthShin)) / (2 * distToTarget * lengthThigh);
        float angleThigh = Mathf.Acos(Mathf.Clamp(cosAngleThigh, -1f, 1f));

        Vector3 planeNormal = Vector3.Cross(thighToTarget, polePos - rootPos).normalized;
        if (isMirrored) planeNormal = -planeNormal;
        planeNormal *= useKneeBend;

        Vector3 thighDir = Quaternion.AngleAxis(angleThigh * Mathf.Rad2Deg, planeNormal) * thighToTarget.normalized;
        Vector3 kneePos = rootPos + (thighDir * lengthThigh);

        ApplyRot(boneThigh, kneePos, planeNormal, useThighOffset);
        ApplyRot(boneShin, targetPos, planeNormal, useShinOffset);
        
        boneFoot.rotation = currentTarget.rotation;
        boneFoot.Rotate(useFootOffset);
    }

    void ApplyRot(Transform bone, Vector3 lookPos, Vector3 upHint, Vector3 offset)
    {
        Vector3 dir = (lookPos - bone.position).normalized;
        if (dir == Vector3.zero) return;
        Quaternion baseRot = Quaternion.LookRotation(dir, upHint);
        bone.rotation = baseRot;
        bone.Rotate(90f, 0, 0);
        bone.Rotate(offset);
    }
}