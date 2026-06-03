using DG.Tweening;
using System.Collections.Generic;
using UnityEngine;

public enum VehicleStatus
{
    Idle,
    Moving,
    Loading,
    ReturningToWorkshop,
    Repairing,
    Broken,
    Strike
}

public class VehicleController : MonoBehaviour
{
    public static float globalRepairThreshold = 30f;

    [Header("Дані транспортного засобу")]
    public VehicleData vehicleData;
    public string customName;

    [Header("Поточний стан")]
    public VehicleStatus status = VehicleStatus.Idle;
    public CityNode currentCity;
    public CityNode targetWorkshop;
    public float condition = 100f;
    private bool isFirstDispatch = true;

    [Header("Обслуговування")]
    public float totalRepairCost = 0f;

    [Header("Завантаження")]
    public int currentLoad = 0;
    public CityNode loadDestination = null;
    public int waitHoursLeft = 0;

    [Header("Активний маршрут")]
    public RouteDefinition activeRoute;
    private RouteDefinition pendingRoute;

    [Header("Налаштування")]
    public float visualSpeed = 1f; 

    private const float BASE_RATE = 10f;
    private const float KM_PER_UNIT = 60f;
    public const float FUEL_PRICE = 1f;

    private Tween moveTween;

    private void Start()
    {
        // Призначення правильної іконки
        if (vehicleData != null && vehicleData.icon != null)
        {
            SpriteRenderer sr = GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.sprite = vehicleData.icon;
                sr.sortingLayerName = "Roads";
                sr.sortingOrder = 40; // Машинки над містами
            }
        }

        FinanceManager.Instance?.RegisterVehicle(this);
        if (WageManager.Instance != null)
        {
            WageManager.Instance.OnStrikeStarted += OnStrikeStarted;
            WageManager.Instance.OnStrikeEnded += OnStrikeEnded;
        }
        if (GameTimeManager.Instance != null)
        {
            GameTimeManager.Instance.OnHourChanged += HandleHourChanged;
        }
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
        if (GameTimeManager.Instance != null)
        {
            GameTimeManager.Instance.OnHourChanged -= HandleHourChanged;
        }
    }

    public void AssignRoute(RouteDefinition route) { StartRoute(route); }
    public void RequestEmergencyRepair() { RequestRepair(); }

    public bool StartRoute(RouteDefinition route)
    {
        if (status == VehicleStatus.Broken || status == VehicleStatus.Repairing) return false;
        if (route == null || !route.IsValid()) return false;

        activeRoute = route;
        gameObject.SetActive(true);

        if (isFirstDispatch)
        {
            currentCity = route.stops[0].city;
            transform.position = currentCity.transform.position;
            isFirstDispatch = false;

            status = VehicleStatus.Loading;
            waitHoursLeft = 2;
        }
        else
        {
            if (currentCity != null && currentCity != route.stops[0].city)
            {
                var path = RoadNetwork.Instance?.FindPath(currentCity, route.stops[0].city);
                if (path != null && path.Count > 1)
                {
                    status = VehicleStatus.Moving;
                    MoveAlongPath(path, 1, route.stops[0].city, true);
                }
            }
            else
            {
                status = VehicleStatus.Loading;
                waitHoursLeft = 2;
            }
        }
        return true;
    }

    public void StopRoute()
    {
        moveTween?.Kill();
        activeRoute = null;
        currentLoad = 0;
        loadDestination = null;
        waitHoursLeft = 0;
        if (status != VehicleStatus.Repairing && status != VehicleStatus.ReturningToWorkshop && status != VehicleStatus.Broken)
        {
            status = VehicleStatus.Idle;
        }
    }

    private void LoadCargo()
    {
        currentLoad = 0;
        loadDestination = null;
        if (activeRoute == null || currentCity == null) return;

        foreach (var stop in activeRoute.stops)
        {
            if (stop.city == currentCity) continue;

            int available = currentCity.GetDemandTo(stop.city, vehicleData.vehicleType);
            if (available > 0)
            {
                currentLoad = currentCity.TakeUnits(stop.city, (int)vehicleData.maxCapacity, vehicleData.vehicleType);
                loadDestination = stop.city;
                return;
            }
        }
    }

    private void MoveToNext()
    {
        if (activeRoute == null || status == VehicleStatus.Broken || status == VehicleStatus.Strike || status == VehicleStatus.Repairing || status == VehicleStatus.ReturningToWorkshop) return;

        CityNode next = activeRoute.GetNextCity(currentCity);
        RoadConnection road = RoadNetwork.Instance?.GetRoad(currentCity, next);

        if (road == null) { StopRoute(); return; }

        RotateTowards(next.transform.position);
        float distUnits = Vector3.Distance(transform.position, next.transform.position);
        float distKm = distUnits * KM_PER_UNIT;

        float actualSpeed = Mathf.Min(vehicleData.maxSpeedKmh, road.roadData.speedLimitKmh);
        float duration = Mathf.Max(0.5f, distUnits / (visualSpeed * (actualSpeed / 150f)));

        status = VehicleStatus.Moving;
        moveTween = transform.DOMove(next.transform.position, duration).SetEase(Ease.Linear)
            .OnComplete(() => ArriveAt(next, road, distKm));
    }

    private void ArriveAt(CityNode city, RoadConnection road, float distKm)
    {
        CityNode prev = currentCity;
        currentCity = city;

        if (currentLoad > 0 && loadDestination == city)
        {
            float inc = (distKm / KM_PER_UNIT) * currentLoad * (prev != null ? prev.activityLevel / 3f : 1f) * BASE_RATE;
            FinanceManager.Instance?.AddIncome(inc);
            currentLoad = 0;
            loadDestination = null;
        }

        FinanceManager.Instance?.AddExpense((distKm / 100f) * vehicleData.fuelPer100km * FUEL_PRICE);
        ApplyWear(road, distKm);

        if (status == VehicleStatus.Broken) return;

        if (condition <= globalRepairThreshold)
        {
            targetWorkshop = GetNearestWorkshopWithFreeSlot();
            if (targetWorkshop != null)
            {
                pendingRoute = activeRoute;
                StopRoute();
                DriveToWorkshop(targetWorkshop);
                return;
            }
        }

        status = VehicleStatus.Loading;
        waitHoursLeft = 2;
    }

    private void ApplyWear(RoadConnection road, float distKm)
    {
        float effectiveDurability = (vehicleData.durability > 0) ? vehicleData.durability : vehicleData.maintenanceCost;
        if (effectiveDurability <= 0) effectiveDurability = 100f;

        float wear = (distKm * road.roadData.wearMultiplier) / effectiveDurability;
        condition = Mathf.Max(0f, condition - wear);

        if (condition <= 0f)
        {
            status = VehicleStatus.Broken;
            moveTween?.Kill();
        }
    }

    private CityNode GetNearestWorkshopWithFreeSlot()
    {
        var allCities = FindObjectsByType<CityNode>(FindObjectsSortMode.None);
        CityNode nearest = null;
        float minDist = float.MaxValue;
        var allVehicles = FindObjectsByType<VehicleController>(FindObjectsSortMode.None);

        foreach (var c in allCities)
        {
            if (c != null && c.hasWorkshop)
            {
                int occupiedSlots = 0;
                foreach (var v in allVehicles)
                {
                    if (v == this) continue;
                    if ((v.status == VehicleStatus.Repairing && v.currentCity == c) ||
                        (v.status == VehicleStatus.ReturningToWorkshop && v.targetWorkshop == c))
                    {
                        occupiedSlots++;
                    }
                }

                if (occupiedSlots < c.repairSlots)
                {
                    float dist = Vector3.Distance(transform.position, c.transform.position);
                    if (dist < minDist) { minDist = dist; nearest = c; }
                }
            }
        }
        return nearest;
    }

    private void DriveToWorkshop(CityNode workshop)
    {
        if (currentCity == workshop) { TryStartRepairing(); return; }

        var path = RoadNetwork.Instance?.FindPath(currentCity, workshop);
        if (path == null || path.Count <= 1) { return; }

        status = VehicleStatus.ReturningToWorkshop;
        MoveAlongPath(path, 1, workshop, false);
    }

    private void MoveAlongPath(List<CityNode> path, int index, CityNode destination, bool isMovingToNewRoute)
    {
        if (index >= path.Count)
        {
            currentCity = destination;
            if (isMovingToNewRoute)
            {
                status = VehicleStatus.Loading;
                waitHoursLeft = 2;
            }
            else
            {
                TryStartRepairing();
            }
            return;
        }

        CityNode next = path[index];
        RoadConnection road = RoadNetwork.Instance?.GetRoad(currentCity, next);
        if (road == null) { return; }

        RotateTowards(next.transform.position);
        float dist = Vector3.Distance(transform.position, next.transform.position);

        float actualSpeed = Mathf.Min(vehicleData.maxSpeedKmh, road.roadData.speedLimitKmh);
        float duration = Mathf.Max(0.3f, dist / (visualSpeed * (actualSpeed / 150f)));

        moveTween = transform.DOMove(next.transform.position, duration).SetEase(Ease.Linear).OnComplete(() =>
        {
            currentCity = next;
            ApplyWear(road, dist * KM_PER_UNIT);
            if (status == VehicleStatus.Broken) return;

            MoveAlongPath(path, index + 1, destination, isMovingToNewRoute);
        });
    }

    private void TryStartRepairing()
    {
        if (currentCity != null && currentCity.hasWorkshop)
        {
            status = VehicleStatus.Repairing;
            float cost = vehicleData.purchaseCost * 0.05f;
            if (FinanceManager.Instance != null && FinanceManager.Instance.CanAfford(cost))
            {
                FinanceManager.Instance.AddExpense(cost);
                totalRepairCost += cost;
            }
            else
            {
                status = VehicleStatus.Broken;
            }
        }
    }

    public void FinishRepairExternal()
    {
        condition = 100f;
        status = VehicleStatus.Idle;
        targetWorkshop = null;

        if (pendingRoute != null)
        {
            var r = pendingRoute;
            pendingRoute = null;
            StartRoute(r);
        }
    }

    public void RequestRepair()
    {
        if (status == VehicleStatus.Repairing || status == VehicleStatus.ReturningToWorkshop) return;

        targetWorkshop = GetNearestWorkshopWithFreeSlot();
        if (targetWorkshop == null) return;

        if (status == VehicleStatus.Broken)
        {
            float towCost = vehicleData.purchaseCost * 0.05f;
            if (FinanceManager.Instance != null && !FinanceManager.Instance.CanAfford(towCost)) return;
            FinanceManager.Instance?.AddExpense(towCost);

            currentCity = targetWorkshop;
            transform.position = targetWorkshop.transform.position;
            TryStartRepairing();
        }
        else
        {
            pendingRoute = activeRoute;
            StopRoute();
            DriveToWorkshop(targetWorkshop);
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

    private void HandleHourChanged(GameDate date)
    {
        if (status == VehicleStatus.Loading)
        {
            waitHoursLeft--;
            if (waitHoursLeft <= 0)
            {
                status = VehicleStatus.Idle;
                if (currentLoad == 0) LoadCargo();
                MoveToNext();
            }
        }
    }

    private void OnStrikeStarted() { moveTween?.Kill(); status = VehicleStatus.Strike; }
    private void OnStrikeEnded() { if (status == VehicleStatus.Strike) { status = VehicleStatus.Idle; if (activeRoute != null) MoveToNext(); } }
}