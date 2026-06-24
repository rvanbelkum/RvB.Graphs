using System.Diagnostics;
using System.Numerics;

namespace RvB.Graphs;

public static class BellmanFord {
    /// <summary>
    /// Implements the Shortest Path Faster Algorithm (SPFA), an improvement of the Bellman–Ford algorithm that computes
    /// single-source shortest paths in a weighted directed graph.
    /// <see href="https://en.wikipedia.org/wiki/Shortest_Path_Faster_Algorithm"/>
    /// </summary>
    /// <typeparam name="TVertex"></typeparam>
    /// <typeparam name="TDist"></typeparam>
    /// <param name="graph"></param>
    /// <param name="source"></param>
    /// <returns></returns>
    public static IEnumerable<(GraphVertex<TVertex, TDist>, TDist)> CalcAllDistances<TVertex, TDist>(IGraph<TVertex, TDist> graph, TVertex source)
        where TVertex : notnull 
        where TDist : INumber<TDist>, IMinMaxValue<TDist> {
        if (!graph.IsDirected) {
            throw new ArgumentException("Bellman-Ford algorithm only works with directed graphs", nameof(graph));
        }
        var vertices = graph.Vertices.Select(v => v);
        return CalcAllDistances(vertices, GetDistance);

        TDist? GetDistance(GraphVertex<TVertex, TDist> vertex1, GraphVertex<TVertex, TDist> vertex2) {
            if (graph.TryGetData(vertex1, vertex2, out var dist)) {
                return dist;
            }
            return default;
        }
    }


    public static IEnumerable<(TVertex Vertex, TDist Distance)> CalcAllDistances<TVertex, TDist>(IEnumerable<TVertex> vertices, Func<TVertex, TVertex, TDist?> getDistance) where TVertex : notnull where TDist : INumber<TDist> {
        Dictionary<TVertex, TDist> distanceTo = new() {
            [vertices.First()] = TDist.Zero
        };

        var vertexCount = vertices.Count();
        for (var round = 0; round < vertexCount - 1; round++) {
            foreach (var vertex1 in vertices) {
                foreach (var vertex2 in vertices) {
                    if (!vertex1.Equals(vertex2)) {
                        var dist = getDistance(vertex1, vertex2);
                        if (dist is null)
                            continue;
                        if (distanceTo.TryGetValue(vertex2, out var smallestDist)) {
                            var newDist = distanceTo[vertex1] + dist;
                            if (newDist < smallestDist) {
                                distanceTo[vertex2] = newDist;
                            }
                        } else {
                            distanceTo[vertex2] = dist;
                        }
                    }
                }
            }
        }

        // Check for negative-weight cycles. The above step guarantees shortest distances if graph
        // doesn't contain negative weight cycle. If we get a shorter path, then there is a cycle.
        foreach (var vertex1 in vertices) {
            foreach (var vertex2 in vertices) {
                var dist = getDistance(vertex1, vertex2);
                if (dist is not null && distanceTo[vertex1] + dist < distanceTo[vertex2]) {
                    yield break; // Negative-weight cycle detected
                }
            }
        }

        foreach (var (vertex, distance) in distanceTo) {
            yield return (vertex, distance);
        }
    }
}
