using UnityEngine;

public class PivotDebug : MonoBehaviour
{
    // Tamaño de los ejes para que sean visibles
    [Range(0.05f, 0.5f)]
    public float axisSize = 0.15f;
    
    // OnDrawGizmos se llama en el Editor y permite dibujar marcadores.
    void OnDrawGizmos()
    {
        // Dibujamos un sistema de coordenadas en la posición del objeto (el pivote)
        
        // Eje X (Rojo) - Derecha
        Gizmos.color = Color.red;
        Gizmos.DrawLine(transform.position, transform.position + transform.right * axisSize);

        // Eje Y (Verde) - Arriba
        Gizmos.color = Color.green;
        Gizmos.DrawLine(transform.position, transform.position + transform.up * axisSize);
        
        // Eje Z (Azul) - Adelante
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(transform.position, transform.position + transform.forward * axisSize);

        // Opcional: Dibujar un pequeño punto en el centro
        Gizmos.color = Color.white;
        Gizmos.DrawSphere(transform.position, 0.02f);
    }
}