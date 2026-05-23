using UnityEngine;


// Будує лінії підсвітки при старті
[RequireComponent(typeof(RouteDefinition))]
public class RouteVisualizer : MonoBehaviour
{
    [Header("Префаб лінії (той самий RoadLinePrefab)")]
    public GameObject linePrefab;

    private RouteDefinition route;

    private void Awake()
    {
        route = GetComponent<RouteDefinition>();
    }

    private void Start()
    {
        BuildHighlightLines();
        route.HideHighlight(); // спочатку приховані
    }

    private void BuildHighlightLines()
    {
        if (!route.IsValid() || linePrefab == null) return;

        route.highlightLines.Clear();

        for (int i = 0; i < route.stops.Count; i++)
        {
            CityNode a = route.stops[i].city;
            CityNode b = route.stops[(i + 1) % route.stops.Count].city;
            if (a == null || b == null) continue;

            var obj = Instantiate(linePrefab, transform);
            var lr = obj.GetComponent<LineRenderer>();
            if (lr == null) continue;

            lr.positionCount = 2;
            lr.SetPosition(0, a.transform.position);
            lr.SetPosition(1, b.transform.position);

            // Ширша за дорогу щоб виглядала як обводка
            lr.startWidth = lr.endWidth = 0.18f;

            // Під дорогою — Roads order=2, ця лінія order=1
            lr.sortingLayerName = "Roads";
            lr.sortingOrder = 1;

            lr.startColor = lr.endColor = route.routeColor;
            lr.enabled = false; // приховано до запуску маршруту

            route.highlightLines.Add(lr);
        }
    }
}