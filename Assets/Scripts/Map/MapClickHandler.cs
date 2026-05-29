using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using System.Collections.Generic;

/// <summary>
/// Обробляє кліки на карті як під час побудови маршруту, так і у звичайному режимі (відкриття інфо міста).
/// </summary>
public class MapClickHandler : MonoBehaviour
{
    public static MapClickHandler Instance { get; private set; }

    [Header("Налаштування кліку")]
    public float clickRadius = 0.6f;

    [Header("Підсвітка міста при наведенні")]
    public Color hoverColor = new Color(1f, 1f, 0f, 1f);
    public Color selectedColor = new Color(0f, 1f, 0.4f, 1f);
    public Color normalColor = Color.white;

    [Header("Префаб для preview-ліній маршруту")]
    public GameObject linePrefab;

    public bool IsActive { get; private set; } = false;

    private List<CityNode> selectedCities = new List<CityNode>();
    private List<LineRenderer> previewLines = new List<LineRenderer>();
    private CityNode hoveredCity = null;
    private CityNode[] allCities;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        allCities = FindObjectsByType<CityNode>(FindObjectsSortMode.None);
    }

    public void StartBuilding()
    {
        IsActive = true;
        selectedCities.Clear();
        ClearPreviewLines();
    }

    public void StopBuilding()
    {
        IsActive = false;
        ClearPreviewLines();
        ResetAllCityColors();
        hoveredCity = null;
        selectedCities.Clear();
    }

    private void Update()
    {
        // ВАЖЛИВО: Не реагувати на кліки/наведення по карті, якщо курсор/натискання зараз над UI-панеллю!
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            if (hoveredCity != null && (!IsActive || !selectedCities.Contains(hoveredCity)))
            {
                SetCityColor(hoveredCity, normalColor);
                hoveredCity = null;
            }
            return;
        }

        HandleHover();

        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            HandleClick();
    }

    private void HandleHover()
    {
        CityNode nearest = GetCityUnderCursor();
        if (nearest == hoveredCity) return;

        if (hoveredCity != null && (!IsActive || !selectedCities.Contains(hoveredCity)))
            SetCityColor(hoveredCity, normalColor);

        hoveredCity = nearest;

        if (hoveredCity != null && (!IsActive || !selectedCities.Contains(hoveredCity)))
            SetCityColor(hoveredCity, hoverColor);
    }

    private void HandleClick()
    {
        CityNode city = GetCityUnderCursor();
        if (city == null) return;

        if (!IsActive)
        {
            // --- ЗВИЧАЙНИЙ РЕЖИМ ---
            // Клік по місту відкриває інформаційну панель міста
            if (CityInfoPanel.Instance != null)
            {
                CityInfoPanel.Instance.OpenPanel(city);
            }
            return;
        }

        // --- РЕЖИМ ПОБУДОВИ МАРШРУТУ ---
        if (Keyboard.current != null && Keyboard.current.leftShiftKey.isPressed && selectedCities.Contains(city))
        {
            RemoveCity(city);
            return;
        }

        if (selectedCities.Count > 0 && selectedCities[selectedCities.Count - 1] == city)
            return;

        AddCity(city);
    }

    private void AddCity(CityNode city)
    {
        selectedCities.Add(city);
        SetCityColor(city, selectedColor);

        if (selectedCities.Count >= 2)
        {
            CityNode prev = selectedCities[selectedCities.Count - 2];
            AddPreviewLine(prev, city);
        }

        RefreshClosingLine();
        RouteBuilderPanel.Instance?.OnCityAdded(city, selectedCities);
    }

    private void RemoveCity(CityNode city)
    {
        int index = selectedCities.IndexOf(city);
        if (index < 0) return;

        selectedCities.RemoveAt(index);
        SetCityColor(city, normalColor);
        RebuildPreviewLines();
        RouteBuilderPanel.Instance?.OnCityRemoved(city, selectedCities);
    }

    private void AddPreviewLine(CityNode a, CityNode b)
    {
        if (linePrefab == null)
        {
            if (RoadNetwork.Instance != null) linePrefab = RoadNetwork.Instance.roadLinePrefab;
            if (linePrefab == null) return;
        }

        var obj = Instantiate(linePrefab, transform);
        var lr = obj.GetComponent<LineRenderer>();
        if (lr == null) { Destroy(obj); return; }

        lr.positionCount = 2;
        lr.SetPosition(0, a.transform.position);
        lr.SetPosition(1, b.transform.position);
        lr.startWidth = lr.endWidth = 0.14f;
        lr.startColor = lr.endColor = new Color(0.2f, 1f, 0.4f, 0.75f);
        lr.sortingLayerName = "Roads";
        lr.sortingOrder = 3;

        previewLines.Add(lr);
    }

    private LineRenderer closingLine;

    private void RefreshClosingLine()
    {
        if (closingLine != null) Destroy(closingLine.gameObject);
        closingLine = null;

        if (selectedCities.Count < 2) return;

        CityNode first = selectedCities[0];
        CityNode last = selectedCities[selectedCities.Count - 1];
        if (first == last) return;

        if (linePrefab == null)
        {
            if (RoadNetwork.Instance != null) linePrefab = RoadNetwork.Instance.roadLinePrefab;
            if (linePrefab == null) return;
        }

        var obj = Instantiate(linePrefab, transform);
        closingLine = obj.GetComponent<LineRenderer>();
        if (closingLine == null) { Destroy(obj); return; }

        closingLine.positionCount = 2;
        closingLine.SetPosition(0, last.transform.position);
        closingLine.SetPosition(1, first.transform.position);
        closingLine.startWidth = closingLine.endWidth = 0.10f;
        closingLine.startColor = closingLine.endColor = new Color(0.2f, 1f, 0.4f, 0.35f);
        closingLine.sortingLayerName = "Roads";
        closingLine.sortingOrder = 3;
    }

    private void RebuildPreviewLines()
    {
        ClearPreviewLines();
        for (int i = 0; i < selectedCities.Count - 1; i++)
            AddPreviewLine(selectedCities[i], selectedCities[i + 1]);
        RefreshClosingLine();
    }

    private void ClearPreviewLines()
    {
        foreach (var lr in previewLines)
            if (lr != null) Destroy(lr.gameObject);
        previewLines.Clear();

        if (closingLine != null) Destroy(closingLine.gameObject);
        closingLine = null;
    }

    private CityNode GetCityUnderCursor()
    {
        if (allCities == null || allCities.Length == 0) return null;
        if (Mouse.current == null) return null;

        Vector2 screenPos = Mouse.current.position.ReadValue();
        if (Camera.main == null) return null;

        Vector3 worldPos = Camera.main.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, 0f));
        worldPos.z = 0f;

        CityNode nearest = null;
        float minDist = clickRadius;

        foreach (var city in allCities)
        {
            if (city == null) continue;
            float dist = Vector2.Distance(worldPos, city.transform.position);
            if (dist < minDist) { minDist = dist; nearest = city; }
        }

        return nearest;
    }

    private void SetCityColor(CityNode city, Color color)
    {
        if (city == null) return;
        var sr = city.GetComponent<SpriteRenderer>();
        if (sr != null) sr.color = color;
    }

    private void ResetAllCityColors()
    {
        if (allCities == null) return;
        foreach (var city in allCities)
            SetCityColor(city, normalColor);
    }

    public List<CityNode> GetSelectedCities() => new List<CityNode>(selectedCities);

    public void RemoveCityExternal(CityNode city, List<CityNode> newList)
    {
        selectedCities = new List<CityNode>(newList);
        if (!selectedCities.Contains(city))
            SetCityColor(city, normalColor);
        RebuildPreviewLines();
    }
}