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
        // 1. DESTRUIR LO ANTERIOR (Sin lógica de reembolso de piezas secundarias)
        if (currentRobotAssembly != null)
        {
            // Nota: Aquí se quitó la llamada a RefundEquippedParts() para probar la estabilidad.
            
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
    
    // NOTA: La función RefundEquippedParts ha sido eliminada de este script.

    void AssemblePartRecursively(Transform parentTransform, Dictionary<string, RobotPartData> selectedParts, int depth = 0)
    {
        if (depth > MAX_DEPTH) 
        {
            Debug.LogError("Límite de recursividad alcanzado. Revisa tu estructura."); 
            return; 
        }

        Socket[] sockets = parentTransform.GetComponentsInChildren<Socket>();

        foreach (Socket socket in sockets)
        {
            RobotPartData partData = null;
            bool isMirrored = false;
            
            // Busca SIEMPRE por el nombre de socket específico.
            if (selectedParts.TryGetValue(socket.socketName, out partData))
            {
                // Determinar si necesita espejo
                if (socket.socketName.EndsWith("_L"))
                {
                    if (socket.acceptedType == PartType.Arms || socket.acceptedType == PartType.Legs)
                    {
                         isMirrored = true;
                    }
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