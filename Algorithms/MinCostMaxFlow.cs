public class MinCostMaxFlow
{
    private readonly List<List<Edge>> _graph;

    public MinCostMaxFlow(int size)
    {
        _graph = Enumerable.Range(0, size)
            .Select(_ => new List<Edge>())
            .ToList();
    }

    public void AddEdge(int from, int to, int cap, int cost)
    {
        _graph[from].Add(new Edge(to, cap, cost, _graph[to].Count));
        _graph[to].Add(new Edge(from, 0, -cost, _graph[from].Count - 1));
    }

    public (int flow, int cost) GetMinCostMaxFlow(int s, int t)
    {
        int flow = 0, cost = 0;
        int n = _graph.Count;

        while (true)
        {
            var dist = Enumerable.Repeat(int.MaxValue, n).ToArray();
            var prevV = new int[n];
            var prevE = new int[n];
            var inQueue = new bool[n];

            dist[s] = 0;
            var q = new Queue<int>();
            q.Enqueue(s);
            inQueue[s] = true;

            while (q.Count > 0)
            {
                var v = q.Dequeue();
                inQueue[v] = false;

                for (int i = 0; i < _graph[v].Count; i++)
                {
                    var e = _graph[v][i];
                    if (e.Capacity > 0 && dist[v] + e.Cost < dist[e.To])
                    {
                        dist[e.To] = dist[v] + e.Cost;
                        prevV[e.To] = v;
                        prevE[e.To] = i;

                        if (!inQueue[e.To])
                        {
                            q.Enqueue(e.To);
                            inQueue[e.To] = true;
                        }
                    }
                }
            }

            if (dist[t] == int.MaxValue) break;

            int addFlow = int.MaxValue;
            for (int v = t; v != s; v = prevV[v])
                addFlow = Math.Min(addFlow, _graph[prevV[v]][prevE[v]].Capacity);

            for (int v = t; v != s; v = prevV[v])
            {
                var e = _graph[prevV[v]][prevE[v]];
                e.Capacity -= addFlow;
                _graph[v][e.Rev].Capacity += addFlow;
            }

            flow += addFlow;
            cost += dist[t] * addFlow;
        }

        return (flow, cost);
    }
}