using UnityEngine;

public class WageManager : MonoBehaviour
{
    public static WageManager Instance { get; private set; }

    [Header("Ставка зарплати (у.о./місяць)")]
    public float currentWage = 500f;

    [Header("Прихований параметр (тільки для перегляду)")]
    [SerializeField] private float expectedWage = 500f;

    public bool IsStrikeActive { get; private set; }
    public bool IsWarningActive { get; private set; }

    private const float INFLATION_MIN = 0.10f;
    private const float INFLATION_MAX = 0.15f;

    public System.Action OnWarningTriggered;
    public System.Action OnStrikeStarted;
    public System.Action OnStrikeEnded;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        if (GameTimeManager.Instance != null)
        {
            GameTimeManager.Instance.OnMonthChanged += OnNewMonth;
            GameTimeManager.Instance.OnYearChanged += OnNewYear;
        }
    }

    private void OnDestroy()
    {
        if (GameTimeManager.Instance != null)
        {
            GameTimeManager.Instance.OnMonthChanged -= OnNewMonth;
            GameTimeManager.Instance.OnYearChanged -= OnNewYear;
        }
    }

    private void OnNewMonth(GameDate date)
    {
        if (IsStrikeActive) return;

        if (currentWage < expectedWage)
        {
            if (!IsWarningActive)
            {
                IsWarningActive = true;
                OnWarningTriggered?.Invoke();
                Debug.LogWarning($"[{date.ToShortString()}] Водії незадоволені! Є 1 місяць до страйку.");
            }
            else
            {
                // Другий місяць — страйк
                IsWarningActive = false;
                StartStrike();
            }
        }
        else
        {
            IsWarningActive = false;
        }
    }

    private void OnNewYear(GameDate date)
    {
        float growth = Random.Range(INFLATION_MIN, INFLATION_MAX);
        expectedWage *= (1f + growth);
        Debug.Log($"[{date.ToShortString()}] Інфляція: очікувана зарплата → {expectedWage:F0} у.о.");
    }

    public void SetWage(float newWage)
    {
        currentWage = Mathf.Max(100f, newWage);
        if (currentWage >= expectedWage)
            IsWarningActive = false;
    }

    public float GetTotalMonthlyWageCost(int vehicleCount)
        => currentWage * vehicleCount;

    // 0=задоволені, 1=попередження, 2=страйк
    public int GetSatisfactionLevel()
    {
        if (IsStrikeActive) return 2;
        if (IsWarningActive) return 1;
        return 0;
    }

    private void StartStrike()
    {
        IsStrikeActive = true;
        OnStrikeStarted?.Invoke();
        Debug.LogError("СТРАЙК! Всі ТЗ зупинено.");
    }

    public bool ResolveStrike(int vehicleCount)
    {
        if (!IsStrikeActive) return false;
        if (currentWage < expectedWage)
        {
            Debug.Log("Спочатку підніміть зарплату!");
            return false;
        }

        float compensation = GetTotalMonthlyWageCost(vehicleCount) * 2f;
        if (!FinanceManager.Instance.CanAfford(compensation))
        {
            Debug.Log("Недостатньо коштів для компенсації!");
            return false;
        }

        FinanceManager.Instance.AddExpense(compensation);
        IsStrikeActive = false;
        OnStrikeEnded?.Invoke();
        Debug.Log($"Страйк завершено. Виплачено: {compensation:F0} у.о.");
        return true;
    }
}