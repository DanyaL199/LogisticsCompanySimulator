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

        for (int i = 0; i < trackedVehicles.Count; i++)
        {
            VehicleController v = trackedVehicles[i];
            GameObject row = activeRows[i];

            var nameText = row.transform.Find("Name_Text")?.GetComponent<TextMeshProUGUI>();
            if (nameText) nameText.text = v.vehicleData != null ? v.vehicleData.vehicleName : "Транспорт";

            var statusText = row.transform.Find("Status_Text")?.GetComponent<TextMeshProUGUI>();
            if (statusText) statusText.text = $"{v.status} ({v.condition:F0}%)";

            var condBar = row.transform.Find("Condition_Bar")?.GetComponent<Image>();
            if (condBar) { condBar.gameObject.SetActive(true); condBar.fillAmount = v.condition / 100f; }

            // "Евакуація"
            var repairBtn = row.transform.Find("Repair_Button")?.GetComponent<Button>() ?? row.transform.Find("Btn_Repair")?.GetComponent<Button>();
            if (repairBtn)
            {
                repairBtn.gameObject.SetActive(true);
                repairBtn.onClick.RemoveAllListeners();
                repairBtn.onClick.AddListener(() => v.RequestRepair());
                repairBtn.interactable = v.condition <= 0f && v.status == VehicleStatus.Broken; // Працює тільки для евакуації
            }

            // "Продати"
            var sellBtn = row.transform.Find("Btn_Sell")?.GetComponent<Button>();
            if (sellBtn)
            {
                sellBtn.gameObject.SetActive(true);
                sellBtn.onClick.RemoveAllListeners();
                sellBtn.onClick.AddListener(() => {
                    if (FinanceManager.Instance != null && v.vehicleData != null)
                        FinanceManager.Instance.AddIncome(v.vehicleData.purchaseCost * 0.4f);
                    trackedVehicles.Remove(v);
                    Destroy(v.gameObject);
                    UpdateRows();
                });
            }

            var routeDropdown = row.transform.Find("Route_Dropdown")?.GetComponent<TMP_Dropdown>();
            if (routeDropdown) { routeDropdown.gameObject.SetActive(true); UpdateRouteDropdown(routeDropdown, v); }
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
            options.Add($"{route.routeName} (Зупинок: {route.stops.Count})");
            if (v.activeRoute == route || v.status == VehicleStatus.ReturningToWorkshop)
            {
                // Якщо їде на ремонт — тримаємо його вибраним візуально як "приписаний"
                if (v.activeRoute == route || (v.status == VehicleStatus.ReturningToWorkshop))
                    currentIndex = i + 1;
            }
        }

        if (dropdown.options.Count != options.Count || dropdown.value != currentIndex)
        {
            dropdown.ClearOptions();
            dropdown.AddOptions(options);
            dropdown.SetValueWithoutNotify(currentIndex);

            dropdown.onValueChanged.RemoveAllListeners();
            dropdown.onValueChanged.AddListener((index) => {
                if (index == 0)
                {
                    v.StopRoute();
                }
                else
                {
                    RouteDefinition selectedRoute = allRoutes[index - 1];
                    v.StartRoute(selectedRoute);
                }
            });
        }
    }
}