namespace RvB.Graphs;

public class MinCutSolver<TVertex> where TVertex : notnull {
    private const int BRUTEFORCEBOUND = 6;

    /// <summary>
    /// Solves the minimum cut problem using FastCut algorithm.
    /// </summary>
    /// <param name="graph">The graph on which the algorithm should be executed.</param>
    /// <returns>The size of the minimum cut.</returns>
    public (int CutSize, HashSet<TVertex> Set1, HashSet<TVertex> Set2) SolveFastcut(Graph<TVertex> graph, int maximum) {
        int minSize = int.MaxValue;
        var result = MinCutSolver<TVertex>.SolveFastcut(new(graph), maximum, ref minSize);
        return result;
    }

    public static (int CutSize, HashSet<TVertex> Set1, HashSet<TVertex> Set2) SolveKarger(Graph<TVertex> graph) {
        var contractedGraph = new ContractedGraph<TVertex>(graph);
        contractedGraph.ContractionSequence(2);
        var cutSize = contractedGraph.GetCutSize();
        var cuts = contractedGraph.GetCuts().ToArray();

        if (cuts.Length == 2) {
            return (cutSize, cuts[0], cuts[1]);
        } else {
            return (0, [], []);
        }
    }

    private static (int CutSize, HashSet<TVertex> Set1, HashSet<TVertex> Set2) SolveFastcut(ContractedGraph<TVertex> graph, int maximum, ref int minSize) {
        if (graph.VertexCount <= BRUTEFORCEBOUND) {
            var result = SolveBruteForce(graph, minSize);
            if (result.CutSize < minSize) {
                minSize = result.CutSize;
                if (result.CutSize <= maximum) {
                    return result;
                }
            }
            return (int.MaxValue, [], []);
        } else {
            int t = Convert.ToInt32(Math.Ceiling(1 + (graph.VertexCount / Math.Sqrt(2))));

            var h1 = new ContractedGraph<TVertex>(graph);
            h1.ContractionSequence(t);
            var h2 = new ContractedGraph<TVertex>(graph);
            h2.ContractionSequence(t);

            var cutH1 = MinCutSolver<TVertex>.SolveFastcut(h1, maximum, ref minSize);
            if (cutH1.CutSize <= maximum) {
                return cutH1;
            }
            var cutH2 = MinCutSolver<TVertex>.SolveFastcut(h2, maximum, ref minSize);
            if (cutH1.CutSize < cutH2.CutSize)
                return cutH1;
            else
                return cutH2;
        }
    }

    private static (int CutSize, HashSet<TVertex> CutSet) s_emptyResult = (int.MaxValue, []);

    /// <summary>
    /// Solves the minimum cut problem.
    /// </summary>
    /// <returns>The cut of minimum size.</returns>
    private static (int CutSize, HashSet<TVertex> Set1, HashSet<TVertex> Set2) SolveBruteForce(ContractedGraph<TVertex> graph, int minSize) {
        var smallestCut = minSize;
        var result = EnumerateSubsets(graph, [], 0, ref smallestCut);
        if (result != s_emptyResult) {
            var cuts = graph.GetCuts(result.CutSet);
            return (result.CutSize, cuts.Item1, cuts.Item2);
        }
        return (0, [], []);
    }

    /// <summary>
    /// Enumerates all the subsets of a set recursively.
    /// </summary>
    /// <param name="vertices">All the elements of the superset.</param>
    /// <param name="subset">The subset.</param>
    /// <param name="index">The current index.</param>
    private static (int CutSize, HashSet<TVertex> CutSet) EnumerateSubsets(ContractedGraph<TVertex> graph, HashSet<TVertex> subset, int index, ref int minSize) {
        int size;
        // Base Condition
        if (index == graph.VertexCount) {
            size = DetermineCutsizeOfSet(graph, subset);
            if (size < minSize) {
                minSize = size;
                return (minSize, new(subset));
            } else {
                return s_emptyResult;
            }
        } else {
            var result1 = EnumerateSubsets(graph, subset, index + 1, ref minSize);
            var vertex = graph.Vertices.ElementAt(index);
            subset.Add(vertex);
            var result2 = EnumerateSubsets(graph, subset, index + 1, ref minSize);
            subset.Remove(vertex);
            if (result1.CutSize < result2.CutSize)
                return result1;
            else
                return result2;
        }
    }

    /// <summary>
    /// Determines the size of the cut for a given partition of the set of vertices.
    /// </summary>
    /// <param name="subset">The first set of the partition.</param>
    private static int DetermineCutsizeOfSet(ContractedGraph<TVertex> graph, HashSet<TVertex> subset) {
        // A cut-set must be a PROPER NON-EMPTY subset of vertices.
        if (subset.Count == 0 || subset.Count == graph.VertexCount) {
            return int.MaxValue;
        }
        int cutsize = graph.GetCutSize(subset);
        return cutsize;
    }
}
