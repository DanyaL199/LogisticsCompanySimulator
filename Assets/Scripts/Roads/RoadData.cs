using UnityEngine;

public enum RoadType { Dirt, Asphalt, Highway }

[CreateAssetMenu(fileName = "NewRoad",
                 menuName = "Logistics/Road Data")]
public class RoadData : ScriptableObject
{
    [Header("Тип дороги")]
    public string roadName;
    public RoadType roadType;

    [Header("Характеристики")]
    public float speedLimitKmh;
    public float wearMultiplier;
    public float buildCost;
}