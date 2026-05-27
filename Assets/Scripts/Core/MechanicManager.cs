using UnityEngine;

public class MechanicManager : MonoBehaviour
{
    public static MechanicManager Instance;

    [Header("Налаштування автопарку")]
    public float globalRepairThreshold = 30f; // Поріг ремонту за замовчуванням
    public float globalWearSpeed = 0.5f; // Швидкість зносу транспорту під час руху

    private void Awake()
    {
        Instance = this;
    }

    public bool NeedsRepair(VehicleController vehicle)
    {
        float threshold = vehicle.useIndividualRepairThreshold ? vehicle.individualRepairThreshold : globalRepairThreshold;
        return vehicle.currentCondition <= threshold;
    }
}