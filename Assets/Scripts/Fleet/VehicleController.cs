using DG.Tweening;
using System.Collections.Generic;
using UnityEngine;

public enum VehicleStatus
{
    Idle,
    Moving,
    ReturningToGarage,
    Repairing,
    Broken,
    Strike
}

public class VehicleController : MonoBehaviour
{
    [Header("Дані транспортного засобу")]
    public VehicleData vehicleData;

    [Header("Гаражі (міста приписки)")]
    public List<CityNode> garageCities = new List<CityNode>();

    [Header("Поточний стан")]
    public VehicleStatus status = VehicleStatus.Idle;
    public CityNode currentCity;
    public float condition = 100f;
    public float currentCondition = 100f;
    public bool useIndividualRepairThreshold = false;
    public float individualRepairThreshold = 20f;

    [Header("Статуси ремонту")]
    public bool isHeadingToGarage = false;
    public bool isInGarage = false;
    public CityNode targetGarageCity; // Місто, куди ми їдемо на ремонт

    [Header("Завантаження")]
    public int currentLoad = 0;
    public CityNode loadDestination = null;

    [Header("Активний маршрут")]
    public RouteDefinition activeRoute;

    [Header("Тестовий маршрут (запускається автоматично)")]
    public RouteDefinition testRoute;

    [Header("Налаштування")]
    public float visualSpeed = 3f;

    private const float BASE_RATE = 10f;
    private const float KM_PER_UNIT = 50f;
    private const float FUEL_PRICE = 1f;

    private Tween moveTween;
    private RouteDefinition pendingRoute;

    // ─── Ініціалізація ───────────────────────────────────────

    private void Start()
    {
        FinanceManager.Instance?.RegisterVehicle(this);

        if (WageManager.Instance != null)
        {
            WageManager.Instance.OnStrikeStarted += OnStrikeStarted;
            WageManager.Instance.OnStrikeEnded += OnStrikeEnded;
        }

        if (testRoute != null)
            StartRoute(testRoute);

        Invoke(nameof(RegisterSelf), 0.1f);
    }

    private void RegisterSelf()
    {
        FindFirstObjectByType<FleetPanelController>()?.RegisterVehicle(this);
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

    // ─── Маршрут ─────────────────────────────────────────────

    public bool StartRoute(RouteDefinition route)
    {
        if (status != VehicleStatus.Idle) return false;
        if (route == null || !route.IsValid()) return false;

        activeRoute = route;
        currentCity = route.stops[0].city;
        transform.position = currentCity.transform.position;

        Debug.Log($"Маршрут запущено: {route.routeName}");
        activeRoute.ShowHighlight(true);

        LoadCargo();
        MoveToNext();
        return true;
    }

    public void StopRoute()
    {
        moveTween?.Kill();
        if (activeRoute != null)
            activeRoute.ShowHighlight(false);
        activeRoute = null;
        currentLoad = 0;
        loadDestination = null;
        status = VehicleStatus.Idle;
    }

    // ─── Завантаження ─────────────────────────────────────────

    private void LoadCargo()
    {
        currentLoad = 0;
        loadDestination = null;

        if (activeRoute == null || currentCity == null) return;
        if (activeRoute.stops == null || activeRoute.stops.Count == 0) return;

        foreach (var stop in activeRoute.stops)
        {
            if (stop == null || stop.city == null) continue;
            if (stop.city == currentCity) continue;
            if (!currentCity.GeneratesIncomeFor(vehicleData.vehicleType)) continue;
            if (currentCity.demands == null || currentCity.demands.Count == 0) continue;

            int available = currentCity.GetDemandTo(stop.city);
            if (available <= 0) continue;

            currentLoad = currentCity.TakeUnits(stop.city,
                                  (int)vehicleData.maxCapacity);
            loadDestination = stop.city;

            Debug.Log($"{vehicleData.vehicleName}: +{currentLoad} од. → {stop.city.cityName}");
            return;
        }
    }

    // ─── Основний рух ────────────────────────────────────────

    private void MoveToNext()
    {
        if (activeRoute == null) return;
        if (status == VehicleStatus.Broken ||
            status == VehicleStatus.Strike ||
            status == VehicleStatus.Repairing ||
            status == VehicleStatus.ReturningToGarage) return;

        CityNode next = activeRoute.GetNextCity(currentCity);
        if (next == null) return;

        RoadConnection road = RoadNetwork.Instance?.GetRoad(currentCity, next);
        if (road == null)
        {
            Debug.LogWarning($"Немає дороги: {currentCity.cityName}→{next.cityName}");
            StopRoute();
            return;
        }

        RotateTowards(next.transform.position);

        float distUnits = Vector3.Distance(
            currentCity.transform.position, next.transform.position);
        float distKm = distUnits * KM_PER_UNIT;
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

        // Дохід якщо привезли вантаж
        if (currentLoad > 0 && loadDestination == city)
        {
            float income = (distKm / KM_PER_UNIT)
                         * currentLoad
                         * (prev != null ? prev.activityLevel / 3f : 1f)
                         * BASE_RATE;
            FinanceManager.Instance?.AddIncome(income);
            Debug.Log($"{vehicleData.vehicleName} → {city.cityName} | +{income:F0} у.о.");
            currentLoad = 0;
            loadDestination = null;
        }
        else if (currentLoad == 0 && city.GeneratesIncomeFor(vehicleData.vehicleType))
        {
            // Старий варіант через DemandManager якщо demands не налаштовані
            int consumed = DemandManager.Instance != null
                ? DemandManager.Instance.ConsumeCapacity(city, (int)vehicleData.maxCapacity)
                : (int)vehicleData.maxCapacity;

            if (consumed > 0)
            {
                float income = (distKm / KM_PER_UNIT)
                             * consumed
                             * (prev != null ? prev.activityLevel / 3f : 1f)
                             * BASE_RATE;
                FinanceManager.Instance?.AddIncome(income);
                Debug.Log($"{vehicleData.vehicleName} → {city.cityName} | +{income:F0} у.о.");
            }
        }

        float fuel = (distKm / 100f) * vehicleData.fuelPer100km * FUEL_PRICE;
        FinanceManager.Instance?.AddExpense(fuel);
        ApplyWear(road, distKm);

        if (status != VehicleStatus.Broken && status != VehicleStatus.Strike)
        {
            status = VehicleStatus.Idle;
            if (currentLoad == 0) LoadCargo();
            MoveToNext();
        }
    }

    private void ApplyWear(RoadConnection road, float distKm)
    {
        float wear = (distKm * road.roadData.wearMultiplier * 1f)
                   / vehicleData.maintenanceCost;
        condition = Mathf.Max(0f, condition - wear);

        if (condition <= 0f)
        {
            status = VehicleStatus.Broken;
            moveTween?.Kill();
            Debug.LogWarning(
                $"{vehicleData.vehicleName} зламався у {currentCity.cityName}!");
        }
    }

    // ─── Ремонт ──────────────────────────────────────────────

    public void RequestRepair()
    {
        if (status == VehicleStatus.Repairing ||
            status == VehicleStatus.ReturningToGarage) return;

        if (status == VehicleStatus.Broken)
        {
            float towCost = vehicleData.purchaseCost * 0.05f;
            float repairCost = vehicleData.purchaseCost * 0.15f;
            float totalCost = towCost + repairCost;

            if (!FinanceManager.Instance.CanAfford(totalCost))
            {
                Debug.Log("Недостатньо коштів для евакуації і ремонту!");
                return;
            }

            FinanceManager.Instance.AddExpense(totalCost);
            Debug.Log($"Евакуація + ремонт: {totalCost:F0} у.о.");

            CityNode garage = GetNearestGarage();
            if (garage != null)
            {
                currentCity = garage;
                transform.position = garage.transform.position;
            }

            StartRepairing();
        }
        else if (condition < 30f)
        {
            pendingRoute = activeRoute;
            StopRoute();
            DriveToGarageForRepair();
        }
    }

    private CityNode GetNearestGarage()
    {
        if (garageCities == null || garageCities.Count == 0) return null;

        CityNode nearest = null;
        float minDist = float.MaxValue;

        foreach (var g in garageCities)
        {
            if (g == null) continue;
            float dist = Vector3.Distance(transform.position, g.transform.position);
            if (dist < minDist) { minDist = dist; nearest = g; }
        }
        return nearest;
    }

    private void DriveToGarageForRepair()
    {
        CityNode garage = GetNearestGarage();
        if (garage == null) { StartRepairing(); return; }
        if (currentCity == garage) { StartRepairing(); return; }

        var path = RoadNetwork.Instance?.FindPath(currentCity, garage);
        if (path == null || path.Count == 0)
        {
            Debug.LogWarning("Немає маршруту до гаража! Ремонт на місці.");
            StartRepairing();
            return;
        }

        status = VehicleStatus.ReturningToGarage;
        Debug.Log($"{vehicleData.vehicleName} їде до гаража: {garage.cityName}");
        MoveAlongPath(path, 0, garage);
    }

    private void MoveAlongPath(List<CityNode> path, int index, CityNode destination)
    {
        if (index >= path.Count)
        {
            currentCity = destination;
            StartRepairing();
            return;
        }

        CityNode next = path[index];
        RoadConnection road = RoadNetwork.Instance?.GetRoad(currentCity, next);

        if (road == null) { StartRepairing(); return; }

        RotateTowards(next.transform.position);

        float dist = Vector3.Distance(transform.position, next.transform.position);
        float speedMod = road.roadData.speedLimitKmh / 150f;
        float duration = Mathf.Max(0.3f, dist / (visualSpeed * speedMod));

        moveTween = transform
            .DOMove(next.transform.position, duration)
            .SetEase(Ease.Linear)
            .OnComplete(() =>
            {
                currentCity = next;
                MoveAlongPath(path, index + 1, destination);
            });
    }

    private void StartRepairing()
    {
        status = VehicleStatus.Repairing;

        // Планове ТО — 8% від ціни (евакуація вже оплачена окремо)
        if (condition > 0f)
        {
            float cost = vehicleData.purchaseCost * 0.08f;
            if (!FinanceManager.Instance.CanAfford(cost))
            {
                Debug.Log("Недостатньо коштів для ТО!");
                status = VehicleStatus.Idle;
                return;
            }
            FinanceManager.Instance.AddExpense(cost);
            Debug.Log($"Планове ТО: {cost:F0} у.о.");
        }

        float repairTime = GetRepairTime();
        Debug.Log($"{vehicleData.vehicleName} на ремонті ({repairTime:F0} сек)...");
        DOVirtual.DelayedCall(repairTime, FinishRepair);
    }

    private float GetRepairTime()
    {
        if (condition > 50f) return 3f;
        if (condition > 30f) return 5f;
        if (condition > 10f) return 8f;
        return 12f;
    }

    private void FinishRepair()
    {
        condition = 100f;
        status = VehicleStatus.Idle;
        Debug.Log($"{vehicleData.vehicleName} відремонтовано!");

        if (pendingRoute != null)
        {
            var r = pendingRoute;
            pendingRoute = null;
            StartRoute(r);
        }
    }

    // ─── Допоміжні ───────────────────────────────────────────

    private void RotateTowards(Vector3 target)
    {
        Vector3 dir = target - transform.position;
        if (dir == Vector3.zero) return;

        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);

        Vector3 scale = transform.localScale;
        scale.y = dir.x < 0 ? -Mathf.Abs(scale.y) : Mathf.Abs(scale.y);
        transform.localScale = scale;
    }

    // ─── Страйк ──────────────────────────────────────────────

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
    //  -─ Ремонт при зношуванні ─────────────────────────────────

    public void DecreaseCondition(float amount)
    {
        if (isInGarage) return; // В гаражі не ламаємося

        currentCondition -= amount * Time.deltaTime;

        // Перевіряємо, чи не час на ремонт
        if (!isHeadingToGarage && MechanicManager.Instance.NeedsRepair(this))
        {
            GoToNearestGarage();
        }
    }

    private void GoToNearestGarage()
    {
        // Шукаємо всі міста на карті
        CityNode[] allCities = FindObjectsOfType<CityNode>();
        CityNode nearestGarage = null;
        float minDistance = Mathf.Infinity;

        foreach (CityNode city in allCities)
        {
            if (city.hasGarage)
            {
                float dist = Vector2.Distance(transform.position, city.transform.position);
                if (dist < minDistance)
                {
                    minDistance = dist;
                    nearestGarage = city;
                }
            }
        }

        if (nearestGarage != null)
        {
            Debug.Log($"{gameObject.name} їде на ремонт у місто {nearestGarage.cityName}");
            isHeadingToGarage = true;
            targetGarageCity = nearestGarage;

        }
        else
        {
            Debug.LogWarning("Машині потрібен ремонт, але на карті немає жодного гаража!");
        }
    }
}