using UnityEngine;

// [CreateAssetMenu] permite crear el archivo de datos desde el menú de Unity.
[CreateAssetMenu(fileName = "New Part", menuName = "Robot/Part Data")]
public class RobotPartData : ScriptableObject // ¡HEREDA DE SCRIPTABLEOBJECT!
{
    [Header("Identificación")]
    public string partName = "Brazo_T1_Alpha";
    public PartType partType = PartType.Arm; 
    public Tier partTier = Tier.T1;        
    
    [Header("Modelo")]
    public GameObject partPrefab; 

    [Header("Restricciones de Núcleo")]
    // Solo es relevante si partType es Core
    public Tier maxAllowedTier = Tier.T1;
    
    [Header("Estadísticas")]
    public float healthBonus = 100f;
    public float energyConsumption = 5f;
    public float weight = 5f;
}