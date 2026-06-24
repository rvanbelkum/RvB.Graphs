using System.Diagnostics;
using System.Numerics;

namespace RvB.Graphs;

public static class AStar {
    public static TDist FindShortestPathDistance<TNode, TDist>(TNode startNode, TNode endNode, Func<TNode, IEnumerable<TNode>> connectedNodes, Func<TNode, TNode, TDist> estimatedDistance) where TNode : notnull where TDist : INumber<TDist> {
        return FindShortestPathDistance(startNode, endNode, n => connectedNodes(n).Select(next => (next, TDist.One)), estimatedDistance);
    }

    /// <summary>
    /// Calculates the minimal path distance in a connected set of nodes of type <typeparamref name="TNode"/>
    /// </summary>
    /// <typeparam name="TNode">Type of the nodes</typeparam>
    /// <typeparam name="TDist">Numeric type, derived from INumber, for specifying the distance</typeparam>
    /// <param name="startNode">Start node</param>
    /// <param name="endNode">End node</param>
    /// <param name="connectedNodes">Function specifying the successors and distances of a given node</param>
    /// <param name="estimatedDistance"></param>
    /// <returns>The distance between <paramref name="startNode"/> and <paramref name="endNode"/> or <typeparamref name="TDist"/>.Zero if a path cannot be found.</returns>
    public static TDist FindShortestPathDistance<TNode, TDist>(TNode startNode, TNode endNode, Func<TNode, IEnumerable<(TNode, TDist)>> connectedNodes, Func<TNode, TNode, TDist> estimatedDistance) where TNode : notnull where TDist : INumber<TDist> {
#if DEBUG
        var timer = Stopwatch.StartNew();
#endif
        var queue = new PriorityQueue<TNode, TDist>();
        HashSet<TNode> visited = [];

        TDist pathDistance = TDist.Zero;
        queue.Enqueue(startNode, TDist.Zero);
        Dictionary<TNode, TDist> minDistances = new() {
            { startNode, TDist.Zero }
        };
        while (queue.TryDequeue(out var currentNode, out _)) {
            if (!visited.Add(currentNode))
                continue;
            if (currentNode!.Equals(endNode)) {
                pathDistance = minDistances[endNode];
                break;
            }
            var nextNodes = connectedNodes(currentNode);
            if (nextNodes is null)
                continue;
            var currentDistance = minDistances[currentNode];
            foreach (var (nextNode, dist) in nextNodes) {
                var distance = currentDistance + dist;
                if (!minDistances.TryGetValue(nextNode, out var prevDist) || distance < prevDist) {
                    var estimatedTotalDistance = distance + estimatedDistance(nextNode, endNode);
                    queue.Enqueue(nextNode, estimatedTotalDistance);
                    minDistances[nextNode] = distance;
                }
            }
        }
#if DEBUG
        timer.Stop();
        Debug.WriteLine($"{nameof(AStar)}.{nameof(FindShortestPathDistance)}: Distance = {pathDistance} (visited {visited.Count} in {timer.ElapsedMilliseconds} ms)");
#endif
        return pathDistance;
    }

    public static IEnumerable<(TNode Node, TDist Distance)> FindShortestPath<TNode, TDist>(TNode startNode, TNode endNode, Func<TNode, IEnumerable<TNode>> connectedNodes, Func<TNode, TNode, TDist> estimatedDistance) where TNode : notnull where TDist : INumber<TDist> {
        return FindShortestPath(startNode, endNode, (n) => connectedNodes(n).Select(p => (p, TDist.One)), estimatedDistance);
    }

    public static IEnumerable<(TNode Node, TDist Distance)> FindShortestPath<TNode, TDist>(TNode startNode, TNode endNode, Func<TNode, IEnumerable<(TNode node, TDist distance)>> connectedNodes, Func<TNode, TNode, TDist> estimatedDistance) where TNode : notnull where TDist : INumber<TDist> {
#if DEBUG
        var timer = Stopwatch.StartNew();
        int pathLength = 0;
        TDist pathDistance = TDist.Zero;
#endif
        var queue = new PriorityQueue<TNode, TDist>();                          // Node x TotalEstimatedDistance
        Dictionary<TNode, (TNode Parent, TDist Distance)> minDistances = [];    // Node x (PrevNode MinDistanceSoFar)
        HashSet<TNode> visited = [];                                            // Visited nodes

        IEnumerable<(TNode Node, TDist Distance)> path = [];
        queue.Enqueue(startNode, TDist.Zero);
        minDistances[startNode] = new() { Distance = TDist.Zero };
        while (queue.TryDequeue(out var currentElement, out _)) {
            if (!visited.Add(currentElement))
                continue;
            if (currentElement!.Equals(endNode)) {
                var path1 = GeneratePath(startNode, endNode, minDistances);
                path = path1;
#if DEBUG
                pathDistance = path1[^1].Distance;
                pathLength = path1.Count;
#endif
                break;
            }
            var nextElements = connectedNodes(currentElement);
            if (nextElements is null)
                continue;
            var (parent, distance) = minDistances[currentElement];
            foreach (var (nextElement, dist) in nextElements) {
                var nextDistance = distance + dist;
                if (!minDistances.TryGetValue(nextElement, out var node) || nextDistance < node.Distance) {
                    var estimatedTotalDistance = nextDistance + estimatedDistance(nextElement, endNode);
                    queue.Enqueue(nextElement, estimatedTotalDistance);
                    minDistances[nextElement] = (currentElement, nextDistance);
                }
            }
        }
#if DEBUG
        timer.Stop();
        Debug.WriteLine($"{nameof(AStar)}.{nameof(FindShortestPath)}: Distance = {pathDistance} ({pathLength} nodes, {visited.Count} visited, finished in {timer.ElapsedMilliseconds} ms");
#endif
        return path;
    }

    private static List<(TNode Node, TDist Distance)> GeneratePath<TNode, TDist>(TNode startNode, TNode endNode, Dictionary<TNode, (TNode Parent, TDist Distance)> visited) where TNode : notnull where TDist : INumber<TDist> {
        var path = new List<(TNode Node, TDist Distance)>();
        var node = endNode;
        while (!startNode.Equals(node)) {
            var (nextNode, distance) = visited[node];
            path.Add((node, distance));
            node = nextNode;
        }
        path.Add((startNode, TDist.Zero));
        path.Reverse();
        return path;
    }
}
