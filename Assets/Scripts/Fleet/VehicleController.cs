using UnityEngine;
using DG.Tweening;

public enum VehicleStatus { Idle, Moving, Broken, Strike }

public class VehicleController : MonoBehaviour
{
    [Header("Дані транспортного засобу")]
    public VehicleData vehicleData;

    [Header("Поточний стан")]
    public VehicleStatus status = VehicleStatus.Idle;
    public CityNode currentCity;
    public float condition = 100f;

    [Header("Активний маршрут")]
    public RouteDefinition activeRoute;

    [Header("Тестовий маршрут (запускається автоматично)")]
    public RouteDefinition testRoute;

    [Header("Налаштування")]
    public float visualSpeed = 3f;

    private const float BASE_RATE = 10f;
    private const float KM_PER_UNIT = 50f;
    private const float FUEL_PRICE = 2f;

    private Tween moveTween;

    private void Start()
    {
        FinanceManager.Instance?.RegisterVehicle(this);

        if (WageManager.Instance != null)
        {
            WageManager.Instance.OnStrikeStarted += OnStrikeStarted;
            WageManager.Instance.OnStrikeEnded += OnStrikeEnded;
        }

        // Автоматичний запуск тестового маршруту
        if (testRoute != null)
            StartRoute(testRoute);
    }

    private void OnDestroy()
    {
        moveTween?.Kill();
        FinanceManager.Instance?.UnregisterVehicle(this);

        if (WageManager.Instance != null)
        {
            WageManager.Instance.OnStrikeStarted -= OnStrikeStarted;
            WageManager.Instance.OnStrikeEnded -= OnStrikeEnded;
        }
    }

    public bool StartRoute(RouteDefinition route)
    {
        if (status != VehicleStatus.Idle)
        {
            Debug.Log($"{vehicleData.vehicleName}: не в стані Idle!");
            return false;
        }
        if (route == null || !route.IsValid())
        {
            Debug.Log("Маршрут невалідний — потрібно мінімум 2 зупинки!");
            return false;
        }

        activeRoute = route;
        currentCity = route.stops[0].city;
        transform.position = currentCity.transform.position;


        Debug.Log($"Маршрут запущено: {route.routeName}");

        activeRoute.ShowHighlight(true);

        MoveToNext();
        return true;
    }

    public void StopRoute()
    {
        moveTween?.Kill();
        status = VehicleStatus.Idle;

        if (activeRoute != null)
            activeRoute.ShowHighlight(false);

        activeRoute = null;
    }

    private void MoveToNext()
    {
        if (activeRoute == null) return;
        if (status == VehicleStatus.Broken ||
            status == VehicleStatus.Strike) return;

        CityNode next = activeRoute.GetNextCity(currentCity);
        if (next == null) return;

        RoadConnection road = RoadNetwork.Instance?.GetRoad(currentCity, next);
        if (road == null)
        {
            Debug.LogWarning($"Немає дороги: {currentCity.cityName} → {next.cityName}. Маршрут зупинено.");
            StopRoute();
            return;
        }

        // Повернути спрайт у напрямку наступного міста
        Vector3 dir = next.transform.position - transform.position;
        if (dir != Vector3.zero)
        {
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);

            // Зберігаємо поточний масштаб
            Vector3 localScale = transform.localScale;

            // Якщо машина їде вліво (напрямок по X від'ємний)
            if (dir.x < 0)
            {
                // Віддзеркалюємо по осі Y
                localScale.y = -Mathf.Abs(localScale.y);
            }
            else // Якщо їде вправо
            {
                // Повертаємо нормальний масштаб
                localScale.y = Mathf.Abs(localScale.y);
            }

            // Застосовуємо новий масштаб
            transform.localScale = localScale;
        }


        float distUnits = Vector3.Distance(
            currentCity.transform.position, next.transform.position);
        float distKm = distUnits * KM_PER_UNIT;
        float effSpeed = Mathf.Min(vehicleData.maxSpeedKmh,
                                    road.roadData.speedLimitKmh);

        // Візуальна тривалість руху
        float speedMod = road.roadData.speedLimitKmh / 150f;
        float duration = Mathf.Max(0.5f, distUnits / (visualSpeed * speedMod));

        status = VehicleStatus.Moving;
        moveTween = transform
            .DOMove(next.transform.position, duration)
            .SetEase(Ease.Linear)
            .OnComplete(() => ArriveAt(next, road, distKm));
    }

    private void ArriveAt(CityNode city, RoadConnection road, float distKm)
    {
        CityNode prev = currentCity;
        currentCity = city;

        // Дохід — тільки якщо місто генерує цей тип
        if (city.GeneratesIncomeFor(vehicleData.vehicleType))
        {
            int consumed = DemandManager.Instance != null
                ? DemandManager.Instance.ConsumeCapacity(city, (int)vehicleData.maxCapacity)
                : (int)vehicleData.maxCapacity;

            if (consumed > 0)
            {
                float income = (distKm / KM_PER_UNIT)
                             * consumed
                             * (prev.activityLevel / 3f)
                             * BASE_RATE;
                FinanceManager.Instance?.AddIncome(income);
                Debug.Log($"[{vehicleData.vehicleName}] {prev.cityName}→{city.cityName} | +{income:F0} у.о.");
            }
            else
            {
                Debug.Log($"[{vehicleData.vehicleName}] {city.cityName} — попит вичерпано.");
            }
        }
        else
        {
            Debug.Log($"[{vehicleData.vehicleName}] {city.cityName} — несумісний тип. Дохід 0.");
        }

        // Витрати на пальне
        float fuel = (distKm / 100f) * vehicleData.fuelPer100km * FUEL_PRICE;
        FinanceManager.Instance?.AddExpense(fuel);

        // Знос
        ApplyWear(road, distKm);

        if (status != VehicleStatus.Broken &&
            status != VehicleStatus.Strike)
        {
            status = VehicleStatus.Idle;
            MoveToNext();
        }
    }

    private void ApplyWear(RoadConnection road, float distKm)
    {
        float wear = (distKm * road.roadData.wearMultiplier * 0.1f)
                      / vehicleData.maintenanceCost;
        condition = Mathf.Max(0f, condition - wear);

        if (condition <= 0f)
        {
            status = VehicleStatus.Broken;
            moveTween?.Kill();
            Debug.LogWarning($"{vehicleData.vehicleName} зламався у {currentCity.cityName}!");
        }
    }

    public bool EmergencyRepair()
    {
        if (status != VehicleStatus.Broken) return false;

        float cost = vehicleData.purchaseCost * 0.15f;
        if (!FinanceManager.Instance.CanAfford(cost))
        {
            Debug.Log("Недостатньо коштів для ремонту!");
            return false;
        }

        FinanceManager.Instance.AddExpense(cost);
        condition = 100f;
        status = VehicleStatus.Idle;

        if (activeRoute != null) MoveToNext();
        Debug.Log($"Ремонт: {cost:F0} у.о. Стан відновлено до 100%.");
        return true;
    }

    private void OnStrikeStarted()
    {
        moveTween?.Kill();
        status = VehicleStatus.Strike;
        Debug.LogWarning($"{vehicleData.vehicleName} — страйк!");
    }

    private void OnStrikeEnded()
    {
        if (status != VehicleStatus.Strike) return;
        status = VehicleStatus.Idle;
        if (activeRoute != null) MoveToNext();
    }
}