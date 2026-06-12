using UnityEngine;

public class WageManager : MonoBehaviour
{
    public static WageManager Instance { get; private set; }

    [Header("Ставка зарплати (грн/місяць)")]
    public float currentWage = 500f;

    private const float INFLATION_MIN = 0.10f;
    private const float INFLATION_MAX = 0.15f;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        if (GameTimeManager.Instance != null)
        {
            GameTimeManager.Instance.OnYearChanged += OnNewYear;
        }
    }

    private void OnDestroy()
    {
        if (GameTimeManager.Instance != null)
        {
            GameTimeManager.Instance.OnYearChanged -= OnNewYear;
        }
    }

    // Щорічне підвищення зарплати водіїв через інфляцію
    private void OnNewYear(GameDate date)
    {
        float growth = Random.Range(INFLATION_MIN, INFLATION_MAX);
        currentWage *= (1f + growth);
        if (NotificationController.Instance != null)
            NotificationController.Instance.Show($"Інфляція! Зарплати водіїв зросли до {currentWage:F0} грн", new Color(1f, 0.7f, 0f), 6f);
    }

    public void SetWage(float newWage)
    {
        currentWage = Mathf.Max(100f, newWage);
    }

    public float GetTotalMonthlyWageCost(int vehicleCount)
        => currentWage * vehicleCount;
}