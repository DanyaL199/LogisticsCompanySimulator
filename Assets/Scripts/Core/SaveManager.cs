using UnityEngine;
using System.IO;
using Newtonsoft.Json;

[System.Serializable]
public class GameSaveData
{
    public float balance;
    public float totalIncome;
    public float totalExpenses;
    public float vehicleAssetsValue;
    public float roadAssetsValue;
    public float currentWage;
    public int saveYear;
    public int saveMonth;
    public int saveDay;
    public int saveHour;
    public float vehicleCondition;
    public string vehicleStatus;
    public string currentCityName;
}

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }

    private string SavePath =>
        Path.Combine(Application.persistentDataPath, "save.json");

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        if (GameTimeManager.Instance != null)
            GameTimeManager.Instance.OnDayChanged += AutoSave;
    }

    private void OnDestroy()
    {
        if (GameTimeManager.Instance != null)
            GameTimeManager.Instance.OnDayChanged -= AutoSave;
    }

    private void AutoSave(GameDate date)
    {
        // Автозбереження кожні 5 ігрових днів
        if (date.day % 5 == 0)
            Save();
    }

    public void Save()
    {
        var fm = FinanceManager.Instance;
        var tm = GameTimeManager.Instance;
        var wm = WageManager.Instance;

        if (fm == null || tm == null) return;

        var data = new GameSaveData
        {
            balance = fm.balance,
            totalIncome = fm.totalIncome,
            totalExpenses = fm.totalExpenses,
            vehicleAssetsValue = fm.vehicleAssetsValue,
            roadAssetsValue = fm.roadAssetsValue,
            currentWage = wm != null ? wm.currentWage : 500f,
            saveYear = tm.CurrentDate.year,
            saveMonth = tm.CurrentDate.month,
            saveDay = tm.CurrentDate.day,
            saveHour = tm.CurrentDate.hour,
        };

        // Знайти перший ТЗ і зберегти його стан
        var vehicle = FindFirstObjectByType<VehicleController>();
        if (vehicle != null)
        {
            data.vehicleCondition = vehicle.condition;
            data.vehicleStatus = vehicle.status.ToString();
            data.currentCityName = vehicle.currentCity?.cityName ?? "";
        }

        string json = JsonConvert.SerializeObject(data, Formatting.Indented);
        File.WriteAllText(SavePath, json);
        Debug.Log($"Збережено: {SavePath}");
    }

    public bool HasSave() => File.Exists(SavePath);

    public GameSaveData Load()
    {
        if (!HasSave()) return null;

        string json = File.ReadAllText(SavePath);
        return JsonConvert.DeserializeObject<GameSaveData>(json);
    }

    public void ApplySave(GameSaveData data)
    {
        if (data == null) return;

        var fm = FinanceManager.Instance;
        var tm = GameTimeManager.Instance;
        var wm = WageManager.Instance;

        if (fm != null)
        {
            fm.balance = data.balance;
            fm.totalIncome = data.totalIncome;
            fm.totalExpenses = data.totalExpenses;
            fm.vehicleAssetsValue = data.vehicleAssetsValue;
            fm.roadAssetsValue = data.roadAssetsValue;
        }

        if (wm != null)
            wm.currentWage = data.currentWage;

        // Відновити ігровий час
        // (GameTimeManager не має публічного сеттера дати —
        //  додамо метод SetDate)
        tm?.SetDate(data.saveYear, data.saveMonth,
                    data.saveDay, data.saveHour);

        // Відновити стан ТЗ
        var vehicle = FindFirstObjectByType<VehicleController>();
        if (vehicle != null)
            vehicle.condition = data.vehicleCondition;

        Debug.Log("Гру відновлено!");
    }

    public void DeleteSave()
    {
        if (HasSave()) File.Delete(SavePath);
    }
}