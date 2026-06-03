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

    [Header("Список доступних типів доріг (для будівництва)")]
    public List<RoadData> availableRoadTypes;

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
        RoadConnection existing = GetRoad(a, b);

        if (existing != null)
        {
            // Upgrade
            if (existing.roadData.roadType >= roadData.roadType) return false;

            existing.roadData = roadData;
            UpdateLineVisuals(existing.lineRenderer, roadData);
            FinanceManager.Instance?.RegisterRoadValue(roadData.buildCost);
            Debug.Log($"Дорога покращена: {a.cityName} ↔ {b.cityName}");
            return true;
        }

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

        UpdateLineVisuals(lr, roadData);
        lr.sortingOrder = 1;

        return lr;
    }

    private void UpdateLineVisuals(LineRenderer lr, RoadData roadData)
    {
        if (lr == null) return;

        lr.startWidth = lr.endWidth = 0.12f;
        Color color = Color.gray;
        if (roadData != null)
        {
            switch (roadData.roadType)
            {
                case RoadType.Dirt: color = new Color(0.4f, 0.25f, 0.1f); break;
                case RoadType.Asphalt: color = new Color(0.4f, 0.4f, 0.4f); lr.startWidth = lr.endWidth = 0.16f; break;
                case RoadType.Highway: color = new Color(0.22f, 0.42f, 0.79f); lr.startWidth = lr.endWidth = 0.18f; break;
            }
        }
        lr.startColor = lr.endColor = color;
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
        if (start == end) return new List<CityNode> { start };
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
        route.ShowHighlight();
    }

    private List<CityNode> BuildPath(Dictionary<CityNode, CityNode> visited,
                                      CityNode start, CityNode end)
    {
        var path = new List<CityNode>();
        var current = end;
        while (current != null)
        {
            path.Add(current);
            current = visited[current];
        }
        path.Reverse();
        return path;
    }
}