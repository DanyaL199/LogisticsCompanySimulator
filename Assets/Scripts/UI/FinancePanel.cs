using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class FinancePanel : MonoBehaviour
{
    public static FinancePanel Instance { get; private set; }

    [Header("UI панель")]
    public GameObject panelRoot;

    [Header("Текстові поля")]
    public TextMeshProUGUI balanceText;
    public TextMeshProUGUI incomeText;
    public TextMeshProUGUI expensesText;
    public TextMeshProUGUI assetsValueText;
    public TextMeshProUGUI wageText;

    [Header("Кнопки")]
    public Button btnToggle;
    public Button btnClose;

    private float updateTimer = 0f;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        if (panelRoot != null) panelRoot.SetActive(false);
        if (btnToggle != null) btnToggle.onClick.AddListener(TogglePanel);
        if (btnClose != null) btnClose.onClick.AddListener(ClosePanel);
    }

    private void Update()
    {
        if (panelRoot != null && panelRoot.activeSelf)
        {
            updateTimer += Time.deltaTime;
            if (updateTimer >= 0.5f)
            {
                updateTimer = 0f;
                RefreshData();
            }
        }
    }

    public void TogglePanel()
    {
        if (panelRoot != null)
        {
            bool nextState = !panelRoot.activeSelf;
            panelRoot.SetActive(nextState);
            if (nextState)
            {
                RefreshData();
                // Коли відкриваємо статистику - закриваємо магазин
                if (ShopPanel.Instance != null) ShopPanel.Instance.CloseShop();
            }
        }
    }

    public void ClosePanel()
    {
        if (panelRoot != null) panelRoot.SetActive(false);
    }

    private void RefreshData()
    {
        if (FinanceManager.Instance == null) return;

        float balance = FinanceManager.Instance.balance;
        float income = FinanceManager.Instance.totalIncome;
        float expenses = FinanceManager.Instance.totalExpenses;

        // Вартість активів = вартість всього транспорту + вартість всіх побудованих доріг
        float assets = FinanceManager.Instance.vehicleAssetsValue + FinanceManager.Instance.roadAssetsValue;

        var vehicles = FindObjectsByType<VehicleController>(FindObjectsSortMode.None);
        float currentWage = WageManager.Instance != null ? WageManager.Instance.currentWage : 0f;
        float totalWage = currentWage * vehicles.Length; // Загальні витрати на ЗП за місяць

        if (balanceText != null) balanceText.text = $"Баланс: {FormatMoney(balance)}";
        if (incomeText != null) incomeText.text = $"Дохід: <color=#00FF00>+{FormatMoney(income)}</color>";
        if (expensesText != null) expensesText.text = $"Витрати: <color=#FF0000>-{FormatMoney(expenses)}</color>";
        if (assetsValueText != null) assetsValueText.text = $"Активи (ТЗ + Дороги): {FormatMoney(assets)}";
        if (wageText != null) wageText.text = $"Зарплата ({vehicles.Length} вод.): {FormatMoney(totalWage)} / міс.";
    }

    // Допоміжний метод для форматування чисел 
    private string FormatMoney(float value)
    {
        string s = Mathf.Abs(value).ToString("F0");
        string result = "";
        int count = 0;
        for (int i = s.Length - 1; i >= 0; i--)
        {
            if (count > 0 && count % 3 == 0)
                result = " " + result;
            result = s[i] + result;
            count++;
        }
        return result + " грн";
    }
}