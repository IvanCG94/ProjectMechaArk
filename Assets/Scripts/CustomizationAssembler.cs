using UnityEngine;
using System.Collections.Generic;

public class CustomizationAssembler : MonoBehaviour
{
    [Header("Referencias de Persistencia")]
    // Referencia al Manager de persistencia de datos (Singleton)
    private RobotPersistenceManager manager;

    [Header("Ensamblaje Actual")]
    // ESTA VARIABLE DEBE SER PÚBLICA (public) para que RobotCustomizationUI pueda acceder a ella.
    public GameObject currentRobotAssembly; 

    private void Start()
    {
        // Obtener la instancia del Singleton
        manager = RobotPersistenceManager.Instance;
        
        // El robot se ensambla por primera vez solo si hay datos, pero usualmente 
        // se espera que la UI llame a RebuildRobot() después de la primera selección de Core.
    }

    /// <summary>
    /// Reconstruye el robot completo desde cero usando las piezas seleccionadas por el jugador.
    /// Esta función se llama cada vez que el jugador selecciona una nueva pieza.
    /// </summary>
    public void RebuildRobot()
    {
        // 1. Limpiar el ensamblaje anterior
        if (currentRobotAssembly != null)
        {
            Destroy(currentRobotAssembly);
            currentRobotAssembly = null;
        }

        // 2. Obtener el Core seleccionado (la pieza base)
        RobotPartData coreData = manager.selectedCoreData;

        if (coreData == null)
        {
            Debug.LogWarning("No se ha seleccionado el Core. No se puede ensamblar el robot.");
            return;
        }

        // 3. Instanciar el Core
        currentRobotAssembly = Instantiate(coreData.partPrefab, transform.position, transform.rotation);
        currentRobotAssembly.transform.SetParent(transform);
        currentRobotAssembly.name = "Robot_Assembly"; // Renombrar la raíz

        // 4. Iniciar el Ensamblaje Recursivo (montar Torso, Brazos, etc.)
        // Pasamos el Transform del Core y la lista de piezas seleccionadas.
        AssemblePartRecursively(currentRobotAssembly.transform, manager.SelectedParts);
    }

    /// <summary>
    /// Ensambla partes de forma recursiva en los sockets de la pieza padre.
    /// Implementa un límite de profundidad para prevenir StackOverflowException.
    /// </summary>
    /// <param name="parentTransform">El Transform de la pieza padre (donde se buscan los sockets).</param>
    /// <param name="selectedParts">El diccionario de piezas seleccionadas (SocketName -> PartData).</param>
    /// <param name="depth">Nivel de recursión actual para la protección contra Stack Overflow.</param>
    void AssemblePartRecursively(Transform parentTransform, Dictionary<string, RobotPartData> selectedParts, int depth = 0)
    {
        // PREVENCIÓN DE STACK OVERFLOW
        if (depth > 50) 
        {
            Debug.LogError("Stack Overflow Prevención: Profundidad de ensamblaje excedida. ¡Hay un posible ciclo infinito en los sockets o los datos!");
            return; 
        }

        // Obtener todos los sockets en la jerarquía de la pieza padre
        Socket[] sockets = parentTransform.GetComponentsInChildren<Socket>();

        foreach (Socket socket in sockets)
        {
            // Usamos socket.socketName como clave para buscar la pieza seleccionada
            if (selectedParts.TryGetValue(socket.socketName, out RobotPartData partData))
            {
                // Si encontramos una pieza seleccionada para este socket:
                
                // 1. Instanciar la pieza
                GameObject newPart = Instantiate(partData.partPrefab);
                
                // 2. Colocar y alinear la pieza en el socket
                newPart.transform.SetParent(socket.transform);
                newPart.transform.localPosition = Vector3.zero;
                newPart.transform.localRotation = Quaternion.identity;
                
                // 3. Aplicar Mirroring (Reflejo) si es un socket simétrico (ej. '_L' para Left)
                // Esta es una convención común para reutilizar prefabs de partes derechas.
                if (socket.socketName.EndsWith("_L")) 
                {
                    // Asume que el modelo se ha creado con orientación derecha y se refleja en X
                    Vector3 mirroredScale = newPart.transform.localScale;
                    mirroredScale.x *= -1; 
                    newPart.transform.localScale = mirroredScale;
                }

                // 4. Continuar el ensamblaje recursivo con la nueva pieza como padre
                // Incrementamos la profundidad para la prevención de Stack Overflow
                AssemblePartRecursively(newPart.transform, selectedParts, depth + 1); 
            }
        }
    }
}