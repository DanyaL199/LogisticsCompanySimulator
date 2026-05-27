using UnityEngine;
using System.Collections.Generic;

public enum CityType { Industrial, Trade, Tourist }

[System.Serializable]
public class CityDemand
{
    public CityNode destination;
    public int currentUnits;  // скільки зараз чекає
    public int maxUnits;      // максимум (ліміт)
    [Range(0.01f, 0.05f)]
    public float annualGrowth;  // 1–5% на рік
}

public class CityNode : MonoBehaviour
{
    [Header("Інформація про місто")]
    public string cityName;
    public CityType cityType;

    [Header("Рівень (1=Мале, 2=Середнє, 3=Велике)")]
    [Range(1, 3)]
    public int activityLevel = 1;

    [Header("Попит до інших міст")]
    public List<CityDemand> demands = new List<CityDemand>();
    [Header("Garage System")]
    // Чи є в цьому місті гараж?
    public bool hasGarage = false;

    // Список транспорту, який зараз стоїть на ремонті в цьому місті
    public List<VehicleController> vehiclesInGarage = new List<VehicleController>();

    private void Start()
    {
        float scale = 0.4f + activityLevel * 0.15f;
        transform.localScale = new Vector3(scale, scale, 1f);

        CreateLabel();

        if (GameTimeManager.Instance != null)
        {
            GameTimeManager.Instance.OnDayChanged += OnDayChanged;
            GameTimeManager.Instance.OnYearChanged += OnYearChanged;
        }
    }

    private void OnDestroy()
    {
        if (GameTimeManager.Instance != null)
        {
            GameTimeManager.Instance.OnDayChanged -= OnDayChanged;
            GameTimeManager.Instance.OnYearChanged -= OnYearChanged;
        }
    }
    private void Update()
    {
        // Якщо є гараж і в ньому є машини - ремонтуємо їх
        if (hasGarage && vehiclesInGarage.Count > 0)
        {
            // Ремонтуємо кожну машину
            for (int i = vehiclesInGarage.Count - 1; i >= 0; i--)
            {
                VehicleController vehicle = vehiclesInGarage[i];
                vehicle.currentCondition += 10f * Time.deltaTime; // Швидкість ремонту

                // Якщо відремонтовано повністю
                if (vehicle.currentCondition >= 100f)
                {
                    vehicle.currentCondition = 100f;
                    vehicle.isInGarage = false;
                    vehicle.isHeadingToGarage = false;
                    vehiclesInGarage.RemoveAt(i);
                    Debug.Log($"{vehicle.name} повністю відремонтовано в {cityName} і повертається на маршрут.");

                    // Тут машина має відновити свій звичайний рух по маршруту
                }
            }
        }
    }
    //  коли гравець купує гараж
    public void BuildGarage()
    {
        if (!hasGarage)
        {
            hasGarage = true;
            Debug.Log($"Гараж успішно збудовано у місті {cityName}");
            // Пізніше ми додамо сюди зняття грошей через FinanceManager
        }
    }
    // ─── Попит ───────────────────────────────────────────────

    // Щодня відновлюється 30% максимуму
    private void OnDayChanged(GameDate date)
    {
        foreach (var d in demands)
        {
            int replenish = Mathf.CeilToInt(d.maxUnits * 0.3f);
            d.currentUnits = Mathf.Min(d.maxUnits, d.currentUnits + replenish);
        }
    }

    // Щороку максимум зростає на annualGrowth
    private void OnYearChanged(GameDate date)
    {
        foreach (var d in demands)
        {
            float growth = Random.Range(0.01f, d.annualGrowth);
            d.maxUnits = Mathf.RoundToInt(d.maxUnits * (1f + growth));
            Debug.Log($"{cityName}→{d.destination?.cityName}: " +
                      $"ліміт попиту → {d.maxUnits}");
        }
    }

    // Транспорт забирає одиниці попиту
    public int TakeUnits(CityNode destination, int capacity)
    {
        var d = demands.Find(x => x.destination == destination);
        if (d == null) return 0;

        int taken = Mathf.Min(d.currentUnits, capacity);
        d.currentUnits -= taken;
        return taken;
    }

    // Скільки одиниць чекає до конкретного міста
    public int GetDemandTo(CityNode destination)
    {
        var d = demands.Find(x => x.destination == destination);
        return d?.currentUnits ?? 0;
    }

    // Скільки одиниць чекає до всіх міст разом
    public int GetTotalDemand()
    {
        int total = 0;
        foreach (var d in demands) total += d.currentUnits;
        return total;
    }

    // Сумісність типу транспорту з типом міста
    public bool GeneratesIncomeFor(VehicleType vt)
    {
        if (cityType == CityType.Trade) return true;
        if (cityType == CityType.Industrial) return vt == VehicleType.Cargo;
        if (cityType == CityType.Tourist) return vt == VehicleType.Passenger;
        return false;
    }

    public int GetDailyDemandLimit() =>
        activityLevel == 3 ? 300 : activityLevel == 2 ? 150 : 60;

    // ─── Назва міста у World Space ───────────────────────────

    private void CreateLabel()
    {
        // Не створювати двічі
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

    // ─── Gizmos (тільки в редакторі) ─────────────────────────

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