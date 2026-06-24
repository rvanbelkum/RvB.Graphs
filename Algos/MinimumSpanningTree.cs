using System.Numerics;

namespace RvB.Graphs;

public static class MinimumSpanningTree {
    /// <summary>
    /// Calculates the minimum spanning tree, represented as a <see cref="Graph{TVertex,TWeight}"/>, using Prim's algorithm (see <see href="https://en.wikipedia.org/wiki/Prim%27s_algorithm"/>)
    /// </summary>
    /// <typeparam name="TVertex">Vertex type.</typeparam>
    /// <typeparam name="TWeight">Weight type (must implement INumber<typeparamref name="TWeight"/>).</typeparam>
    /// <param name="graph"></param>
    /// <returns>The minimum spanning tree as a <see cref="Graph{TVertex,TWeight}"/></returns>
    public static Graph<TVertex, TWeight> CalculateMinimumSpanningTree<TVertex, TWeight>(this IGraph<TVertex, TWeight> graph) where TVertex : notnull where TWeight : INumber<TWeight>, IMinMaxValue<TWeight> {
        return MSTPrim(graph);
    }

    private static Graph<TVertex, TWeight> MSTPrim<TVertex, TWeight>(IGraph<TVertex, TWeight> graph) where TVertex : notnull where TWeight : INumber<TWeight>, IMinMaxValue<TWeight> {
        Graph<TVertex, TWeight> mst = new(graph.GraphType);
        if (graph.VertexCount == 0) {
            return mst;
        }
        var verticesToProcess = graph.Vertices.Select(v => v.Vertex).ToHashSet();
        var distances = new Dictionary<TVertex, TWeight>();
        foreach (var nodeToProcess in verticesToProcess) {
            distances[nodeToProcess] = TWeight.MaxValue;
        }
        var startVertex = verticesToProcess.First();
        distances[startVertex] = TWeight.Zero;

        TVertex DeleteMin() {
            TWeight minWeight = TWeight.MaxValue;
            TVertex? minVertex = default;
            foreach (var vertex in verticesToProcess) {
                var d = distances[vertex];
                if (d <= minWeight) {
                    minWeight = d;
                    minVertex = vertex;
                }
            }
            if (minVertex is null) {
                throw new Exception();
            }
            verticesToProcess.Remove(minVertex);
            return minVertex;
        }

        while (verticesToProcess.Count > 0) {
            var minVertex = DeleteMin();
            foreach (var (vertex, weight) in graph.Connections(minVertex)) {
                var d = distances[vertex];
                if (verticesToProcess.Contains(vertex) && weight < d) {
                    distances[vertex] = weight;
                    mst.AddEdge(minVertex, vertex, weight);
                }
            }
            if (graph.IsDirected) {
                foreach (var (vertex, weight) in graph.Preceding(minVertex)) {
                    var d = distances[vertex];
                    if (verticesToProcess.Contains(vertex) && weight < d) {
                        distances[vertex] = weight;
                        mst.AddEdge(vertex, minVertex, weight);
                    }
                }
            }
        }
        return mst;
    }
}
