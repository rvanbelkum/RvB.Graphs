using System.Numerics;

namespace RvB.Graphs;

public class Dijkstra {
    /// <summary>
    /// Calculates the minimal path distance in a connected set of nodes of type <typeparamref name="TNode"/>.
    /// </summary>
    /// <typeparam name="TNode">Type of the nodes</typeparam>
    /// <typeparam name="TDist">Numeric type for specifying the distance</typeparam>
    /// <param name="source">Start node</param>
    /// <param name="destination">End node</param>
    /// <param name="connectedNodes">Function specifying the connected nodes and distance of a given node</param>
    /// <param name="maxDistance">Optional parameter specifying the maximum distance. If exceeded, the calculation will end prematurely</param>
    /// <returns>The distance between <paramref name="source"/> and <paramref name="destination"/> or <typeparamref name="TDist"/>.Zero if a path can not be established</returns>
    public static TDist CalcMinimalDistance<TNode, TDist>(TNode source, TNode destination, Func<TNode, IEnumerable<TNode>> connectedNodes, TDist? maxDistance = default)
        where TNode : notnull where TDist : INumber<TDist> {

        var maxDist = maxDistance ?? TDist.Zero;
        return CalcMinimalDistance(source, destination, (n) => connectedNodes(n).Select(c => (c, TDist.One)), maxDist);
    }

    /// <summary>
    /// Calculates the minimal path distance in a connected set of nodes of type <typeparamref name="TNode"/>.
    /// </summary>
    /// <typeparam name="TNode">Type of the nodes</typeparam>
    /// <typeparam name="TDist">Numeric type for specifying the distance</typeparam>
    /// <param name="source">Start node</param>
    /// <param name="destination">End node</param>
    /// <param name="connectedNodes">Function specifying the connected nodes and distance of a given node</param>
    /// <param name="maxDistance">Optional parameter specifying the maximum distance. If exceeded, the calculation will end prematurely</param>
    /// <returns>The distance between <paramref name="source"/> and <paramref name="destination"/> or <typeparamref name="TDist"/>.Zero if a path can not be established</returns>
    public static TDist CalcMinimalDistance<TNode, TDist>(TNode source, TNode destination, Func<TNode, IEnumerable<(TNode Node, TDist Distance)>> connectedNodes)
        where TNode : notnull where TDist : INumber<TDist> {

        return CalcMinimalDistance(source, destination, connectedNodes, TDist.Zero);
    }

    /// <summary>
    /// Calculates the minimal path distance in a connected set of nodes of type <typeparamref name="TNode"/>.
    /// </summary>
    /// <typeparam name="TNode">Type of the nodes</typeparam>
    /// <typeparam name="TDist">Numeric type for specifying the distance</typeparam>
    /// <param name="source">Start node</param>
    /// <param name="destination">End node</param>
    /// <param name="connectedNodes">FFunction specifying the connected nodes and distance of a given node</param>
    /// <param name="maxDistance">Specifies the maximum distance. If a path distance not exceeding <paramref name="maxDistance"/> cannot be found, <typeparamref name="TDist"/>.Zero will be returned.</param>
    /// <returns>The distance between <paramref name="source"/> and <paramref name="destination"/> or <typeparamref name="TDist"/>.Zero if a path cannot be found that is less than <paramref name="maxDistance"/>.</returns>
    public static TDist CalcMinimalDistance<TNode, TDist>(TNode source, TNode destination, Func<TNode, IEnumerable<(TNode Node, TDist Distance)>> connectedNodes, TDist maxDistance)
        where TNode : notnull where TDist : INumber<TDist> {

        return CalcMinimalDistance(source, (item) => destination.Equals(item), connectedNodes, maxDistance);
    }

    public static TDist CalcMinimalDistance<TNode, TDist>(TNode source, Func<TNode, bool> IsAtEnd, Func<TNode, IEnumerable<(TNode Node, TDist Distance)>> connectedNodes, TDist maxDistance)
        where TNode : notnull where TDist : INumber<TDist> {

        HashSet<TNode> visited = [];
        PriorityQueue<TNode, TDist> toDo = new();

        TDist pathDistance = TDist.Zero;
        toDo.Enqueue(source, TDist.Zero);
        while (toDo.TryDequeue(out var item, out var distance)) {
            if (!visited.Add(item))
                continue;
            if (IsAtEnd(item)) {
                pathDistance = distance;
                break;
            }
            var connected = connectedNodes(item);
            if (connected is null)
                continue;
            foreach (var (successor, delta) in connected) {
                var newDistance = distance + delta;
                if (maxDistance != TDist.Zero && newDistance > maxDistance)
                    continue;
                toDo.Enqueue(successor, newDistance);
            }
        }
        return pathDistance;
    }

    public static IEnumerable<(TNode Node, TDist Distance)> CalcMinimalDistances<TNode, TDist>(TNode source, Func<TNode, IEnumerable<TNode>> connectedNodes)
        where TNode : notnull where TDist : INumber<TDist> {

        return CalcMinimalDistances(source, (n) => connectedNodes(n).Select(c => (c, TDist.One)));
    }

    /// <summary>
    /// Calculates the minimal path distances from <paramref name="source"/> to all other reachable nodes in a connected set of nodes of type <typeparamref name="TNode"/>.
    /// </summary>
    /// <typeparam name="TNode">Type of the nodes</typeparam>
    /// <typeparam name="TDist">Numeric type for specifying the distance</typeparam>
    /// <param name="source">Start node</param>
    /// <param name="connectedNodes">Function specifying the connected nodes and distance of a given node</param>
    /// <returns>The distances between <paramref name="source"/> and all other reachable nodes</returns>
    public static IEnumerable<(TNode Node, TDist Distance)> CalcMinimalDistances<TNode, TDist>(TNode source, Func<TNode, IEnumerable<(TNode Node, TDist Distance)>> connectedNodes)
        where TNode : notnull where TDist : INumber<TDist> {
        return CalcMinimalDistances(source, connectedNodes, TDist.Zero);
    }

    /// <summary>
    /// Calculates the minimal path distances from <paramref name="source"/> to all other reachable nodes in a connected set of nodes of type <typeparamref name="TNode"/>.
    /// </summary>
    /// <typeparam name="TNode">Type of the nodes</typeparam>
    /// <typeparam name="TDist">Numeric type for specifying the distance</typeparam>
    /// <param name="source">Start node</param>
    /// <param name="connectedNodes">Function specifying the connected nodes and distance of a given node</param>
    /// <param name="maxDistance">Specifies the maximum distance. Nodes exceeding this distances are not returned</param>
    /// <returns>The distances between <paramref name="source"/> and all other reachable nodes</returns>
    public static IEnumerable<(TNode Node, TDist Distance)> CalcMinimalDistances<TNode, TDist>(TNode source, Func<TNode, IEnumerable<(TNode Node, TDist Distance)>> connectedNodes, TDist maxDistance)
        where TNode : notnull where TDist : INumber<TDist> {

        HashSet<TNode> visited = [];
        PriorityQueue<TNode, TDist> toDo = new();

        toDo.Enqueue(source, TDist.Zero);
        while (toDo.TryDequeue(out var item, out var distance)) {
            if (!visited.Add(item))
                continue;
            if (!item.Equals(source))
                yield return (item, distance);
            var connected = connectedNodes(item);
            if (connected is null)
                continue;
            foreach (var (successor, delta) in connected) {
                var newDistance = distance + delta;
                if (maxDistance != TDist.Zero && newDistance > maxDistance)
                    continue;
                toDo.Enqueue(successor, newDistance);
            }
        }
    }

    public static List<(TNode Node, TDist Distance)> GetMinimalPath<TNode, TDist>(TNode source, TNode destination, Func<TNode, IEnumerable<(TNode Node, TDist Distance)>> successors)
        where TNode : notnull where TDist : INumber<TDist> {

        return GetMinimalPath(source, destination, successors, TDist.Zero);
    }

    public static List<(TNode Node, TDist Distance)> GetMinimalPath<TNode, TDist>(TNode source, TNode destination, Func<TNode, IEnumerable<(TNode Node, TDist Distance)>> connectedNodes, TDist maxDistance)
        where TNode : notnull where TDist : INumber<TDist> {

        return GetMinimalPath(source, (item) => destination.Equals(item), connectedNodes, maxDistance);
    }

    public static List<(TNode Node, TDist Distance)> GetMinimalPath<TNode, TDist>(TNode source, Func<TNode, bool> IsAtEnd, Func<TNode, IEnumerable<(TNode Node, TDist Distance)>> connectedNodes, TDist maxDistance)
        where TNode : notnull where TDist : INumber<TDist> {

        var path = new List<(TNode Node, TDist Distance)>();
        foreach (var (node, distance) in GetMinimalPathNodes(source, IsAtEnd, connectedNodes, maxDistance)) { 
            path.Add((node, distance));
        }
        path.Reverse();
        return path;
    }

    public static IEnumerable<(TNode Node, TDist Distance)> GetMinimalPathNodes<TNode, TDist>(TNode source, TNode destination, Func<TNode, IEnumerable<(TNode Node, TDist Distance)>> connectedNodes, TDist maxDistance)
        where TNode : notnull where TDist : INumber<TDist> {
        return GetMinimalPathNodes(source, (item) => destination.Equals(item), connectedNodes, maxDistance);
    }

    public static IEnumerable<(TNode Node, TDist Distance)> GetMinimalPathNodes<TNode, TDist>(TNode source, Func<TNode, bool> IsAtEnd, Func<TNode, IEnumerable<(TNode Node, TDist Distance)>> connectedNodes, TDist maxDistance)
        where TNode : notnull where TDist : INumber<TDist> {

        Dictionary<TNode, (TNode? Parent, TDist Distance)> visited = [];      // Item => (PrevItem, Distance)
        PriorityQueue<(TNode Node, TNode? Parent), TDist> toDo = new();   // (Item, PrevItem), Distance

        IEnumerable<(TNode Node, TDist Distance)> path = [];
        toDo.Enqueue((source, default(TNode)), TDist.Zero);
        while (toDo.TryDequeue(out var value, out var distance)) {
            var (item, prev) = value;
            if (visited.ContainsKey(item))
                continue;
            visited.Add(item, (prev, distance));
            if (IsAtEnd(item)) {
                var node = item;
                while (node != null && !source!.Equals(node)) {
                    var (nextNode, dist) = visited[node];
                    yield return (node, dist);
                    node = nextNode;
                }
                yield return (source, TDist.Zero);
                break;
            }
            var connected = connectedNodes(item);
            if (connected is null)
                continue;
            foreach (var (successor, delta) in connected) {
                var newDistance = distance + delta;
                if (maxDistance != TDist.Zero && newDistance > maxDistance)
                    continue;
                toDo.Enqueue((successor, item), newDistance);
            }
        }
    }

    private sealed record class LinkedNode<TNode, TDist>(TNode Node, LinkedNode<TNode, TDist>? Prev, TDist Distance) { }

    public static IEnumerable<List<(TNode Node, TDist Distance)>> GetMinimalPaths<TNode, TDist>(TNode source, Func<TNode, bool> IsAtEnd, Func<TNode, IEnumerable<(TNode Node, TDist Distance)>> connectedNodes, TDist maxDistance)
        where TNode : notnull where TDist : INumber<TDist>, IMinMaxValue<TDist> {

        foreach (var path in GetMinimalPathsNodes(source, IsAtEnd, connectedNodes, maxDistance)) {
            var pathNodes = new List<(TNode Node, TDist Distance)>();
            foreach (var node in path) {
                pathNodes.Add(node);
            }
            pathNodes.Reverse();
            yield return pathNodes;
        }
    }
    
    public static IEnumerable<IEnumerable<(TNode Node, TDist Distance)>> GetMinimalPathsNodes<TNode, TDist>(TNode source, Func<TNode, bool> IsAtEnd, Func<TNode, IEnumerable<(TNode Node, TDist Distance)>> connectedNodes, TDist maxDistance)
        where TNode : notnull where TDist : INumber<TDist>, IMinMaxValue<TDist> {

        Dictionary<TNode, TDist> distances = [];
        PriorityQueue<LinkedNode<TNode, TDist>, TDist> toDo = new();

        toDo.Enqueue(new(source, null, TDist.Zero), TDist.Zero);
        distances[source] = TDist.Zero;
        TDist minDistance = maxDistance == TDist.Zero ? TDist.MaxValue : maxDistance;
        while (toDo.TryDequeue(out var linkedNode, out var distance)) {
            if (distance > minDistance) {
                continue;
            }
            var item = linkedNode.Node;
            if (IsAtEnd(item)) {
                minDistance = distance;
                yield return GeneratePath(linkedNode);
            }
            foreach (var (successor, delta) in connectedNodes(item)) {
                var newDistance = distance + delta;
                if (newDistance > minDistance)
                    continue;
                if (distances.TryGetValue(successor, out var knownDistance) && knownDistance < newDistance) {
                    continue;
                }
                distances[successor] = newDistance;
                toDo.Enqueue(new(successor, linkedNode, newDistance), newDistance);
            }
        }

        static IEnumerable<(TNode Node, TDist Distance)> GeneratePath(LinkedNode<TNode, TDist>? node) {
            while (node != null) {
                yield return (node.Node, node.Distance);
                node = node.Prev;
            }
        }
    }

    private static List<(TNode Node, TDist Distance)> GeneratePath<TNode, TDist>(TNode startNode, TNode endNode, Dictionary<TNode, (TNode? Parent, TDist Distance)> visited)
        where TNode : notnull where TDist : INumber<TDist> {

        var path = new List<(TNode Node, TDist Distance)>();
        var node = endNode;
        while (node != null && !startNode!.Equals(node)) {
            var (nextNode, distance) = visited[node];
            path.Add((node, distance));
            node = nextNode;
        }
        path.Add((startNode, TDist.Zero));
        path.Reverse();
        return path;
    }

    private static List<(TNode Node, TDist Distance)> GeneratePath<TNode, TDist>(LinkedNode<TNode, TDist>? node) {
        var path = new List<(TNode Node, TDist Distance)>();
        while (node != null) {
            path.Add((node.Node, node.Distance));
            node = node.Prev;
        }
        path.Reverse();
        return path;
    }
}

public class DFS {
    public enum EnumOrder {
        TopDown,
        BottomUp,
        LeavesOnly
    }
    public static IEnumerable<TNode> Enumerate<TNode>(TNode start, Func<TNode, IEnumerable<TNode>> successors) {
        HashSet<TNode> visited = [];
        Stack<(TNode, int)> stack = new();
        stack.Push((start, 0));
        while (stack.Count > 0) {
            var (next, depth) = stack.Pop();
            if (!visited.Add(next)) {
                continue;
            }
            yield return next;
            var fSuccessors = successors(next);
            if (fSuccessors is not null) {
                foreach (var successor in fSuccessors) {
                    stack.Push((successor, depth + 1));
                }
            }
        }
    }
}
