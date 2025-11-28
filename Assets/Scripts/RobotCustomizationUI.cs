using UnityEngine;
using UnityEngine.UI; 
using System.Collections.Generic;
using TMPro; 
using System.Linq; 

public class RobotCustomizationUI : MonoBehaviour
{
    [Header("Referencias de Componentes")]
    public CustomizationAssembler assembler; 
    
    [Header("Elementos de UI")]
    public Transform partSelectionPanel; 
    public GameObject partButtonPrefab; 
    public Button startGameButton;      

    [Header("Estado Interno")]
    private List<Socket> currentTorsoSockets = new List<Socket>();
    private string targetSocketName; 

    void Start()
    {
        // Al iniciar, mostramos la selección de Núcleos
        InitializeCoreSelection();
        
        // Configurar el botón de inicio
        if (startGameButton != null)
        {
            startGameButton.onClick.AddListener(RobotPersistenceManager.Instance.StartGame);
            CheckIfRobotIsComplete(); // Verifica si se puede iniciar
        }
    }
    
    // =================================================================================
    // PASO 1: SELECCIÓN DEL NÚCLEO (CORE)
    // =================================================================================

    void InitializeCoreSelection()
    {
        ClearSelectionPanel();
        CheckIfRobotIsComplete();

        // Obtener solo piezas de tipo Core
        List<RobotPartData> coreParts = RobotPersistenceManager.Instance.GetAvailablePartsByFilter(PartType.Core);

        foreach (RobotPartData part in coreParts)
        {
            CreateButton(part.partName, () => SelectCoreAndBuild(part));
        }
    }

    public void SelectCoreAndBuild(RobotPartData selectedCore)
    {
        RobotPersistenceManager manager = RobotPersistenceManager.Instance;

        // 1. Guardar Core (y actualizar la restricción de Tier si aplica)
        manager.selectedCoreData = selectedCore; 
        
        // 2. Guardar elección para ensamblaje (usamos el tipo como clave genérica para el Core)
        manager.SelectPartForSocket(PartType.Core.ToString(), selectedCore); 

        // 3. Reconstruir visualmente
        assembler.RebuildRobot();
        
        // 4. Proceder a seleccionar el Torso
        InitializeTorsoSelection();
    }

    // =================================================================================
    // PASO 2: SELECCIÓN DEL TORSO
    // =================================================================================

    void InitializeTorsoSelection()
    {
        ClearSelectionPanel();
        CheckIfRobotIsComplete();
        
        // AÑADIDO: Botón para volver al Core
        CreateButton("<< CAMBIAR NÚCLEO", () => InitializeCoreSelection());

        // Obtener Torsos compatibles
        List<RobotPartData> torsoParts = RobotPersistenceManager.Instance.GetAvailablePartsByFilter(PartType.Torso);

        foreach (RobotPartData part in torsoParts)
        {
            string btnText = $"{part.partName} (T{(int)part.partTier + 1})";
            CreateButton(btnText, () => SelectTorsoAndFindSockets(part));
        }
    }

    public void SelectTorsoAndFindSockets(RobotPartData selectedTorso)
    {
        RobotPersistenceManager manager = RobotPersistenceManager.Instance;

        // 1. Obtener el ensamblaje actual
        Transform coreAssembly = assembler.currentRobotAssembly.transform;
        
        // 2. Buscar el socket del Torso en el Core
        Socket coreTorsoSocket = coreAssembly.GetComponentsInChildren<Socket>()
            .FirstOrDefault(s => s.acceptedType == PartType.Torso);

        if (coreTorsoSocket == null)
        {
             Debug.LogError("Error Crítico: El Core seleccionado no tiene un Socket configurado con 'Accepted Type: Torso'.");
             return;
        }

        // 3. Guardar la elección usando el nombre REAL del socket
        manager.SelectPartForSocket(coreTorsoSocket.socketName, selectedTorso); 
        
        // 4. Reconstruir para que aparezca el Torso visualmente
        assembler.RebuildRobot(); 

        // 5. Encontrar el objeto Torso recién instanciado (buscamos la nueva referencia)
        coreAssembly = assembler.currentRobotAssembly.transform;
        Socket refreshedSocket = coreAssembly.GetComponentsInChildren<Socket>()
            .FirstOrDefault(s => s.socketName == coreTorsoSocket.socketName);

        if (refreshedSocket == null || refreshedSocket.transform.childCount == 0)
        {
            Debug.LogError("No se pudo encontrar el Torso instanciado después del ensamblaje.");
            return;
        }

        GameObject instanciatedTorso = refreshedSocket.transform.GetChild(0).gameObject;

        // 6. Obtener todos los sockets que trae el nuevo Torso (Brazos, Cabeza, etc.)
        Socket[] foundSockets = instanciatedTorso.GetComponentsInChildren<Socket>();
        currentTorsoSockets = new List<Socket>(foundSockets);
        
        // 7. Ir al menú de Sockets
        InitializeSocketMenu();
    }

    // =================================================================================
    // PASO 3: MENÚ DE SOCKETS (Brazos Independientes y Navegación)
    // =================================================================================

    // Muestra una lista de "Slots" vacíos o llenos (Ej: "Brazo Derecho", "Cabeza")
    void InitializeSocketMenu()
    {
        ClearSelectionPanel();
        CheckIfRobotIsComplete();

        // AÑADIDO: Botón para volver al Torso
        CreateButton("<< CAMBIAR TORSO", () => InitializeTorsoSelection());

        foreach (Socket socket in currentTorsoSockets)
        {
            // Formatear un nombre bonito para el botón
            string displayName = FormatSocketName(socket.socketName);
            
            // Verificar si ya hay algo equipado
            bool isFilled = RobotPersistenceManager.Instance.SelectedParts.ContainsKey(socket.socketName);
            string status = isFilled ? "[LISTO]" : "[VACÍO]";
            
            string buttonText = $"{displayName} \n<size=70%>{status}</size>";

            // El clic lleva a la selección de piezas para este socket específico
            CreateButton(buttonText, () => ShowPartSelectionForSocket(socket.socketName, socket.acceptedType));
        }
    }

    // =================================================================================
    // PASO 4: SELECCIÓN DE PIEZA ESPECÍFICA
    // =================================================================================

    // Muestra las piezas disponibles para un socket específico
    void ShowPartSelectionForSocket(string socketName, PartType requiredType)
    {
        ClearSelectionPanel();

        // Guardamos qué socket estamos editando
        targetSocketName = socketName;

        // Botón de Volver al menú de Sockets
        CreateButton("<< VOLVER AL CUERPO", () => InitializeSocketMenu());

        // Obtener piezas disponibles
        List<RobotPartData> parts = RobotPersistenceManager.Instance.GetAvailablePartsByFilter(requiredType);

        foreach (RobotPartData part in parts)
        {
            CreateButton(part.partName, () => AssignPartToSpecificSocket(part));
        }
    }

    void AssignPartToSpecificSocket(RobotPartData partData)
    {
        if (string.IsNullOrEmpty(targetSocketName)) return;

        // 1. Guardar la pieza en el socket específico
        RobotPersistenceManager.Instance.SelectPartForSocket(targetSocketName, partData);

        // 2. Reconstruir el robot
        assembler.RebuildRobot();

        // 3. Volver al menú del cuerpo
        InitializeSocketMenu();
    }

    // =================================================================================
    // UTILIDADES
    // =================================================================================
    
    // Verifica si el robot tiene al menos un Core y un Torso para habilitar el botón Start Game
    private void CheckIfRobotIsComplete()
    {
        if (startGameButton == null) return;
        
        RobotPersistenceManager manager = RobotPersistenceManager.Instance;

        // Requisito mínimo: Core y Torso seleccionados
        bool hasCore = manager.selectedCoreData != null;
        
        // Asumimos que si hay un Core, y el jugador pasó al menú de Sockets, ya seleccionó un Torso.
        // También podemos verificar si el diccionario SelectedParts tiene la clave del socket del Torso.
        bool hasTorso = manager.SelectedParts.Any(kvp => kvp.Value.partType == PartType.Torso); 
        
        startGameButton.interactable = hasCore && hasTorso;
    }


    // Función auxiliar para crear botones rápidamente
    void CreateButton(string text, UnityEngine.Events.UnityAction action)
    {
        // ... (Tu implementación de CreateButton)
        GameObject buttonGO = Instantiate(partButtonPrefab, partSelectionPanel);
        
        TMP_Text buttonText = buttonGO.GetComponentInChildren<TMP_Text>();
        if (buttonText != null)
        {
            buttonText.text = text;
        }

        Button btn = buttonGO.GetComponent<Button>();
        btn.onClick.AddListener(action);
    }

    void ClearSelectionPanel()
    {
        // ... (Tu implementación de ClearSelectionPanel)
        foreach (Transform child in partSelectionPanel)
        {
            Destroy(child.gameObject);
        }
    }

    // Hace que "Socket_Arm_L" se vea como "Brazo Izquierdo"
    string FormatSocketName(string rawName)
    {
        return rawName
            .Replace("Socket_", "")
            .Replace("Arm", "Brazo")
            .Replace("Head", "Cabeza")
            .Replace("Legs", "Piernas")
            .Replace("_L", " Izq.")
            .Replace("_R", " Der.");
    }
}