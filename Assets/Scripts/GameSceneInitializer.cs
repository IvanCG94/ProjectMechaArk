using UnityEngine;
using System.Collections.Generic;

public class GameSceneInitializer : MonoBehaviour
{
    void Start()
    {
        if (RobotPersistenceManager.Instance == null || RobotPersistenceManager.Instance.SelectedCoreData == null)
        {
            Debug.LogError("Datos del robot no encontrados. Cargando escena de customización.");
            return;
        }

        RobotPersistenceManager manager = RobotPersistenceManager.Instance;
        RobotPartData coreData = manager.SelectedCoreData;
        Dictionary<string, RobotPartData> selectedParts = manager.SelectedParts;
        
        AssembleFinalRobot(coreData, selectedParts);
    }

    void AssembleFinalRobot(RobotPartData coreData, Dictionary<string, RobotPartData> selectedParts)
    {
        GameObject robotRoot = Instantiate(coreData.PartPrefab, transform.position, transform.rotation);
        robotRoot.transform.SetParent(transform); 
        robotRoot.name = "Player_Robot";

        AssemblePartRecursively(robotRoot.transform, selectedParts);
        Debug.Log("¡Robot ensamblado en la escena de juego!");
    }

    void AssemblePartRecursively(Transform parentTransform, Dictionary<string, RobotPartData> selectedParts, int depth = 0)
    {
        if (depth > 50) return;

        Socket[] sockets = parentTransform.GetComponentsInChildren<Socket>();

        foreach (Socket socket in sockets)
        {
            RobotPartData partData = null;
            bool isMirrored = false;
            
            bool foundSpecific = selectedParts.TryGetValue(socket.socketName, out partData);

            // Búsqueda por clave genérica para piezas dobles
            if (!foundSpecific && (socket.acceptedType == PartType.Legs || socket.acceptedType == PartType.Arms))
            {
                selectedParts.TryGetValue("Socket_" + socket.acceptedType.ToString(), out partData);
            }
            
            if (partData != null)
            {
                if (socket.socketName.EndsWith("_L") && (socket.acceptedType == PartType.Arms || socket.acceptedType == PartType.Legs))
                {
                    isMirrored = true;
                }

                GameObject newPart = Instantiate(partData.PartPrefab);
                
                newPart.transform.SetParent(socket.transform);
                newPart.transform.localPosition = Vector3.zero;
                newPart.transform.localRotation = Quaternion.identity;
                
                if (isMirrored) 
                {
                    Vector3 mirroredScale = newPart.transform.localScale;
                    mirroredScale.x *= -1; 
                    newPart.transform.localScale = mirroredScale;
                }

                AssemblePartRecursively(newPart.transform, selectedParts, depth + 1); 
            }
        }
    }
}