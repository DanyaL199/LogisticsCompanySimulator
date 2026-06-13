using UnityEngine;
using System.Collections.Generic;

public enum CityType { Industrial, Trade, Tourist }


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
    public int mechanics = 1;
    public int maxMechanics = 10;

    [Header("Майстерня")]
    public bool hasWorkshop = false;
    public int repairSlots = 2; 
    public const int MAX_REPAIR_SLOTS = 5;

    [Header("Попит (Автоматично генерується)")]
    public List<CityDemand> demands = new List<CityDemand>();

    private void Start()
    {
        float scale = 0.4f + activityLevel * 0.15f;
        transform.localScale = new Vector3(scale, scale, 1f);

        CreateLabel();

        var sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
         
            sr.sortingLayerName = "Roads";
            sr.sortingOrder = 30;
        }


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
            if (c == this) continue; 

            CityDemand d = new CityDemand();
            d.destination = c;
            d.annualGrowth = Random.Range(0.01f, 0.05f);


            d.currentCargo = Random.Range(40, 201);
            d.currentPassengers = Random.Range(40, 201);


            d.maxCargo = d.currentCargo * 5;
            d.maxPassengers = d.currentPassengers * 5;

            demands.Add(d);
        }
    }

    private void OnDayChanged(GameDate date)
    {

        foreach (var d in demands)
        {
            int replenishC = Mathf.CeilToInt(d.maxCargo * 0.10f);
            d.currentCargo = Mathf.Min(d.maxCargo, d.currentCargo + replenishC);

            int replenishP = Mathf.CeilToInt(d.maxPassengers * 0.10f);
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

        float repairAmountPerHour = Mathf.Max(25f, mechanics * 40f) / 24f; 
        foreach (var v in repairingHere)
        {
            v.condition += repairAmountPerHour;
            if (v.condition >= 100f) { v.condition = 100f; v.FinishRepairExternal(); }
        }
    }


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
        tmp.fontSize = 12f;
        tmp.alignment = TMPro.TextAlignmentOptions.Center;
        tmp.color = Color.white;
        tmp.fontStyle = TMPro.FontStyles.Bold;
        tmp.GetComponent<MeshRenderer>().sortingLayerName = "Roads";
        tmp.GetComponent<MeshRenderer>().sortingOrder = 31;
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