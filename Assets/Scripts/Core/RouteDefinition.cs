using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class RouteStop
{
    public CityNode city;
}

public class RouteDefinition : MonoBehaviour
{
    [Header("Назва маршруту")]
    public string routeName;

    [Header("Колір маршруту (обирає гравець)")]
    public Color routeColor = new Color(1f, 0.6f, 0f);

    [Header("Зупинки по порядку")]
    public List<RouteStop> stops = new List<RouteStop>();

    [Header("Статистика")]
    public float incomeStats = 0f;

    [HideInInspector]
    public List<LineRenderer> highlightLines = new List<LineRenderer>();

    public bool IsValid() => stops != null && stops.Count >= 2;

    // Повертає наступне місто в маршруті після поточного
    public CityNode GetNextCity(CityNode current)
    {
        for (int i = 0; i < stops.Count; i++)
        {
            if (stops[i].city == current)
                return stops[(i + 1) % stops.Count].city;
        }
        return stops[0].city;
    }

    public void ShowHighlight()
    {
        foreach (var lr in highlightLines)
        {
            if (lr == null) continue;
            lr.startColor = routeColor;
            lr.endColor = routeColor;
            lr.enabled = true;
        }
    }

    public void HideHighlight()
    {
        foreach (var lr in highlightLines)
            if (lr != null) lr.enabled = false;
    }
}