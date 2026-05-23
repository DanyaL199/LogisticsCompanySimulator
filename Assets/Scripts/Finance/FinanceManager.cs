using UnityEngine;
using System.Collections.Generic;

public class FinanceManager : MonoBehaviour
{
    public static FinanceManager Instance { get; private set; }

    [Header("Початковий баланс")]
    public float startingBalance = 120000f;

    [Header("Поточний баланс")]
    public float balance;

    [Header("Статистика")]
    public float totalIncome;
    public float totalExpenses;

    [Header("Активи компанії")]
    public float vehicleAssetsValue;
    public float roadAssetsValue;

    public float CompanyValue => balance + vehicleAssetsValue + roadAssetsValue;

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

    private void OnDestroy()
    {
        if (GameTimeManager.Instance != null)
        {
            GameTimeManager.Instance.OnDayChanged -= OnNewDay;
            GameTimeManager.Instance.OnMonthChanged -= OnNewMonth;
        }
    }

    public void OnNewDay(GameDate date)
    {
        foreach (var v in vehicles)
        {
            if (v == null) continue;
            AddExpense(v.vehicleData.maintenanceCost / 30f);
        }
    }

    public void OnNewMonth(GameDate date)
    {
        if (WageManager.Instance != null)
        {
            float wages = WageManager.Instance
                .GetTotalMonthlyWageCost(vehicles.Count);
            AddExpense(wages);
            Debug.Log($"[{date.ToShortString()}] Зарплати: {wages:F0} у.о. | Баланс: {balance:F0} у.о.");
        }

        if (balance < 0f)
            Debug.LogWarning("УВАГА: Баланс від'ємний!");
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
    public float GetProfit() => totalIncome - totalExpenses;

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