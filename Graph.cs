using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RvB.Graphs;

public abstract class GraphBase<TVertex> : IGraph<TVertex> where TVertex : notnull {
    private static readonly JsonSerializerOptions s_jsonOptions = new() {
        WriteIndented = true,
        ReferenceHandler = ReferenceHandler.Preserve
    };

    protected readonly Dictionary<TVertex, HashSet<TVertex>> _nextVertices;
    private readonly Dictionary<TVertex, HashSet<TVertex>> _prevVertices;

    /// <summary>
    /// Creates an empty graph.
    /// </summary>
    public GraphBase(GraphType type) : this(type, 32) { }

    /// <summary>
    /// Creates an empty graph with an expected amount of vertices.
    /// </summary>
    /// <param name="vertexCount">The expected amount of vertices.</param>
    /// <exception cref="ArgumentException">When the expected amount of vertices is invalid.</exception>
    public GraphBase(GraphType type, int vertexCount) {
        if (vertexCount < 0) {
            throw new ArgumentException("There must be a non-negative number of vertices");
        }
        if (!type.HasFlag(GraphType.Directed) && !type.HasFlag(GraphType.Undirected)) {
            throw new ArgumentException($"Graph type must be either directed or undirected");
        }
        GraphType = type;
        EdgeCount = 0;
        _nextVertices = new(vertexCount);
        _prevVertices = new(vertexCount);
    }

    /// <summary>
    /// Creates a new graph based on another <see cref="IGraph{TVertex}"/> graph.
    /// </summary>
    /// <param name="graph">The graph to replicate.</param>
    public GraphBase(IGraph<TVertex> graph) : this(graph.GraphType, graph.VertexCount) {
        foreach (var edge in graph.Edges) {
            TryAddEdge(edge.Vertex1, edge.Vertex2);
        }
    }

    public GraphBase(GraphType type, IEnumerable<(TVertex, IEnumerable<TVertex>)> edges) : this(type) {
        foreach (var (source, destinations) in edges) {
            foreach (var destination in destinations) {
                _ = TryAddEdge(source, destination);
            }
        }
    }

    public GraphType GraphType { get; }

    public bool IsDirected => GraphType.HasFlag(GraphType.Directed);

    public virtual string Serialize() {
        var serializedGraph = new SerializedGraph<TVertex> {
            GraphType = GraphType,
            Vertices = [.. _nextVertices.Keys],
            Edges = Edges.Select(e => new Edge<TVertex>(e.Vertex1, e.Vertex2)).ToArray()
        };
        return JsonSerializer.Serialize(serializedGraph, s_jsonOptions);
    }

    public static Graph<TVertex>? Deserialize(string json) {
        var serializedGraph = JsonSerializer.Deserialize<SerializedGraph<TVertex>>(json, s_jsonOptions);
        if (serializedGraph is null)
            return null;
        var graph = new Graph<TVertex>(serializedGraph.GraphType);
        foreach (var vertex in serializedGraph.Vertices) {
            graph.AddVertex(vertex);
        }
        foreach (var edge in serializedGraph.Edges) {
            graph.TryAddEdge(edge.Vertex1, edge.Vertex2);
        }
        return graph;
    }

    /// <inheritdoc/>
    public int EdgeCount { get; private set; }

    /// <inheritdoc/>
    public IEnumerable<GraphEdge<TVertex>> Edges {
        get {
            foreach (var (vertex, connections) in _nextVertices) {
                foreach (var connectedVertex in connections) {
                    yield return new(this, vertex, connectedVertex);
                }
            }
        }
    }

    /// <inheritdoc/>
    public int VertexCount => _nextVertices.Count;

    public GraphVertex<TVertex> this[TVertex vertex] => new(this, vertex);

    /// <inheritdoc/>
    public IEnumerable<GraphVertex<TVertex>> Vertices
        => _nextVertices.Keys.AsEnumerable().Select(v => new GraphVertex<TVertex>(this, v));

    public bool ContainsEdge(in GraphEdge<TVertex> edge) {
        if (edge.Graph != this)
            return false;
        return ContainsEdge(edge.Vertex1, edge.Vertex2);
    }

    public bool ContainsEdge(TVertex vertex1, TVertex vertex2) {
        ArgumentNullException.ThrowIfNull(vertex1, nameof(vertex1));
        ArgumentNullException.ThrowIfNull(vertex2, nameof(vertex2));
        if (_nextVertices.TryGetValue(vertex1, out var edges) && edges.Contains(vertex2)) {
            return true;
        }
        if (GraphType.HasFlag(GraphType.Undirected) && _nextVertices.TryGetValue(vertex2, out edges) && edges.Contains(vertex1)) {
            return true;
        }
        return false;
    }

    /// <summary>
    /// Attempts to add an edge between the specified vertices. Returns a value indicating whether the edge was
    /// successfully added.
    /// </summary>
    /// <remarks>If the vertices do not already exist in the graph, they are added automatically. In
    /// undirected graphs, only one edge can exist between two vertices, regardless of direction.</remarks>
    /// <param name="vertex1">The source vertex of the edge to add. Cannot be null.</param>
    /// <param name="vertex2">The target vertex of the edge to add. Cannot be null.</param>
    /// <returns>true if the edge was added; otherwise, false. Returns false if the edge already exists.</returns>
    /// <exception cref="ArgumentException">Thrown if vertex1 and vertex2 are equal, as self-edges are not allowed.</exception>
    public bool TryAddEdge(TVertex vertex1, TVertex vertex2) {
        ArgumentNullException.ThrowIfNull(vertex1, nameof(vertex1));
        ArgumentNullException.ThrowIfNull(vertex2, nameof(vertex2));
        if (!GraphType.HasFlag(GraphType.AllowSelfEdges) && vertex1.Equals(vertex2)) {
            throw new ArgumentException("Cannot add edge with same source and target");
        }

        AddVertex(vertex1);
        AddVertex(vertex2);
        if (GraphType.HasFlag(GraphType.Undirected)) {
            // Check for preference of either (Vertex1, Vertex2) or (Vertex2, Vertex1): only one can exist in an undirected graph
            var nextVertices1 = _nextVertices[vertex1];
            var nextVertices2 = _nextVertices[vertex2];

            bool added = !nextVertices1.Contains(vertex2) && !nextVertices2.Contains(vertex1);
            if (added) {
                nextVertices1.Add(vertex2);
                _prevVertices[vertex2].Add(vertex1);
                EdgeCount++;
            }
            return added;
        } else {
            bool added = _nextVertices[vertex1].Add(vertex2);
            if (added) {
                _prevVertices[vertex2].Add(vertex1);
                EdgeCount++;
            }
            return added;
        }
    }

    /// <summary>
    /// Adds an edge between the specified vertices in the graph.
    /// </summary>
    /// <param name="vertex">The first vertex to connect. Cannot be null.</param>
    /// <param name="vertex2">The second vertex to connect. Cannot be null.</param>
    /// <returns>The current graph instance with the new edge added.</returns>
    public IGraph<TVertex> AddEdge(TVertex vertex, TVertex vertex2) {
        TryAddEdge(vertex, vertex2);
        return this;
    }

    /// <summary>
    /// Adds edges from the specified vertex to each vertex in the provided collection.
    /// </summary>
    /// <remarks>If an edge already exists between the source vertex and a target vertex, it will not be
    /// duplicated. This method returns the same graph instance to support fluent chaining.</remarks>
    /// <param name="vertex">The source vertex from which edges will be added.</param>
    /// <param name="vertices">A collection of target vertices to which edges will be created from the source vertex. Cannot be null.</param>
    /// <returns>The current graph instance with the new edges added.</returns>
    public IGraph<TVertex> AddEdges(TVertex vertex, IEnumerable<TVertex> vertices) {
        foreach (var vertex2 in vertices) {
            TryAddEdge(vertex, vertex2);
        }
        return this;
    }

    /// <summary>
    /// Adds multiple edges to the graph between the specified pairs of vertices.
    /// </summary>
    /// <remarks>Edges are only added if both vertices are valid and the edge does not already exist. This
    /// method allows chaining by returning the graph instance.</remarks>
    /// <param name="edges">A collection of tuples, each containing two vertices to connect with an edge. If an edge already exists between
    /// a pair, it is not added again.</param>
    /// <returns>The current graph instance with the specified edges added.</returns>
    public IGraph<TVertex> AddEdges(IEnumerable<(TVertex Vertex1, TVertex Vertex2)> edges) {
        foreach (var (Vertex1, Vertex2) in edges) {
            TryAddEdge(Vertex1, Vertex2);
        }
        return this;
    }

    public bool RemoveEdge(in GraphEdge<TVertex> edge) {
        if (edge.Graph != this)
            return false;
        return RemoveEdge(edge.Vertex1, edge.Vertex2);
    }

    public bool RemoveEdge(TVertex vertex1, TVertex vertex2) {
        ArgumentNullException.ThrowIfNull(vertex1, nameof(vertex1));
        ArgumentNullException.ThrowIfNull(vertex2, nameof(vertex2));
        var removed = false;
        if (_nextVertices.TryGetValue(vertex1, out var vertices)) {
            removed = vertices.Remove(vertex2);
            if (removed) {
                _prevVertices[vertex2].Remove(vertex1);
                OnEdgeRemoved(vertex1, vertex2);
                EdgeCount--;
            }
        }
        if (!removed && GraphType.HasFlag(GraphType.Undirected) && _nextVertices.TryGetValue(vertex2, out vertices)) {
            removed = vertices.Remove(vertex1);
            if (removed) {
                _prevVertices[vertex1].Remove(vertex2);
                OnEdgeRemoved(vertex2, vertex1);
                EdgeCount--;
            }
        }
        return removed;
    }

    /// <inheritdoc/>
    public bool ContainsVertex(TVertex vertex) {
        return _nextVertices.ContainsKey(vertex);
    }

    /// <inheritdoc/>
    public bool AddVertex(TVertex vertex) {
        if (!_nextVertices.ContainsKey(vertex)) {
            _nextVertices[vertex] = [];
            _prevVertices[vertex] = [];
            return true;
        }
        return false;
    }

    public bool RemoveVertex(in GraphVertex<TVertex> vertex) {
        if (vertex.Graph != this)
            return false;
        return RemoveVertex(vertex.Vertex);
    }

    /// <inheritdoc/>
    public bool RemoveVertex(TVertex vertex) {
        if (!_nextVertices.ContainsKey(vertex))
            return false;
        RemoveAllEdges(vertex);
        _nextVertices.Remove(vertex);
        _prevVertices.Remove(vertex);
        return true;
    }

    /// <inheritdoc/>
    public IEnumerable<GraphVertex<TVertex>> Connections(TVertex vertex) {
        if (_nextVertices.TryGetValue(vertex, out var targets)) {
            foreach (var target in targets) {
                yield return new(this, target);
            }
        }
        if (GraphType.HasFlag(GraphType.Undirected)) {
            if (_prevVertices.TryGetValue(vertex, out var sources)) {
                foreach (var source in sources) {
                    yield return new(this, source);
                }
            }
        }
    }

    [Obsolete("Use Connections(TVertex vertex) instead")]
    public IEnumerable<GraphVertex<TVertex>> Neighbors(TVertex vertex)
        => Connections(vertex);

    /// <inheritdoc/>
    public IEnumerable<GraphVertex<TVertex>> Preceding(TVertex vertex) {
        if (GraphType.HasFlag(GraphType.Undirected)) {
            foreach (var connected in Connections(vertex)) {
                yield return connected;
            }
        } else {
            if (_prevVertices.TryGetValue(vertex, out var sources)) {
                foreach (var source in sources) {
                    yield return new(this, source);
                }
            }
        }
    }

    public bool IsCyclic() {
        var toVisit = Vertices.ToHashSet();
        while (toVisit.Count > 0) {
            var node = toVisit.First();
            HashSet<(TVertex, TVertex)> path = [];
            Queue<GraphVertex<TVertex>> queue = [];
            queue.Enqueue(node);
            while (queue.TryDequeue(out var nextNode)) {
                if (!toVisit.Remove(nextNode))
                    return true;
                foreach (var connected in nextNode.Connections) {
                    if (GraphType.HasFlag(GraphType.Undirected) && (!path.Add((nextNode, connected)) || !path.Add((connected, nextNode)))) {
                        continue;
                    }
                    queue.Enqueue(connected);
                }
            }
        }
        return false;
    }

    /// <inheritdoc/>
    public int MinDistance(TVertex source, TVertex destination) {
        return Dijkstra.CalcMinimalDistance(source, destination, (v) => Connections(v).Select(v => (v.Vertex, 1)), 0);
    }

    /// <inheritdoc/>
    public IEnumerable<(TVertex Vertex, int Distance)> MinDistancePath(TVertex source, TVertex destination) {
        return Dijkstra.GetMinimalPath(source, destination, (v) => Connections(v).Select(v => (v.Vertex, 1)), 0);
    }

    public IEnumerable<(TVertex Vertex, int Distance)> ShortestPath(TVertex source, TVertex destination) {
        return Dijkstra.GetMinimalPath(source, destination, (v) => Connections(v).Select(v => (v.Vertex, 1)), 0);
    }

    public IEnumerable<(TVertex Vertex, int Distance)> Reachable(TVertex source) {
        return Dijkstra.CalcMinimalDistances<TVertex, int>(source, (v) => Connections(v).Select(c => c.Vertex));
    }

    protected virtual void OnEdgeRemoved(TVertex vertex1, TVertex vertex2) { }

    private void RemoveAllEdges(TVertex vertex) {
        foreach (var nextVertex in _nextVertices[vertex]) {
            if (_prevVertices[nextVertex].Remove(vertex)) {
                OnEdgeRemoved(vertex, nextVertex);
                EdgeCount--;
            }
        }
        foreach (var prevVertex in _prevVertices[vertex]) {
            if (_nextVertices[prevVertex].Remove(vertex)) {
                OnEdgeRemoved(prevVertex, vertex);
                EdgeCount--;
            }
        }
    }

    /// <summary>
    /// Performs a topological sort of the vertices in the directed graph and returns a list representing a valid linear
    /// ordering.
    /// </summary>
    /// <remarks>This method uses Kahn's algorithm to compute the topological sort. If the graph contains one
    /// or more cycles, the method returns null to indicate that no valid topological ordering exists.</remarks>
    /// <returns>A list of vertices in topologically sorted order if the graph is acyclic; otherwise, null if the graph contains
    /// a cycle.</returns>
    /// <exception cref="Exception">Thrown if the graph is not directed. Topological sorting is only supported for directed graphs.</exception>
    public List<TVertex>? TopologicalSort() {
        if (!GraphType.HasFlag(GraphType.Directed))
            throw new Exception("Topological sort is only supported in a directed graph");

        var vertexDegrees = _nextVertices.Keys.ToDictionary(v => v, _ => 0);
        foreach (var vertex in Vertices) {
            foreach (var adjacent in Adjacent(vertex)) {
                ref var degree = ref CollectionsMarshal.GetValueRefOrAddDefault(vertexDegrees, adjacent, out _);
                degree += 1;
            }
        }

        var queue = new Queue<TVertex>();
        foreach (var (vertex, degree) in vertexDegrees) {
            if (degree == 0) {
                //vertexDegrees.Remove(vertex);
                queue.Enqueue(vertex);
            }
        }

        var sortedVertices = new List<TVertex>();
        while (queue.TryDequeue(out var vertex)) {
            sortedVertices.Add(vertex);
            foreach (var adjacent in Adjacent(vertex)) {
                ref var degree = ref CollectionsMarshal.GetValueRefOrAddDefault(vertexDegrees, adjacent, out _);
                if (--degree == 0) {
                    //vertexDegrees.Remove(next);
                    queue.Enqueue(adjacent);
                }
            }
        }
        if (vertexDegrees.Values.Any(v => v != 0)) {
            // It's a cyclic graph!
            return null;
        }
        return sortedVertices;

        HashSet<TVertex> Adjacent(TVertex vertex)
            => _nextVertices.TryGetValue(vertex, out var targets) ? targets : [];
    }
}

public sealed class Graph<TVertex> : GraphBase<TVertex>, IEquatable<Graph<TVertex>> where TVertex : notnull {
    /// <summary>
    /// Creates an empty graph of the specified type.
    /// </summary>
    /// <param name="type">The graph type.</param>
    public Graph(GraphType type) : this(type, 32) { }

    /// <summary>
    /// Creates an empty graph of the specified type with an expected amount of vertices.
    /// </summary>
    /// <param name="type">The graph type.</param>
    /// <param name="vertexCount">The expected amount of vertices.</param>
    /// <exception cref="ArgumentException">When the expected amount of vertices is invalid.</exception>
    public Graph(GraphType type, int vertexCount) : base(type, vertexCount) { }

    /// <summary>
    /// Creates a new graph based on another <see cref="IGraph{TVertex}"/> graph.
    /// </summary>
    /// <param name="graph">The graph to replicate.</param>
    public Graph(IGraph<TVertex> graph) : base(graph) { }

    public Graph(GraphType type, IEnumerable<(TVertex, IEnumerable<TVertex>)> edges) : base(type, edges) { }

    public bool Equals(Graph<TVertex>? otherGraph) {
        if (ReferenceEquals(this, otherGraph))
            return true;
        if (otherGraph is null || otherGraph.VertexCount != VertexCount || otherGraph.EdgeCount != EdgeCount || otherGraph.GraphType != GraphType)
            return false;
        HashSet<TVertex> vertices = [.. _nextVertices.Keys];
        foreach (var (otherSource, otherTargets) in otherGraph._nextVertices) {
            if (!vertices.Remove(otherSource)) {
                return false;
            }
            var targets = new HashSet<TVertex>(_nextVertices[otherSource]);
            foreach (var target in otherTargets) {
                if (!targets.Remove(target)) {
                    return false;
                }
            }
            if (targets.Count > 0) {
                return false;
            }
        }
        if (vertices.Count > 0) {
            return false;
        }
        return true;
    }

    public override bool Equals(object? obj) {
        return obj is Graph<TVertex> graph && Equals(graph);
    }

    public override int GetHashCode() {
        var hashCode = 17 + 23 * GraphType.GetHashCode();
        foreach (var source in _nextVertices.Keys.Order()) {
            hashCode = hashCode * 23 + source.GetHashCode();
            int hashTargets = 17;
            foreach (var target in _nextVertices[source].Order()) {
                hashTargets = hashTargets * 23 + target.GetHashCode();
            }
            hashCode = hashCode * 23 + hashTargets;
        }
        return hashCode;
    }
}

public sealed class Graph<TVertex, TData> : GraphBase<TVertex>, IGraph<TVertex, TData> where TVertex : notnull {
    private static readonly JsonSerializerOptions s_jsonOptions = new() {
        WriteIndented = true,
        ReferenceHandler = ReferenceHandler.Preserve
    };

    private Dictionary<(TVertex, TVertex), TData> _edgeData;

    /// <summary>
    /// Creates an empty graph.
    /// </summary>
    /// <param name="type">The graph type.</param>
    public Graph(GraphType type) : this(type, 32) { }

    /// <summary>
    /// Creates an empty graph with an expected amount of vertices.
    /// </summary>
    /// <param name="type">The graph type.</param>
    /// <param name="v">The expected amount of vertices.</param>
    /// <exception cref="ArgumentException">When the expected amount of vertices is invalid.</exception>
    public Graph(GraphType type, int v) : base(type, v) {
        _edgeData = new(2 * v);
    }

    /// <summary>
    /// Creates a new graph based on another <see cref="IGraph{TVertex}"/> graph.
    /// </summary>
    /// <param name="graph">The graph to replicate.</param>
    public Graph(Graph<TVertex, TData> graph) : this(graph.GraphType, graph.VertexCount) {
        foreach (var edge in graph.Edges) {
            AddEdge(edge.Vertex1, edge.Vertex2, edge.Data);
        }
    }

    public override string Serialize() {
        var serializedGraph = new SerializedGraph<TVertex, TData> {
            GraphType = GraphType,
            Vertices = [.. Vertices],
            Edges = Edges.Select(e => new Graphs.Edge<TVertex, TData>(e.Vertex1, e.Vertex2, GetData(e.Vertex1, e.Vertex2))).ToArray()
        };
        return JsonSerializer.Serialize(serializedGraph, s_jsonOptions);
    }

    public static new Graph<TVertex, TData>? Deserialize(string json) {
        var serializedGraph = JsonSerializer.Deserialize<SerializedGraph<TVertex, TData>>(json, s_jsonOptions);
        if (serializedGraph == null) {
            return null;
        }
        var graph = new Graph<TVertex, TData>(serializedGraph.GraphType);
        foreach (var vertex in serializedGraph.Vertices) {
            graph.AddVertex(vertex);
        }
        foreach (var edge in serializedGraph.Edges) {
            graph.AddEdge(edge.Vertex1, edge.Vertex2, edge.Data);
        }
        return graph;
    }

    public new IEnumerable<GraphEdge<TVertex, TData>> Edges {
        get {
            foreach (var (vertex1, vertex2) in base.Edges) {
                var data = GetData(vertex1, vertex2);
                yield return new(this, vertex1, vertex2, data);
            }
        }
    }

    public new IEnumerable<GraphVertex<TVertex, TData>> Vertices {
        get {
            foreach (var vertex in base.Vertices) {
                yield return new(this, vertex);
            }
        }
    }

    public bool TryAddEdge(TVertex vertex1, TVertex vertex2, TData data) {
        var added = TryAddEdge(vertex1, vertex2);
        if (added) {
            _edgeData[(vertex1, vertex2)] = data;
        }
        return added;
    }

    /// <summary>    /// 
    /// Adds an edge between the specified vertices with the associated data and returns the updated graph instance.
    /// </summary>
    /// <remarks>If an edge already exists between the specified vertices, its data will not be updated.</remarks>
    /// <param name="vertex1">The first vertex to connect. Cannot be null.</param>
    /// <param name="vertex2">The second vertex to connect. Cannot be null.</param>
    /// <param name="data">The data to associate with the edge between the specified vertices.</param>
    /// <returns>The graph instance with the new edge added.</returns>
    public IGraph<TVertex, TData> AddEdge(TVertex vertex1, TVertex vertex2, TData data) {
        TryAddEdge(vertex1, vertex2, data);
        return this;
    }

    /// <summary>
    /// Adds multiple edges from the specified vertex to a collection of target vertices with associated data.
    /// </summary>
    /// <remarks>If an edge already exists between the source and a target vertex, it will not be duplicated.
    /// This method is useful for efficiently adding several edges from a single vertex in one operation.</remarks>
    /// <param name="vertex">The source vertex from which edges will be added.</param>
    /// <param name="vertices">A collection of tuples, each containing a target vertex and the data associated with the edge to be added.</param>
    /// <returns>The current graph instance with the new edges added.</returns>
    public IGraph<TVertex, TData> AddEdges(TVertex vertex, IEnumerable<(TVertex, TData)> vertices) {
        foreach (var (vertex2, data) in vertices) {
            TryAddEdge(vertex, vertex2, data);
        }
        return this;
    }

    /// <summary>
    /// Adds multiple edges to the graph using the specified collection of vertex pairs and associated data.
    /// </summary>
    /// <remarks>Edges are added only if they do not already exist in the graph. If an edge between the
    /// specified vertices already exists, it will not be duplicated.</remarks>
    /// <param name="edges">A collection of tuples, each containing two vertices and the data associated with the edge to be added. The
    /// vertices represent the endpoints of the edge.</param>
    /// <returns>The current graph instance with the specified edges added.</returns>
    public IGraph<TVertex, TData> AddEdges(IEnumerable<(TVertex Vertex1, TVertex Vertex2, TData Data)> edges) {
        foreach (var (vertex1, vertex2, data) in edges) {
            TryAddEdge(vertex1, vertex2, data);
        }
        return this;
    }

    public new IEnumerable<GraphConnected<TVertex, TData>> Connections(TVertex vertex) {
        foreach (var neighbor in base.Connections(vertex)) {
            yield return new(this, neighbor, GetData(vertex, neighbor));
        }
    }

    [Obsolete("Use Connections(TVertex vertex) instead")]
    public new IEnumerable<GraphConnected<TVertex, TData>> Neighbors(TVertex vertex)
        => Connections(vertex);

    /// <inheritdoc/>
    public new IEnumerable<GraphConnected<TVertex, TData>> Preceding(TVertex vertex) {
        foreach (var precedent in base.Preceding(vertex)) {
            yield return new(this, precedent, GetData(precedent, vertex));
        }
    }

    public TData this[in GraphEdge<TVertex> edge] {
        get {
            if (edge.Graph != this)
                throw new ArgumentException("Edge does not belong to the graph", nameof(edge));
            return this[edge.Vertex1, edge.Vertex2];
        }
        set {
            if (edge.Graph != this)
                throw new ArgumentException("Edge does not belong to the graph", nameof(edge));
            this[edge.Vertex1, edge.Vertex2] = value;
        }
    }

    public TData this[TVertex vertex1, TVertex vertex2] {
        get => GetEdgeData(vertex1, vertex2);
        set => UpdateData(vertex1, vertex2, value);
    }

    public TData GetData(in GraphEdge<TVertex> edge) {
        if (edge.Graph != this)
            throw new ArgumentException("Edge does not belong to the graph", nameof(edge));
        return GetEdgeData(edge.Vertex1, edge.Vertex2);
    }

    public TData GetData(TVertex vertex1, TVertex vertex2) => GetEdgeData(vertex1, vertex2);

    public bool TryGetData(TVertex vertex1, TVertex vertex2, [MaybeNullWhen(false)] out TData data)
        => TryGetEdgeData(vertex1, vertex2, out data);

    private TData GetEdgeData(TVertex vertex1, TVertex vertex2) {
        if (TryGetEdgeData(vertex1, vertex2, out var data)) {
            return data;
        }
        return default!;
    }

    public bool TryGetEdgeData(TVertex vertex1, TVertex vertex2, [MaybeNullWhen(false)] out TData data) {
        if (_edgeData.TryGetValue((vertex1, vertex2), out data)) {
            return true;
        }
        if (GraphType.HasFlag(GraphType.Undirected) && _edgeData.TryGetValue((vertex2, vertex1), out data)) {
            return true;
        }
        data = default;
        return false;
    }

    protected override void OnEdgeRemoved(TVertex vertex1, TVertex vertex2) {
        RemoveData(vertex1, vertex2);
    }

    private void UpdateData(TVertex vertex1, TVertex vertex2, TData data) {
        if (!_edgeData.ContainsKey((vertex1, vertex2))) {
            if (GraphType.HasFlag(GraphType.Undirected) && !_edgeData.ContainsKey((vertex2, vertex1))) {
                _edgeData[(vertex2, vertex1)] = data;
            }
            return;
        }
        _edgeData[(vertex1, vertex2)] = data;
    }

    private void RemoveData(TVertex vertex1, TVertex vertex2) {
        if (!_edgeData.Remove((vertex1, vertex2)) && GraphType.HasFlag(GraphType.Undirected)) {
            _edgeData.Remove((vertex2, vertex1));
        }
    }
}

public static partial class GraphExtension {
    public static IEnumerable<(TVertex Vertex, TWeight Weight)> MinWeightedPath<TVertex, TData, TWeight>(this IGraph<TVertex, TData> graph, TVertex vertex1, TVertex vertex2) where TVertex : notnull where TData : IEdgeDataWeight<TWeight> where TWeight : INumber<TWeight> {
        return Dijkstra.GetMinimalPath(vertex1, vertex2, (v) => graph.Connections(v).Select(n => (n.Vertex, n.Data.Weight)), TWeight.Zero);
    }
    public static IEnumerable<(TVertex Vertex, TWeight Weight)> MinWeightedPath<TVertex, TWeight>(this IGraph<TVertex, TWeight> graph, TVertex vertex1, TVertex vertex2) where TVertex : notnull where TWeight : INumber<TWeight> {
        return Dijkstra.GetMinimalPath(vertex1, vertex2, (v) => graph.Connections(v).Select(n => (n.Vertex, n.Data)), TWeight.Zero);
    }
    public static TWeight MinWeightedDistance<TVertex, TData, TWeight>(this IGraph<TVertex, TData> graph, TVertex vertex1, TVertex vertex2) where TData : IEdgeDataWeight<TWeight> where TVertex : notnull where TWeight : INumber<TWeight> {
        return Dijkstra.CalcMinimalDistance(vertex1, vertex2, (v) => graph.Connections(v).Select(n => (n.Vertex, n.Data.Weight)), TWeight.Zero);
    }
    public static TWeight MinWeightedDistance<TVertex, TWeight>(this IGraph<TVertex, TWeight> graph, TVertex vertex1, TVertex vertex2) where TVertex : notnull where TWeight : INumber<TWeight> {
        return Dijkstra.CalcMinimalDistance(vertex1, vertex2, (v) => graph.Connections(v).Select(n => (n.Vertex, n.Data)), TWeight.Zero);
    }
    public static IEnumerable<(TVertex Vertex, TWeight Weight)> MinWeightedDistances<TVertex, TData, TWeight>(this IGraph<TVertex, TData> graph, TVertex source) where TData : IEdgeDataWeight<TWeight> where TVertex : notnull where TWeight : INumber<TWeight> {
        return Dijkstra.CalcMinimalDistances(source, (v) => graph.Connections(v).Select(n => (n.Vertex, n.Data.Weight)), TWeight.Zero);
    }
    public static IEnumerable<(TVertex Vertex, TWeight Weight)> MinWeightedDistances<TVertex, TWeight>(this IGraph<TVertex, TWeight> graph, TVertex source) where TVertex : notnull where TWeight : INumber<TWeight> {
        return Dijkstra.CalcMinimalDistances(source, (v) => graph.Connections(v).Select(n => (n.Vertex, n.Data)), TWeight.Zero);
    }
}
