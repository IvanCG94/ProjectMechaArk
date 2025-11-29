using UnityEngine;

[CreateAssetMenu(fileName = "NewPartData", menuName = "Robot/Part Data")]
public class RobotPartData : ScriptableObject
{
    [Header("Identificación")]
    [field: SerializeField] public string PartName { get; private set; } = "Pieza Nueva";
    [field: SerializeField] public PartType PartType { get; private set; } = PartType.Arms;
    [field: SerializeField] public string PartID { get; private set; }
    [field: SerializeField] public Tier PartTier { get; private set; } = Tier.T1;
    
    [Header("Modelo")]
    [field: SerializeField] public GameObject PartPrefab { get; private set; }

    [Header("Restricciones")]
    [field: SerializeField] public Tier MaxAllowedTier { get; private set; } = Tier.T3; 

    [Header("Estadísticas")]
    [field: SerializeField] public float HealthBonus { get; private set; } = 100f;
    [field: SerializeField] public float EnergyConsumption { get; private set; } = 5f;
    [field: SerializeField] public float Weight { get; private set; } = 5f;
}