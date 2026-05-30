using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class GaragePanel : MonoBehaviour
{
    public static GaragePanel Instance { get; private set; }

    [Header("Вікно панелі Гаража")]
    public GameObject panelObj;

    [Header("Кнопки")]
    public Button btnOpenGarage;
    public Button btnCloseGarage;

    [Header("UI Списки")]
    public Transform listContent;
    public GameObject vehicleRowPrefab; // Використовуй префаб рядка з Авто-парку

    private List<GameObject> activeRows = new List<GameObject>();
    private float refreshTimer = 0f;

    private void Awake()
    {
        if (Instance == null) Instance = this;

        if (btnOpenGarage != null) btnOpenGarage.onClick.AddListener(OpenPanel);
        if (btnCloseGarage != null) btnCloseGarage.onClick.AddListener(ClosePanel);

        if (panelObj != null) panelObj.SetActive(false);
    }

    public void OpenPanel()
    {
        panelObj.SetActive(true);
        RefreshList();
    }

    public void ClosePanel()
    {
        panelObj.SetActive(false);
    }

    private void Update()
    {
        if (panelObj == null || !panelObj.activeSelf) return;

        // Оновлюємо інформацію двічі на секунду
        refreshTimer += Time.deltaTime;
        if (refreshTimer >= 0.5f)
        {
            refreshTimer = 0f;
            RefreshList();
        }
    }

    private void RefreshList()
    {
        if (listContent == null || vehicleRowPrefab == null) return;

        // Шукаємо авто, які пов'язані з ремонтом
        var allVehicles = FindObjectsByType<VehicleController>(FindObjectsSortMode.None);
        var repairVehicles = new List<VehicleController>();

        foreach (var v in allVehicles)
        {
            if (v.status == VehicleStatus.Repairing ||
                v.status == VehicleStatus.ReturningToGarage ||
                v.status == VehicleStatus.Broken)
            {
                repairVehicles.Add(v);
            }
        }

        // Балансуємо активні UI рядки
        while (activeRows.Count < repairVehicles.Count)
        {
            activeRows.Add(Instantiate(vehicleRowPrefab, listContent));
        }
        while (activeRows.Count > repairVehicles.Count)
        {
            var row = activeRows[activeRows.Count - 1];
            activeRows.RemoveAt(activeRows.Count - 1);
            Destroy(row);
        }

        // Заповнюємо дані у кожному рядку
        for (int i = 0; i < repairVehicles.Count; i++)
        {
            var v = repairVehicles[i];
            var row = activeRows[i];

            var nameText = row.transform.Find("Name_Text")?.GetComponent<TextMeshProUGUI>();
            var statusText = row.transform.Find("Status_Text")?.GetComponent<TextMeshProUGUI>();
            var condBar = row.transform.Find("Condition_Bar")?.GetComponent<Image>();

            // Ховаємо елементи керування (в гаражі машині не міняють маршрут)
            var routeDropdown = row.transform.Find("Route_Dropdown");
            if (routeDropdown != null) routeDropdown.gameObject.SetActive(false);

            var repairBtn = row.transform.Find("Repair_Button");
            if (repairBtn != null) repairBtn.gameObject.SetActive(false);

            if (nameText != null) nameText.text = v.vehicleData != null ? v.vehicleData.vehicleName : "Невідоме авто";

            if (statusText != null)
            {
                if (v.status == VehicleStatus.Repairing) statusText.text = $"Ремонтується...";
                else if (v.status == VehicleStatus.ReturningToGarage) statusText.text = $"Їде на ремонт...";
                else if (v.status == VehicleStatus.Broken) statusText.text = "Чекає евакуатор";
            }

            if (condBar != null) condBar.fillAmount = v.condition / 100f;
        }
    }
}