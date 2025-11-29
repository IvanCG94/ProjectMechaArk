using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Linq;

[System.Serializable]
public class InventoryEntry 
{
    public string PartID;
    public int Count;    
}

public class RobotPersistenceManager : MonoBehaviour
{
    public static RobotPersistenceManager Instance { get; private set; }

    // --- BASE DE DATOS ---
    [Header("Base de Datos Global")]
    [SerializeField] private List<RobotPartData> _allPartsData = new List<RobotPartData>();
    private Dictionary<string, RobotPartData> _partIDToDataMap = new Dictionary<string, RobotPartData>();

    // --- INVENTARIO ---
    [Header("Inventario Inicial (Llenar en el Inspector)")]
    [SerializeField] private List<InventoryEntry> _initialInventoryList = new List<InventoryEntry>();
    private Dictionary<string, int> _playerInventory = new Dictionary<string, int>(); 
    public IReadOnlyDictionary<string, int> PlayerInventory => _playerInventory;

    // --- SELECCIONES ACTIVAS ---
    public Dictionary<string, RobotPartData> SelectedParts { get; private set; } = new Dictionary<string, RobotPartData>();
    public RobotPartData SelectedCoreData { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeDataMaps(); 
            InitializeInventory(); 
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InitializeDataMaps()
    {
        _partIDToDataMap.Clear();
        foreach (var part in _allPartsData)
        {
            if (!string.IsNullOrEmpty(part.PartID))
            {
                _partIDToDataMap.TryAdd(part.PartID, part);
            }
        }
    }
    
    private void InitializeInventory()
    {
        _playerInventory.Clear();
        foreach (var entry in _initialInventoryList)
        {
            if (!string.IsNullOrEmpty(entry.PartID) && !_playerInventory.ContainsKey(entry.PartID))
            {
                _playerInventory.Add(entry.PartID, entry.Count);
            }
        }
    }

    // --- API DE INVENTARIO Y SELECCION ---

    public int GetPartCount(string partID)
    {
        _playerInventory.TryGetValue(partID, out int count);
        return count;
    }

    public void AddItemToInventory(string partID, int amount)
    {
        if (_playerInventory.ContainsKey(partID))
            _playerInventory[partID] += amount;
        else
            _playerInventory.Add(partID, amount);
    }

    public void RemoveItemFromInventory(string partID, int amount)
    {
        if (_playerInventory.ContainsKey(partID))
        {
            _playerInventory[partID] -= amount;
            if (_playerInventory[partID] < 0) _playerInventory[partID] = 0;
        }
    }

    public List<RobotPartData> GetAvailablePartsByFilter(PartType filterType)
    {
        return _allPartsData.Where(part => part.PartType == filterType).ToList();
    }
    
    // [LOGICA DE INDEPENDENCIA TOTAL] Guarda siempre con el nombre del socket específico.
    public void SelectPartForSocket(string socketName, RobotPartData partData)
    {
        if (partData.PartType == PartType.Core)
        {
            SelectedCoreData = partData;
        }

        SelectedParts[socketName] = partData; 
    }

    public void StartGame()
    {
        if (SelectedCoreData == null)
        {
            Debug.LogError("No se puede iniciar sin un Core.");
            return;
        }
        SceneManager.LoadScene("Scene_Game"); 
    }
}