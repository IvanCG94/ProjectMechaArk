using UnityEngine;
using Unity.Cinemachine; // <--- CAMBIO IMPORTANTE: El nuevo namespace

public class CameraFinder : MonoBehaviour
{
    void Update()
    {
        // 1. Intentamos obtener el componente moderno (CM 3.x)
        var cam = GetComponent<CinemachineCamera>();
        
        // Si no existe, intentamos buscar el legacy por si acaso (aunque en 3.1.5 seguramente es el de arriba)
        // var camLegacy = GetComponent<CinemachineFreeLook>(); 

        // 2. Verificamos si encontramos la cámara y si le falta el objetivo
        if (cam != null && cam.Follow == null)
        {
            GameObject player = GameObject.Find("Player_Robot");
            
            if (player != null)
            {
                // Asignar el objetivo
                cam.Follow = player.transform;
                cam.LookAt = player.transform; 
                
                Debug.Log("Cámara: Objetivo 'Player_Robot' encontrado y asignado.");
            }
        }
    }
}