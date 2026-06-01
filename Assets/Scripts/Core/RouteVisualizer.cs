using UnityEngine;

[RequireComponent(typeof(RouteDefinition))]
public class RouteVisualizer : MonoBehaviour
{
    public GameObject linePrefab;
    private RouteDefinition route;

    private void Awake() { route = GetComponent<RouteDefinition>(); }

    private void Start()
    {
        BuildHighlightLines();
        route.HideHighlight();
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
            lr.startWidth = lr.endWidth = 0.18f;
            lr.sortingLayerName = "Roads";
            lr.sortingOrder = 1;
            lr.startColor = lr.endColor = route.routeColor;
            lr.enabled = false;

            route.highlightLines.Add(lr);
        }
    }
}