using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class RoadConnection
{
    public CityNode cityA;
    public CityNode cityB;
    public RoadData roadData;
    public LineRenderer lineRenderer;
}

public class RoadNetwork : MonoBehaviour
{
    public static RoadNetwork Instance { get; private set; }

    [Header("Дороги (заповнюються вручну або через BuildRoad)")]
    public List<RoadConnection> roads = new List<RoadConnection>();

    [Header("Префаб лінії дороги")]
    public GameObject roadLinePrefab;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        // Намалювати лінії для доріг доданих вручну через Inspector
        foreach (var road in roads)
        {
            if (road.lineRenderer == null)
                road.lineRenderer = CreateLine(road.cityA, road.cityB, road.roadData);
        }
    }

    public bool RoadExists(CityNode a, CityNode b)
    {
        return roads.Exists(r =>
            (r.cityA == a && r.cityB == b) ||
            (r.cityA == b && r.cityB == a));
    }

    public RoadConnection GetRoad(CityNode a, CityNode b)
    {
        return roads.Find(r =>
            (r.cityA == a && r.cityB == b) ||
            (r.cityA == b && r.cityB == a));
    }

    public bool BuildRoad(CityNode a, CityNode b, RoadData roadData)
    {
        if (RoadExists(a, b)) return false;

        var lr = CreateLine(a, b, roadData);

        roads.Add(new RoadConnection
        {
            cityA = a,
            cityB = b,
            roadData = roadData,
            lineRenderer = lr
        });

        FinanceManager.Instance?.RegisterRoadValue(roadData.buildCost);
        Debug.Log($"Дорога збудована: {a.cityName} ↔ {b.cityName}");
        return true;
    }

    private LineRenderer CreateLine(CityNode a, CityNode b, RoadData roadData)
    {
        if (roadLinePrefab == null) return null;

        var obj = Instantiate(roadLinePrefab, transform);
        var lr = obj.GetComponent<LineRenderer>();
        if (lr == null) return null;

        lr.positionCount = 2;
        lr.SetPosition(0, a.transform.position);
        lr.SetPosition(1, b.transform.position);
        lr.startWidth = lr.endWidth = 0.12f;

        Color color = Color.gray;
        if (roadData != null && roadData.roadType == RoadType.Highway)
            color = new Color(0.22f, 0.42f, 0.79f);
        lr.startColor = lr.endColor = color;

        return lr;
    }

    public List<CityNode> GetNeighbors(CityNode city)
    {
        var result = new List<CityNode>();
        foreach (var r in roads)
        {
            if (r.cityA == city) result.Add(r.cityB);
            else if (r.cityB == city) result.Add(r.cityA);
        }
        return result;
    }

    public List<CityNode> FindPath(CityNode start, CityNode end)
    {
        var queue = new Queue<CityNode>();
        var visited = new Dictionary<CityNode, CityNode>();
        queue.Enqueue(start);
        visited[start] = null;

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (current == end) return BuildPath(visited, start, end);

            foreach (var neighbor in GetNeighbors(current))
            {
                if (!visited.ContainsKey(neighbor))
                {
                    visited[neighbor] = current;
                    queue.Enqueue(neighbor);
                }
            }
        }
        return null;
    }
    public void SetRouteHighlight(RouteDefinition route, bool hasVehicle)
    {
        if (route == null || !route.IsValid()) return;

        Color color = hasVehicle
            ? new Color(1.0f, 0.55f, 0.0f) // яскравий оранжевий
            : new Color(0.6f, 0.35f, 0.0f); // тьмяний оранжевий

        for (int i = 0; i < route.stops.Count; i++)
        {
            CityNode a = route.stops[i].city;
            CityNode b = route.stops[(i + 1) % route.stops.Count].city;

            RoadConnection road = GetRoad(a, b);
            if (road?.lineRenderer != null)
            {
                road.lineRenderer.startColor = color;
                road.lineRenderer.endColor = color;
            }
        }
    }

    private List<CityNode> BuildPath(Dictionary<CityNode, CityNode> visited,
                                      CityNode start, CityNode end)
    {
        var path = new List<CityNode>();
        var current = end;
        while (current != start)
        {
            path.Add(current);
            current = visited[current];
        }
        path.Reverse();
        return path;
    }

}