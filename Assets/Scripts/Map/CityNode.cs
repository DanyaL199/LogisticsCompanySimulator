using UnityEngine;
using System.Collections.Generic;

public enum CityType { Industrial, Trade, Tourist }

// Тепер попит розділено на товари та людей!
[System.Serializable]
public class CityDemand
{
    public CityNode destination;
    public int currentCargo;
    public int maxCargo;
    public int currentPassengers;
    public int maxPassengers;
    [Range(0.01f, 0.05f)]
    public float annualGrowth;
}

public class CityNode : MonoBehaviour
{
    [Header("Інформація про місто")]
    public string cityName;
    public CityType cityType;

    [Header("Рівень (1=Мале, 2=Середнє, 3=Велике)")]
    [Range(1, 3)]
    public int activityLevel = 1;

    [Header("Персонал")]
    public int mechanics = 0;
    public int maxMechanics = 10;

    [Header("Майстерня")]
    public bool hasWorkshop = false;
    public int repairSlots = 2; // Зі старту 2 слоти
    public const int MAX_REPAIR_SLOTS = 5;

    [Header("Попит (Автоматично генерується)")]
    public List<CityDemand> demands = new List<CityDemand>();

    private void Start()
    {
        float scale = 0.4f + activityLevel * 0.15f;
        transform.localScale = new Vector3(scale, scale, 1f);

        CreateLabel();

        // Генеруємо рандомний попит між містами після мікро-затримки (щоб всі міста створились)
        Invoke(nameof(GenerateRandomDemands), 0.1f);

        if (GameTimeManager.Instance != null)
        {
            GameTimeManager.Instance.OnDayChanged += OnDayChanged;
            GameTimeManager.Instance.OnYearChanged += OnYearChanged;
            GameTimeManager.Instance.OnHourChanged += OnHourChanged;
        }
    }

    private void OnDestroy()
    {
        if (GameTimeManager.Instance != null)
        {
            GameTimeManager.Instance.OnDayChanged -= OnDayChanged;
            GameTimeManager.Instance.OnYearChanged -= OnYearChanged;
            GameTimeManager.Instance.OnHourChanged -= OnHourChanged;
        }
    }

    private void GenerateRandomDemands()
    {
        demands.Clear();
        var allCities = FindObjectsByType<CityNode>(FindObjectsSortMode.None);

        foreach (var c in allCities)
        {
            if (c == this) continue; // Немає попиту везти до самого себе

            CityDemand d = new CityDemand();
            d.destination = c;
            d.annualGrowth = Random.Range(0.01f, 0.05f);

            // Чим більший рівень обох міст, тим більший загальний попит між ними
            int baseTotalDemand = (activityLevel * 45) + (c.activityLevel * 45) + Random.Range(10, 50);

            // Визначаємо коефіцієнт (частку вантажів) залежно від типу НАШОГО міста
            float cargoRatio = 0.5f;
            if (cityType == CityType.Industrial) cargoRatio = 0.8f;      // 80% Ван. : 20% Пас.
            else if (cityType == CityType.Tourist) cargoRatio = 0.2f;    // 20% Ван. : 80% Пас.
            else if (cityType == CityType.Trade) cargoRatio = 0.5f;      // 50% Ван. : 50% Пас.

            d.maxCargo = Mathf.RoundToInt(baseTotalDemand * cargoRatio);
            d.maxPassengers = Mathf.RoundToInt(baseTotalDemand * (1f - cargoRatio));

            // Даємо трохи початкового накопичення (від 30% до 80% від макс)
            d.currentCargo = Mathf.RoundToInt(d.maxCargo * Random.Range(0.3f, 0.8f));
            d.currentPassengers = Mathf.RoundToInt(d.maxPassengers * Random.Range(0.3f, 0.8f));

            demands.Add(d);
        }
    }

    private void OnDayChanged(GameDate date)
    {
        // Щодня попит відновлюється на 30%
        foreach (var d in demands)
        {
            int replenishC = Mathf.CeilToInt(d.maxCargo * 0.3f);
            d.currentCargo = Mathf.Min(d.maxCargo, d.currentCargo + replenishC);

            int replenishP = Mathf.CeilToInt(d.maxPassengers * 0.3f);
            d.currentPassengers = Mathf.Min(d.maxPassengers, d.currentPassengers + replenishP);
        }
    }

    private void OnYearChanged(GameDate date)
    {
        // Щороку місто росте 
        foreach (var d in demands)
        {
            float growth = Random.Range(0.01f, d.annualGrowth);
            d.maxCargo = Mathf.RoundToInt(d.maxCargo * (1f + growth));
            d.maxPassengers = Mathf.RoundToInt(d.maxPassengers * (1f + growth));
        }
    }

    private void OnHourChanged(GameDate date)
    {
        if (!hasWorkshop) return;

        var allVehicles = FindObjectsByType<VehicleController>(FindObjectsSortMode.None);
        List<VehicleController> repairingHere = new List<VehicleController>();

        foreach (var v in allVehicles)
        {
            if (v.currentCity == this && v.status == VehicleStatus.Repairing)
                repairingHere.Add(v);
        }

        float repairAmountPerHour = (5f + (mechanics * 10f)) / 24f;
        foreach (var v in repairingHere)
        {
            v.condition += repairAmountPerHour;
            if (v.condition >= 100f) { v.condition = 100f; v.FinishRepairExternal(); }
        }
    }

    // Тепер ми перевіряємо, який тип ми хочемо забрати!
    public int TakeUnits(CityNode destination, int capacity, VehicleType type)
    {
        var d = demands.Find(x => x.destination == destination);
        if (d == null) return 0;

        if (type == VehicleType.Cargo)
        {
            int taken = Mathf.Min(d.currentCargo, capacity);
            d.currentCargo -= taken;
            return taken;
        }
        else
        {
            int taken = Mathf.Min(d.currentPassengers, capacity);
            d.currentPassengers -= taken;
            return taken;
        }
    }

    // Який попит для конкретного типу?
    public int GetDemandTo(CityNode destination, VehicleType type)
    {
        var d = demands.Find(x => x.destination == destination);
        if (d == null) return 0;
        return type == VehicleType.Cargo ? d.currentCargo : d.currentPassengers;
    }

    public void BuildWorkshop()
    {
        if (FinanceManager.Instance.CanAfford(25000f))
        {
            FinanceManager.Instance.AddExpense(25000f);
            hasWorkshop = true;
        }
    }

    public void HireMechanic()
    {
        if (mechanics >= maxMechanics) return;
        int hireCost = 1000;
        if (FinanceManager.Instance.CanAfford(hireCost))
        {
            FinanceManager.Instance.AddExpense(hireCost);
            mechanics++;
        }
    }

    public void UpgradeWorkshop()
    {
        if (repairSlots >= MAX_REPAIR_SLOTS) return;
        float upgradeCost = 5000f;
        if (FinanceManager.Instance.CanAfford(upgradeCost))
        {
            FinanceManager.Instance.AddExpense(upgradeCost);
            repairSlots++;
        }
    }

    public bool GeneratesIncomeFor(VehicleType vt)
    {
        // Для базових споживань (їзда пустим) зберігаємо стару логіку профільності:
        if (cityType == CityType.Industrial) return vt == VehicleType.Cargo;
        if (cityType == CityType.Tourist) return vt == VehicleType.Passenger;
        return true;
    }

    public int GetDailyDemandLimit() => activityLevel == 3 ? 300 : activityLevel == 2 ? 150 : 60;

    private void CreateLabel()
    {
        if (transform.Find("CityLabel") != null) return;
        var obj = new GameObject("CityLabel");
        obj.transform.SetParent(transform);
        obj.transform.localPosition = new Vector3(0f, 0.9f, 0f);
        obj.transform.localScale = new Vector3(0.3f, 0.3f, 1f);
        var tmp = obj.AddComponent<TMPro.TextMeshPro>();
        tmp.text = cityName;
        tmp.fontSize = 8f;
        tmp.alignment = TMPro.TextAlignmentOptions.Center;
        tmp.color = Color.white;
        tmp.fontStyle = TMPro.FontStyles.Bold;
        tmp.sortingOrder = 10;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = cityType switch
        {
            CityType.Industrial => Color.yellow,
            CityType.Tourist => Color.green,
            CityType.Trade => Color.cyan,
            _ => Color.white
        };
        Gizmos.DrawWireSphere(transform.position, 0.4f);
    }
}