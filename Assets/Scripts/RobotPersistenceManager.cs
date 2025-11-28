using UnityEngine;
using System.Collections.Generic;
using System.Linq; // Necesario para la función LINQ (FirstOrDefault, etc.)

public class RobotPersistenceManager : MonoBehaviour 
{
    // Singleton estático para acceder desde cualquier parte.
    public static RobotPersistenceManager Instance; 
    
    [Header("Inventario Global")]
    // Todas las piezas que el jugador puede seleccionar.
    public List<RobotPartData> availableParts; 

    [Header("Piezas Seleccionadas")]
    // El Core, que establece el límite de Tier.
    public RobotPartData selectedCoreData;
    
    // Diccionario para guardar las elecciones del jugador.
    // Key: socketName (Ej: "Brazo_R"), Value: Ficha Técnica (RobotPartData) de la pieza elegida.
    public Dictionary<string, RobotPartData> SelectedParts = 
        new Dictionary<string, RobotPartData>(); 

    [Header("Debug/Estado")]
    public string nextSceneName = "Scene_Game"; 
    
    // --- Lógica de Singleton y Persistencia ---
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Evita que se destruya al cambiar de escena.
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // --- FUNCIONES CLAVE PARA EL MENÚ DE PERSONALIZACIÓN ---

    // 1. Función para obtener solo las piezas compatibles con el Tier del Core.
    public List<RobotPartData> GetAvailablePartsByFilter(PartType typeFilter)
    {
        if (selectedCoreData == null)
        {
            // Si no hay Core seleccionado, solo ofrece Cores.
            return availableParts.Where(p => p.partType == PartType.Core).ToList();
        }
        
        // Obtiene el Tier máximo permitido por el Core.
        Tier maxTier = selectedCoreData.maxAllowedTier; 

        // Devuelve las piezas que cumplen con el Tipo y el Tier.
        return availableParts
            .Where(p => p.partType == typeFilter && p.partTier <= maxTier)
            .ToList();
    }
    
    // 2. Función para guardar la elección de una pieza en un Socket.
    public void SelectPartForSocket(string socketName, RobotPartData partData)
    {
        if (partData == null) return;
        
        // Si el Socket ya existe en el diccionario, actualiza el valor. Si no, lo agrega.
        SelectedParts[socketName] = partData;
        
        Debug.Log($"Guardado: {partData.partName} en socket {socketName}");
    }

    // 3. Función para iniciar el juego.
    public void StartGame()
    {
        // En un juego real, aquí se verificaría que todos los sockets obligatorios estén llenos.
        if (selectedCoreData == null)
        {
            Debug.LogError("¡Debe seleccionar un Núcleo (Core) antes de empezar!");
            return;
        }

        // Carga la escena de juego.
        UnityEngine.SceneManagement.SceneManager.LoadScene(nextSceneName);
    }
}