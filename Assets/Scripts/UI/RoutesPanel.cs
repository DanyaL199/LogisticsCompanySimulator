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
    private List<GameObject>      rows   = new List<GameObject>();

    // ─── Lifecycle ───────────────────────────────────────────

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        btnToggle?.onClick.AddListener(TogglePanel);
        btnClose ?.onClick.AddListener(TogglePanel);
        panelRoot?.SetActive(false);

        // Знайти вже існуючі маршрути на сцені
        var existing = FindObjectsByType<RouteDefinition>(FindObjectsSortMode.None);
        foreach (var r in existing)
            if (!routes.Contains(r)) routes.Add(r);
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
        if (panelRoot != null && panelRoot.activeSelf)
            RebuildRows();
    }

    // ─── Побудова рядків ─────────────────────────────────────

    private void RebuildRows()
    {
        // Очистити старі рядки
        foreach (var r in rows) if (r != null) Destroy(r);
        rows.Clear();

        // Прибрати null-записи (видалені маршрути)
        routes.RemoveAll(r => r == null);

        if (routes.Count == 0)
        {
            var empty = CreateLabelRow("Маршрутів ще немає.\nСтворіть перший через кнопку \"Новий маршрут\".",
                                       new Color(0.6f, 0.6f, 0.6f));
            rows.Add(empty);
            return;
        }

        foreach (var route in routes)
        {
            var captured = route;
            GameObject row = CreateRouteRow(route, captured);
            rows.Add(row);
        }
    }

    private GameObject CreateRouteRow(RouteDefinition route, RouteDefinition captured)
    {
        if (routeRowPrefab != null && routesContent != null)
            return CreateRowFromPrefab(route, captured);
        else
            return CreateRowDynamic(route, captured);
    }

    // ─── Рядок з prefab ──────────────────────────────────────

    private GameObject CreateRowFromPrefab(RouteDefinition route, RouteDefinition captured)
    {
        var row = Instantiate(routeRowPrefab, routesContent);

        // Назва маршруту
        var nameTMP = row.transform.Find("Route_Name")?.GetComponent<TextMeshProUGUI>();
        if (nameTMP != null)
            nameTMP.text = route.routeName;

        // Статус: кількість ТЗ і статус
        var statusTMP = row.transform.Find("Route_Status")?.GetComponent<TextMeshProUGUI>();
        if (statusTMP != null)
            statusTMP.text = GetRouteStatusText(route);

        // Кнопка "Призначити ТЗ"
        var btnAssign = row.transform.Find("Btn_Assign")?.GetComponent<Button>();
        if (btnAssign != null)
            btnAssign.onClick.AddListener(() => OpenAssignPanel(captured));

        // Кнопка "Зупинити"
        var btnStop = row.transform.Find("Btn_Stop")?.GetComponent<Button>();
        if (btnStop != null)
            btnStop.onClick.AddListener(() => StopRoute(captured, row));

        return row;
    }

    // ─── Динамічний рядок (без prefab) ───────────────────────

    private GameObject CreateRowDynamic(RouteDefinition route, RouteDefinition captured)
    {
        // Контейнер рядка
        var row = new GameObject($"RouteRow_{route.routeName}", typeof(RectTransform));
        if (routesContent != null) row.transform.SetParent(routesContent, false);
        var rowRT = row.GetComponent<RectTransform>();
        rowRT.sizeDelta = new Vector2(0, 70);

        // Фон
        var bg = row.AddComponent<Image>();
        bg.color = new Color(0.12f, 0.15f, 0.12f, 0.9f);

        // ── Назва маршруту ──
        var nameObj = new GameObject("Route_Name", typeof(RectTransform));
        nameObj.transform.SetParent(row.transform, false);
        var nameRT = nameObj.GetComponent<RectTransform>();
        nameRT.anchorMin = new Vector2(0, 0.6f);
        nameRT.anchorMax = new Vector2(1, 1f);
        nameRT.offsetMin = new Vector2(8, 0);
        nameRT.offsetMax = new Vector2(-8, -4);
        var nameTMP = nameObj.AddComponent<TextMeshProUGUI>();
        nameTMP.text = route.routeName;
        nameTMP.fontSize = 13;
        nameTMP.fontStyle = FontStyles.Bold;
        nameTMP.color = Color.white;
        nameTMP.alignment = TextAlignmentOptions.MidlineLeft;

        // ── Статус / зупинки ──
        var statusObj = new GameObject("Route_Status", typeof(RectTransform));
        statusObj.transform.SetParent(row.transform, false);
        var statusRT = statusObj.GetComponent<RectTransform>();
        statusRT.anchorMin = new Vector2(0, 0.3f);
        statusRT.anchorMax = new Vector2(1, 0.6f);
        statusRT.offsetMin = new Vector2(8, 0);
        statusRT.offsetMax = new Vector2(-8, 0);
        var statusTMP = statusObj.AddComponent<TextMeshProUGUI>();
        statusTMP.text = GetRouteStatusText(route);
        statusTMP.fontSize = 11;
        statusTMP.color = new Color(0.75f, 0.75f, 0.75f);
        statusTMP.alignment = TextAlignmentOptions.MidlineLeft;

        // ── Кнопка "Призначити ТЗ" ──
        var btnAssignObj = CreateSmallButton(row.transform,
            anchorMin: new Vector2(0f, 0f), anchorMax: new Vector2(0.48f, 0.32f),
            offset: new Vector4(6, 3, -3, -3),
            label: "Призначити ТЗ",
            color: new Color(0.15f, 0.45f, 0.15f));
        btnAssignObj.GetComponent<Button>().onClick.AddListener(() => OpenAssignPanel(captured));

        // ── Кнопка "Зупинити" ──
        var btnStopObj = CreateSmallButton(row.transform,
            anchorMin: new Vector2(0.52f, 0f), anchorMax: new Vector2(1f, 0.32f),
            offset: new Vector4(3, 3, -6, -3),
            label: "Зупинити",
            color: new Color(0.45f, 0.12f, 0.12f));
        btnStopObj.GetComponent<Button>().onClick.AddListener(() => StopRoute(captured, row));

        // Розділювач
        var divObj = new GameObject("Divider", typeof(RectTransform));
        divObj.transform.SetParent(row.transform, false);
        var divRT = divObj.GetComponent<RectTransform>();
        divRT.anchorMin = new Vector2(0, 0);
        divRT.anchorMax = new Vector2(1, 0);
        divRT.sizeDelta = new Vector2(0, 1);
        divRT.anchoredPosition = new Vector2(0, -0.5f);
        var divImg = divObj.AddComponent<Image>();
        divImg.color = new Color(0.3f, 0.3f, 0.3f, 0.5f);

        return row;
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
        // (повноцінний попап — наступний крок)
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

        Debug.Log($"[RoutesPanel] Маршрут зупинено і видалено.");
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
