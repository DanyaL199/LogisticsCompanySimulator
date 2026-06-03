using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections.Generic;

public class RouteBuilderPanel : MonoBehaviour
{
    public static RouteBuilderPanel Instance { get; private set; }

    [Header("Кореневий об'єкт панелі")]
    public GameObject panelRoot;

    [Header("Контейнер рядків міст (з Vertical Layout Group)")]
    public Transform stopsListParent;

    [Header("Префаб рядка міста у списку")]
    public GameObject stopRowPrefab;

    [Header("Текст відстані")]
    public TextMeshProUGUI distanceText;

    [Header("Текст попередження (немає дороги тощо)")]
    public TextMeshProUGUI warningText;

    [Header("Кнопки")]
    public Button btnNewRoute;
    public Button btnConfirm;
    public Button btnCancel;

    [Header("Префаб маршруту (RouteDefinition + RouteVisualizer)")]
    public GameObject routePrefab;

    [Header("Батько для нових маршрутів у Hierarchy")]
    public Transform routesParent;

    private const float KM_PER_UNIT = 50f;
    private List<CityNode> currentStops = new List<CityNode>();
    private List<GameObject> stopRows = new List<GameObject>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        btnNewRoute?.onClick.AddListener(() => OpenPanel(null));
        btnConfirm?.onClick.AddListener(OnConfirm);
        btnCancel?.onClick.AddListener(OnCancel);
        panelRoot?.SetActive(false);
        SetWarning("");
    }

    private void Update()
    {
        if (panelRoot != null && panelRoot.activeSelf)
            if (Keyboard.current != null && Keyboard.current.backspaceKey.wasPressedThisFrame)
                RemoveLastCity();
    }

    public void OpenPanel(CityNode startCity = null)
    {
        currentStops.Clear();
        ClearStopRows();
        SetWarning("");
        UpdateDistanceText();
        panelRoot?.SetActive(true);
        MapClickHandler.Instance?.StartBuilding(startCity);
    }

    private void ClosePanel()
    {
        panelRoot?.SetActive(false);
        MapClickHandler.Instance?.StopBuilding();
    }

    public void OnCityAdded(CityNode city, List<CityNode> current)
    {
        currentStops = new List<CityNode>(current);
        RebuildStopRows();
        UpdateDistanceText();
        ValidateRoute();
    }

    public void OnCityRemoved(CityNode city, List<CityNode> current)
    {
        currentStops = new List<CityNode>(current);
        RebuildStopRows();
        UpdateDistanceText();
        ValidateRoute();
    }

    private void RemoveLastCity()
    {
        if (currentStops.Count == 0) return;
        CityNode last = currentStops[currentStops.Count - 1];
        currentStops.RemoveAt(currentStops.Count - 1);
        RebuildStopRows();
        UpdateDistanceText();
        ValidateRoute();
        MapClickHandler.Instance?.RemoveCityExternal(last, currentStops);
    }

    private void RemoveCityAt(int index)
    {
        if (index < 0 || index >= currentStops.Count) return;
        CityNode city = currentStops[index];
        currentStops.RemoveAt(index);
        RebuildStopRows();
        UpdateDistanceText();
        ValidateRoute();
        MapClickHandler.Instance?.RemoveCityExternal(city, currentStops);
    }

    private void RebuildStopRows()
    {
        ClearStopRows();

        for (int i = 0; i < currentStops.Count; i++)
        {
            int captured = i;
            CityNode city = currentStops[i];

            GameObject row;

            if (stopRowPrefab != null && stopsListParent != null)
            {
                row = Instantiate(stopRowPrefab, stopsListParent);
            }
            else
            {
                row = new GameObject($"StopRow_{i}", typeof(RectTransform));
                if (stopsListParent != null)
                    row.transform.SetParent(stopsListParent, false);
                var rt = row.GetComponent<RectTransform>();
                rt.sizeDelta = new Vector2(0, 28);

                var textObj = new GameObject("Label", typeof(RectTransform));
                textObj.transform.SetParent(row.transform, false);
                var textRT = textObj.GetComponent<RectTransform>();
                textRT.anchorMin = Vector2.zero;
                textRT.anchorMax = Vector2.one;
                textRT.offsetMin = new Vector2(4, 0);
                textRT.offsetMax = new Vector2(-36, 0);
                var tmp = textObj.AddComponent<TextMeshProUGUI>();
                tmp.fontSize = 13;
                tmp.color = Color.white;
                tmp.alignment = TextAlignmentOptions.MidlineLeft;

                var btnObj = new GameObject("BtnRemove", typeof(RectTransform));
                btnObj.transform.SetParent(row.transform, false);
                var btnRT = btnObj.GetComponent<RectTransform>();
                btnRT.anchorMin = new Vector2(1, 0);
                btnRT.anchorMax = new Vector2(1, 1);
                btnRT.offsetMin = new Vector2(-32, 2);
                btnRT.offsetMax = new Vector2(-2, -2);
                var img = btnObj.AddComponent<Image>();
                img.color = new Color(0.7f, 0.15f, 0.15f, 0.85f);
                var btn = btnObj.AddComponent<Button>();
                var xTextObj = new GameObject("X", typeof(RectTransform));
                xTextObj.transform.SetParent(btnObj.transform, false);
                var xRT = xTextObj.GetComponent<RectTransform>();
                xRT.anchorMin = Vector2.zero; xRT.anchorMax = Vector2.one;
                xRT.offsetMin = xRT.offsetMax = Vector2.zero;
                var xTmp = xTextObj.AddComponent<TextMeshProUGUI>();
                xTmp.text = "X";
                xTmp.fontSize = 12;
                xTmp.alignment = TextAlignmentOptions.Center;
                xTmp.color = Color.white;
                btn.onClick.AddListener(() => RemoveCityAt(captured));
            }

            var labelTMP = row.transform.Find("Label")?.GetComponent<TextMeshProUGUI>();
            if (labelTMP == null) labelTMP = row.GetComponentInChildren<TextMeshProUGUI>();
            if (labelTMP != null)
                labelTMP.text = $"{i + 1}. {GetTypeTag(city.cityType)} {city.cityName}";

            var removeBtn = row.transform.Find("BtnRemove")?.GetComponent<Button>();
            if (removeBtn != null)
            {
                removeBtn.onClick.RemoveAllListeners();
                removeBtn.onClick.AddListener(() => RemoveCityAt(captured));
            }

            stopRows.Add(row);
        }

        if (currentStops.Count >= 2)
        {
            var closingRow = CreateLabelRow($"(коло) -> {currentStops[0].cityName}",
                                            new Color(0.5f, 0.9f, 0.5f, 0.7f));
            stopRows.Add(closingRow);
        }
    }

    private GameObject CreateLabelRow(string text, Color color)
    {
        var row = new GameObject("ClosingRow", typeof(RectTransform));
        if (stopsListParent != null)
            row.transform.SetParent(stopsListParent, false);
        var rt = row.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(0, 22);

        var textObj = new GameObject("Label", typeof(RectTransform));
        textObj.transform.SetParent(row.transform, false);
        var tRT = textObj.GetComponent<RectTransform>();
        tRT.anchorMin = Vector2.zero; tRT.anchorMax = Vector2.one;
        tRT.offsetMin = new Vector2(4, 0); tRT.offsetMax = Vector2.zero;
        var tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = 11;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.MidlineLeft;
        return row;
    }

    private void ClearStopRows()
    {
        foreach (var r in stopRows)
            if (r != null) Destroy(r);
        stopRows.Clear();
    }

    private void UpdateDistanceText()
    {
        if (distanceText == null) return;

        if (currentStops.Count < 2)
        {
            distanceText.text = "Відстань: —";
            return;
        }

        float totalKm = 0f;
        for (int i = 0; i < currentStops.Count; i++)
        {
            CityNode a = currentStops[i];
            CityNode b = currentStops[(i + 1) % currentStops.Count];
            totalKm += Vector3.Distance(a.transform.position, b.transform.position) * KM_PER_UNIT;
        }
        distanceText.text = $"Відстань кола: {totalKm:F0} км";
    }

    private void ValidateRoute()
    {
        if (currentStops.Count < 2) { SetWarning(""); return; }

        string missing = FindMissingRoad();
        if (missing != null)
            SetWarning($"Немає дороги: {missing}");
        else
            SetWarning("");
    }

    private string FindMissingRoad()
    {
        if (RoadNetwork.Instance == null) return null;
        for (int i = 0; i < currentStops.Count; i++)
        {
            CityNode a = currentStops[i];
            CityNode b = currentStops[(i + 1) % currentStops.Count];
            if (!RoadNetwork.Instance.RoadExists(a, b))
                return $"{a.cityName} - {b.cityName}";
        }
        return null;
    }

    private void OnConfirm()
    {
        if (currentStops.Count < 2)
        {
            SetWarning("Оберіть мінімум 2 міста.");
            return;
        }
        if (FindMissingRoad() != null)
        {
            SetWarning("Спочатку побудуйте відсутні дороги.");
            return;
        }

        RouteDefinition route = CreateRouteDefinition();
        if (route == null) { SetWarning("Помилка створення маршруту."); return; }

        Debug.Log($"[RouteBuilderPanel] Маршрут '{route.routeName}' створено.");

        RoutesPanel.Instance?.OnRouteCreated(route);
        ClosePanel();

        RouteVisualizer.RebuildAllHighlights();
    }

    private void OnCancel() => ClosePanel();

    private RouteDefinition CreateRouteDefinition()
    {
        GameObject obj;

        if (routePrefab != null)
            obj = Instantiate(routePrefab, routesParent != null ? routesParent : transform);
        else
        {
            obj = new GameObject("Route_New");
            if (routesParent != null) obj.transform.SetParent(routesParent);
            obj.AddComponent<RouteDefinition>();
            var vis = obj.AddComponent<RouteVisualizer>();
            if (RoadNetwork.Instance != null) vis.linePrefab = RoadNetwork.Instance.roadLinePrefab;
        }

        var route = obj.GetComponent<RouteDefinition>();
        if (route == null) { Destroy(obj); return null; }

        // Додаємо рандомний колір для нового маршруту якщо він створюється (щоб відрізнялись)
        route.routeColor = new Color(Random.Range(0.2f, 1f), Random.Range(0.2f, 1f), Random.Range(0.2f, 1f));

        string first = currentStops[0].cityName;
        string last = currentStops[currentStops.Count - 1].cityName;
        route.routeName = first == last ? $"{first} (коло)" : $"{first} - {last}";
        obj.name = $"Route_{first}_{last}";

        route.stops.Clear();
        foreach (var city in currentStops)
            route.stops.Add(new RouteStop { city = city });

        var visualizer = obj.GetComponent<RouteVisualizer>();
        if (visualizer != null) visualizer.BuildHighlightLines();

        return route;
    }

    private void SetWarning(string msg)
    {
        if (warningText == null) return;
        warningText.text = msg;
        warningText.gameObject.SetActive(!string.IsNullOrEmpty(msg));
    }

    private string GetTypeTag(CityType type)
    {
        switch (type)
        {
            case CityType.Industrial: return "[Пром]";
            case CityType.Trade: return "[Торг]";
            case CityType.Tourist: return "[Тур]";
            default: return "";
        }
    }
}