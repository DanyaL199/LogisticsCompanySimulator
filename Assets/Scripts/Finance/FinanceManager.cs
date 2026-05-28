using UnityEngine;
using System.Collections.Generic;

public class FinanceManager : MonoBehaviour
{
    public static FinanceManager Instance { get; private set; }

    [Header("Баланс")]
    public float startingBalance = 120000f;
    public float balance;

    [Header("Статистика")]
    public float totalIncome;
    public float totalExpenses;

    [Header("Активи компнаї")]
    public float vehicleAssetsValue;
    public float roadAssetsValue;

    private List<VehicleController> vehicles = new List<VehicleController>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
        balance = startingBalance;
    }

    private void Start()
    {
        if (GameTimeManager.Instance != null)
        {
            GameTimeManager.Instance.OnDayChanged += OnNewDay;
            GameTimeManager.Instance.OnMonthChanged += OnNewMonth;
        }
    }

    public void OnNewDay(GameDate date)
    {
        foreach (var v in vehicles)
        {
            if (v == null) continue;
            // Щоденне ТО (незначне)
            AddExpense(v.vehicleData.maintenanceCost / 30f);
        }
    }

    public void OnNewMonth(GameDate date)
    {
        // Зарплати водіям
        if (WageManager.Instance != null)
        {
            float wages = WageManager.Instance.GetTotalMonthlyWageCost(vehicles.Count);
            AddExpense(wages);
        }

        // Обслуговування гаражів та механіків
        float facilitiesCost = 0f;
        var allCities = FindObjectsByType<CityNode>(FindObjectsSortMode.None);
        foreach (var c in allCities)
        {
            if (c.hasGarage)
            {
                facilitiesCost += 500f; // Технічне утримання гаража
                facilitiesCost += c.mechanics * 500f; // Зарплата механікам
            }
        }

        if (facilitiesCost > 0)
        {
            AddExpense(facilitiesCost);
            Debug.Log($"[{date.ToShortString()}] Витрати на інфраструктуру: {facilitiesCost:F0} у.о.");
        }
    }

    public void AddIncome(float amount)
    {
        balance += amount;
        totalIncome += amount;
    }

    public void AddExpense(float amount)
    {
        balance -= amount;
        totalExpenses += amount;
    }

    public bool CanAfford(float amount) => balance >= amount;

    public void RegisterVehicle(VehicleController v)
    {
        if (!vehicles.Contains(v))
        {
            vehicles.Add(v);
            vehicleAssetsValue += v.vehicleData.purchaseCost;
        }
    }

    public void UnregisterVehicle(VehicleController v)
    {
        if (vehicles.Remove(v))
            vehicleAssetsValue -= v.vehicleData.purchaseCost;
    }

    public void RegisterRoadValue(float cost) => roadAssetsValue += cost;
    public void UnregisterRoadValue(float cost) => roadAssetsValue -= cost;
}