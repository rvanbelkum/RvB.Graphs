using System.Numerics;

namespace RvB.Graphs;

public static class FloydWarshall {
    /// <summary>
    /// Implements Floyd–Warshall algorithm for finding shortest paths in a directed weighted graph with positive or negative
    /// edge weights (but with no negative cycles).
    /// <see href="https://en.wikipedia.org/wiki/Floyd%E2%80%93Warshall_algorithm"/>
    /// </summary>
    /// <typeparam name="TVertex"></typeparam>
    /// <typeparam name="TDist"></typeparam>
    /// <param name="graph"></param>
    /// <returns></returns>
    public static IEnumerable<(TVertex Vertex1, TVertex Vertex2, TDist Distance)> CalcAllMinimalDistances<TVertex, TDist>(IGraph<TVertex, TDist> graph) where TVertex : notnull where TDist : INumber<TDist> {
        if (!graph.IsDirected) {
            throw new ArgumentException("Floyd-Warshall algorithm only works with directed graphs", nameof(graph));
        }
        return CalcAllMinimalDistances(graph.Vertices.Select(v => v.Vertex), (v) => graph.Connections(v).Select(n => (n.Vertex, n.Data)));
    }

    public static IEnumerable<(TVertex Vertex1, TVertex Vertex2, TDist Distance)> CalcAllMinimalDistances<TVertex, TDist>(IEnumerable<TVertex> vertices, Func<TVertex, IEnumerable<(TVertex Vertex, TDist Distance)>> connectedVertices) where TVertex : notnull where TDist : INumber<TDist> {
        var distances = new Dictionary<(TVertex Vertex1, TVertex Vertex2), TDist>();
        foreach (var vertex in vertices) {
            distances[(vertex, vertex)] = TDist.Zero;
            var neighbors = connectedVertices(vertex);
            if (neighbors is null)
                continue;
            foreach (var (target, dist) in neighbors) {
                distances[(vertex, target)] = dist;
            }
        }
        foreach (var v1 in vertices) {
            foreach (var v2 in vertices) {
                foreach (var v3 in vertices) {
                    if (distances.TryGetValue((v1, v3), out var d1) &&
                        distances.TryGetValue((v3, v2), out var d2) &&
                        (!distances.TryGetValue((v1, v2), out var d0) || d0 > d1 + d2)) {
                        distances[(v1, v2)] = d1 + d2;
                    }
                }
            }
        }
        foreach (var (edges, distance) in distances) {
            yield return (edges.Vertex1, edges.Vertex2, distance);
        }
    }
}
