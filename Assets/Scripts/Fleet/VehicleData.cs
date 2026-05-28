using UnityEngine;

public enum VehicleType { Cargo, Passenger }

[CreateAssetMenu(fileName = "NewVehicle",
                 menuName = "Logistics/Vehicle Data")]
public class VehicleData : ScriptableObject
{
    [Header("Ідентифікація")]
    public string vehicleName;
    public VehicleType vehicleType;

    [Header("Характеристики")]
    public int maxCapacity;        // одиниці вантажу або кількість пасажирів
    public float maxSpeedKmh;      // обмежується типом дороги
    public Sprite icon;            // іконка для UI
    [Header("Економіка")]
    public float purchaseCost;     // вартість придбання
    public float fuelPer100km;     // витрата пального
    public float maintenanceCost;  // вартість ТО (базова за одиницю ремонту)
}