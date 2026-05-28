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

    [Header("Гараж та персонал")]
    public bool hasGarage = false;
    public int mechanics = 0;

    [Header("Попит до інших міст")]
    public List<CityDemand> demands = new List<CityDemand>();

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

    // ─── Попит ───────────────────────────────────────────────

    private void OnDayChanged(GameDate date)
    {
        foreach (var d in demands)
        {
            int replenish = Mathf.CeilToInt(d.maxUnits * 0.3f);
            d.currentUnits = Mathf.Min(d.maxUnits, d.currentUnits + replenish);
        }
    }

    private void OnYearChanged(GameDate date)
    {
        foreach (var d in demands)
        {
            float growth = Random.Range(0.01f, d.annualGrowth);
            d.maxUnits = Mathf.RoundToInt(d.maxUnits * (1f + growth));
        }
    }

    public int TakeUnits(CityNode destination, int capacity)
    {
        var d = demands.Find(x => x.destination == destination);
        if (d == null) return 0;

        int taken = Mathf.Min(d.currentUnits, capacity);
        d.currentUnits -= taken;
        return taken;
    }

    public int GetDemandTo(CityNode destination)
    {
        var d = demands.Find(x => x.destination == destination);
        return d?.currentUnits ?? 0;
    }

    public int GetTotalDemand()
    {
        int total = 0;
        foreach (var d in demands) total += d.currentUnits;
        return total;
    }

    public bool GeneratesIncomeFor(VehicleType vt)
    {
        if (cityType == CityType.Trade) return true;
        if (cityType == CityType.Industrial) return vt == VehicleType.Cargo;
        if (cityType == CityType.Tourist) return vt == VehicleType.Passenger;
        return false;
    }

    public int GetDailyDemandLimit() =>
        activityLevel == 3 ? 300 : activityLevel == 2 ? 150 : 60;

    public void BuildGarage()
    {
        hasGarage = true;
        Debug.Log($"Гараж збудовано у місті {cityName}");
    }

    // ─── Назва міста у World Space ───────────────────────────

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

    // -─── Найм механіка ───────────────────────────────
    public void HireMechanic()
    {
        int hireCost = 1000; // Вартість найму одного механіка
        if (FinanceManager.Instance.CanAfford(hireCost))
        {
            FinanceManager.Instance.AddExpense(hireCost);
            mechanics++;
            Debug.Log("Механіка найнято! Тепер їх: " + mechanics);
        }
        else
        {
            Debug.LogWarning("Недостатньо коштів для найму механіка!");
        }
    }

    // ─── Обробка кліку по місту ───────────────────────────────
    private void OnMouseDown()
    {
        // Перевіряємо чи не клікнули ми по UI (по вікнах/кнопках)
        if (UnityEngine.EventSystems.EventSystem.current != null &&
            UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        // Відкриваємо інфо-панель
        if (CityInfoPanel.Instance != null)
        {
            CityInfoPanel.Instance.OpenPanel(this);
        }
    }
}