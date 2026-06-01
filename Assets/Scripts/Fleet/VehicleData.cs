using UnityEngine;

public enum VehicleType { Cargo, Passenger }

[CreateAssetMenu(fileName = "NewVehicle", menuName = "Logistics/Vehicle Data")]
public class VehicleData : ScriptableObject
{
    [Header("Ідентифікація")]
    public string vehicleName;
    public VehicleType vehicleType;

    [Header("Характеристики")]
    public int maxCapacity;        // Місткість (вантаж або пасажири)
    public float maxSpeedKmh;      // Швидкість (км/год) для магазину
    public float durability;       // Міцність (напряму впливає на те, як повільно ламається авто)
    public Sprite icon;            // Іконка для UI та відображення на мапі

    [Header("Економіка")]
    public float purchaseCost;     // Вартість придбання (у.о.)
    public float fuelPer100km;     // Витрата пального (л/100км)
    public float maintenanceCost;  // Собівартість поточного обслуговування (або базова вартість ТО)
}