namespace RvB.Graphs;

internal readonly struct Edge<TVertex> {
    public readonly TVertex Vertex1 { get; init; }

    public readonly TVertex Vertex2 { get; init; }

    public Edge(TVertex vertex1, TVertex vertex2) {
        Vertex1 = vertex1;
        Vertex2 = vertex2;
    }

    public void Deconstruct(out TVertex vertex1, out TVertex vertex2) {
        vertex1 = Vertex1;
        vertex2 = Vertex2;
    }

    public override string ToString() => $"{Vertex1} - {Vertex2}";
}

internal readonly struct Edge<TVertex, TData> {
    public readonly TVertex Vertex1 { get; init; }

    public readonly TVertex Vertex2 { get; init; }

    public readonly TData Data { get; init; }

    public Edge(TVertex vertex1, TVertex vertex2, TData data) {
        Vertex1 = vertex1;
        Vertex2 = vertex2;
        Data = data;
    }

    public void Deconstruct(out TVertex vertex1, out TVertex vertex2, out TData data) {
        vertex1 = Vertex1;
        vertex2 = Vertex2;
        data = Data;
    }

    public override string ToString() => $"{Vertex1} - {Vertex2} ({Data})";
}

internal class SerializedGraph<TVertex> {
    public GraphType GraphType { get; set; }
    required public TVertex[] Vertices { get; set; }
    required public Edge<TVertex>[] Edges { get; set; }
}

internal class SerializedGraph<TVertex, TData> {
    public GraphType GraphType { get; set; }
    required public TVertex[] Vertices { get; set; }
    required public Edge<TVertex, TData>[] Edges { get; set; }
}
