using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FleetPanelController : MonoBehaviour
{
    [Header("UI Elements")]
    public GameObject fleetPanel;
    public Transform contentTransform;
    public GameObject vehicleRowPrefab;

    [Header("Кнопки відкриття/закриття")]
    public Button btnToggleFleet;
    public Button btnCloseFleet;

    private List<VehicleController> trackedVehicles = new List<VehicleController>();
    private List<GameObject> activeRows = new List<GameObject>();

    private bool isPanelOpen = false;
    private float refreshTimer = 0f;

    private void Start()
    {
        fleetPanel.SetActive(false);
        if (btnToggleFleet != null) btnToggleFleet.onClick.AddListener(TogglePanel);
        if (btnCloseFleet != null) btnCloseFleet.onClick.AddListener(ClosePanel);
    }

    public void RegisterVehicle(VehicleController vc)
    {
        if (!trackedVehicles.Contains(vc))
        {
            trackedVehicles.Add(vc);
            if (isPanelOpen) UpdateRows();
        }
    }

    private void TogglePanel()
    {
        isPanelOpen = !isPanelOpen;
        fleetPanel.SetActive(isPanelOpen);
        if (isPanelOpen) UpdateRows();
    }

    private void ClosePanel()
    {
        isPanelOpen = false;
        fleetPanel.SetActive(false);
    }

    private void Update()
    {
        if (!isPanelOpen) return;
        refreshTimer += Time.deltaTime;
        if (refreshTimer >= 0.5f)
        {
            refreshTimer = 0f;
            UpdateRows();
        }
    }

    private void UpdateRows()
    {
        trackedVehicles.RemoveAll(v => v == null);

        // Синхронізація кількості рядків
        while (activeRows.Count < trackedVehicles.Count)
        {
            GameObject newRow = Instantiate(vehicleRowPrefab, contentTransform);
            activeRows.Add(newRow);
        }
        while (activeRows.Count > trackedVehicles.Count)
        {
            GameObject rowToRemove = activeRows[activeRows.Count - 1];
            activeRows.RemoveAt(activeRows.Count - 1);
            Destroy(rowToRemove);
        }

        // Оновлюємо дані у рядках
        for (int i = 0; i < trackedVehicles.Count; i++)
        {
            VehicleController v = trackedVehicles[i];
            GameObject row = activeRows[i];

            var nameText = row.transform.Find("Name_Text")?.GetComponent<TextMeshProUGUI>();
            if (nameText) nameText.text = v.vehicleData != null ? v.vehicleData.vehicleName : "Транспорт";

            var statusText = row.transform.Find("Status_Text")?.GetComponent<TextMeshProUGUI>();
            if (statusText) statusText.text = v.status.ToString();

            var condBar = row.transform.Find("Condition_Bar")?.GetComponent<Image>();
            if (condBar) condBar.fillAmount = v.condition / 100f;

            var statsText = row.transform.Find("Stats_Text")?.GetComponent<TextMeshProUGUI>();
            if (statsText) statsText.text = $"Завантажено: {v.currentLoad}";

            var repairBtn = row.transform.Find("Repair_Button")?.GetComponent<Button>();
            if (repairBtn)
            {
                repairBtn.onClick.RemoveAllListeners();
                repairBtn.onClick.AddListener(() => v.RequestRepair());
                repairBtn.interactable = v.condition < 100f && v.status != VehicleStatus.Repairing;
            }

            // Логіка оновлення Dropdown списку маршрутів
            var routeDropdown = row.transform.Find("Route_Dropdown")?.GetComponent<TMP_Dropdown>();
            if (routeDropdown)
            {
                UpdateRouteDropdown(routeDropdown, v);
            }
        }
    }

    private void UpdateRouteDropdown(TMP_Dropdown dropdown, VehicleController v)
    {
        RouteDefinition[] allRoutes = FindObjectsByType<RouteDefinition>(FindObjectsSortMode.None);
        List<string> options = new List<string> { "Немає маршруту" };
        int currentIndex = 0;

        for (int i = 0; i < allRoutes.Length; i++)
        {
            var route = allRoutes[i];
            string routeDesc = $"{route.routeName} (Зупинок: {route.stops.Count})";
            options.Add(routeDesc);

            if (v.activeRoute == route)
                currentIndex = i + 1; // +1 бо 0 резервується під "Немає маршруту"
        }

        // Оновлюємо список лише якщо маршрути змінились, щоб не збивати фокус у меню
        if (dropdown.options.Count != options.Count || dropdown.value != currentIndex)
        {
            dropdown.ClearOptions();
            dropdown.AddOptions(options);
            dropdown.SetValueWithoutNotify(currentIndex);

            dropdown.onValueChanged.RemoveAllListeners();
            dropdown.onValueChanged.AddListener((index) => {
                v.StopRoute(); // Завжди зупиняємо старий
                if (index > 0)
                {
                    RouteDefinition selectedRoute = allRoutes[index - 1];
                    v.StartRoute(selectedRoute); // Стартуємо новий, якщо обрано
                }
            });
        }
    }
}