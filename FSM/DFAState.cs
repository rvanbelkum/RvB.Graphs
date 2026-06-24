using System.Diagnostics;

namespace RvB.Graphs.FSM;

public sealed class DFAState<T> : IState where T : notnull {
    private readonly int _id;
    private readonly bool _isEndState = false;
    // Transitions: Token => States
    private readonly DFATransitionMap<T> _transitionMap = new();

    public IEnumerable<(T Token, DFAState<T> State)> Transitions => _transitionMap;

    public DFAState<T> this[T token] => _transitionMap[token];

    public int Id => _id;

    public bool IsEndState => _isEndState;

    public DFAState(int id, bool isEndState) {
        _id = id;
        _isEndState = isEndState;
    }

    public void SetNextState(T token, DFAState<T> next) {
        if (!_transitionMap.TryGetValue(token, out var state)) {
            _transitionMap.Set(token, next);
        } else {
            Debug.Assert(state.Equals(next));
        }
    }

    public void Optimize() {
        while (true) {
            var dfas = new List<DFAState<T>>();
            CollectDFAs(dfas);

            var remap = new Dictionary<DFAState<T>, DFAState<T>>();
            for (int i = dfas.Count - 1; i >= 0; --i) {
                var iNextStates = dfas[i]._transitionMap.GetNextStatesByState();
                for (int j = i - 1; j >= 0; --j) {
                    var jNextStates = dfas[j]._transitionMap.GetNextStatesByState();
                    bool equiv = iNextStates.Count == jNextStates.Count && dfas[i].IsEndState == dfas[j].IsEndState;
                    if (equiv) {
                        foreach (var x in iNextStates.Keys) {
                            var y = x;
                            if (y == dfas[i])
                                y = dfas[j]; // Handle self-referencing
                            if (!(jNextStates.TryGetValue(y, out var yNextStates) && iNextStates[x].SequenceEqual(yNextStates)) &&
                                !(jNextStates.TryGetValue(x, out var xNextStates) && iNextStates[x].SequenceEqual(xNextStates))) {
                                equiv = false;
                                break;
                            }
                        }
                        if (equiv) {
                            remap.Add(dfas[i], dfas[j]);
                            break;
                        }
                    }
                }
            }
            if (remap.Count == 0)
                break;
            var visited = new HashSet<DFAState<T>>();
            RemapNFAs(remap, visited);
        }
    }

    public string RenderEdges(List<string> endStates) {
        return RenderEdges(endStates, []);
    }

    private string RenderEdges(List<string> endStates, List<object> visited) {
        string edges = "";
        if (!visited.Contains(this)) {
            visited.Add(this);
            if (IsEndState)
                endStates.Add($"S{Id}");
            foreach (var (token, state) in _transitionMap) {
                string label = token.ToString()!;
                label = label.Replace("\"", "\\\"");
                edges += $"\tS{Id} -> S{state.Id} [label=\"{label}\"]\r\n";
                edges += state.RenderEdges(endStates, visited);
            }
        }
        return edges;
    }

    private void CollectDFAs(List<DFAState<T>> dfas) {
        if (!dfas.Contains(this)) {
            dfas.Add(this);
            foreach (var (_, state) in _transitionMap) {
                state.CollectDFAs(dfas);
            }
        }
    }

    private void RemapNFAs(Dictionary<DFAState<T>, DFAState<T>> remap, HashSet<DFAState<T>> visited) {
        if (visited.Add(this)) {
            foreach (var (token, state) in _transitionMap.ToArray()) {
                if (remap.TryGetValue(state, out var value)) {
                    _transitionMap[token] = value;
                }
                _transitionMap[token].RemapNFAs(remap, visited);
            }
        }
    }

    public override string ToString() {
        return $"{Id}: {_transitionMap}";
    }
}
