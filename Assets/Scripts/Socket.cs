using UnityEngine;

public class Socket : MonoBehaviour // ¡HEREDA DE MONOBEHAVIOUR!
{
    [Header("Restricciones")]
    public PartType acceptedType = PartType.Arm; 
    public Tier acceptedTier = Tier.T1;         

    [Header("Debug")]
    public string socketName = "Brazo_R"; 
    public Color debugColor = Color.yellow;

    void OnDrawGizmos()
    {
        // ... (El código de visualización del Gizmo)
        Gizmos.color = debugColor;
        Gizmos.DrawWireSphere(transform.position, 0.2f);
        Gizmos.DrawLine(transform.position, transform.position + transform.forward * 0.5f); 
    }
}