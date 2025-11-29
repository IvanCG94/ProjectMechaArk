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
        if (currentRobotAssembly != null)
        {
            Destroy(currentRobotAssembly);
            currentRobotAssembly = null;
        }

        RobotPartData coreData = manager.SelectedCoreData;
        if (coreData == null) return;

        currentRobotAssembly = Instantiate(coreData.PartPrefab, transform.position, transform.rotation);
        currentRobotAssembly.transform.SetParent(transform);
        currentRobotAssembly.name = "Robot_Assembly";

        AssemblePartRecursively(currentRobotAssembly.transform, manager.SelectedParts);
    }

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
                // Determinar si necesita espejo (aplica a cualquier parte bilateral que termine en _L)
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