using UnityEngine;
using Unity.Cinemachine; 

public class CameraFinder : MonoBehaviour
{
    [Tooltip("Multiplicador de Zoom. Aumenta para alejar más la cámara.")]
    public float cameraZoomMultiplier = 4.0f; 

    public void CalibrateCamera(Transform target, float robotHeight)
    {
        Debug.Log($"DIAGNÓSTICO CÁMARA: Recibido target y altura {robotHeight}m.");
        
        // Usamos FindFirstObjectByType
        var cam = FindFirstObjectByType<CinemachineCamera>(); 
        
        if (cam == null) {
            Debug.LogError("ERROR GRAVE CÁMARA: No se encontró el componente CinemachineCamera en la escena.");
            return;
        }
        
        cam.Follow = target;
        cam.LookAt = target; 
        
        float targetRadius = robotHeight * cameraZoomMultiplier;
        Debug.Log($"DIAGNÓSTICO CÁMARA: Distancia calculada (Radius): {targetRadius}m.");

        // Aplicar a la extensión de Cinemachine
        var orbital = cam.GetComponent<CinemachineOrbitalFollow>();
        var thirdPerson = cam.GetComponent<CinemachineThirdPersonFollow>();

        if (orbital != null)
        {
            orbital.Radius = targetRadius;
            Debug.Log("DIAGNÓSTICO CÁMARA: Aplicado a CinemachineOrbitalFollow.");
            return;
        }
        
        if (thirdPerson != null) 
        {
            thirdPerson.CameraDistance = targetRadius;
            Debug.Log("DIAGNÓSTICO CÁMARA: Aplicado a CinemachineThirdPersonFollow.");
            return;
        }
        
        Debug.LogWarning("DIAGNÓSTICO CÁMARA: No se encontró ningún componente de extensión (Orbital/ThirdPerson) para aplicar el zoom.");
    }

    void Update()
    {
        // Lógica de búsqueda inicial (Fallback)
        var cam = FindFirstObjectByType<CinemachineCamera>();
        
        if (cam != null && cam.Follow == null)
        {
            GameObject player = GameObject.Find("Player_Robot");
            
            if (player != null)
            {
                // Este valor es solo para el fallback si no se llama desde GameSceneInitializer.
                float defaultHeight = 8.223858f; 
                CalibrateCamera(player.transform, defaultHeight); 
            }
        }
    }
}