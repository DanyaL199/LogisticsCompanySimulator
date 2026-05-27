using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

/// <summary>
/// Обробляє кліки на карті під час побудови маршруту.
/// Прикріпити до окремого GameObject (наприклад, "MapInput" у Settings).
/// Активується/деактивується через RouteBuilderPanel.
/// </summary>
public class MapClickHandler : MonoBehaviour
{
    public static MapClickHandler Instance { get; private set; }

    [Header("Налаштування кліку")]
    [Tooltip("Радіус (у world units) для детектування кліку на місто")]
    public float clickRadius = 0.6f;

    [Header("Підсвітка міста при наведенні")]
    public Color hoverColor = new Color(1f, 1f, 0f, 1f);   // жовтий
    public Color selectedColor = new Color(0f, 1f, 0.4f, 1f); // зелений
    public Color normalColor = Color.white;

    [Header("Префаб для preview-ліній маршруту")]
    [Tooltip("Той самий RoadLinePrefab що у RoadNetwork")]
    public GameObject linePrefab;

    // ─── Стан ────────────────────────────────────────────────

    public bool IsActive { get; private set; } = false;

    // Міста обрані гравцем у поточній сесії побудови
    private List<CityNode> selectedCities = new List<CityNode>();

    // Тимчасові лінії preview (очищаємо при скасуванні)
    private List<LineRenderer> previewLines = new List<LineRenderer>();

    // Місто під курсором
    private CityNode hoveredCity = null;

    // Усі міста на сцені (кешуємо один раз)
    private CityNode[] allCities;

    // ─── Ініціалізація ───────────────────────────────────────

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        allCities = FindObjectsByType<CityNode>(FindObjectsSortMode.None);
    }

    // ─── Активація / деактивація ─────────────────────────────

    /// <summary>Викликається RouteBuilderPanel коли гравець натискає "Новий маршрут"</summary>
    public void StartBuilding()
    {
        IsActive = true;
        selectedCities.Clear();
        ClearPreviewLines();
        Debug.Log("[MapClickHandler] Режим побудови маршруту активовано. Клікайте на міста.");
    }

    /// <summary>Викликається при Confirm або Cancel</summary>
    public void StopBuilding()
    {
        IsActive = false;
        ClearPreviewLines();
        ResetAllCityColors();
        hoveredCity = null;
        selectedCities.Clear();
    }

    // ─── Update ──────────────────────────────────────────────

    private void Update()
    {
        if (!IsActive) return;

        HandleHover();

        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            HandleClick();
    }

    // ─── Hover ───────────────────────────────────────────────

    private void HandleHover()
    {
        CityNode nearest = GetCityUnderCursor();

        if (nearest == hoveredCity) return;

        // Зняти підсвітку з попереднього
        if (hoveredCity != null && !selectedCities.Contains(hoveredCity))
            SetCityColor(hoveredCity, normalColor);

        hoveredCity = nearest;

        // Підсвітити новий (якщо ще не обраний)
        if (hoveredCity != null && !selectedCities.Contains(hoveredCity))
            SetCityColor(hoveredCity, hoverColor);
    }

    // ─── Клік ────────────────────────────────────────────────

    private void HandleClick()
    {
        CityNode city = GetCityUnderCursor();
        if (city == null) return;

        // Shift+клік на вже обране місто — видалити з ланцюжка
        if (Keyboard.current != null && Keyboard.current.leftShiftKey.isPressed && selectedCities.Contains(city))
        {
            RemoveCity(city);
            return;
        }

        // Не дозволяти дублікати сусідніх зупинок (A→A)
        if (selectedCities.Count > 0 && selectedCities[selectedCities.Count - 1] == city)
            return;

        AddCity(city);
    }

    // ─── Додавання / видалення міст ──────────────────────────

    private void AddCity(CityNode city)
    {
        selectedCities.Add(city);
        SetCityColor(city, selectedColor);

        // Намалювати preview-лінію від попереднього міста
        if (selectedCities.Count >= 2)
        {
            CityNode prev = selectedCities[selectedCities.Count - 2];
            AddPreviewLine(prev, city);
        }

        // Також замкнути кільце (preview від останнього до першого)
        RefreshClosingLine();

        RouteBuilderPanel.Instance?.OnCityAdded(city, selectedCities);
        Debug.Log($"[MapClickHandler] Додано: {city.cityName} (зупинок: {selectedCities.Count})");
    }

    private void RemoveCity(CityNode city)
    {
        int index = selectedCities.IndexOf(city);
        if (index < 0) return;

        selectedCities.RemoveAt(index);
        SetCityColor(city, normalColor);

        // Перемалювати всі preview-лінії
        RebuildPreviewLines();

        RouteBuilderPanel.Instance?.OnCityRemoved(city, selectedCities);
        Debug.Log($"[MapClickHandler] Видалено: {city.cityName}");
    }

    // ─── Preview-лінії ───────────────────────────────────────

    private void AddPreviewLine(CityNode a, CityNode b)
    {
        if (linePrefab == null)
        {
            // Спробувати взяти prefab з RoadNetwork якщо не призначено вручну
            if (RoadNetwork.Instance != null)
                linePrefab = RoadNetwork.Instance.roadLinePrefab;
            if (linePrefab == null) return;
        }

        var obj = Instantiate(linePrefab, transform);
        var lr = obj.GetComponent<LineRenderer>();
        if (lr == null) { Destroy(obj); return; }

        lr.positionCount = 2;
        lr.SetPosition(0, a.transform.position);
        lr.SetPosition(1, b.transform.position);
        lr.startWidth = lr.endWidth = 0.14f;

        // Пунктирний вигляд через прозорість
        lr.startColor = lr.endColor = new Color(0.2f, 1f, 0.4f, 0.75f);
        lr.sortingLayerName = "Roads";
        lr.sortingOrder = 3; // поверх доріг і підсвіток

        previewLines.Add(lr);
    }

    /// <summary>Лінія що замикає кільце (останнє місто → перше)</summary>
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
        // Пунктирна замикаюча лінія — трохи прозоріша
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

    // ─── Допоміжні ───────────────────────────────────────────

    private CityNode GetCityUnderCursor()
    {
        if (allCities == null || allCities.Length == 0) return null;

        if (Mouse.current == null) return null;
        Vector2 screenPos = Mouse.current.position.ReadValue();
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

    // ─── Публічний доступ ────────────────────────────────────


    public List<CityNode> GetSelectedCities() => new List<CityNode>(selectedCities);


    public void RemoveCityExternal(CityNode city, List<CityNode> newList)
    {
        // Синхронізувати внутрішній список
        selectedCities = new List<CityNode>(newList);

        // Скинути колір видаленого міста
        if (!selectedCities.Contains(city))
            SetCityColor(city, normalColor);

        // Перемалювати всі preview-лінії
        RebuildPreviewLines();
    }
}