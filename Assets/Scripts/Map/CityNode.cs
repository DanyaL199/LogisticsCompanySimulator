using UnityEngine;

public enum CityType { Industrial, Trade, Tourist }

public class CityNode : MonoBehaviour
{
    [Header("Інформація про місто")]
    public string cityName;
    public CityType cityType;

    [Header("Рівень міста: 1=Мале, 2=Середнє, 3=Велике")]
    [Range(1, 3)]
    public int activityLevel = 1;

    private void Start()
    {
        float scale = 0.4f + activityLevel * 0.15f;
        transform.localScale = new Vector3(scale, scale, 1f);
    }

    public int GetDailyDemandLimit()
    {
        if (activityLevel == 3) return 300;
        if (activityLevel == 2) return 150;
        return 60;
    }

    public bool GeneratesIncomeFor(VehicleType vehicleType)
    {
        if (cityType == CityType.Trade) return true;
        if (cityType == CityType.Industrial) return vehicleType == VehicleType.Cargo;
        if (cityType == CityType.Tourist) return vehicleType == VehicleType.Passenger;
        return false;
    }

    private void OnDrawGizmos()
    {
        if (cityType == CityType.Industrial) Gizmos.color = Color.yellow;
        else if (cityType == CityType.Tourist) Gizmos.color = Color.green;
        else Gizmos.color = Color.cyan;

        Gizmos.DrawWireSphere(transform.position, 0.4f);

#if UNITY_EDITOR
        UnityEditor.Handles.Label(
            transform.position + Vector3.up * 0.8f,
            cityName
        );
#endif
    }
}