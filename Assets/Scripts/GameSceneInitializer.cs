using UnityEngine;
using System.Collections.Generic;

public class GameSceneInitializer : MonoBehaviour
{
    private const int MAX_DEPTH = 50;
    
    // Referencia al script que maneja la cámara
    private CameraFinder cameraFinder; 
    
    // Asignado fuera del script: public PhysicMaterial zeroFrictionMaterial;

    void Start()
    {
        if (RobotPersistenceManager.Instance == null || RobotPersistenceManager.Instance.SelectedCoreData == null)
        {
            Debug.LogError("Datos no encontrados.");
            return;
        }

        // LÍNEA MODERNA: Usamos FindFirstObjectByType para evitar el warning 'obsolete'.
        cameraFinder = FindFirstObjectByType<CameraFinder>();

        if (cameraFinder == null)
        {
            Debug.LogError("ERROR GRAVE: CameraFinder no encontrado en la escena. El zoom fallará.");
        } else {
            Debug.Log("DIAGNÓSTICO: Referencia a CameraFinder encontrada con éxito.");
        }

        AssembleFinalRobot(RobotPersistenceManager.Instance.SelectedCoreData, RobotPersistenceManager.Instance.SelectedParts);
    }

    void AssembleFinalRobot(RobotPartData coreData, Dictionary<string, RobotPartData> selectedParts)
    {
        GameObject robotRoot = Instantiate(coreData.PartPrefab, transform.position, transform.rotation);
        robotRoot.name = "Player_Robot";

        AssemblePartRecursively(robotRoot.transform, selectedParts);
        Physics.SyncTransforms(); 

        Rigidbody rb = robotRoot.AddComponent<Rigidbody>();
        CapsuleCollider col = robotRoot.AddComponent<CapsuleCollider>();
        // Note: Se asume que el PhysicMaterial se asigna en el Inspector o por otra lógica.

        FitColliderToChildren(robotRoot, col, rb);

        RobotController controller = robotRoot.AddComponent<RobotController>();
        controller.groundLayers = -1; 
        
        Debug.Log("¡Robot Jugable Listo!");
    }

    void FitColliderToChildren(GameObject root, CapsuleCollider col, Rigidbody rb)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>();
        
        if (renderers.Length == 0) return;

        Bounds combinedBounds = new Bounds();
        bool hasBounds = false;

        foreach (Renderer r in renderers)
        {
            if (r is SkinnedMeshRenderer || r is MeshRenderer)
            {
                if (!hasBounds) { combinedBounds = r.bounds; hasBounds = true; }
                else { combinedBounds.Encapsulate(r.bounds); }
            }
        }

        if (hasBounds)
        {
            float height = combinedBounds.size.y; 
            Vector3 localCenter = combinedBounds.center - root.transform.position;

            // 1. Aplicar al Collider (Con el tamaño MUNDO 8.22m)
            col.height = height;
            col.center = new Vector3(0, localCenter.y, 0); 
            
            float width = (combinedBounds.size.x + combinedBounds.size.z) / 2f;
            col.radius = width / 2f; 

            // 2. Masa dinámica
            rb.mass = height * 25f; 
            
            // 3. LLamada a la calibración de cámara
            if (cameraFinder != null)
            {
                Debug.Log($"DIAGNÓSTICO: Llamando a CalibrateCamera con altura: {height}m");
                cameraFinder.CalibrateCamera(root.transform, height);
            } else {
                Debug.LogWarning("DIAGNÓSTICO: cameraFinder es nulo. No se pudo llamar.");
            }

            Debug.Log($"Robot Medido -> Altura: {height}m | Centro Y: {localCenter.y}");
            
            // 4. AYUDA VISUAL (DEBUG)
            var debug = root.AddComponent<BoundsVisualizer>();
            debug.center = col.center;
            debug.size = new Vector3(col.radius * 2, col.height, col.radius * 2);
        }
    }

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