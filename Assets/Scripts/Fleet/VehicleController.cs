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
    public string customName;

    [Header("Гаражі (міста приписки)")]
    public List<CityNode> garageCities = new List<CityNode>();

    [Header("Поточний стан")]
    public VehicleStatus status = VehicleStatus.Idle;
    public CityNode currentCity;
    public float condition = 100f;

    [Header("Обслуговування")]
    public bool useIndividualRepairThreshold = false;
    public float individualRepairThreshold = 30f;
    public float totalRepairCost = 0f;
    public float totalFuelCost = 0f;
    public float totalIncome = 0f;

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
    public const float FUEL_PRICE = 1f;

    private Tween moveTween;
    private RouteDefinition pendingRoute;

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

            currentLoad = currentCity.TakeUnits(stop.city, (int)vehicleData.maxCapacity);
            loadDestination = stop.city;

            return;
        }
    }

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
            float income = (distKm / KM_PER_UNIT) * currentLoad * (prev != null ? prev.activityLevel / 3f : 1f) * BASE_RATE;
            FinanceManager.Instance?.AddIncome(income);
            totalIncome += income;
            if (activeRoute != null) activeRoute.incomeStats += income;
            currentLoad = 0;
            loadDestination = null;
        }
        else if (currentLoad == 0 && city.GeneratesIncomeFor(vehicleData.vehicleType))
        {
            int consumed = DemandManager.Instance != null
                ? DemandManager.Instance.ConsumeCapacity(city, (int)vehicleData.maxCapacity)
                : (int)vehicleData.maxCapacity;

            if (consumed > 0)
            {
                float income = (distKm / KM_PER_UNIT) * consumed * (prev != null ? prev.activityLevel / 3f : 1f) * BASE_RATE;
                FinanceManager.Instance?.AddIncome(income);
                totalIncome += income;
                if (activeRoute != null) activeRoute.incomeStats += income;
            }
        }

        float fuelC = (distKm / 100f) * vehicleData.fuelPer100km * FUEL_PRICE;
        FinanceManager.Instance?.AddExpense(fuelC);
        totalFuelCost += fuelC;
        ApplyWear(road, distKm);

        if (status != VehicleStatus.Broken && status != VehicleStatus.Strike && status != VehicleStatus.ReturningToGarage && status != VehicleStatus.Repairing)
        {
            status = VehicleStatus.Idle;
            if (currentLoad == 0) LoadCargo();

            float threshold = useIndividualRepairThreshold ? individualRepairThreshold : (MechanicManager.Instance?.globalRepairThreshold ?? 30f);
            if (condition <= threshold)
            {
                DriveToGarageForRepair();
            }
            else
            {
                MoveToNext();
            }
        }
    }

    private void ApplyWear(RoadConnection road, float distKm)
    {
        float wear = (distKm * road.roadData.wearMultiplier * 1f) / vehicleData.maintenanceCost;
        condition = Mathf.Max(0f, condition - wear);

        if (condition <= 0f)
        {
            status = VehicleStatus.Broken;
            moveTween?.Kill();
        }
    }

    public void RequestRepair()
    {
        if (status == VehicleStatus.Repairing || status == VehicleStatus.ReturningToGarage) return;

        if (status == VehicleStatus.Broken)
        {
            float towCost = vehicleData.purchaseCost * 0.05f;
            float repairCost = vehicleData.purchaseCost * 0.15f;
            float totalCost = towCost + repairCost;

            if (FinanceManager.Instance != null && !FinanceManager.Instance.CanAfford(totalCost)) return;

            FinanceManager.Instance?.AddExpense(totalCost);
            totalRepairCost += totalCost;

            CityNode garage = GetNearestGarage();
            if (garage != null)
            {
                currentCity = garage;
                transform.position = garage.transform.position;
            }

            StartRepairing();
        }
        else
        {
            float threshold = useIndividualRepairThreshold ? individualRepairThreshold : (MechanicManager.Instance?.globalRepairThreshold ?? 30f);
            if (condition < threshold || condition < 60f) // Можна ремонтувати якщо стан менше певного % вручну
            {
                pendingRoute = activeRoute;
                StopRoute();
                DriveToGarageForRepair();
            }
        }
    }

    private CityNode GetNearestGarage()
    {
        var allCities = FindObjectsByType<CityNode>(FindObjectsSortMode.None);
        CityNode nearest = null;
        float minDist = float.MaxValue;

        foreach (var c in allCities)
        {
            if (c != null && c.hasGarage)
            {
                float dist = Vector3.Distance(transform.position, c.transform.position);
                if (dist < minDist) { minDist = dist; nearest = c; }
            }
        }

        if (nearest == null && garageCities != null && garageCities.Count > 0)
        {
            foreach (var g in garageCities)
            {
                if (g == null) continue;
                float dist = Vector3.Distance(transform.position, g.transform.position);
                if (dist < minDist) { minDist = dist; nearest = g; }
            }
        }
        return nearest;
    }

    private void DriveToGarageForRepair()
    {
        if (currentCity != null && currentCity.hasGarage) { StartRepairing(); return; }

        CityNode garage = GetNearestGarage();
        if (garage == null) { StartRepairing(); return; }
        if (currentCity == garage) { StartRepairing(); return; }

        var path = RoadNetwork.Instance?.FindPath(currentCity, garage);
        if (path == null || path.Count <= 1)
        {
            StartRepairing();
            return;
        }

        status = VehicleStatus.ReturningToGarage;
        MoveAlongPath(path, 1, garage);
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
                if (currentCity == destination || currentCity.hasGarage)
                {
                    StartRepairing();
                }
                else
                {
                    MoveAlongPath(path, index + 1, destination);
                }
            });
    }

    private void StartRepairing()
    {
        status = VehicleStatus.Repairing;

        if (condition > 0f)
        {
            float cost = vehicleData.purchaseCost * 0.08f;
            if (FinanceManager.Instance != null && !FinanceManager.Instance.CanAfford(cost))
            {
                status = VehicleStatus.Idle;
                return;
            }
            FinanceManager.Instance?.AddExpense(cost);
            totalRepairCost += cost;
        }

        float repairTime = currentCity != null && currentCity.hasGarage ? Mathf.Max(1f, 5f - (currentCity.mechanics * 0.5f)) : 5f;
        DOVirtual.DelayedCall(repairTime, FinishRepair);
    }

    private void FinishRepair()
    {
        condition = 100f;
        status = VehicleStatus.Idle;

        if (pendingRoute != null)
        {
            var r = pendingRoute;
            pendingRoute = null;
            StartRoute(r);
        }
    }

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

    private void OnStrikeStarted()
    {
        moveTween?.Kill();
        status = VehicleStatus.Strike;
    }

    private void OnStrikeEnded()
    {
        if (status != VehicleStatus.Strike) return;
        status = VehicleStatus.Idle;
        if (activeRoute != null) MoveToNext();
    }
}