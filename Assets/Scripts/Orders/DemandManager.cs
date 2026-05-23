using UnityEngine;
using System.Collections.Generic;

public class DemandManager : MonoBehaviour
{
    public static DemandManager Instance { get; private set; }

    private Dictionary<CityNode, int> demandLeft = new Dictionary<CityNode, int>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        if (GameTimeManager.Instance != null)
            GameTimeManager.Instance.OnDayChanged += ResetDemand;
    }

    private void OnDestroy()
    {
        if (GameTimeManager.Instance != null)
            GameTimeManager.Instance.OnDayChanged -= ResetDemand;
    }

    private void ResetDemand(GameDate date)
    {
        demandLeft.Clear();
        Debug.Log($"[{date.ToShortString()}] Попит міст скинуто.");
    }

    public int ConsumeCapacity(CityNode city, int requested)
    {
        if (!demandLeft.ContainsKey(city))
            demandLeft[city] = city.GetDailyDemandLimit();

        int available = demandLeft[city];
        int consumed = Mathf.Min(available, requested);
        demandLeft[city] -= consumed;
        return consumed;
    }

    public int GetRemainingDemand(CityNode city)
    {
        if (!demandLeft.ContainsKey(city))
            return city.GetDailyDemandLimit();
        return demandLeft[city];
    }
}