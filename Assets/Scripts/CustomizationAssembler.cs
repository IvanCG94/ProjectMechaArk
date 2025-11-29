using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class CustomizationAssembler : MonoBehaviour
{
    public GameObject currentRobotAssembly; 
    private RobotPersistenceManager manager;
    private const int MAX_DEPTH = 50; 

    private void Start()
    {
        manager = RobotPersistenceManager.Instance;
    }

    public void RebuildRobot()
    {
        // 1. DESTRUIR LO ANTERIOR
        if (currentRobotAssembly != null)
        {
            Destroy(currentRobotAssembly);
            currentRobotAssembly = null;
        }

        RobotPartData coreData = manager.SelectedCoreData;
        if (coreData == null) return;

        // 2. Ensamblar el nuevo Core
        currentRobotAssembly = Instantiate(coreData.PartPrefab, transform.position, transform.rotation);
        currentRobotAssembly.transform.SetParent(transform);
        currentRobotAssembly.name = "Robot_Assembly";

        // 3. Iniciar el Ensamblaje Recursivo
        AssemblePartRecursively(currentRobotAssembly.transform, manager.SelectedParts);
    }
    
    void AssemblePartRecursively(Transform parentTransform, Dictionary<string, RobotPartData> selectedParts, int depth = 0)
    {
        if (depth > MAX_DEPTH) 
        {
            Debug.LogError("Límite de recursividad alcanzado."); 
            return; 
        }

        Socket[] sockets = parentTransform.GetComponentsInChildren<Socket>();

        foreach (Socket socket in sockets)
        {
            RobotPartData partData = null;
            bool isMirrored = false;
            
            // Busca la pieza en el diccionario usando el nombre del socket actual
            if (selectedParts.TryGetValue(socket.socketName, out partData))
            {
                // Determinar si es lado Izquierdo para aplicar espejo visual
                // Y AHORA TAMBIÉN para propagar el nombre a los hijos.
                string sideSuffix = "";

                if (socket.socketName.EndsWith("_L"))
                {
                    sideSuffix = "_L";
                    if (socket.acceptedType == PartType.Arms || socket.acceptedType == PartType.Legs)
                    {
                         isMirrored = true;
                    }
                }
                else if (socket.socketName.EndsWith("_R"))
                {
                    sideSuffix = "_R";
                }

                GameObject newPart = Instantiate(partData.PartPrefab);
                
                newPart.transform.SetParent(socket.transform);
                newPart.transform.localPosition = Vector3.zero;
                newPart.transform.localRotation = Quaternion.identity;
                
                // === NUEVA LÓGICA: Propagar Lateralidad a los Sockets Hijos ===
                // Si estamos en un lado (L o R), renombramos los sockets de la nueva pieza
                // para que sean únicos (Ej: "Socket_Weapon" -> "Socket_Weapon_L")
                if (!string.IsNullOrEmpty(sideSuffix))
                {
                    PropagateSideToSockets(newPart.transform, sideSuffix);
                }
                // ===============================================================

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

    // Función auxiliar para renombrar sockets hijos dinámicamente
    void PropagateSideToSockets(Transform partRoot, string suffix)
    {
        // Obtenemos SOLO los sockets directos de esta pieza nueva
        // (Usamos true en includeInactive por si acaso)
        Socket[] childSockets = partRoot.GetComponentsInChildren<Socket>(true);

        foreach (Socket childSocket in childSockets)
        {
            // Solo renombramos si el socket NO tiene ya el sufijo (evita Socket_L_L)
            if (!childSocket.socketName.EndsWith(suffix))
            {
                childSocket.socketName = childSocket.socketName + suffix;
            }
        }
    }
}