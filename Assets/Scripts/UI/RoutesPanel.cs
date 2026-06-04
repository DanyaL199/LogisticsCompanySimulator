using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class RoutesPanel : MonoBehaviour
{
    public static RoutesPanel Instance { get; private set; }

    [Header("Кореневий об'єкт панелі")]
    public GameObject panelRoot;

    [Header("Контейнер рядків маршрутів")]
    public Transform routesContent;

    [Header("Префаб рядка маршруту")]
    public GameObject routeRowPrefab;

    [Header("Кнопки")]
    public Button btnToggle;
    public Button btnClose;

    private List<RouteDefinition> routes = new List<RouteDefinition>();
    private List<GameObject> rows = new List<GameObject>();
    private float refreshTimer = 0f;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        btnToggle?.onClick.AddListener(TogglePanel);
        btnClose?.onClick.AddListener(TogglePanel);
        panelRoot?.SetActive(false);

        // Знайти вже існуючі маршрути на сцені
        var existing = FindObjectsByType<RouteDefinition>(FindObjectsSortMode.None);
        foreach (var r in existing)
            if (!routes.Contains(r)) routes.Add(r);
    }

    private void Update()
    {
        if (panelRoot != null && panelRoot.activeSelf)
        {
            refreshTimer += Time.deltaTime;
            if (refreshTimer >= 1f)
            {
                refreshTimer = 0f;
                UpdateRowsStats();
            }
        }
    }

    private void TogglePanel()
    {
        bool next = !(panelRoot != null && panelRoot.activeSelf);
        panelRoot?.SetActive(next);
        if (next)
        {
            RebuildRows();
            if (FleetPanelController.Instance != null) FleetPanelController.Instance.ClosePanel();
        }
    }

    public void ClosePanel()
    {
        if (panelRoot != null) panelRoot.SetActive(false);
    }

    public void OnRouteCreated(RouteDefinition route)
    {
        if (!routes.Contains(route))
            routes.Add(route);

        if (panelRoot != null && panelRoot.activeSelf) RebuildRows();
    }

    private void RebuildRows()
    {
        foreach (var r in rows) if (r != null) Destroy(r);
        rows.Clear();

        routes.RemoveAll(r => r == null);

        if (routes.Count == 0)
        {
            rows.Add(CreateLabelRow("Маршрутів ще немає.\nСтворіть перший через кнопку \"Новий маршрут\".",
                                       new Color(0.6f, 0.6f, 0.6f)));
            return;
        }

        foreach (var route in routes)
        {
            rows.Add(CreateRouteRow(route, route));
        }
    }

    private GameObject CreateRouteRow(RouteDefinition route, RouteDefinition captured)
    {
        if (routeRowPrefab == null)
        {
            Debug.LogError("routeRowPrefab is missing! Please assign it in the Inspector.");
            return null;
        }

        var row = Instantiate(routeRowPrefab, routesContent);

        var updater = row.GetComponent<RouteRowUpdater>();
        if (updater == null) updater = row.AddComponent<RouteRowUpdater>();

        updater.route = route;

        var nameTMP = row.transform.Find("Route_Name")?.GetComponent<TextMeshProUGUI>();
        if (nameTMP != null) nameTMP.text = GetRouteTitle(route);

        updater.statusText = row.transform.Find("Route_Status")?.GetComponent<TextMeshProUGUI>();

        var btnAssign = row.transform.Find("Btn_Assign")?.GetComponent<Button>() ?? row.transform.Find("AssignButton")?.GetComponent<Button>();
        if (btnAssign != null)
        {
            btnAssign.onClick.RemoveAllListeners();
            btnAssign.onClick.AddListener(() => OpenAssignPanel(captured));
        }

        var btnStop = row.transform.Find("Btn_Delete")?.GetComponent<Button>() ?? row.transform.Find("Btn_Stop")?.GetComponent<Button>() ?? row.transform.Find("StopButton")?.GetComponent<Button>();
        if (btnStop != null)
        {
            btnStop.onClick.RemoveAllListeners();
            btnStop.onClick.AddListener(() => StopRoute(captured, row));
        }

        updater.UpdateStats();
        return row;
    }

    private string GetRouteTitle(RouteDefinition route)
    {
        string routeTitle = route.routeName;
        if (route.stops != null && route.stops.Count > 0)
        {
            var names = new List<string>();
            foreach (var s in route.stops) if (s?.city != null) names.Add(s.city.cityName);
            routeTitle = string.Join(" - ", names);
        }
        return routeTitle;
    }

    private void UpdateRowsStats()
    {
        var updaters = GetComponentsInChildren<RouteRowUpdater>();
        foreach (var u in updaters) u.UpdateStats();
    }

    private void OpenAssignPanel(RouteDefinition route)
    {
        var all = FindObjectsByType<VehicleController>(FindObjectsSortMode.None);
        List<VehicleController> idle = new List<VehicleController>();
        foreach (var v in all)
            if (v.status == VehicleStatus.Idle) idle.Add(v);

        if (idle.Count == 0)
        {
            Debug.LogWarning("[RoutesPanel] Немає вільних ТЗ для призначення.");
            return;
        }

        if (idle.Count == 1)
        {
            AssignVehicle(idle[0], route);
            return;
        }

        Debug.Log($"[RoutesPanel] Знайдено {idle.Count} вільних ТЗ. Призначається перший: {idle[0].vehicleData.vehicleName}");
        AssignVehicle(idle[0], route);
    }

    private void AssignVehicle(VehicleController vehicle, RouteDefinition route)
    {
        bool ok = vehicle.StartRoute(route);
        if (ok)
        {
            Debug.Log($"[RoutesPanel] {vehicle.vehicleData.vehicleName} призначено на '{route.routeName}'");
            RebuildRows(); // оновити статус у списку
        }
        else
        {
            Debug.LogWarning($"[RoutesPanel] Не вдалось запустити маршрут для {vehicle.vehicleData.vehicleName}");
        }
    }

    private void StopRoute(RouteDefinition route, GameObject row)
    {
        var all = FindObjectsByType<VehicleController>(FindObjectsSortMode.None);
        foreach (var v in all)
            if (v.activeRoute == route) v.StopRoute();

        routes.Remove(route);
        if (route != null) Destroy(route.gameObject);
        if (row != null) { rows.Remove(row); Destroy(row); }

        Invoke(nameof(RebuildHighlightsDelayed), 0.1f);
    }

    private void RebuildHighlightsDelayed()
    {
        RouteVisualizer.RebuildAllHighlights();
    }

    private GameObject CreateLabelRow(string text, Color color)
    {
        var row = new GameObject("EmptyLabel", typeof(RectTransform));
        if (routesContent != null) row.transform.SetParent(routesContent, false);
        var rt = row.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(0, 50);
        var textObj = new GameObject("Label", typeof(RectTransform));
        textObj.transform.SetParent(row.transform, false);
        var tRT = textObj.GetComponent<RectTransform>();
        tRT.anchorMin = Vector2.zero; tRT.anchorMax = Vector2.one;
        tRT.offsetMin = new Vector2(8, 4); tRT.offsetMax = new Vector2(-8, -4);
        var tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = text; tmp.fontSize = 12;
        tmp.color = color; tmp.alignment = TextAlignmentOptions.MidlineLeft;
        return row;
    }
}

public class RouteRowUpdater : MonoBehaviour
{
    public RouteDefinition route;
    public TextMeshProUGUI statusText;

    public void UpdateStats()
    {
        if (route == null || statusText == null) return;

        var all = FindObjectsByType<VehicleController>(FindObjectsSortMode.None);
        int count = 0; foreach (var v in all) if (v.activeRoute == route) count++;

        string profitT = $"<color=#A0E0A0>$ PROFIT</color>   {route.incomeStats:F0}";
        string vehT = $"<color=#A0E0A0>VEHICLES COUNT</color>   {count}";

        statusText.text = profitT + "          " + vehT;
    }
}