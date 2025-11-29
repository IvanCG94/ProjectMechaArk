using UnityEngine;
using UnityEngine.UI; 
using System.Collections.Generic;
using TMPro; 
using System.Linq; 

public class RobotCustomizationUI : MonoBehaviour
{
    [Header("Referencias")]
    public CustomizationAssembler assembler; 
    public Transform partSelectionPanel; 
    public GameObject partButtonPrefab;  
    public Button startGameButton;      

    [Header("Estado de Navegación")]
    // Pila para recordar dónde estábamos antes de "entrar" a una pieza (Breadcrumbs)
    private Stack<GameObject> _navigationHistory = new Stack<GameObject>();
    
    // El objeto cuyos sockets estamos viendo actualmente (puede ser el Torso, un Brazo, etc.)
    private GameObject _currentContextRoot; 
    
    private string targetSocketName; 
    private string _currentSocketDisplayName; 

    void Start()
    {
        InitializeCoreSelection(); 
        
        if (startGameButton != null)
        {
            startGameButton.onClick.AddListener(RobotPersistenceManager.Instance.StartGame);
            CheckIfRobotIsComplete();
        }
    }
    
    // =================================================================================
    // NAVEGACIÓN PRINCIPAL (CORE -> TORSO)
    // =================================================================================

    public void InitializeCoreSelection()
    {
        ClearSelectionPanel();
        _navigationHistory.Clear(); // Reiniciar historial
        CheckIfRobotIsComplete();

        var coreParts = RobotPersistenceManager.Instance.GetAvailablePartsByFilter(PartType.Core);
        foreach (var part in coreParts)
        {
            CreateButton(part.PartName, () => SelectCoreAndBuild(part));
        }
    }

    public void SelectCoreAndBuild(RobotPartData selectedCore)
    {
        var manager = RobotPersistenceManager.Instance;
        manager.SelectPartForSocket(PartType.Core.ToString(), selectedCore); 
        assembler.RebuildRobot();
        InitializeTorsoSelection();
    }

    void InitializeTorsoSelection()
    {
        ClearSelectionPanel();
        _navigationHistory.Clear();
        CheckIfRobotIsComplete();
        
        CreateButton("<< CAMBIAR NÚCLEO", () => InitializeCoreSelection());

        var torsoParts = RobotPersistenceManager.Instance.GetAvailablePartsByFilter(PartType.Torso);
        foreach (var part in torsoParts)
        {
            string btnText = $"{part.PartName} (T{(int)part.PartTier + 1})";
            CreateButton(btnText, () => SelectTorsoAndFindSockets(part));
        }
    }

    public void SelectTorsoAndFindSockets(RobotPartData selectedTorso)
    {
        var manager = RobotPersistenceManager.Instance;
        var coreAssembly = assembler.currentRobotAssembly.transform;
        
        // 1. Lógica de Reembolso (Igual que antes)
        manager.SelectedParts.TryGetValue(manager.SelectedCoreData.PartType.ToString(), out RobotPartData oldCoreData);
        string torsoKey = coreAssembly.GetComponentsInChildren<Socket>()
            .FirstOrDefault(s => s.acceptedType == PartType.Torso)?.socketName ?? "Socket_Torso";
        manager.SelectedParts.TryGetValue(torsoKey, out RobotPartData oldTorsoData);

        if (oldTorsoData != null && oldTorsoData.PartID != selectedTorso.PartID)
        {
            RefundAllEquippedChildren();
        }
        
        // 2. Montaje
        var coreTorsoSocket = coreAssembly.GetComponentsInChildren<Socket>()
            .FirstOrDefault(s => s.acceptedType == PartType.Torso);

        if (coreTorsoSocket == null) { Debug.LogError("Error: Core no tiene socket de Torso."); return; }

        manager.SelectPartForSocket(coreTorsoSocket.socketName, selectedTorso); 
        assembler.RebuildRobot(); 

        // 3. Encontrar el objeto físico del Torso para iniciar la navegación ahí
        coreAssembly = assembler.currentRobotAssembly.transform;
        var refreshedSocket = coreAssembly.GetComponentsInChildren<Socket>()
            .FirstOrDefault(s => s.socketName == coreTorsoSocket.socketName);

        if (refreshedSocket == null || refreshedSocket.transform.childCount == 0) return;

        var instanciatedTorso = refreshedSocket.transform.GetChild(0).gameObject;
        
        // ESTABLECER EL CONTEXTO INICIAL: El Torso
        _currentContextRoot = instanciatedTorso;
        _navigationHistory.Clear(); // Estamos en la raíz de la personalización
        
        InitializeSocketMenu();
    }

    // =================================================================================
    // MENÚ DE SOCKETS (Navegación Dinámica)
    // =================================================================================

    public void InitializeSocketMenu()
    {
        ClearSelectionPanel();
        CheckIfRobotIsComplete();

        // BOTÓN DE RETROCESO INTELIGENTE
        if (_navigationHistory.Count > 0)
        {
            // Si hay historial, volvemos al padre anterior (Subir nivel)
            CreateButton("<< ATRÁS (Subir Nivel)", () => GoBackOneLevel());
        }
        else
        {
            // Si no hay historial, significa que estamos en el Torso, así que volvemos a seleccionar Torsos
            CreateButton("<< CAMBIAR TORSO", () => InitializeTorsoSelection());
        }

        // TÍTULO DEL CONTEXTO
        string contextName = _currentContextRoot.name.Replace("(Clone)", "");
        GameObject titleBtn = CreateButton($"[VISTA: {contextName}]", null);
        titleBtn.GetComponent<Button>().interactable = false;

        // BUSCAR SOCKETS EN EL CONTEXTO ACTUAL
        // Usamos GetComponentsInChildren<Socket> pero solo queremos los hijos DIRECTOS o lógicos de esta pieza.
        // Como los sockets son hijos directos en el prefab, esto funciona.
        Socket[] socketsInContext = _currentContextRoot.GetComponentsInChildren<Socket>();

        foreach (Socket socket in socketsInContext)
        {
            // Filtro: Evitar mostrar sockets que pertenecen a piezas NIETAS (hijos de hijos).
            // Solo queremos los sockets que pertenecen físicamente a _currentContextRoot.
            // La forma más simple es verificar si el padre del socket es el _currentContextRoot.
            if (socket.transform.parent != _currentContextRoot.transform) continue;

            string displayName = FormatSocketName(socket.socketName);
            string keyToCheck = socket.socketName; 
            
            bool isFilled = RobotPersistenceManager.Instance.SelectedParts.ContainsKey(keyToCheck);
            string status = isFilled ? "[OCUPADO]" : "[VACÍO]";
            
            // 1. Botón Principal: Seleccionar Pieza para este Socket
            string buttonText = $"{displayName} \n<size=70%>{status}</size>";
            CreateButton(buttonText, () => ShowPartSelectionForSocket(socket.socketName, socket.acceptedType));

            // 2. [NUEVO] Botón de Navegación (Drill-Down): "Entrar" en la pieza equipada
            if (isFilled)
            {
                // Buscar si la pieza equipada tiene sockets hijos
                Transform equippedPartTransform = socket.transform.GetChild(0); // Asumimos que la pieza está montada
                if (equippedPartTransform != null)
                {
                    Socket[] subSockets = equippedPartTransform.GetComponentsInChildren<Socket>();
                    if (subSockets.Length > 0)
                    {
                        // Crear un botón especial indentado o de otro color para entrar
                        string subMenuText = $"   > Editar {displayName}";
                        CreateButton(subMenuText, () => EnterSubLevel(equippedPartTransform.gameObject));
                    }
                }
            }
        }
    }

    // Función para "Entrar" en una pieza (Bajar nivel)
    void EnterSubLevel(GameObject newContextRoot)
    {
        _navigationHistory.Push(_currentContextRoot); // Guardar dónde estábamos
        _currentContextRoot = newContextRoot;         // Establecer nuevo contexto
        InitializeSocketMenu();                       // Refrescar menú
    }

    // Función para "Salir" de una pieza (Subir nivel)
    void GoBackOneLevel()
    {
        if (_navigationHistory.Count > 0)
        {
            _currentContextRoot = _navigationHistory.Pop(); // Recuperar el anterior
            InitializeSocketMenu();
        }
    }

    // =================================================================================
    // SELECCIÓN DE PIEZAS (Igual que antes, pero llama a InitializeSocketMenu al final)
    // =================================================================================

    void ShowPartSelectionForSocket(string socketName, PartType requiredType)
    {
        ClearSelectionPanel();
        var manager = RobotPersistenceManager.Instance;
        targetSocketName = socketName;

        _currentSocketDisplayName = FormatSocketName(socketName);
        GameObject titleButton = CreateButton($"[SLOT: {_currentSocketDisplayName}]", null); 
        titleButton.GetComponent<Button>().interactable = false;
        
        // Volver al menú del contexto actual
        CreateButton("<< CANCELAR", () => InitializeSocketMenu());
        
        string keyToSearch = targetSocketName;
        manager.SelectedParts.TryGetValue(keyToSearch, out RobotPartData currentlyEquippedPart);

        var parts = manager.GetAvailablePartsByFilter(requiredType);

        foreach (RobotPartData part in parts)
        {
            int availableCount = manager.GetPartCount(part.PartID);
            bool isCurrentlyEquipped = currentlyEquippedPart != null && currentlyEquippedPart.PartID == part.PartID;
            int actualAvailable = availableCount + (isCurrentlyEquipped ? 1 : 0);
            bool isInteractable = actualAvailable > 0;
            
            string countText = isInteractable ? $"[{actualAvailable}]" : "[AGOTADO]";
            string equippedStatus = isCurrentlyEquipped ? " (EQ)" : "";
            string btnText = $"{part.PartName} {countText}{equippedStatus}";

            GameObject buttonGO = CreateButton(btnText, () => AssignPartToSpecificSocket(part));
            Button btn = buttonGO.GetComponent<Button>();
            
            if (!isInteractable && !isCurrentlyEquipped)
            {
                 btn.interactable = false;
                 buttonGO.GetComponentInChildren<TMP_Text>().color = Color.gray;
            }
        }
    }

    void AssignPartToSpecificSocket(RobotPartData newPartData)
    {
        if (string.IsNullOrEmpty(targetSocketName)) return;

        var manager = RobotPersistenceManager.Instance;
        string newPartID = newPartData.PartID;
        string keyToUse = targetSocketName;
        
        manager.SelectedParts.TryGetValue(keyToUse, out RobotPartData oldPartData);

        // Lógica de Deselección
        if (oldPartData != null && oldPartData.PartID == newPartID)
        {
            manager.AddItemToInventory(newPartID, 1);
            manager.SelectedParts.Remove(keyToUse); 
            assembler.RebuildRobot();
            
            // IMPORTANTE: Al desequipar, debemos refrescar el contexto actual
            // porque si estábamos viendo los hijos de esta pieza, eso sería un error.
            // Por seguridad, si desequipamos algo, recargamos el menú actual.
            RefreshContextAfterChange();
            return; 
        }
        
        // Reembolso y Gasto
        if (oldPartData != null) manager.AddItemToInventory(oldPartData.PartID, 1);
        manager.RemoveItemFromInventory(newPartID, 1);
        
        manager.SelectPartForSocket(keyToUse, newPartData);

        assembler.RebuildRobot();
        
        // Refrescar menú para mostrar el nuevo botón de "Editar >" si aplica
        RefreshContextAfterChange();
    }

    void RefreshContextAfterChange()
    {
        // Pequeño truco: RebuildRobot destruye los objetos antiguos.
        // Nuestras referencias en _currentContextRoot podrían haberse perdido (MissingReferenceException).
        // Necesitamos re-encontrar el objeto _currentContextRoot en la nueva jerarquía ensamblada.
        
        // Si estábamos en el Torso (raíz de navegación), es fácil:
        if (_navigationHistory.Count == 0)
        {
            // Buscar el nuevo torso instanciado
             var coreAssembly = assembler.currentRobotAssembly.transform;
             var torsoSocket = coreAssembly.GetComponentsInChildren<Socket>().FirstOrDefault(s => s.acceptedType == PartType.Torso);
             if (torsoSocket != null && torsoSocket.transform.childCount > 0)
             {
                 _currentContextRoot = torsoSocket.transform.GetChild(0).gameObject;
             }
        }
        else
        {
            // SI estábamos en profundidad (dentro de un brazo), y cambiamos algo,
            // la referencia vieja se destruyó. Encontrar la nueva ruta es complejo.
            // POR SEGURIDAD Y SIMPLICIDAD: Si cambias una pieza, te devolvemos al nivel superior (Torso).
            // Esto evita crashes por referencias perdidas.
            _navigationHistory.Clear();
            var coreAssembly = assembler.currentRobotAssembly.transform;
             var torsoSocket = coreAssembly.GetComponentsInChildren<Socket>().FirstOrDefault(s => s.acceptedType == PartType.Torso);
             if (torsoSocket != null && torsoSocket.transform.childCount > 0)
             {
                 _currentContextRoot = torsoSocket.transform.GetChild(0).gameObject;
             }
        }
        
        InitializeSocketMenu();
    }

    // =================================================================================
    // UTILIDADES (Sin cambios mayores)
    // =================================================================================
    
    private void RefundAllEquippedChildren()
    {
        var manager = RobotPersistenceManager.Instance;
        List<string> keysToRefundAndRemove = new List<string>();

        foreach (var pair in manager.SelectedParts)
        {
            RobotPartData part = pair.Value;
            string key = pair.Key;
            if (part.PartType != PartType.Core && part.PartType != PartType.Torso)
            {
                manager.AddItemToInventory(part.PartID, 1);
                keysToRefundAndRemove.Add(key);
            }
        }
        foreach(string key in keysToRefundAndRemove) manager.SelectedParts.Remove(key);
    }
    
    private void CheckIfRobotIsComplete()
    {
        if (startGameButton == null) return;
        var manager = RobotPersistenceManager.Instance;
        bool hasCore = manager.SelectedCoreData != null;
        bool hasTorso = manager.SelectedParts.Any(kvp => kvp.Value.PartType == PartType.Torso); 
        startGameButton.interactable = hasCore && hasTorso;
    }

    GameObject CreateButton(string text, UnityEngine.Events.UnityAction action)
    {
        GameObject buttonGO = Instantiate(partButtonPrefab, partSelectionPanel);
        TMP_Text buttonTextTMP = buttonGO.GetComponentInChildren<TMP_Text>();
        if (buttonTextTMP != null) buttonTextTMP.text = text;
        else buttonGO.GetComponentInChildren<Text>().text = text;

        Button btn = buttonGO.GetComponent<Button>();
        if(action != null) btn.onClick.AddListener(action);
        return buttonGO;
    }

    void ClearSelectionPanel()
    {
        foreach (Transform child in partSelectionPanel) Destroy(child.gameObject);
    }

    string FormatSocketName(string rawName)
    {
        return rawName.Replace("Socket_", "").Replace("Arms", "Brazo").Replace("Torso", "Torso")
            .Replace("Head", "Cabeza").Replace("Legs", "Piernas").Replace("_L", " Izq.").Replace("_R", " Der.");
    }
}