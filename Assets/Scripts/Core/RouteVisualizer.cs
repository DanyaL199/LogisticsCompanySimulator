using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(RouteDefinition))]
public class RouteVisualizer : MonoBehaviour
{
    public GameObject linePrefab;
    private RouteDefinition route;

    private void Start()
    {
        Invoke(nameof(BuildHighlightLines), 0.1f);
    }

    public void BuildHighlightLines()
    {
        if (route == null) route = GetComponent<RouteDefinition>();
        if (linePrefab == null && RoadNetwork.Instance != null) linePrefab = RoadNetwork.Instance.roadLinePrefab;

        foreach (var lr in route.highlightLines)
        {
            if (lr != null) Destroy(lr.gameObject);
        }
        route.highlightLines.Clear();

        if (!route.IsValid() || linePrefab == null) return;

        for (int i = 0; i < route.stops.Count; i++)
        {
            CityNode a = route.stops[i].city;
            CityNode b = route.stops[(i + 1) % route.stops.Count].city;
            if (a == null || b == null) continue;

            var obj = Instantiate(linePrefab, transform);
            var lr = obj.GetComponent<LineRenderer>();
            if (lr == null) continue;

            lr.numCornerVertices = 4;
            lr.numCapVertices = 4;

            int overlapIndex = GetSegmentOverlapIndex(a, b, i);

            Vector3 startPos = a.transform.position;
            Vector3 endPos = b.transform.position;


            Vector3 consistentStart = (a.GetInstanceID() < b.GetInstanceID()) ? startPos : endPos;
            Vector3 consistentEnd = (a.GetInstanceID() < b.GetInstanceID()) ? endPos : startPos;


            Vector3 dir = (consistentEnd - consistentStart).normalized;
            Vector3 normal = new Vector3(-dir.y, dir.x, 0);

            int sign = (overlapIndex % 2 == 0) ? 1 : -1;
            int step = (overlapIndex / 2) + 1;
            float offsetAmount = step * sign * 0.12f;

            Vector3 offset = normal * offsetAmount;


            Vector3 dirAtoB = (endPos - startPos).normalized;
            float dist = Vector3.Distance(startPos, endPos);
            float taperDist = Mathf.Min(0.5f, dist * 0.25f);

            lr.positionCount = 4;
            lr.SetPosition(0, startPos);
            lr.SetPosition(1, startPos + dirAtoB * taperDist + offset);
            lr.SetPosition(2, endPos - dirAtoB * taperDist + offset);
            lr.SetPosition(3, endPos);

            lr.startWidth = lr.endWidth = 0.08f;
            lr.sortingLayerName = "Roads";
            lr.sortingOrder = 2 + overlapIndex; 

            lr.startColor = lr.endColor = route.routeColor;

            route.highlightLines.Add(lr);
        }

        route.ShowHighlight();
    }

    private int GetSegmentOverlapIndex(CityNode a, CityNode b, int stopIndex)
    {
        var allRoutes = FindObjectsByType<RouteDefinition>(FindObjectsSortMode.None);

        List<(RouteDefinition route, int sIdx)> traversing = new List<(RouteDefinition, int)>();

        foreach (var r in allRoutes)
        {
            if (r == null || !r.IsValid()) continue;

            for (int j = 0; j < r.stops.Count; j++)
            {
                CityNode a2 = r.stops[j].city;
                CityNode b2 = r.stops[(j + 1) % r.stops.Count].city;
                if ((a == a2 && b == b2) || (a == b2 && b == a2))
                {
                    traversing.Add((r, j));
                }
            }
        }

        traversing.Sort((t1, t2) => {
            int cmp = t1.route.GetInstanceID().CompareTo(t2.route.GetInstanceID());
            if (cmp == 0) return t1.sIdx.CompareTo(t2.sIdx);
            return cmp;
        });

        for (int j = 0; j < traversing.Count; j++)
        {
            if (traversing[j].route == route && traversing[j].sIdx == stopIndex)
                return j;
        }

        return 0;
    }

    public static void RebuildAllHighlights()
    {
        var allVis = FindObjectsByType<RouteVisualizer>(FindObjectsSortMode.None);
        foreach (var v in allVis)
        {
            if (v != null) v.BuildHighlightLines();
        }
    }

}