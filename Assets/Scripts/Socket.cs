using UnityEngine;

public class Socket : MonoBehaviour
{
    public PartType acceptedType;
    public string socketName;

    private GameObject _visualIndicator;
    private Renderer _indicatorRenderer;

    private void Awake()
    {
        CreateVisualIndicator();
    }

    private void CreateVisualIndicator()
    {
        // 1. Crear esfera
        _visualIndicator = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        _visualIndicator.name = "Hover_Indicator";
        
        // 2. Configurar posición y escala
        _visualIndicator.transform.SetParent(transform);
        _visualIndicator.transform.localPosition = Vector3.zero;
        _visualIndicator.transform.localRotation = Quaternion.identity;
        _visualIndicator.transform.localScale = Vector3.one * 0.25f; 

        // 3. Eliminar collider (Importante)
        Destroy(_visualIndicator.GetComponent<Collider>());
        
        // 4. Configurar el Material con nuestro Shader de Rayos X
        _indicatorRenderer = _visualIndicator.GetComponent<Renderer>();
        
        // Buscamos el shader que acabamos de crear por su nombre
        Shader xrayShader = Shader.Find("Custom/SocketXRay");
        
        if (xrayShader != null)
        {
            Material xRayMat = new Material(xrayShader);
            // Color Amarillo Brillante con Transparencia
            xRayMat.color = new Color(1f, 0.8f, 0f, 0.6f); 
            _indicatorRenderer.material = xRayMat;
        }
        else
        {
            // Fallback por si acaso no creaste el archivo shader
            Debug.LogWarning("No se encontró el shader 'Custom/SocketXRay'. Usando Standard.");
            _indicatorRenderer.material.color = Color.yellow;
        }

        // 5. Apagar por defecto
        _visualIndicator.SetActive(false);
    }

    public void ToggleHighlight(bool isActive)
    {
        if (_visualIndicator != null)
        {
            _visualIndicator.SetActive(isActive);
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, 0.1f);
    }
}