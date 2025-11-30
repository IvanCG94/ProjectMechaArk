using UnityEngine;
using System.Collections.Generic;

public class GameSceneInitializer : MonoBehaviour
{
    private const int MAX_DEPTH = 50;

    void Start()
    {
        if (RobotPersistenceManager.Instance == null || RobotPersistenceManager.Instance.SelectedCoreData == null)
        {
            Debug.LogError("Datos no encontrados.");
            return;
        }

        AssembleFinalRobot(RobotPersistenceManager.Instance.SelectedCoreData, RobotPersistenceManager.Instance.SelectedParts);
    }

    void AssembleFinalRobot(RobotPartData coreData, Dictionary<string, RobotPartData> selectedParts)
    {
        // 1. Instanciar
        GameObject robotRoot = Instantiate(coreData.PartPrefab, transform.position, transform.rotation);
        robotRoot.name = "Player_Robot";

        // 2. Ensamblar todas las partes
        AssemblePartRecursively(robotRoot.transform, selectedParts);

        // =============================================================
        // CORRECCIÓN CRÍTICA: FORZAR ACTUALIZACIÓN DE FÍSICAS
        // =============================================================
        // Esto obliga a Unity a calcular las posiciones reales de las piernas/brazos
        // ANTES de que intentemos medirlos. Sin esto, miden 0 o están en el origen.
        Physics.SyncTransforms(); 
        // =============================================================

        // 3. Ajustar Collider y Masa
        Rigidbody rb = robotRoot.AddComponent<Rigidbody>();
        CapsuleCollider col = robotRoot.AddComponent<CapsuleCollider>();

        FitColliderToChildren(robotRoot, col, rb);

        // 4. Agregar Cerebro
        RobotController controller = robotRoot.AddComponent<RobotController>();
        controller.groundLayers = -1; 
        
        Debug.Log("¡Robot Jugable Listo!");
    }

    void FitColliderToChildren(GameObject root, CapsuleCollider col, Rigidbody rb)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>();
        
        if (renderers.Length == 0) return;

        // 1. Inicializar los bounds con el PRIMER renderer válido que encontremos
        Bounds combinedBounds = new Bounds();
        bool hasBounds = false;

        foreach (Renderer r in renderers)
        {
            // Ignoramos partículas o trails, solo queremos cuerpo sólido
            if (r is SkinnedMeshRenderer || r is MeshRenderer)
            {
                if (!hasBounds)
                {
                    combinedBounds = r.bounds;
                    hasBounds = true;
                }
                else
                {
                    combinedBounds.Encapsulate(r.bounds);
                }
            }
        }

        if (hasBounds)
        {
            // 2. Calcular dimensiones
            float height = combinedBounds.size.y;
            Vector3 localCenter = combinedBounds.center - root.transform.position;

            // 3. Aplicar al Collider (ESTO USA ESCALA MUNDO - EL PROBLEMA)
            col.height = height;
            col.center = new Vector3(0, localCenter.y, 0); 
            
            // Usamos el promedio de ancho y largo para el radio
            float width = (combinedBounds.size.x + combinedBounds.size.z) / 2f;
            col.radius = width / 2f; 

            // 4. Masa dinámica
            rb.mass = height * 25f; 

            Debug.Log($"Robot Medido -> Altura: {height}m | Centro Y: {localCenter.y}");
            
            // === AYUDA VISUAL (DEBUG) ===
            // ESTO ESTÁ ROTO AHORA POR LA ESCALA
            var debug = root.AddComponent<BoundsVisualizer>();
            debug.center = col.center;
            debug.size = new Vector3(col.radius * 2, col.height, col.radius * 2);
        }
    }

    // ... (El resto de funciones: AssemblePartRecursively y PropagateSideToSockets deben estar en tu archivo)
    void AssemblePartRecursively(Transform parentTransform, Dictionary<string, RobotPartData> selectedParts, int depth = 0)
    {
        if (depth > MAX_DEPTH) return;
        Socket[] sockets = parentTransform.GetComponentsInChildren<Socket>();

        foreach (Socket socket in sockets)
        {
            RobotPartData partData = null;
            if (selectedParts.TryGetValue(socket.socketName, out partData))
            {
                string sideSuffix = "";
                bool isMirrored = false;

                if (socket.socketName.EndsWith("_L"))
                {
                    sideSuffix = "_L";
                    if (socket.acceptedType == PartType.Arms || socket.acceptedType == PartType.Legs) isMirrored = true;
                }
                else if (socket.socketName.EndsWith("_R")) sideSuffix = "_R";

                GameObject newPart = Instantiate(partData.PartPrefab);
                newPart.transform.SetParent(socket.transform);
                newPart.transform.localPosition = Vector3.zero;
                newPart.transform.localRotation = Quaternion.identity;

                if (!string.IsNullOrEmpty(sideSuffix)) PropagateSideToSockets(newPart.transform, sideSuffix);
                
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

    void PropagateSideToSockets(Transform partRoot, string suffix)
    {
        Socket[] childSockets = partRoot.GetComponentsInChildren<Socket>(true);
        foreach (Socket childSocket in childSockets)
        {
            if (!childSocket.socketName.EndsWith(suffix)) childSocket.socketName = childSocket.socketName + suffix;
        }
    }
}

// ASUMIMOS QUE ESTA CLASE AUXILIAR ESTÁ DEFINIDA AL FINAL DEL ARCHIVO:
public class BoundsVisualizer : MonoBehaviour
{
    public Vector3 center;
    public Vector3 size;

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(1, 0, 0, 0.5f);
        Gizmos.DrawCube(transform.position + transform.TransformDirection(center), size);
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(transform.position + transform.TransformDirection(center), size);
    }
}