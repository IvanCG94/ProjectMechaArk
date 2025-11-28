using UnityEngine;
using System.Collections.Generic;

public class RobotTestRig : MonoBehaviour
{
    [Header("Punto de Anclaje")]
    public Transform hipsConnectionPoint; 

    [Header("Blueprints de Torsos")]
    public GameObject torso2ArmsPrefab;
    public GameObject torso4ArmsPrefab;
    
    [Header("Datos de Piezas de Prueba")]
    public RobotPartData testArmData_T1; // Usaremos ESTE para todos los brazos

    private GameObject currentTorso;
    private List<GameObject> equippedLimbs = new List<GameObject>();

    void Start()
    {
        EquipTorso(torso2ArmsPrefab);
    }

    void Update()
    {
        // Asume que la Opción 1 (Both) está activa en Player Settings
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            EquipTorso(torso2ArmsPrefab);
        }
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            EquipTorso(torso4ArmsPrefab);
        }
    }

    void EquipTorso(GameObject torsoPrefab)
    {
        if (currentTorso != null)
        {
            Destroy(currentTorso);
        }
        equippedLimbs.Clear();

        currentTorso = Instantiate(torsoPrefab);
        currentTorso.transform.SetParent(hipsConnectionPoint);
        currentTorso.transform.localPosition = Vector3.zero;
        currentTorso.transform.localRotation = Quaternion.identity;

        PopulateSockets();
    }
    
    // --- NUEVOS MÉTODOS DE VALIDACIÓN Y ENSAMBLAJE ---
    
    bool IsPartCompatible(Socket socket, RobotPartData partData)
    {
        // 1. Validación de TIPO (Ej: Brazo va en Socket de Brazo)
        if (socket.acceptedType != partData.partType)
        {
            Debug.LogError($"ERROR: El socket '{socket.socketName}' solo acepta {socket.acceptedType}.");
            return false;
        }
        // 2. Validación de TIER
        if (socket.acceptedTier != partData.partTier)
        {
            Debug.LogError($"ERROR: El socket '{socket.socketName}' requiere Tier {socket.acceptedTier}.");
            return false;
        }
        return true; 
    }

    void PopulateSockets()
    {
        Socket[] foundSockets = currentTorso.GetComponentsInChildren<Socket>();

        foreach (Socket socket in foundSockets)
        {
            // Para la prueba, intentamos equipar el Brazo T1 en CUALQUIER socket.
            RobotPartData partToEquip = testArmData_T1; 
            
            if (partToEquip == null) continue;

            // Paso 1: Validación
            if (!IsPartCompatible(socket, partToEquip))
            {
                // Si falla la validación, pasamos al siguiente socket.
                continue; 
            }

            // Paso 2: Ensamblaje
            GameObject newLimb = Instantiate(partToEquip.partPrefab);
            newLimb.transform.SetParent(socket.transform);
            newLimb.transform.localPosition = Vector3.zero;
            newLimb.transform.localRotation = Quaternion.identity;

            // Paso 3: Mirroring (Reflejo)
            if (socket.socketName.EndsWith("_L")) // Verifica si el nombre termina en "_L"
            {
                Vector3 mirroredScale = newLimb.transform.localScale;
                mirroredScale.x *= -1; // Invierte el eje X
                newLimb.transform.localScale = mirroredScale;
            }

            equippedLimbs.Add(newLimb);
            Debug.Log($"Ensamblado: {partToEquip.partName} en {socket.socketName}");
        }
    }
}