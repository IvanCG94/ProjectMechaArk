using UnityEngine;

public class RigLegDriver : MonoBehaviour
{
    [Header("Referencias")]
    public Transform rigTarget; 
    public Transform raycastOrigin; 

    [Header("Ajustes de Raycast")]
    public LayerMask groundLayer;
    public float raycastOffset = 0.5f; 
    public float totalLegLength = 3.0f; 

    // Aquí guardaremos la "diferencia" exacta entre el cuerpo y el pie
    // tal como viene en el prefab original.
    private Quaternion initialRotationOffset;
    private bool initialized = false;

    void Start()
    {
        if (rigTarget != null)
        {
            // 1. CAPTURA DEL ESTADO "PLANO" (ORIGINAL DEL PREFAB)
            // Calculamos qué rotación tiene el pie relativa al cuerpo del robot.
            // Quaternion.Inverse(A) * B  <-- Esto nos da la diferencia entre A y B.
            initialRotationOffset = Quaternion.Inverse(transform.root.rotation) * rigTarget.rotation;
            initialized = true;
        }
    }

    void LateUpdate()
    {
        if (!initialized || rigTarget == null || raycastOrigin == null) return;

        UpdateFootPosition();
    }

    void UpdateFootPosition()
    {
        RaycastHit hit;
        Vector3 rayStart = raycastOrigin.position + (Vector3.up * raycastOffset);
        float rayDist = totalLegLength + raycastOffset;

        // Debug Visual
        Debug.DrawLine(rayStart, rayStart + (Vector3.down * rayDist), Color.yellow);

        if (Physics.Raycast(rayStart, Vector3.down, out hit, rayDist, groundLayer))
        {
            // --- POSICIÓN ---
            rigTarget.position = hit.point;

            // --- ROTACIÓN ADAPTATIVA ---
            
            // A. Calculamos la orientación del terreno
            //    Tomamos el frente del robot y lo "aplanamos" sobre la normal del suelo
            Vector3 robotForward = transform.root.forward;
            Vector3 projectedForward = Vector3.ProjectOnPlane(robotForward, hit.normal).normalized;
            
            //    Creamos una rotación que mira al frente, pero con la "cabeza" (Up) alineada a la normal
            Quaternion groundOrientation = Quaternion.LookRotation(projectedForward, hit.normal);

            // B. Aplicamos el Offset Original
            //    (Orientación del Terreno) * (Offset Original del Prefab)
            rigTarget.rotation = groundOrientation * initialRotationOffset;

            Debug.DrawLine(rayStart, hit.point, Color.green);
        }
        else
        {
            // --- AIRE ---
            rigTarget.position = rayStart + (Vector3.down * totalLegLength);
            
            // En el aire, volvemos a la rotación relativa al cuerpo original
            rigTarget.rotation = transform.root.rotation * initialRotationOffset;
        }
    }
}