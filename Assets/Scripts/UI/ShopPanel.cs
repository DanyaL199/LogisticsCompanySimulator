using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ShopPanel : MonoBehaviour
{
    public static ShopPanel Instance { get; private set; }

    [Header("Об'єкт панелі (видима частина)")]
    public GameObject panelObj;

    [Header("Кнопки керування вікном")]
    public Button btnOpenShop;
    public Button btnCloseShop;

    [Header("UI Списки")]
    public Transform listContent;
    public GameObject vehicleItemPrefab;

    [Header("База Машин (Вантажні та Пасажирські)")]
    public List<VehicleData> availableVehicles = new List<VehicleData>();

    [Header("Префаб для створення у світі")]
    [Tooltip("ВАЖЛИВО: Перетягни сюди префаб звичайної машини, що має VehicleController на собі")]
    public GameObject vehicleWorldPrefab;

    private void Awake()
    {
        if (Instance == null) Instance = this;

        // Налаштовуємо кнопки
        if (btnOpenShop != null)
        {
            btnOpenShop.onClick.RemoveAllListeners();
            btnOpenShop.onClick.AddListener(TogglePanel);
        }

        if (btnCloseShop != null)
        {
            btnCloseShop.onClick.RemoveAllListeners();
            btnCloseShop.onClick.AddListener(ClosePanel);
        }

        if (panelObj != null) panelObj.SetActive(false);
    }

    public void TogglePanel()
    {
        if (panelObj != null && panelObj.activeSelf) ClosePanel();
        else OpenPanel();
    }

    public void OpenPanel()
    {
        panelObj.SetActive(true);
        PopulateShop();
    }

    public void ClosePanel()
    {
        panelObj.SetActive(false);
    }

    private void PopulateShop()
    {
        foreach (Transform child in listContent) Destroy(child.gameObject);

        foreach (var vData in availableVehicles)
        {
            if (vData == null) continue;

            GameObject row = Instantiate(vehicleItemPrefab, listContent);

            var nameText = row.transform.Find("Name_Text")?.GetComponent<TextMeshProUGUI>();
            var statsText = row.transform.Find("Stats_Text")?.GetComponent<TextMeshProUGUI>();
            var priceText = row.transform.Find("Price_Text")?.GetComponent<TextMeshProUGUI>();
            var buyBtn = row.transform.Find("Buy_Button")?.GetComponent<Button>();
            var iconImg = row.transform.Find("Vehicle_Image")?.GetComponent<Image>();

            if (iconImg != null && vData.icon != null) iconImg.sprite = vData.icon;
            if (nameText != null) nameText.text = vData.vehicleName;

            if (statsText != null)
            {
                statsText.text = $"Місткість: {vData.maxCapacity} | Швидкість: {vData.maxSpeedKmh} км/год\n" +
                                 $"Пальне: {vData.fuelPer100km} л/100км | Міцність: {vData.maintenanceCost}";
            }

            if (priceText != null) priceText.text = $"{vData.purchaseCost:F0} у.о.";

            if (buyBtn != null)
            {
                // Захоплюємо змінну для замикання
                var dataToBuy = vData;
                buyBtn.onClick.RemoveAllListeners();
                buyBtn.onClick.AddListener(() => BuyVehicle(dataToBuy));
            }
        }
    }

    private void BuyVehicle(VehicleData data)
    {
        if (FinanceManager.Instance == null) return;

        if (vehicleWorldPrefab == null)
        {
            Debug.LogError("ПОМИЛКА: Не призначено vehicleWorldPrefab в інспекторі ShopPanel!");
            return;
        }

        if (!FinanceManager.Instance.CanAfford(data.purchaseCost))
        {
            Debug.LogWarning("Недостатньо грошей для придбання!");
            return;
        }

        CityNode spawnCity = FindSpawnCity();
        if (spawnCity == null)
        {
            Debug.LogWarning("У вас немає жодного гаража! Збудуйте гараж у будь-якому місті спочатку.");
            return;
        }

        FinanceManager.Instance.AddExpense(data.purchaseCost);

        // Спавнимо авто 
        GameObject newVehObj = Instantiate(vehicleWorldPrefab, spawnCity.transform.position, Quaternion.identity);
        newVehObj.name = data.vehicleName + "_" + Random.Range(100, 999);

        // Переміщаємо у папку "Vehicles"
        GameObject vehiclesFolder = GameObject.Find("Vehicles");
        if (vehiclesFolder != null) newVehObj.transform.SetParent(vehiclesFolder.transform);
        else
        {
            vehiclesFolder = new GameObject("Vehicles");
            newVehObj.transform.SetParent(vehiclesFolder.transform);
        }

        var controller = newVehObj.GetComponent<VehicleController>();
        if (controller != null)
        {
            controller.vehicleData = data;
            controller.currentCity = spawnCity;
            controller.garageCities = new List<CityNode> { spawnCity };
            controller.status = VehicleStatus.Idle;

            // МИТТЄВО додаємо до автопарку, щоб оновилося вікно
            var fleetPanel = FindFirstObjectByType<FleetPanelController>();
            if (fleetPanel != null)
            {
                fleetPanel.RegisterVehicle(controller);
            }
        }

        Debug.Log($"Придбано {data.vehicleName}. Машина очікує в місті {spawnCity.cityName}.");
    }

    private CityNode FindSpawnCity()
    {
        // 1. Спочатку перевіряємо, чи відкрите якесь місто в CityInfoPanel і чи є там гараж
        if (CityInfoPanel.Instance != null && CityInfoPanel.Instance.SelectedCity != null)
        {
            if (CityInfoPanel.Instance.SelectedCity.hasGarage)
                return CityInfoPanel.Instance.SelectedCity;
        }

        // 2. Якщо вікно міста закрите, шукаємо перше-ліпше місто на карті, де є гараж
        CityNode[] allCities = FindObjectsByType<CityNode>(FindObjectsSortMode.None);
        foreach (var city in allCities)
        {
            if (city.hasGarage) return city;
        }

        return null;
    }
}