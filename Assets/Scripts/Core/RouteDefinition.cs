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
    public Color routeColor = new Color(1f, 0.6f, 0f); // оранжевий за замовч.

    [Header("Зупинки по порядку")]
    public List<RouteStop> stops = new List<RouteStop>();

    // Лінії підсвітки (створюються автоматично)
    [HideInInspector]
    public List<LineRenderer> highlightLines = new List<LineRenderer>();

    public bool IsValid() => stops != null && stops.Count >= 2;

    public CityNode GetNextCity(CityNode current)
    {
        for (int i = 0; i < stops.Count; i++)
        {
            if (stops[i].city == current)
                return stops[(i + 1) % stops.Count].city;
        }
        return stops[0].city;
    }

    // Показати підсвітку маршруту
    public void ShowHighlight(bool hasVehicle)
    {
        Color color = hasVehicle
            ? routeColor
            : new Color(routeColor.r * 0.4f,
                        routeColor.g * 0.4f,
                        routeColor.b * 0.4f, 0.8f);

        foreach (var lr in highlightLines)
        {
            if (lr == null) continue;
            lr.startColor = color;
            lr.endColor = color;
            lr.enabled = true;
        }
    }

    public void HideHighlight()
    {
        foreach (var lr in highlightLines)
            if (lr != null) lr.enabled = false;
    }
    //  коли гравець додає нове місто в маршрут
    public void UpdateRouteName()
    {
        if (stops == null || stops.Count == 0) return;

        // Створюємо порожній список для назв міст
        List<string> cityNames = new List<string>();

        // Перебираємо всі зупинки в маршруті і дістаємо назви міст
        foreach (var stop in stops)
        {
            if (stop.city != null)
            {
                cityNames.Add(stop.city.cityName);
            }
        }

        routeName = string.Join(" - ", cityNames);
    }
}