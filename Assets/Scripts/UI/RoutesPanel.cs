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
    [Tooltip("Якщо пусто — рядки генеруються динамічно")]
    public GameObject routeRowPrefab;

    [Header("Кнопки")]
    public Button btnToggle;
    public Button btnClose;

    // Всі відомі маршрути (додаються при створенні або знаходяться на старті)
    private List<RouteDefinition> routes = new List<RouteDefinition>();
    private List<GameObject> rows = new List<GameObject>();
    private float refreshTimer = 0f;

    // ─── Lifecycle ───────────────────────────────────────────

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

    // ─── Відкрити / закрити ──────────────────────────────────

    private void TogglePanel()
    {
        bool next = !(panelRoot != null && panelRoot.activeSelf);
        panelRoot?.SetActive(next);
        if (next) RebuildRows();
    }

    // ─── Додавання маршруту ───────────────────────────────────

    public void OnRouteCreated(RouteDefinition route)
    {
        if (!routes.Contains(route))
            routes.Add(route);

        // Якщо панель відкрита — оновити список
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
        if (routeRowPrefab != null && routesContent != null)
            return CreateRowFromPrefab(route, captured);
        else
            return CreateRowDynamic(route, captured);
    }

    private GameObject CreateRowFromPrefab(RouteDefinition route, RouteDefinition captured)
    {
        var row = Instantiate(routeRowPrefab, routesContent);

        var nameTMP = row.transform.Find("Route_Name")?.GetComponent<TextMeshProUGUI>();
        if (nameTMP != null) nameTMP.text = route.routeName;

        var statusTMP = row.transform.Find("Route_Status")?.GetComponent<TextMeshProUGUI>();
        if (statusTMP != null) statusTMP.text = GetRouteStatusText(route);

        var btnAssign = row.transform.Find("Btn_Assign")?.GetComponent<Button>() ?? row.transform.Find("AssignButton")?.GetComponent<Button>();
        if (btnAssign != null) btnAssign.onClick.AddListener(() => OpenAssignPanel(captured));

        var btnStop = row.transform.Find("Btn_Stop")?.GetComponent<Button>() ?? row.transform.Find("StopButton")?.GetComponent<Button>();
        if (btnStop != null) btnStop.onClick.AddListener(() => StopRoute(captured, row));

        return row;
    }

    private GameObject CreateRowDynamic(RouteDefinition route, RouteDefinition captured)
    {
        var row = new GameObject($"RouteRow_{route.routeName}", typeof(RectTransform));
        if (routesContent != null) row.transform.SetParent(routesContent, false);

        // Name of the Route
        string routeTitle = route.routeName;
        if (route.stops != null && route.stops.Count > 0)
        {
            var names = new List<string>();
            foreach (var s in route.stops) if (s?.city != null) names.Add(s.city.cityName);
            routeTitle = string.Join(" - ", names);
        }

        // Динамічний розрахунок висоти на основі довжини назви
        int estimatedLines = Mathf.CeilToInt((float)routeTitle.Length / 35f);
        float rowHeight = Mathf.Max(70f, 35f + (estimatedLines * 20f));

        var rowRT = row.GetComponent<RectTransform>();
        rowRT.sizeDelta = new Vector2(0, rowHeight);

        // Main Background (Green theme like in the screenshot)
        var bg = row.AddComponent<Image>();
        bg.color = new Color(0.14f, 0.44f, 0.22f, 1f);

        // Border below
        var borderObj = new GameObject("Border", typeof(RectTransform));
        borderObj.transform.SetParent(row.transform, false);
        var borderRT = borderObj.GetComponent<RectTransform>();
        borderRT.anchorMin = new Vector2(0, 0); borderRT.anchorMax = new Vector2(1, 0);
        borderRT.sizeDelta = new Vector2(0, 2);
        var borderImg = borderObj.AddComponent<Image>();
        borderImg.color = new Color(1f, 1f, 1f, 0.3f);

        // Route Status / Stats (Фіксовано знизу)
        var statusObj = new GameObject("Route_Status", typeof(RectTransform));
        statusObj.transform.SetParent(row.transform, false);
        var statusRT = statusObj.GetComponent<RectTransform>();
        statusRT.anchorMin = new Vector2(0, 0f);
        statusRT.anchorMax = new Vector2(1, 0f); // Прив'язка до низу
        statusRT.offsetMin = new Vector2(12, 4);
        statusRT.offsetMax = new Vector2(-60, 35); // Фіксована висота 31
        var statusTMP = statusObj.AddComponent<TextMeshProUGUI>();
        statusTMP.fontSize = 13;
        statusTMP.fontStyle = FontStyles.Bold;
        statusTMP.alignment = TextAlignmentOptions.MidlineLeft;
        statusTMP.color = Color.white;

        // Route Name (Додаємо перенесення рядків та прив'язку до вільного місця)
        var nameObj = new GameObject("Route_Name", typeof(RectTransform));
        nameObj.transform.SetParent(row.transform, false);
        var nameRT = nameObj.GetComponent<RectTransform>();
        nameRT.anchorMin = new Vector2(0, 0f);
        nameRT.anchorMax = new Vector2(1, 1f); // Розтягнути
        nameRT.offsetMin = new Vector2(12, 35); // Починається над статусом
        nameRT.offsetMax = new Vector2(-60, -4);
        var nameTMP = nameObj.AddComponent<TextMeshProUGUI>();
        nameTMP.text = routeTitle;
        nameTMP.fontSize = 15;
        nameTMP.fontStyle = FontStyles.Bold;
        nameTMP.color = Color.white;
        nameTMP.alignment = TextAlignmentOptions.BottomLeft;
        nameTMP.textWrappingMode = TextWrappingModes.Normal;

        var updater = row.AddComponent<RouteRowUpdater>();
        updater.route = route;
        updater.statusText = statusTMP;
        updater.UpdateStats();

        // Delete Button (Right Side)
        var btnStopObj = new GameObject("Btn_Delete", typeof(RectTransform));
        btnStopObj.transform.SetParent(row.transform, false);
        var stopRT = btnStopObj.GetComponent<RectTransform>();
        stopRT.anchorMin = new Vector2(1, 0);
        stopRT.anchorMax = new Vector2(1, 1);
        stopRT.offsetMin = new Vector2(-60, 0);
        stopRT.offsetMax = new Vector2(0, 0);

        var imgStop = btnStopObj.AddComponent<Image>();
        imgStop.color = new Color(0.12f, 0.38f, 0.18f, 1f);

        var btnStop = btnStopObj.AddComponent<Button>();
        btnStop.onClick.AddListener(() => StopRoute(captured, row));

        // Separator line
        var sepObj = new GameObject("Separator", typeof(RectTransform));
        sepObj.transform.SetParent(btnStopObj.transform, false);
        var sepRT = sepObj.GetComponent<RectTransform>();
        sepRT.anchorMin = new Vector2(0, 0); sepRT.anchorMax = new Vector2(0, 1);
        sepRT.sizeDelta = new Vector2(2, 0);
        var sepImg = sepObj.AddComponent<Image>();
        sepImg.color = new Color(1f, 1f, 1f, 0.2f);

        // Icon Trash (Замінено на X)
        var iconObj = new GameObject("Icon", typeof(RectTransform));
        iconObj.transform.SetParent(btnStopObj.transform, false);
        var iconRT = iconObj.GetComponent<RectTransform>();
        iconRT.anchorMin = Vector2.zero; iconRT.anchorMax = Vector2.one;
        iconRT.offsetMin = iconRT.offsetMax = Vector2.zero;
        var tmpIcon = iconObj.AddComponent<TextMeshProUGUI>();
        tmpIcon.text = "✖";
        tmpIcon.fontSize = 20;
        tmpIcon.alignment = TextAlignmentOptions.Center;
        tmpIcon.color = Color.white;

        return row;
    }

    private void UpdateRowsStats()
    {
        var updaters = GetComponentsInChildren<RouteRowUpdater>();
        foreach (var u in updaters) u.UpdateStats();
    }

    private GameObject CreateSmallButton(Transform parent,
        Vector2 anchorMin, Vector2 anchorMax, Vector4 offset,
        string label, Color color)
    {
        var obj = new GameObject("Btn", typeof(RectTransform));
        obj.transform.SetParent(parent, false);
        var rt = obj.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = new Vector2(offset.x, offset.y);
        rt.offsetMax = new Vector2(offset.z, offset.w);
        var img = obj.AddComponent<Image>();
        img.color = color;
        var btn = obj.AddComponent<Button>();
        var cb = btn.colors;
        cb.highlightedColor = new Color(color.r + 0.15f, color.g + 0.15f, color.b + 0.15f);
        btn.colors = cb;

        var textObj = new GameObject("Text", typeof(RectTransform));
        textObj.transform.SetParent(obj.transform, false);
        var tRT = textObj.GetComponent<RectTransform>();
        tRT.anchorMin = Vector2.zero; tRT.anchorMax = Vector2.one;
        tRT.offsetMin = tRT.offsetMax = Vector2.zero;
        var tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 10;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;

        return obj;
    }

    // ─── Дії ─────────────────────────────────────────────────
    private void OpenAssignPanel(RouteDefinition route)
    {
        // Знайти вільні ТЗ
        var all = FindObjectsByType<VehicleController>(FindObjectsSortMode.None);
        List<VehicleController> idle = new List<VehicleController>();
        foreach (var v in all)
            if (v.status == VehicleStatus.Idle) idle.Add(v);

        if (idle.Count == 0)
        {
            Debug.LogWarning("[RoutesPanel] Немає вільних ТЗ для призначення.");
            return;
        }

        // Якщо є тільки один вільний ТЗ — призначаємо одразу
        if (idle.Count == 1)
        {
            AssignVehicle(idle[0], route);
            return;
        }

        // Якщо є кілька — призначити перший і залогувати варіанти
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
        // Знайти всі ТЗ на цьому маршруті і зупинити
        var all = FindObjectsByType<VehicleController>(FindObjectsSortMode.None);
        foreach (var v in all)
            if (v.activeRoute == route) v.StopRoute();

        routes.Remove(route);
        if (route != null) Destroy(route.gameObject);
        if (row != null) { rows.Remove(row); Destroy(row); }

        // Оновлюємо решту маршрутів щоб їхні лінії правильно перешикувалися
        Invoke(nameof(RebuildHighlightsDelayed), 0.1f);
    }

    private void RebuildHighlightsDelayed()
    {
        RouteVisualizer.RebuildAllHighlights();
    }

    // ─── Допоміжні ───────────────────────────────────────────

    private string GetRouteStatusText(RouteDefinition route)
    {
        // Підрахувати скільки ТЗ на цьому маршруті
        var all = FindObjectsByType<VehicleController>(FindObjectsSortMode.None);
        int count = 0;
        foreach (var v in all)
            if (v.activeRoute == route) count++;

        // Зупинки
        string stops = "";
        if (route.stops != null && route.stops.Count > 0)
        {
            var names = new List<string>();
            foreach (var s in route.stops)
                if (s?.city != null) names.Add(s.city.cityName);
            stops = string.Join(" - ", names);
        }

        string vehicleInfo = count == 0
            ? "<color=#FF8888>Без транспорту</color>"
            : $"<color=#88FF88>ТЗ: {count}</color>";

        return $"{stops}\n{vehicleInfo}";
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