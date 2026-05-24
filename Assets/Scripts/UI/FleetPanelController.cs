using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;

public class FleetPanelController : MonoBehaviour
{
    [Header("Панель")]
    public GameObject fleetPanel;
    public Transform vehicleListParent;
    public GameObject vehicleRowPrefab;
    public Button btnToggleFleet;

    private List<VehicleController> trackedVehicles = new List<VehicleController>();
    private List<GameObject> rows = new List<GameObject>();

    private bool isPanelOpen = false;
    private float refreshTimer = 0f;
    private const float REFRESH_INTERVAL = 0.5f;

    private void Start()
    {
        btnToggleFleet?.onClick.AddListener(TogglePanel);
        fleetPanel?.SetActive(false);
    }

    public void RegisterVehicle(VehicleController v)
    {
        if (trackedVehicles.Contains(v)) return;
        trackedVehicles.Add(v);

        // Створити рядок одразу при реєстрації
        if (vehicleRowPrefab != null && vehicleListParent != null)
        {
            var row = Instantiate(vehicleRowPrefab, vehicleListParent);
            rows.Add(row);

            // Підписати кнопку ремонту один раз
            var repairBtn = row.transform.Find("Repair_Button")
                               ?.GetComponent<Button>();
            if (repairBtn != null)
            {
                var captured = v;
                repairBtn.onClick.AddListener(() =>
                {
                    if (captured.status == VehicleStatus.Broken)
                        captured.EmergencyRepair();
                    else
                        captured.PlannedRepair();
                });
            }
        }
    }

    private void TogglePanel()
    {
        isPanelOpen = !isPanelOpen;
        fleetPanel?.SetActive(isPanelOpen);
        if (isPanelOpen) UpdateRows();
    }

    private void Update()
    {
        if (!isPanelOpen) return;

        refreshTimer += Time.deltaTime;
        if (refreshTimer >= REFRESH_INTERVAL)
        {
            refreshTimer = 0f;
            UpdateRows();
        }
    }

    // Оновлює значення існуючих рядків без їх знищення
    private void UpdateRows()
    {
        trackedVehicles.RemoveAll(v => v == null);

        for (int i = 0; i < trackedVehicles.Count; i++)
        {
            if (i >= rows.Count) break;

            var v = trackedVehicles[i];
            var row = rows[i];
            if (row == null) continue;

            // Назва
            var nameText = row.transform.Find("Name_Text")
                              ?.GetComponent<TextMeshProUGUI>();
            if (nameText != null)
                nameText.text = v.vehicleData.vehicleName;

            // Статус
            var statusText = row.transform.Find("Status_Text")
                                ?.GetComponent<TextMeshProUGUI>();
            if (statusText != null)
            {
                statusText.text = GetStatusText(v.status);
                statusText.color = GetStatusColor(v.status);
            }

            // Смужка стану
            var condBar = row.transform.Find("Condition_Bar")
                             ?.GetComponent<Image>();
            if (condBar != null)
            {
                condBar.fillAmount = v.condition / 100f;
                condBar.color = v.condition > 50f ? Color.green
                              : v.condition > 25f ? Color.yellow
                              : Color.red;
            }

            // Кнопка ремонту — тільки оновлюємо стан, не перепідписуємо
            var repairBtn = row.transform.Find("Repair_Button")
                               ?.GetComponent<Button>();
            if (repairBtn != null)
            {
                bool canRepair = v.status == VehicleStatus.Broken
                              || v.condition < 40f;

                repairBtn.interactable = canRepair;

                var img = repairBtn.GetComponent<Image>();
                if (img != null)
                    img.color = canRepair
                        ? new Color(0.7f, 0.15f, 0.15f, 1f)
                        : new Color(0.3f, 0.3f, 0.3f, 0.5f);
            }
        }
    }

    private string GetStatusText(VehicleStatus s)
    {
        switch (s)
        {
            case VehicleStatus.Idle: return "У гаражі";
            case VehicleStatus.Moving: return "У дорозі";
            case VehicleStatus.Broken: return "Зламаний!";
            case VehicleStatus.Strike: return "Страйк!";
            default: return "—";
        }
    }

    private Color GetStatusColor(VehicleStatus s)
    {
        switch (s)
        {
            case VehicleStatus.Idle: return new Color(0.7f, 0.7f, 0.7f);
            case VehicleStatus.Moving: return new Color(0.2f, 0.8f, 0.4f);
            case VehicleStatus.Broken: return new Color(1f, 0.3f, 0.3f);
            case VehicleStatus.Strike: return new Color(1f, 0.6f, 0f);
            default: return Color.white;
        }
    }
}