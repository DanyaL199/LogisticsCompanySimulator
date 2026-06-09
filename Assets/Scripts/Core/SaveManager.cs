using UnityEngine;
using System.IO;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;

[System.Serializable]
public class CityDemandSave
{
    public string destinationName;
    public int currentCargo;
    public int maxCargo;
    public int currentPassengers;
    public int maxPassengers;
    public float annualGrowth;
}

[System.Serializable]
public class CityNodeSave
{
    public string cityName;
    public bool hasWorkshop;
    public int mechanics;
    public int repairSlots;
    public List<CityDemandSave> demands;
}

[System.Serializable]
public class RoadConnectionSave
{
    public string cityAName;
    public string cityBName;
    public RoadType roadType;
}

[System.Serializable]
public class RouteDefinitionSave
{
    public string routeName;
    public float r, g, b, a;
    public List<string> routeStops;
    public float incomeStats;
}

[System.Serializable]
public class VehicleSave
{
    public string dataName;
    public string customName;
    public VehicleStatus status;
    public float condition;
    public float totalRepairCost;
    public float monthlyProfit;
    public float allTimeProfit;
    public int currentLoad;
    public int waitHoursLeft;
    public string currentCityName;
    public string targetWorkshopName;
    public string loadDestinationName;
    public string activeRouteName;
    public float posX, posY;
}

[System.Serializable]
public class GameSaveData
{
    public float balance;
    public float totalIncome;
    public float totalExpenses;
    public float vehicleAssetsValue;
    public float roadAssetsValue;
    public float currentWage;
    public int saveYear;
    public int saveMonth;
    public int saveDay;
    public int saveHour;

    public List<CityNodeSave> cities = new List<CityNodeSave>();
    public List<RoadConnectionSave> roads = new List<RoadConnectionSave>();
    public List<RouteDefinitionSave> routes = new List<RouteDefinitionSave>();
    public List<VehicleSave> vehicles = new List<VehicleSave>();
}

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }

    private string SavePath => Path.Combine(Application.persistentDataPath, "save.json");

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        if (GameTimeManager.Instance != null)
            GameTimeManager.Instance.OnDayChanged += AutoSave;
    }

    private void OnDestroy()
    {
        if (GameTimeManager.Instance != null)
            GameTimeManager.Instance.OnDayChanged -= AutoSave;
    }

    private void AutoSave(GameDate date)
    {
        if (date.day % 5 == 0) Save();
    }

    public void Save()
    {
        var fm = FinanceManager.Instance;
        var tm = GameTimeManager.Instance;
        var wm = WageManager.Instance;

        if (fm == null || tm == null) return;

        var data = new GameSaveData
        {
            balance = fm.balance,
            totalIncome = fm.totalIncome,
            totalExpenses = fm.totalExpenses,
            vehicleAssetsValue = fm.vehicleAssetsValue,
            roadAssetsValue = fm.roadAssetsValue,
            currentWage = wm != null ? wm.currentWage : 500f,
            saveYear = tm.CurrentDate.year,
            saveMonth = tm.CurrentDate.month,
            saveDay = tm.CurrentDate.day,
            saveHour = tm.CurrentDate.hour
        };

        // Save Cities
        var allCities = FindObjectsByType<CityNode>(FindObjectsSortMode.None);
        foreach (var c in allCities)
        {
            var cSave = new CityNodeSave
            {
                cityName = c.cityName,
                hasWorkshop = c.hasWorkshop,
                mechanics = c.mechanics,
                repairSlots = c.repairSlots,
                demands = new List<CityDemandSave>()
            };

            if (c.demands != null)
            {
                foreach (var d in c.demands)
                {
                    cSave.demands.Add(new CityDemandSave
                    {
                        destinationName = d.destination?.cityName,
                        currentCargo = d.currentCargo,
                        maxCargo = d.maxCargo,
                        currentPassengers = d.currentPassengers,
                        maxPassengers = d.maxPassengers,
                        annualGrowth = d.annualGrowth
                    });
                }
            }
            data.cities.Add(cSave);
        }

        // Save Roads
        if (RoadNetwork.Instance != null && RoadNetwork.Instance.roads != null)
        {
            foreach (var r in RoadNetwork.Instance.roads)
            {
                if (r.cityA == null || r.cityB == null || r.roadData == null) continue;
                data.roads.Add(new RoadConnectionSave
                {
                    cityAName = r.cityA.cityName,
                    cityBName = r.cityB.cityName,
                    roadType = r.roadData.roadType
                });
            }
        }

        // Save Routes
        var allRoutes = FindObjectsByType<RouteDefinition>(FindObjectsSortMode.None);
        foreach (var r in allRoutes)
        {
            var rSave = new RouteDefinitionSave
            {
                routeName = r.routeName,
                r = r.routeColor.r,
                g = r.routeColor.g,
                b = r.routeColor.b,
                a = r.routeColor.a,
                incomeStats = r.incomeStats,
                routeStops = new List<string>()
            };
            if (r.stops != null)
            {
                foreach (var s in r.stops) if (s.city != null) rSave.routeStops.Add(s.city.cityName);
            }
            data.routes.Add(rSave);
        }

        // Save Vehicles
        var allVehicles = FindObjectsByType<VehicleController>(FindObjectsSortMode.None);
        foreach (var v in allVehicles)
        {
            data.vehicles.Add(new VehicleSave
            {
                dataName = v.vehicleData != null ? v.vehicleData.name : "",
                customName = v.customName,
                status = v.status,
                condition = v.condition,
                totalRepairCost = v.totalRepairCost,
                monthlyProfit = v.monthlyProfit,
                allTimeProfit = v.allTimeProfit,
                currentLoad = v.currentLoad,
                waitHoursLeft = v.waitHoursLeft,
                currentCityName = v.currentCity?.cityName,
                targetWorkshopName = v.targetWorkshop?.cityName,
                loadDestinationName = v.loadDestination?.cityName,
                activeRouteName = v.activeRoute?.routeName,
                posX = v.transform.position.x,
                posY = v.transform.position.y
            });
        }

        string json = JsonConvert.SerializeObject(data, Formatting.Indented);
        File.WriteAllText(SavePath, json);
        Debug.Log($"Гра збережена: {SavePath}");
    }

    public bool HasSave() => File.Exists(SavePath);

    public GameSaveData Load()
    {
        if (!HasSave()) return null;
        string json = File.ReadAllText(SavePath);
        return JsonConvert.DeserializeObject<GameSaveData>(json);
    }

    public void ApplySave(GameSaveData data)
    {
        if (data == null) return;
        var fm = FinanceManager.Instance;
        var tm = GameTimeManager.Instance;
        var wm = WageManager.Instance;

        if (fm != null)
        {
            fm.balance = data.balance;
            fm.totalIncome = data.totalIncome;
            fm.totalExpenses = data.totalExpenses;
            fm.vehicleAssetsValue = data.vehicleAssetsValue;
            fm.roadAssetsValue = data.roadAssetsValue;
        }

        if (wm != null) wm.currentWage = data.currentWage;
        tm?.SetDate(data.saveYear, data.saveMonth, data.saveDay, data.saveHour);

        var allCities = FindObjectsByType<CityNode>(FindObjectsSortMode.None);
        var cityMap = new Dictionary<string, CityNode>();
        foreach (var c in allCities) cityMap[c.cityName] = c;

        // Restore Cities
        if (data.cities != null)
        {
            foreach (var cSave in data.cities)
            {
                if (cityMap.TryGetValue(cSave.cityName, out CityNode c))
                {
                    c.hasWorkshop = cSave.hasWorkshop;
                    c.mechanics = cSave.mechanics;
                    c.repairSlots = cSave.repairSlots;

                    if (cSave.demands != null && c.demands != null)
                    {
                        foreach (var dSave in cSave.demands)
                        {
                            var d = c.demands.Find(x => x.destination != null && x.destination.cityName == dSave.destinationName);
                            if (d != null)
                            {
                                d.currentCargo = dSave.currentCargo;
                                d.maxCargo = dSave.maxCargo;
                                d.currentPassengers = dSave.currentPassengers;
                                d.maxPassengers = dSave.maxPassengers;
                                d.annualGrowth = dSave.annualGrowth;
                            }
                        }
                    }
                }
            }
        }

        // Restore Roads
        if (data.roads != null && RoadNetwork.Instance != null)
        {
            foreach (var rSave in data.roads)
            {
                if (cityMap.TryGetValue(rSave.cityAName, out CityNode cA) && cityMap.TryGetValue(rSave.cityBName, out CityNode cB))
                {
                    var rData = RoadNetwork.Instance.availableRoadTypes?.Find(x => x.roadType == rSave.roadType);
                    if (rData != null)
                    {
                        if (!RoadNetwork.Instance.RoadExists(cA, cB))
                        {
                            RoadNetwork.Instance.BuildRoad(cA, cB, rData);
                        }
                        else
                        {
                            var existing = RoadNetwork.Instance.GetRoad(cA, cB);
                            existing.roadData = rData; // Upgrade directly safely
                        }
                    }
                }
            }
        }

        // Delete all old Routes and Vehicles
        var oldRoutes = FindObjectsByType<RouteDefinition>(FindObjectsSortMode.None);
        foreach (var r in oldRoutes) Destroy(r.gameObject);

        var oldVehicles = FindObjectsByType<VehicleController>(FindObjectsSortMode.None);
        foreach (var v in oldVehicles) Destroy(v.gameObject);

        var rebuiltRoutes = new Dictionary<string, RouteDefinition>();

        // Restore Routes
        if (data.routes != null)
        {
            foreach (var rSave in data.routes)
            {
                GameObject rObj = new GameObject($"Route_{rSave.routeName}");
                var route = rObj.AddComponent<RouteDefinition>();
                route.routeName = rSave.routeName;
                route.routeColor = new Color(rSave.r, rSave.g, rSave.b, rSave.a);
                route.incomeStats = rSave.incomeStats;
                route.stops = new List<RouteStop>();

                if (rSave.routeStops != null)
                {
                    foreach (var sName in rSave.routeStops)
                    {
                        if (cityMap.TryGetValue(sName, out CityNode c))
                        {
                            route.stops.Add(new RouteStop { city = c });
                        }
                    }
                }
                rebuiltRoutes[route.routeName] = route;

                if (RoutesPanel.Instance != null && RoutesPanel.Instance.gameObject.activeInHierarchy)
                    RoutesPanel.Instance.OnRouteCreated(route);
            }
        }

        // Restore Vehicles
        if (data.vehicles != null && ShopPanel.Instance != null)
        {
            foreach (var vSave in data.vehicles)
            {
                var vData = ShopPanel.Instance.availableVehicles?.Find(x => x.name == vSave.dataName);
                if (vData != null && ShopPanel.Instance.vehicleWorldPrefab != null)
                {
                    Vector3 pos = new Vector3(vSave.posX, vSave.posY, 0f);
                    GameObject newVeh = Instantiate(ShopPanel.Instance.vehicleWorldPrefab, pos, Quaternion.identity);
                    var vc = newVeh.GetComponent<VehicleController>();
                    if (vc != null)
                    {
                        vc.vehicleData = vData;
                        vc.customName = vSave.customName;
                        vc.status = vSave.status;
                        vc.condition = vSave.condition;
                        vc.totalRepairCost = vSave.totalRepairCost;
                        vc.monthlyProfit = vSave.monthlyProfit;
                        vc.allTimeProfit = vSave.allTimeProfit;
                        vc.currentLoad = vSave.currentLoad;
                        vc.waitHoursLeft = vSave.waitHoursLeft;

                        if (!string.IsNullOrEmpty(vSave.currentCityName) && cityMap.TryGetValue(vSave.currentCityName, out CityNode cCur))
                            vc.currentCity = cCur;
                        if (!string.IsNullOrEmpty(vSave.targetWorkshopName) && cityMap.TryGetValue(vSave.targetWorkshopName, out CityNode cWork))
                            vc.targetWorkshop = cWork;
                        if (!string.IsNullOrEmpty(vSave.loadDestinationName) && cityMap.TryGetValue(vSave.loadDestinationName, out CityNode cLoad))
                            vc.loadDestination = cLoad;
                        if (!string.IsNullOrEmpty(vSave.activeRouteName) && rebuiltRoutes.TryGetValue(vSave.activeRouteName, out RouteDefinition activeR))
                            vc.activeRoute = activeR;
                    }
                }
            }
        }

        Debug.Log("Гру відновлено повністю!");
    }

    public void DeleteSave()
    {
        if (HasSave()) File.Delete(SavePath);
    }

    public void RestartGame()
    {
        DeleteSave();
        UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
    }
}