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

    [Header("Estado Interno UI")]
    private List<Socket> currentTorsoSockets = new List<Socket>();
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
    // NAVEGACIÓN Y FLUJO PRINCIPAL
    // =================================================================================

    public void InitializeCoreSelection()
    {
        ClearSelectionPanel();
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
        
        // 1. OBTENER EL TORSO VIEJO (para lógica de reembolso)
        manager.SelectedParts.TryGetValue(manager.SelectedCoreData.PartType.ToString(), out RobotPartData oldCoreData);
        string torsoKey = coreAssembly.GetComponentsInChildren<Socket>()
            .FirstOrDefault(s => s.acceptedType == PartType.Torso)?.socketName ?? "Socket_Torso";
        manager.SelectedParts.TryGetValue(torsoKey, out RobotPartData oldTorsoData);

        // 2. [LÓGICA DE REEMBOLSO EN UI] Si el Torso viejo existe Y es diferente al nuevo
        if (oldTorsoData != null && oldTorsoData.PartID != selectedTorso.PartID)
        {
            // Ejecutar la limpieza de Brazos, Cabezas, Piernas, etc.
            RefundAllEquippedChildren();
        }
        
        // 3. CONTINUAR CON EL MONTAJE DEL NUEVO TORSO
        var coreTorsoSocket = coreAssembly.GetComponentsInChildren<Socket>()
            .FirstOrDefault(s => s.acceptedType == PartType.Torso);

        if (coreTorsoSocket == null) { Debug.LogError("Error: Core no tiene socket de Torso."); return; }

        manager.SelectPartForSocket(coreTorsoSocket.socketName, selectedTorso); 
        assembler.RebuildRobot(); 

        coreAssembly = assembler.currentRobotAssembly.transform;
        var refreshedSocket = coreAssembly.GetComponentsInChildren<Socket>()
            .FirstOrDefault(s => s.socketName == coreTorsoSocket.socketName);

        if (refreshedSocket == null || refreshedSocket.transform.childCount == 0) return;

        var instanciatedTorso = refreshedSocket.transform.GetChild(0).gameObject;
        currentTorsoSockets = new List<Socket>(instanciatedTorso.GetComponentsInChildren<Socket>());
        
        InitializeSocketMenu();
    }

    public void InitializeSocketMenu()
    {
        ClearSelectionPanel();
        CheckIfRobotIsComplete();

        CreateButton("<< VOLVER AL CUERPO", () => InitializeTorsoSelection());

        foreach (Socket socket in currentTorsoSockets)
        {
            string displayName = FormatSocketName(socket.socketName);
            
            string keyToCheck = socket.socketName; 
            
            bool isFilled = RobotPersistenceManager.Instance.SelectedParts.ContainsKey(keyToCheck);
            string status = isFilled ? "[LISTO]" : "[VACÍO]";
            
            string buttonText = $"{displayName} \n<size=70%>{status}</size>";

            CreateButton(buttonText, () => ShowPartSelectionForSocket(socket.socketName, socket.acceptedType));
        }
    }

    // --- SELECCIÓN CON INVENTARIO Y NOMBRE DE SOCKET ---
    void ShowPartSelectionForSocket(string socketName, PartType requiredType)
    {
        ClearSelectionPanel();
        var manager = RobotPersistenceManager.Instance;
        targetSocketName = socketName;

        _currentSocketDisplayName = FormatSocketName(socketName);
        GameObject titleButton = CreateButton($"[EDITANDO: {_currentSocketDisplayName}]", null); 
        titleButton.GetComponent<Button>().interactable = false;
        
        CreateButton("<< VOLVER AL CUERPO", () => InitializeSocketMenu());
        
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

        // 1. DESELECCIÓN (UN-EQUIP)
        if (oldPartData != null && oldPartData.PartID == newPartID)
        {
            manager.AddItemToInventory(newPartID, 1);
            manager.SelectedParts.Remove(keyToUse); 
            
            assembler.RebuildRobot();
            InitializeSocketMenu();
            return; 
        }
        
        // 2. REEMBOLSO DE VIEJO Y GASTO DE NUEVO
        if (oldPartData != null)
        {
            manager.AddItemToInventory(oldPartData.PartID, 1);
        }
        
        manager.RemoveItemFromInventory(newPartID, 1);
        
        // 3. Guardar la nueva elección
        manager.SelectPartForSocket(keyToUse, newPartData);

        assembler.RebuildRobot();
        InitializeSocketMenu();
    }
    
    // =================================================================================
    // LÓGICA DE REEMBOLSO (MOVIMIENTO DEL ENSAMBLADOR A LA UI)
    // =================================================================================
    
    /// <summary>
    /// Devuelve al inventario todas las piezas secundarias conectadas al torso actual.
    /// Se llama cuando el usuario va a cambiar el Torso por uno diferente.
    /// </summary>
    private void RefundAllEquippedChildren()
    {
        var manager = RobotPersistenceManager.Instance;
        
        // Obtenemos una lista de las claves a eliminar (Brazos, Cabeza, Piernas, Accesorios)
        List<string> keysToRefundAndRemove = new List<string>();

        foreach (var pair in manager.SelectedParts)
        {
            RobotPartData part = pair.Value;
            string key = pair.Key;

            // Buscamos todas las piezas que NO son Core ni Torso.
            if (part.PartType != PartType.Core && part.PartType != PartType.Torso)
            {
                // Reembolsamos la pieza al inventario
                manager.AddItemToInventory(part.PartID, 1);
                keysToRefundAndRemove.Add(key);
            }
        }
        
        // Eliminamos las piezas del diccionario SelectedParts
        foreach(string key in keysToRefundAndRemove)
        {
            manager.SelectedParts.Remove(key);
        }
    }

    // =================================================================================
    // UTILIDADES
    // =================================================================================
    
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
        if (buttonTextTMP != null)
        {
            buttonTextTMP.text = text;
        }
        else
        {
            Text buttonText = buttonGO.GetComponentInChildren<Text>();
            if (buttonText != null)
            {
                 buttonText.text = text;
            }
        }

        Button btn = buttonGO.GetComponent<Button>();
        if(action != null)
        {
            btn.onClick.AddListener(action);
        }
        
        return buttonGO;
    }

    void ClearSelectionPanel()
    {
        foreach (Transform child in partSelectionPanel)
        {
            Destroy(child.gameObject);
        }
    }

    string FormatSocketName(string rawName)
    {
        return rawName
            .Replace("Socket_", "")
            .Replace("Arms", "Brazo")
            .Replace("Torso", "Torso")
            .Replace("Head", "Cabeza")
            .Replace("Legs", "Piernas")
            .Replace("_L", " Izq.")
            .Replace("_R", " Der.");
    }
}