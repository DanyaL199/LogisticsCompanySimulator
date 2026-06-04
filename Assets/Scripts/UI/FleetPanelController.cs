using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FleetPanelController : MonoBehaviour
{
    public static FleetPanelController Instance { get; private set; }

    [Header("UI Elements")]
    public GameObject fleetPanel;
    public Transform contentTransform;
    public GameObject vehicleRowPrefab;

    [Header("Кнопки відкриття/закриття")]
    public Button btnToggleFleet;
    public Button btnCloseFleet;

    private List<VehicleController> trackedVehicles = new List<VehicleController>();
    private List<GameObject> activeRows = new List<GameObject>();
    private float refreshTimer = 0f;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        if (fleetPanel != null) fleetPanel.SetActive(false);
        if (btnToggleFleet != null) btnToggleFleet.onClick.AddListener(TogglePanel);
        if (btnCloseFleet != null) btnCloseFleet.onClick.AddListener(ClosePanel);
    }

    public void RegisterVehicle(VehicleController vc)
    {
        if (!trackedVehicles.Contains(vc))
        {
            trackedVehicles.Add(vc);
            if (fleetPanel != null && fleetPanel.activeSelf) UpdateRows();
        }
    }

    private int lastToggleFrame = -1;

    public void TogglePanel()
    {
        if (Time.frameCount == lastToggleFrame) return;
        lastToggleFrame = Time.frameCount;

        if (fleetPanel == null) return;
        bool next = !fleetPanel.activeSelf;
        fleetPanel.SetActive(next);

        if (next)
        {
            if (RoutesPanel.Instance != null) RoutesPanel.Instance.ClosePanel();


            UpdateRows();
        }
    }

    public void ClosePanel()
    {
        if (fleetPanel != null) fleetPanel.SetActive(false);
    }

    private void Update()
    {
        if (fleetPanel == null || !fleetPanel.activeSelf) return;

        refreshTimer += Time.deltaTime;
        if (refreshTimer >= 1f)
        {
            refreshTimer = 0f;
            UpdateRowsStats();
        }
    }

    private void UpdateRows()
    {
        trackedVehicles.RemoveAll(v => v == null);

        while (activeRows.Count < trackedVehicles.Count)
        {
            if (vehicleRowPrefab == null)
            {
                Debug.LogError("vehicleRowPrefab is missing! Please assign it in Inspector.");
                break;
            }

            GameObject newRow = Instantiate(vehicleRowPrefab, contentTransform);

            if (newRow.GetComponent<VehicleRowUpdater>() == null)
            {
                newRow.AddComponent<VehicleRowUpdater>();
            }

            activeRows.Add(newRow);
        }

        while (activeRows.Count > trackedVehicles.Count)
        {
            GameObject rowToRemove = activeRows[activeRows.Count - 1];
            activeRows.RemoveAt(activeRows.Count - 1);
            Destroy(rowToRemove);
        }

        int rowsToUpdate = Mathf.Min(activeRows.Count, trackedVehicles.Count);
        for (int i = 0; i < rowsToUpdate; i++)
        {
            UpdateRowContent(activeRows[i], trackedVehicles[i]);
        }
    }

    private void UpdateRowContent(GameObject row, VehicleController v)
    {
        var updater = row.GetComponent<VehicleRowUpdater>();
        if (updater == null) return;

        updater.vehicle = v;
        updater.UpdateData();


        if (updater.btnSell != null)
        {
            updater.btnSell.onClick.RemoveAllListeners();
            updater.btnSell.onClick.AddListener(() => {
                if (FinanceManager.Instance != null && v.vehicleData != null)
                    FinanceManager.Instance.AddIncome(v.vehicleData.purchaseCost * 0.4f);
                trackedVehicles.Remove(v);
                Destroy(v.gameObject);
                UpdateRows();
            });
        }

        if (updater.btnRepair != null)
        {
            updater.btnRepair.onClick.RemoveAllListeners();
            updater.btnRepair.onClick.AddListener(() => {
                v.RequestRepair();
                updater.UpdateStats();
                if (v.status != VehicleStatus.Repairing && v.status != VehicleStatus.ReturningToWorkshop)
                {
                    if (NotificationController.Instance != null) NotificationController.Instance.Show("Немає доступної майстерні або вільних місць!", Color.red);
                }
            });
        }

        if (updater.routeDropdown != null)
        {
            UpdateRouteDropdown(updater.routeDropdown, v);
        }
        else if (updater.btnNextRoute != null)
        {
            updater.btnNextRoute.onClick.RemoveAllListeners();
            updater.btnNextRoute.onClick.AddListener(() => {
                CycleToNextRoute(v);
                UpdateRowsStats();
            });
        }
    }

    private void UpdateRowsStats()
    {
        var updaters = contentTransform.GetComponentsInChildren<VehicleRowUpdater>();
        foreach (var u in updaters) u.UpdateStats();
    }

    private void CycleToNextRoute(VehicleController v)
    {
        RouteDefinition[] allRoutes = FindObjectsByType<RouteDefinition>(FindObjectsSortMode.None);
        if (allRoutes.Length == 0) return;

        int currentIndex = -1;
        for (int i = 0; i < allRoutes.Length; i++)
        {
            if (allRoutes[i] == v.activeRoute) currentIndex = i;
        }

        int nextIndex = currentIndex + 1;
        if (nextIndex >= allRoutes.Length) 
        {
            v.StopRoute();
        }
        else
        {
            v.StartRoute(allRoutes[nextIndex]);
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
            string routeTitle = route.routeName;
            if (route.stops != null && route.stops.Count > 0)
            {
                var names = new List<string>();
                foreach (var s in route.stops) if (s?.city != null) names.Add(s.city.cityName);
                routeTitle = string.Join(" - ", names);
            }
            options.Add(routeTitle);

            if (v.activeRoute == route || v.status == VehicleStatus.ReturningToWorkshop)
            {
                if (v.activeRoute == route || (v.status == VehicleStatus.ReturningToWorkshop))
                    currentIndex = i + 1;
            }
        }

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


            var updater = dropdown.GetComponentInParent<VehicleRowUpdater>();
            if (updater != null) updater.UpdateStats();
        });
    }
}

public class VehicleRowUpdater : MonoBehaviour
{
    public VehicleController vehicle;
    public TextMeshProUGUI nameText;
    public Image iconImage;
    public Image condFill;
    public TextMeshProUGUI statsText;
    public TextMeshProUGUI routeText;

    public Button btnSell;
    public Button btnRepair;
    public TMP_Dropdown routeDropdown;
    public Button btnNextRoute;

    private bool isResolved = false;

    private void AutoResolveReferences()
    {
        if (isResolved) return;
        isResolved = true;

        TMP_Dropdown[] allDropdowns = GetComponentsInChildren<TMP_Dropdown>(true);
        if (allDropdowns.Length > 0 && routeDropdown == null)
        {
            routeDropdown = allDropdowns[0];
            if (routeDropdown.template != null)
            {
                var itemText = routeDropdown.template.GetComponentInChildren<TextMeshProUGUI>(true);
                if (itemText != null)
                {
                    itemText.textWrappingMode = TextWrappingModes.NoWrap;
                    itemText.overflowMode = TextOverflowModes.Overflow;
                }
            }
        }

        Button[] allButtons = GetComponentsInChildren<Button>(true);
        foreach (var b in allButtons)
        {
            if (routeDropdown != null && b.transform.IsChildOf(routeDropdown.transform)) continue;

            string btnName = b.gameObject.name.ToLower();
            string textLower = "";
            var txt = b.GetComponentInChildren<TextMeshProUGUI>(true);
            if (txt != null && !string.IsNullOrEmpty(txt.text)) textLower = txt.text.ToLower().Trim();

            if (btnName.Contains("repair") || btnName.Contains("fix") || textLower.Contains("fix") || textLower.Contains("repair") || textLower.Contains("ремонт"))
            {
                if (btnRepair == null) btnRepair = b;
            }
            else if (btnName.Contains("sell") || btnName.Contains("del") || btnName.Contains("remove") || textLower == "x" || textLower.Contains("sell") || textLower.Contains("продати") || textLower.Contains("del") || textLower.Contains("видалити"))
            {
                if (btnSell == null) btnSell = b;
            }
            else if (btnName.Contains("next") || textLower.Contains("next") || textLower.Contains("маршрут") || textLower.Contains("route"))
            {
                if (btnNextRoute == null) btnNextRoute = b;
            }
        }

        TextMeshProUGUI[] allTexts = GetComponentsInChildren<TextMeshProUGUI>(true);
        foreach (var t in allTexts)
        {
            if (routeDropdown != null && t.transform.IsChildOf(routeDropdown.transform)) continue;

            string n = t.gameObject.name.ToLower();
            if (n.Contains("name") && nameText == null) nameText = t;
            else if ((n.Contains("stat") || t.text.Contains("1M:") || t.text.Contains("ALL:")) && statsText == null) statsText = t;
            else if (n.Contains("route") && !n.Contains("next") && routeText == null) routeText = t;
        }

        if (nameText == null && allTexts.Length > 0 && routeDropdown != null && !allTexts[0].transform.IsChildOf(routeDropdown.transform))
        {
            nameText = allTexts[0];
        }

        Image[] allImages = GetComponentsInChildren<Image>(true);
        foreach (var img in allImages)
        {
            if (routeDropdown != null && img.transform.IsChildOf(routeDropdown.transform)) continue;

            string n = img.gameObject.name.ToLower();
            if (n.Contains("icon") && iconImage == null) iconImage = img;
            else if ((n.Contains("fill") || n.Contains("cond")) && condFill == null) condFill = img;
        }
    }

    public void UpdateData()
    {
        AutoResolveReferences();
        if (vehicle == null) return;

        if (nameText != null)
        {
            nameText.text = vehicle.vehicleData != null ?
                $"{vehicle.vehicleData.vehicleName} <color=#AAAAAA><size=80%>{vehicle.customName}</size></color>" : "Транспорт";
        }

        if (iconImage != null)
        {
            if (vehicle.vehicleData != null && vehicle.vehicleData.icon != null)
            {
                iconImage.sprite = vehicle.vehicleData.icon;
                iconImage.gameObject.SetActive(true);
            }
            else
            {
                iconImage.gameObject.SetActive(false);
            }
        }

        UpdateStats();
    }

    public void UpdateStats()
    {
        try
        {
            if (vehicle == null) return;

            if (condFill != null)
            {
                condFill.fillAmount = vehicle.condition / 100f;
                condFill.color = vehicle.condition > 60f ? Color.green : (vehicle.condition > 30f ? Color.yellow : Color.red);
            }

            if (statsText != null)
            {
                string monthProfitText = $"<color=#A0E0A0>1M:</color> {vehicle.monthlyProfit:F0}";
                string totalProfitText = $"<color=#A0E0A0>ALL:</color> {vehicle.allTimeProfit:F0}";
                statsText.text = $"{monthProfitText}        {totalProfitText}";
            }

            if (btnRepair != null)
            {
                btnRepair.interactable = vehicle.condition <= 99f || vehicle.status == VehicleStatus.Broken;
                var img = btnRepair.GetComponent<Image>();
                if (img != null) img.color = btnRepair.interactable ? new Color(0.12f, 0.38f, 0.18f, 1f) : new Color(0.1f, 0.2f, 0.1f, 1f);
            }

            if (routeText != null)
            {
                if (vehicle.activeRoute != null)
                {
                    string r = vehicle.activeRoute.routeName;
                    if (vehicle.activeRoute.stops != null && vehicle.activeRoute.stops.Count > 0)
                    {
                        var names = new List<string>();
                        foreach (var s in vehicle.activeRoute.stops) if (s?.city != null) names.Add(s.city.cityName);
                        r = string.Join(" - ", names);
                    }
                    routeText.text = r;
                }
                else
                {
                    routeText.text = "<color=#AAAAAA>Немає маршруту</color>";
                }
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[VehicleRowUpdater.UpdateStats] Caught error: {ex.Message}");
        }
    }
}