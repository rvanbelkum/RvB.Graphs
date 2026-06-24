using System.Diagnostics.CodeAnalysis;
using System.Numerics;

namespace RvB.Graphs;

/// <summary>
/// The type of graph.
/// </summary>
[Flags]
public enum GraphType {
    Undirected = 1,
    Directed = 2,
    AllowSelfEdges = 128
}

public readonly struct GraphVertex<TVertex> where TVertex : notnull {
    public IGraph<TVertex> Graph { get; }
    public TVertex Vertex { get; }
    public GraphVertex(IGraph<TVertex> graph, TVertex v) {
        Graph = graph;
        Vertex = v;
    }

    public bool Remove()
        => Graph.RemoveVertex(this);

    public IEnumerable<GraphVertex<TVertex>> Connections
        => Graph.Connections(Vertex);

    public IEnumerable<GraphVertex<TVertex>> Neighbors
        => Connections;

    public IEnumerable<GraphVertex<TVertex>> Preceding
        => Graph.Preceding(Vertex);

    public static implicit operator TVertex(GraphVertex<TVertex> v)
        => v.Vertex;

    public override string? ToString()
        => Vertex.ToString();

    public override bool Equals([NotNullWhen(true)] object? obj) {
        if (obj is not null && obj is GraphVertex<TVertex> vertex)
            return Graph == vertex.Graph && Vertex.Equals(vertex.Vertex);
        return false;
    }

    public override int GetHashCode()
        => HashCode.Combine(Graph, Vertex);

    public static bool operator ==(GraphVertex<TVertex> left, GraphVertex<TVertex> right)
        => left.Equals(right);

    public static bool operator !=(GraphVertex<TVertex> left, GraphVertex<TVertex> right)
        => !(left == right);
}

public readonly struct GraphVertex<TVertex, TData> where TVertex : notnull {
    public IGraph<TVertex, TData> Graph { get; }
    public TVertex Vertex { get; }
    public GraphVertex(IGraph<TVertex, TData> graph, TVertex v) {
        Graph = graph;
        Vertex = v;
    }

    public bool Remove()
        => Graph.RemoveVertex(Vertex);

    public IEnumerable<GraphConnected<TVertex, TData>> Neighbors
        => Graph.Connections(Vertex);

    public IEnumerable<GraphConnected<TVertex, TData>> Preceding
        => Graph.Preceding(Vertex);

    public static implicit operator TVertex(GraphVertex<TVertex, TData> v)
        => v.Vertex;

    public override string? ToString()
        => Vertex?.ToString();

    public override bool Equals([NotNullWhen(true)] object? obj) {
        if (obj is not null && obj is GraphVertex<TVertex, TData> vertex)
            return Graph == vertex.Graph && Vertex.Equals(vertex.Vertex);
        return false;
    }

    public override int GetHashCode()
        => HashCode.Combine(Graph, Vertex);

    public static bool operator ==(GraphVertex<TVertex, TData> left, GraphVertex<TVertex, TData> right)
        => left.Equals(right);

    public static bool operator !=(GraphVertex<TVertex, TData> left, GraphVertex<TVertex, TData> right)
        => !(left == right);
}

public readonly struct GraphEdge<TVertex> where TVertex : notnull {
    public IGraph<TVertex> Graph { get; }
    public TVertex Vertex1 { get; }
    public TVertex Vertex2 { get; }

    public GraphEdge(IGraph<TVertex> graph, TVertex vertex1, TVertex vertex2) {
        Graph = graph;
        Vertex1 = vertex1;
        Vertex2 = vertex2;
    }

    public bool Remove()
        => Graph.RemoveEdge(this);

    public void Deconstruct(out TVertex vertex1, out TVertex vertex2) {
        vertex1 = Vertex1;
        vertex2 = Vertex2;
    }

    public override string ToString()
        => $"{Vertex1} - {Vertex2}";

    public override bool Equals([NotNullWhen(true)] object? obj) {
        if (obj is not null && obj is GraphEdge<TVertex> edge)
            return Graph == edge.Graph && Vertex1.Equals(edge.Vertex1) && Vertex2.Equals(edge.Vertex2);
        return false;
    }

    public override int GetHashCode()
        => HashCode.Combine(Graph, Vertex1, Vertex2);

    public static bool operator ==(GraphEdge<TVertex> left, GraphEdge<TVertex> right)
        => left.Equals(right);

    public static bool operator !=(GraphEdge<TVertex> left, GraphEdge<TVertex> right)
        => !(left == right);
}

public readonly struct GraphEdge<TVertex, TData> where TVertex : notnull {
    public IGraph<TVertex, TData> Graph { get; }
    public TVertex Vertex1 { get; }
    public TVertex Vertex2 { get; }
    public TData Data { get; }

    public GraphEdge(IGraph<TVertex, TData> graph, TVertex vertex1, TVertex vertex2, TData data) {
        Graph = graph;
        Vertex1 = vertex1;
        Vertex2 = vertex2;
        Data = data;
    }

    public bool Remove()
        => Graph.RemoveEdge(Vertex1, Vertex2);

    public void Deconstruct(out TVertex vertex1, out TVertex vertex2, out TData data) {
        vertex1 = Vertex1;
        vertex2 = Vertex2;
        data = Data;
    }

    public override string ToString()
        => $"{Vertex1} - {Vertex2} ({Data})";

    public override bool Equals([NotNullWhen(true)] object? obj) {
        if (obj is not null && obj is GraphEdge<TVertex, TData> edge) {
            bool dataEqual;
            if (Data is null)
                dataEqual = edge.Data is null;
            else
                dataEqual = Data.Equals(edge.Data);
            return Graph == edge.Graph && Vertex1.Equals(edge.Vertex1) && Vertex2.Equals(edge.Vertex2) && dataEqual;
        }
        return false;
    }

    public override int GetHashCode()
        => HashCode.Combine(Graph, Vertex1, Vertex2, Data);

    public static bool operator ==(GraphEdge<TVertex, TData> left, GraphEdge<TVertex, TData> right)
        => left.Equals(right);

    public static bool operator !=(GraphEdge<TVertex, TData> left, GraphEdge<TVertex, TData> right)
        => !(left == right);
}

public readonly struct GraphConnected<TVertex, TData> where TVertex : notnull {
    public IGraph<TVertex> Graph { get; }
    public TVertex Vertex { get; }
    public TData Data { get; }

    public GraphConnected(IGraph<TVertex> graph, TVertex vertex, TData data) {
        Graph = graph;
        Vertex = vertex;
        Data = data;
    }

    public void Deconstruct(out TVertex vertex, out TData data) {
        vertex = Vertex;
        data = Data;
    }
    public override string ToString()
        => $"{Vertex} ({Data})";

    public override bool Equals([NotNullWhen(true)] object? obj) {
        if (obj is not null && obj is GraphConnected<TVertex, TData> connected) {
            bool dataEqual;
            if (Data is null)
                dataEqual = connected.Data is null;
            else
                dataEqual = Data.Equals(connected.Data);
            return Graph == connected.Graph && Vertex.Equals(connected.Vertex) && dataEqual;
        }
        return false;
    }

    public override int GetHashCode()
        => HashCode.Combine(Graph, Vertex, Data);

    public static bool operator ==(GraphConnected<TVertex, TData> left, GraphConnected<TVertex, TData> right)
        => left.Equals(right);

    public static bool operator !=(GraphConnected<TVertex, TData> left, GraphConnected<TVertex, TData> right)
        => !(left == right);
}

public interface IEdgeDataWeight<TWeight> where TWeight : INumber<TWeight> {
    public TWeight Weight { get; }
}

public interface IGraph<TVertex> where TVertex : notnull {
    GraphType GraphType { get; }

    bool IsDirected { get; }

    int EdgeCount { get; }
    IEnumerable<GraphEdge<TVertex>> Edges { get; }

    int VertexCount { get; }
    IEnumerable<GraphVertex<TVertex>> Vertices { get; }

    bool ContainsEdge(in GraphEdge<TVertex> edge);
    bool ContainsEdge(TVertex vertex1, TVertex vertex2);

    bool TryAddEdge(TVertex vertex1, TVertex vertex2);

    IGraph<TVertex> AddEdge(TVertex vertex1, TVertex vertex2);

    IGraph<TVertex> AddEdges(TVertex vertex, IEnumerable<TVertex> vertices);

    IGraph<TVertex> AddEdges(IEnumerable<(TVertex Vertex1, TVertex Vertex2)> edges);

    bool RemoveEdge(in GraphEdge<TVertex> edge);
    bool RemoveEdge(TVertex vertex1, TVertex vertex2);

    bool ContainsVertex(TVertex vertex);

    bool AddVertex(TVertex vertex);

    bool RemoveVertex(in GraphVertex<TVertex> vertex);
    bool RemoveVertex(TVertex vertex);

    bool IsCyclic();

    int MinDistance(TVertex source, TVertex destination);
    IEnumerable<(TVertex Vertex, int Distance)> MinDistancePath(TVertex source, TVertex destination);

    IEnumerable<GraphVertex<TVertex>> Connections(TVertex vertex);

    [Obsolete("Use Connections(TVertex vertex) instead.")]
    IEnumerable<GraphVertex<TVertex>> Neighbors(TVertex vertex);

    IEnumerable<GraphVertex<TVertex>> Preceding(TVertex vertex);

    string Serialize();
}

public interface IGraph<TVertex, TData> : IGraph<TVertex> where TVertex : notnull {
    new IEnumerable<GraphEdge<TVertex, TData>> Edges { get; }

    new IEnumerable<GraphVertex<TVertex, TData>> Vertices { get; }

    bool TryAddEdge(TVertex vertex1, TVertex vertex2, TData data);

    IGraph<TVertex, TData> AddEdge(TVertex vertex1, TVertex vertex2, TData data);

    IGraph<TVertex, TData> AddEdges(TVertex vertex, IEnumerable<(TVertex, TData)> vertices);

    IGraph<TVertex, TData> AddEdges(IEnumerable<(TVertex Vertex1, TVertex Vertex2, TData Data)> edges);

    new IEnumerable<GraphConnected<TVertex, TData>> Connections(TVertex vertex);
    
    [Obsolete("Use Connections(TVertex vertex) instead.")]
    new IEnumerable<GraphConnected<TVertex, TData>> Neighbors(TVertex vertex);

    new IEnumerable<GraphConnected<TVertex, TData>> Preceding(TVertex vertex);

    TData this[in GraphEdge<TVertex> edge] { get; set; }
    TData this[TVertex vertex1, TVertex vertex2] { get; set; }

    TData GetData(in GraphEdge<TVertex> edge);
    TData GetData(TVertex vertex1, TVertex vertex2);

    bool TryGetData(TVertex vertex1, TVertex vertex2, [MaybeNullWhen(false)] out TData data);
}
