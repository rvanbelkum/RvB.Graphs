namespace RvB.Graphs;

public class ContractedGraph<TVertex> : GraphBase<TVertex> where TVertex : notnull {
    private readonly Graph<TVertex> _originalGraph;
    private readonly Dictionary<TVertex, HashSet<TVertex>> _merges = [];

    public ContractedGraph(Graph<TVertex> graph) : base(graph.GraphType, graph.VertexCount) {
        _originalGraph = graph;
        foreach (var edge in graph.Edges) {
            TryAddEdge(edge.Vertex1, edge.Vertex2);
        }
    }

    public ContractedGraph(ContractedGraph<TVertex> compactedGraph) : base(compactedGraph.GraphType, compactedGraph.VertexCount) {
        _originalGraph = compactedGraph._originalGraph;
        foreach (var edge in compactedGraph.Edges) {
            TryAddEdge(edge.Vertex1, edge.Vertex2);
        }
        foreach (var (vertex, merges) in compactedGraph._merges) {
            _merges[vertex] = new(merges);
        }
    }

    public void ContractRandomEdge() {
        if (EdgeCount < 1) {
            throw new InvalidOperationException("There must be at least one edge to perform contraction.");
        }

        var randomGenerator = new Random();
        TVertex originVertex = Vertices.ElementAt(randomGenerator.Next(0, Vertices.Count()));
        var neighbors = Connections(originVertex);
        TVertex endVertex = neighbors.ElementAt(randomGenerator.Next(0, neighbors.Count()));

        foreach (TVertex vertex in Connections(endVertex)) {
            if (!vertex.Equals(originVertex))
                TryAddEdge(originVertex, vertex);
        }
        RemoveVertex(endVertex);

        if (!_merges.TryGetValue(originVertex, out var firstMerges)) {
            firstMerges = [originVertex];
            _merges[originVertex] = firstMerges;
        }
        firstMerges.Add(endVertex);
        if (_merges.TryGetValue(endVertex, out var secondMerges)) {
            firstMerges.UnionWith(secondMerges);
            _merges.Remove(endVertex);
        }
    }

    /// <summary>
    /// Performs a contraction sequence on a given graph until a given amount of vertices has been reached.
    /// </summary>
    /// <param name="limitVtx">The number of vertices after which the sequence should stop.</param>
    /// <returns></returns>
    public void ContractionSequence(int limitVtx) {
        while (VertexCount > limitVtx) {
            ContractRandomEdge();
        }
    }

    public IEnumerable<int> GetCutSizes() {
        return _merges.Values.Select(m => m.Count);
    }

    public int GetCutSize(IEnumerable<TVertex> subset1) {
        HashSet<TVertex> vertices = [];
        int cutSize = 0;
        foreach (var vertex in subset1) {
            if (_merges.TryGetValue(vertex, out var merged)) {
                vertices.UnionWith(merged);
            } else {
                vertices.Add(vertex);
            }
        }

        foreach (var vertex in vertices) {
            foreach (var connected in _originalGraph.Connections(vertex)) {
                if (!vertices.Contains(connected.Vertex))
                    cutSize += 1;
            }
        }
        return cutSize;
    }

    public int GetCutSize() {
        if (_merges.Count != 2)
            return int.MaxValue;

        int cutSize = 0;
        var merge1 = _merges.Values.ElementAt(0);
        var merge2 = _merges.Values.ElementAt(1);
        if (merge1.Count > merge2.Count)
            (merge1, merge2) = (merge2, merge1);
        foreach (var vertex in merge1) {
            foreach (var connected in _originalGraph.Connections(vertex)) {
                if (merge2.Contains(connected.Vertex))
                    cutSize += 1;
            }
        }
        return cutSize;
    }

    public IEnumerable<HashSet<TVertex>> GetCuts() {
        foreach (var merge in _merges.Values)
            yield return merge;
    }

    public (HashSet<TVertex>, HashSet<TVertex>) GetCuts(IEnumerable<TVertex> vertices) {
        HashSet<TVertex> set1 = [];
        foreach (var vertex in vertices) {
            if (_merges.TryGetValue(vertex, out var merged)) {
                set1.UnionWith(merged);
            } else {
                set1.Add(vertex);
            }
        }
        HashSet<TVertex> set2 = new(_originalGraph.Vertices.Where(v => !set1.Contains(v)).Select(v => v.Vertex));
        return (set1, set2);
    }
}
