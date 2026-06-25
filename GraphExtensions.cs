using RvB.Collections;
using System.Numerics;
using System.Text;

namespace RvB.Graphs;

#if NET10_0_OR_GREATER
public static class GraphExtensionsAdj {
    extension(IGraph<int> graph) {
        public static Graph<int> FromAdjacencyList(int[][] adjacencyList, bool allowSelfEdges = false) {
            var graphType = allowSelfEdges ? GraphType.Directed | GraphType.AllowSelfEdges : GraphType.Directed;
            return new Graph<int>(graphType, adjacencyList.Index().Select(item => (item.Index, item.Item.AsEnumerable())));
        }

        public static Graph<int> FromAdjacencyMatrix(bool[,] adjacencyMatrix, bool allowSelfEdges = false) {
            var graphType = allowSelfEdges ? GraphType.Directed | GraphType.AllowSelfEdges : GraphType.Directed;
            var newGraph = new Graph<int>(graphType);
            for (int i = 0; i < adjacencyMatrix.GetLength(0); i += 1) {
                for (int j = 0; j < adjacencyMatrix.GetLength(1); j += 1) {
                    if (adjacencyMatrix[i, j]) {
                        newGraph.AddEdge(i, j);
                    }
                }
            }
            return newGraph;
        }

        public bool[,]? ToAdjacencyMatrix() {
            if (graph.VertexCount == 0)
                return default;
            var minValue = int.MaxValue;
            var maxValue = int.MinValue;
            foreach (var vertex in graph.Vertices) {
                minValue = int.Min(minValue, vertex.Vertex);
                maxValue = int.Max(maxValue, vertex.Vertex);
            }
            var size = maxValue - minValue + 1;
            bool[,] adjacencyMatrix = new bool[size, size];
            foreach (var vertex in graph.Vertices) {
                foreach (var conn in vertex.Connections) {
                    adjacencyMatrix[vertex, conn] = true;
                }
            }
            return adjacencyMatrix;
        }
    }

    extension<TVertex>(Graph<TVertex>) where TVertex : notnull {
        public static Graph<TVertex> FromAdjacencyList((TVertex, TVertex[])[] adjacencyList, bool allowSelfEdges = false) {
            var graphType = allowSelfEdges ? GraphType.Directed | GraphType.AllowSelfEdges : GraphType.Directed;
            return new Graph<TVertex>(graphType, adjacencyList.Select(a => (a.Item1, a.Item2.AsEnumerable())));
        }
        public static Graph<TVertex> FromAdjacencyList(IEnumerable<(TVertex, IEnumerable<TVertex>)> adjacencyList, bool allowSelfEdges = false) {
            var graphType = allowSelfEdges ? GraphType.Directed | GraphType.AllowSelfEdges : GraphType.Directed;
            return new Graph<TVertex>(graphType, adjacencyList.Select(a => (a.Item1, a.Item2.AsEnumerable())));
        }
    }
}
#endif

public static class GraphExtensions {
    private class KeyValue<TKey, TValue> : IEquatable<KeyValue<TKey, TValue>> where TKey : notnull {
        public KeyValue(TKey key, TValue value) {
            Key = key;
            Value = value;
        }

        public TKey Key { get; }
        public TValue Value { get; }

        public bool Equals(KeyValue<TKey, TValue>? other) {
            if (other is not null) {
                return Key.Equals(other.Key);
            }
            return false;
        }

        public override bool Equals(object? obj)
            => obj is KeyValue<TKey, TValue> minCostVertex && Equals(minCostVertex);

        public override int GetHashCode() {
            return Key.GetHashCode();
        }

        public static implicit operator KeyValue<TKey, TValue>(TKey key)
            => new(key, default!);
    }

    extension<TVertex, TWeight>(IGraph<TVertex, TWeight> graph) where TVertex : notnull where TWeight : INumber<TWeight> {
        public IEnumerable<(TVertex Vertex1, TVertex Vertex2, TWeight Weight)> MinimumSpanningTree() {
            var unexplored = new HashSet<TVertex>(graph.Vertices.Select(v => v.Vertex));

            var pq = new FibonacciPriorityQueue<KeyValue<TVertex, TVertex>, TWeight>();
            while (unexplored.Count > 0 || pq.Count > 0) {
                TVertex currentVertex;
                if (pq.TryDequeue(out var item, out var weight)) {
                    currentVertex = item.Key;
                    if (!unexplored.Remove(currentVertex)) {
                        continue;
                    }
                    yield return (item.Key, item.Value, weight);
                } else {
                    currentVertex = unexplored.First();
                    unexplored.Remove(currentVertex);
                }

                foreach (var neighbor in graph.Connections(currentVertex)) {
                    if (!unexplored.Contains(neighbor.Vertex)) {
                        continue;
                    }
                    if (pq.TryGetNode(neighbor.Vertex, out var node)) {
                        if (neighbor.Data < node.Priority) {
                            pq.AdjustPriority(node, neighbor.Data);
                        }
                    } else {
                        pq.Enqueue(new(neighbor.Vertex, currentVertex), neighbor.Data);
                    }
                }
            }
        }

        public IEnumerable<(TVertex Vertex1, TVertex Vertex2, TWeight Weight)> MaximumSpanningTree() {
            var unexplored = new HashSet<TVertex>(graph.Vertices.Select(v => v.Vertex));

            var pq = new FibonacciPriorityQueue<KeyValue<TVertex, TVertex>, TWeight>(ReverseComparer<TWeight>.Default);
            while (unexplored.Count > 0 || pq.Count > 0) {
                TVertex currentVertex;
                if (pq.TryDequeue(out var item, out var weight)) {
                    currentVertex = item.Key;
                    if (!unexplored.Remove(currentVertex)) {
                        continue;
                    }
                    yield return (item.Key, item.Value, weight);
                } else {
                    currentVertex = unexplored.First();
                    unexplored.Remove(currentVertex);
                }

                foreach (var neighbor in graph.Connections(currentVertex)) {
                    if (!unexplored.Contains(neighbor.Vertex)) {
                        continue;
                    }
                    if (pq.TryGetNode(neighbor.Vertex, out var node)) {
                        if (neighbor.Data > node.Priority) {
                            pq.AdjustPriority(node, neighbor.Data);
                        }
                    } else {
                        pq.Enqueue(new(neighbor.Vertex, currentVertex), neighbor.Data);
                    }
                }
            }
        }
    }
}

public static class GraphVixExtensions {
    extension<TVertex>(IGraph<TVertex> graph) where TVertex : notnull {
        public string ToDot() {
            var dot = new StringBuilder();
            if (graph.GraphType.HasFlag(GraphType.Directed)) {
                dot.AppendLine("digraph DiGraph1 {");
                foreach (var vertex in graph.Vertices) {
                    foreach (var connected in vertex.Connections) {
                        dot.AppendLine($"\t\"{vertex}\" -> \"{connected}\";");
                    }
                }
                dot.AppendLine("}");
            } else {
                var edges = new HashSet<(TVertex, TVertex)>();
                dot.AppendLine("graph Graph1 {");
                foreach (var vertex in graph.Vertices) {
                    foreach (var connected in vertex.Connections) {
                        if (edges.Add((connected, vertex))) {
                            dot.AppendLine($"\t\"{vertex}\" -- \"{connected}\";");
                            edges.Add((vertex, connected));
                        }
                    }
                }
                dot.AppendLine("}");
            }
            return dot.ToString();
        }
    }
}
