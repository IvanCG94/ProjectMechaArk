using UnityEngine;

public class Socket : MonoBehaviour
{
    public PartType acceptedType;
    public string socketName;
    
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, 0.1f);
    }
}