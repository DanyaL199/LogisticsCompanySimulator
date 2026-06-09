using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ShopPanel : MonoBehaviour
{
    // === ДОДАНО: Статичний екземпляр для доступу з SaveManager ===
    public static ShopPanel Instance { get; private set; }

    [Header("UI References")]
    public GameObject panelObj;
    public Button btnOpenShop;
    public Button btnCloseShop;
    public RectTransform listContent;

    [Header("Prefabs")]
    public GameObject vehicleItemPrefab;
    public GameObject vehicleWorldPrefab;

    [Header("Available Vehicles")]
    public List<VehicleData> availableVehicles;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        if (btnOpenShop != null)
            btnOpenShop.onClick.AddListener(OpenShop);

        if (btnCloseShop != null)
            btnCloseShop.onClick.AddListener(CloseShop);

        CloseShop(); // Ховаємо панель на початку
    }

    public void OpenShop()
    {
        panelObj.SetActive(true);
        PopulateShop();  // Оновлюємо список щоразу при відкритті

        if (RoutesPanel.Instance != null)
            RoutesPanel.Instance.ClosePanel();
        if (FleetPanelController.Instance != null)
            FleetPanelController.Instance.ClosePanel();
    }

    public void CloseShop()
    {
        panelObj.SetActive(false);
    }

    private void PopulateShop()
    {
        // Очищаємо старі елементи
        foreach (Transform child in listContent)
        {
            Destroy(child.gameObject);
        }

        // Створюємо нові
        foreach (var vData in availableVehicles)
        {
            GameObject item = Instantiate(vehicleItemPrefab, listContent);

            TextMeshProUGUI[] allTexts = item.GetComponentsInChildren<TextMeshProUGUI>(true);

            // 1. НАЗВА 
            TextMeshProUGUI titleText = FindTMP(allTexts, "title", "name", "vehiclename");
            if (titleText == null && allTexts.Length > 0) titleText = allTexts[0];
            if (titleText != null) titleText.text = vData.vehicleName;

            // 2. ЦІНА
            TextMeshProUGUI priceText = FindTMP(allTexts, "price", "cost", "value");
            if (priceText == null && allTexts.Length > 1) priceText = allTexts[1];
            if (priceText != null) priceText.text = $"{vData.purchaseCost} у.о.";

            // 3. ХАРАКТЕРИСТИКИ
            TextMeshProUGUI statsText = FindTMP(allTexts, "stat", "info", "desc", "detail");
            if (statsText == null && allTexts.Length > 2) statsText = allTexts[2];
            if (statsText != null)
            {
                statsText.text = $"Місткість: {vData.maxCapacity} | Швидкість: {vData.maxSpeedKmh} км/год\n" +
                                 $"Пальне: {vData.fuelPer100km} л/100км | Міцність: {vData.maintenanceCost}";
            }

            // 4. ІКОНКА
            Image iconImg = null;
            Image[] allImages = item.GetComponentsInChildren<Image>(true);
            foreach (var img in allImages)
            {
                if (img.gameObject == item) continue;
                string n = img.name.ToLower();
                if (n.Contains("icon") || n.Contains("img") || n.Contains("vehicle") || n.Contains("sprite"))
                {
                    iconImg = img;
                    break;
                }
            }
            if (iconImg == null && allImages.Length > 1) iconImg = allImages[1];

            if (iconImg != null && vData.icon != null)
            {
                iconImg.sprite = vData.icon;
                iconImg.color = Color.white;
            }

            // 5. КНОПКА "КУПИТИ"
            Button buyBtn = item.GetComponentInChildren<Button>(true);
            if (buyBtn != null)
            {
                buyBtn.onClick.RemoveAllListeners();
                var capturedData = vData;
                buyBtn.onClick.AddListener(() => BuyVehicle(capturedData));
            }
        }
    }

    private TextMeshProUGUI FindTMP(TextMeshProUGUI[] texts, params string[] keywords)
    {
        foreach (var t in texts)
        {
            string n = t.name.ToLower();
            foreach (var k in keywords)
            {
                if (n.Contains(k)) return t;
            }
        }
        return null;
    }

    private void BuyVehicle(VehicleData vData)
    {
        if (FinanceManager.Instance == null) return;

        if (FinanceManager.Instance.CanAfford(vData.purchaseCost))
        {
            FinanceManager.Instance.AddExpense(vData.purchaseCost);

            Vector3 hiddenPosition = new Vector3(10000f, 10000f, 0f);
            GameObject newVeh = Instantiate(vehicleWorldPrefab, hiddenPosition, Quaternion.identity);

            VehicleController vc = newVeh.GetComponent<VehicleController>();
            if (vc != null)
            {
                vc.vehicleData = vData;
                vc.customName = $"{vData.vehicleName}_{Random.Range(100, 999)}";
                vc.status = VehicleStatus.Idle;
            }

            Debug.Log($"Придбано {vData.vehicleName}. Відкрийте Автопарк для призначення маршруту!");
        }
        else
        {
            Debug.LogWarning("Недостатньо грошей для покупки!");
            if (NotificationController.Instance != null)
                NotificationController.ShowMessage("Недостатньо коштів для покупки!", Color.red);
        }
    }
}