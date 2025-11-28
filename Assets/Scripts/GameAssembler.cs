using UnityEngine;
using System.Collections.Generic; // ¡DEBES AGREGAR ESTA LÍNEA!

public class GameAssembler : MonoBehaviour // ¡MonoBehaviour!
{
    // Función que se llama desde GameSceneInitializer
    public void FinalAssemble(Dictionary<string, RobotPartData> parts, RobotPartData coreData)
    {
        // 1. Instanciar el Core (Este es el nuevo origen (0, 0, 0))
        GameObject coreGO = Instantiate(coreData.partPrefab, transform.position, transform.rotation);
        coreGO.transform.SetParent(transform); // Parent al objeto GameAssembler
        
        // 2. Montar las Partes restantes (Torso, Brazos, etc.)
        AssemblePart(coreGO.transform, parts);
    }
    
    // Función para buscar Sockets y ensamblar las piezas
    void AssemblePart(Transform parentTransform, Dictionary<string, RobotPartData> parts)
    {
        // 1. Encontrar todos los sockets en el Core/Torso/Pierna (parentTransform)
        Socket[] sockets = parentTransform.GetComponentsInChildren<Socket>();
        
        foreach(Socket socket in sockets)
        {
            // 2. Verificar si el jugador eligió una pieza para este socket
            if (parts.TryGetValue(socket.socketName, out RobotPartData partData))
            {
                // 3. Ensamblar la pieza
                GameObject newPart = Instantiate(partData.partPrefab, socket.transform.position, socket.transform.rotation, socket.transform);
                
                // --- Aquí va la lógica de alineación y mirroring ---
                newPart.transform.localPosition = Vector3.zero;
                newPart.transform.localRotation = Quaternion.identity;
                
                // Lógica de Mirroring (asumiendo que RobotTestRig tiene una función de mirroring)
                if (socket.socketName.EndsWith("_L")) 
                {
                    Vector3 mirroredScale = newPart.transform.localScale;
                    mirroredScale.x *= -1; 
                    newPart.transform.localScale = mirroredScale;
                }

                // 4. Continuar el ensamblaje (Recursividad)
                // Si la pieza ensamblada tiene más sockets (ej. un torso), busca más piezas.
                AssemblePart(newPart.transform, parts); 
            }
        }
    }
}