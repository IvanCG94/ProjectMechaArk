using UnityEngine;
using UnityEngine.UI; 
using UnityEngine.EventSystems; 
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
    private Stack<GameObject> _navigationHistory = new Stack<GameObject>();
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
        _navigationHistory.Clear(); 
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
        
        // 1. Lógica de Reembolso
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

        // 3. Establecer Contexto Inicial
        coreAssembly = assembler.currentRobotAssembly.transform;
        var refreshedSocket = coreAssembly.GetComponentsInChildren<Socket>()
            .FirstOrDefault(s => s.socketName == coreTorsoSocket.socketName);

        if (refreshedSocket == null || refreshedSocket.transform.childCount == 0) return;

        // [CORRECCIÓN] Usar la función segura para obtener el modelo, ignorando el indicador visual
        GameObject instanciatedTorso = GetMountedModel(refreshedSocket.transform);
        
        if (instanciatedTorso != null)
        {
            _currentContextRoot = instanciatedTorso;
            _navigationHistory.Clear(); 
            InitializeSocketMenu();
        }
    }

    // =================================================================================
    // MENÚ DE SOCKETS (Con Navegación Profunda y Highlight Visual)
    // =================================================================================

    public void InitializeSocketMenu()
    {
        ClearSelectionPanel();
        CheckIfRobotIsComplete();

        // BOTÓN DE RETROCESO
        if (_navigationHistory.Count > 0)
        {
            CreateButton("<< ATRÁS (Subir Nivel)", () => GoBackOneLevel());
        }
        else
        {
            CreateButton("<< CAMBIAR TORSO", () => InitializeTorsoSelection());
        }

        // TÍTULO DEL CONTEXTO
        string contextName = _currentContextRoot.name.Replace("(Clone)", "");
        GameObject titleBtn = CreateButton($"[VISTA: {contextName}]", null);
        titleBtn.GetComponent<Button>().interactable = false;

        // BUSCAR SOCKETS
        Socket[] socketsInContext = _currentContextRoot.GetComponentsInChildren<Socket>();

        foreach (Socket socket in socketsInContext)
        {
            if (socket.transform.parent != _currentContextRoot.transform) continue;

            string displayName = FormatSocketName(socket.socketName);
            string keyToCheck = socket.socketName; 
            
            bool isFilled = RobotPersistenceManager.Instance.SelectedParts.ContainsKey(keyToCheck);
            string status = isFilled ? "[OCUPADO]" : "[VACÍO]";
            
            // 1. Botón Principal: Seleccionar Pieza
            string buttonText = $"{displayName} \n<size=70%>{status}</size>";
            GameObject socketBtnObj = CreateButton(buttonText, () => ShowPartSelectionForSocket(socket.socketName, socket.acceptedType));
            
            AddHoverEventsToButton(socketBtnObj, socket);

            // 2. Botón de Navegación (Drill-Down)
            if (isFilled)
            {
                // [CORRECCIÓN] Buscar el modelo montado ignorando el indicador
                GameObject equippedPartObj = GetMountedModel(socket.transform);
                
                if (equippedPartObj != null)
                {
                    Socket[] subSockets = equippedPartObj.GetComponentsInChildren<Socket>();
                    if (subSockets.Length > 0)
                    {
                        string subMenuText = $"   > Editar {displayName}";
                        GameObject subBtnObj = CreateButton(subMenuText, () => EnterSubLevel(equippedPartObj));
                        
                        AddHoverEventsToButton(subBtnObj, socket);
                    }
                }
            }
        }
    }

    // FUNCIÓN AUXILIAR PARA EL HOVER
    private void AddHoverEventsToButton(GameObject buttonObj, Socket targetSocket)
    {
        EventTrigger trigger = buttonObj.GetComponent<EventTrigger>();
        if (trigger == null) trigger = buttonObj.AddComponent<EventTrigger>();

        EventTrigger.Entry entryEnter = new EventTrigger.Entry();
        entryEnter.eventID = EventTriggerType.PointerEnter;
        entryEnter.callback.AddListener((data) => { targetSocket.ToggleHighlight(true); });
        trigger.triggers.Add(entryEnter);

        EventTrigger.Entry entryExit = new EventTrigger.Entry();
        entryExit.eventID = EventTriggerType.PointerExit;
        entryExit.callback.AddListener((data) => { targetSocket.ToggleHighlight(false); });
        trigger.triggers.Add(entryExit);
    }

    void EnterSubLevel(GameObject newContextRoot)
    {
        _navigationHistory.Push(_currentContextRoot); 
        _currentContextRoot = newContextRoot;         
        InitializeSocketMenu();                       
    }

    void GoBackOneLevel()
    {
        if (_navigationHistory.Count > 0)
        {
            _currentContextRoot = _navigationHistory.Pop(); 
            InitializeSocketMenu();
        }
    }

    // =================================================================================
    // SELECCIÓN DE PIEZAS
    // =================================================================================

    void ShowPartSelectionForSocket(string socketName, PartType requiredType)
    {
        ClearSelectionPanel();
        var manager = RobotPersistenceManager.Instance;
        targetSocketName = socketName;

        _currentSocketDisplayName = FormatSocketName(socketName);
        GameObject titleButton = CreateButton($"[SLOT: {_currentSocketDisplayName}]", null); 
        titleButton.GetComponent<Button>().interactable = false;
        
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

        // Deselección
        if (oldPartData != null && oldPartData.PartID == newPartID)
        {
            manager.AddItemToInventory(newPartID, 1);
            manager.SelectedParts.Remove(keyToUse); 
            assembler.RebuildRobot();
            RefreshContextAfterChange();
            return; 
        }
        
        // Reembolso y Gasto
        if (oldPartData != null) manager.AddItemToInventory(oldPartData.PartID, 1);
        manager.RemoveItemFromInventory(newPartID, 1);
        
        manager.SelectPartForSocket(keyToUse, newPartData);

        assembler.RebuildRobot();
        RefreshContextAfterChange();
    }

    void RefreshContextAfterChange()
    {
        // Al reconstruir, perdemos referencias. Volvemos al Torso por seguridad.
        _navigationHistory.Clear();
        var coreAssembly = assembler.currentRobotAssembly.transform;
        var torsoSocket = coreAssembly.GetComponentsInChildren<Socket>().FirstOrDefault(s => s.acceptedType == PartType.Torso);
        if (torsoSocket != null && torsoSocket.transform.childCount > 0)
        {
             // [CORRECCIÓN] Usar la función segura
             GameObject torsoModel = GetMountedModel(torsoSocket.transform);
             if (torsoModel != null)
             {
                 _currentContextRoot = torsoModel;
             }
        }
        InitializeSocketMenu();
    }

    // =================================================================================
    // UTILIDADES
    // =================================================================================
    
    // [NUEVA FUNCIÓN CRÍTICA] Busca el modelo real y evita el "Hover_Indicator"
    private GameObject GetMountedModel(Transform socketTransform)
    {
        foreach (Transform child in socketTransform)
        {
            if (child.name != "Hover_Indicator")
            {
                return child.gameObject;
            }
        }
        return null;
    }
    
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