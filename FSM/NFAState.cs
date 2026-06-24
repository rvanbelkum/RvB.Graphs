using System.Text;

namespace RvB.Graphs.FSM;

public sealed class NFAState<T> : IState where T : notnull {
    private bool _isEndState = false;

    private NFATransitionMap<T> _transitionMap = new();

    public int Id { get; }

    public bool IsEndState => _isEndState;

    internal NFAState(int _id) : this(_id, false) { }

    internal NFAState(int _id, bool isEndState) {
        Id = _id;
        _isEndState = isEndState;
    }

    public void AddTransition(NFAState<T> state) {
        _transitionMap.Add(state);
    }

    public void AddTransition(T token, NFAState<T> state) {
        _transitionMap.Add(token, state);
    }

    public NFAState<T> Clone(NFA<T> nfa) {
        var map = new Dictionary<NFAState<T>, NFAState<T>>();
        return Clone(nfa, map);
    }
    
    public NFAState<T> Clone(NFA<T> nfa, Dictionary<NFAState<T>, NFAState<T>> map) {
        if (!map.ContainsKey(this)) {
            var clone = nfa.CreateState(IsEndState);
            clone._transitionMap = _transitionMap.Clone(nfa, map);
            map.Add(this, clone);
        }
        return map[this];
    }

    public void Optimize() {
        EliminateEpsilonTransitions([]);
    }

    public void SetClosure(NFAState<T> state, bool keepEndState) {
        var visited = new HashSet<NFAState<T>>();
        SetClosure(state, keepEndState, visited);
    }

    public string RenderEdges(List<string> endStates) {
        var edges = new StringBuilder();
        RenderEdges(edges, endStates, []);
        return edges.ToString();
    }

    private void RenderEdges(StringBuilder edges, List<string> endStates, HashSet<NFAState<T>> visited) {
        if (visited.Add(this)) {
            if (IsEndState) {
                endStates.Add($"S{Id}");
            }
            foreach (var (state, label) in _transitionMap.GetTransitionsByState()) {
                edges.AppendLine($"\tS{Id} -> S{state.Id} [label=\"{label.Replace("\"", "\\\"")}\"]");
                state.RenderEdges(edges, endStates, visited);
            }
        }
    }

    internal static DFAState<T> CreateDFAState(DFA<T> dfa, NFAState<T> startState) {
        return CreateDFAState(dfa, [startState], new(HashSetEqualityComparer<NFAState<T>>.Default));
    }

    private static DFAState<T> CreateDFAState(DFA<T> dfa, HashSet<NFAState<T>> stateSet, Dictionary<HashSet<NFAState<T>>, DFAState<T>> dfaMap) {
        if (!dfaMap.TryGetValue(stateSet, out var dfaState)) {
            var reachable = new NFATransitionMap<T>();
            var isEndState = false;

            foreach (var state in stateSet) {
                isEndState |= state.IsEndState;
                foreach (var (token, nextStates) in state._transitionMap.GetNonΕTransitions()) {
                    if (reachable.TryGetValue(token, out var reachableStates)) {
                        reachableStates.AddRange(nextStates);
                    } else {
                        reachable[token] = [.. nextStates];
                    }
                }
            }
            dfaState = dfa.CreateState(isEndState);
            dfaMap.Add(stateSet, dfaState);
            foreach (var (token, states) in reachable.GetNonΕTransitions()) {
                var nextDfaState = CreateDFAState(dfa, states, dfaMap);
                dfaState.SetNextState(token, nextDfaState);
            }
        }
        return dfaState;
    }

    private void SetClosure(NFAState<T> state, bool keepEndState, HashSet<NFAState<T>> visited) {
        if (visited.Add(this)) {
            if (IsEndState) {
                AddTransition(state);
                if (!keepEndState) {
                    _isEndState = false;
                }
            } else {
                foreach (var nextState in _transitionMap.GetPossibleNextStates()) {
                    nextState.SetClosure(state, keepEndState, visited);
                }
            }
        }
    }

    private void EliminateEpsilonTransitions(HashSet<NFAState<T>> visited) {
        if (visited.Add(this)) {
            bool isEndState = IsEndState;
            NFATransitionMap<T> newTransitions = new();
            GetEpsilonReacheables(ref isEndState, newTransitions, []);
            _isEndState = isEndState;

            _transitionMap = newTransitions;

            foreach (var (token, states) in _transitionMap.GetNonΕTransitions()) {
                foreach (var state in states) {
                    state.EliminateEpsilonTransitions(visited);
                }
            }
        }
    }

    private void GetEpsilonReacheables(ref bool endState, NFATransitionMap<T> map, HashSet<NFAState<T>> visited) {
        if (visited.Add(this)) {
            foreach (var state in _transitionMap.GetΕStates()) {
                state.GetEpsilonReacheables(ref endState, map, visited);
            }
            foreach (var (token, states) in _transitionMap.GetNonΕTransitions()) {
                foreach (var state in states) {
                    map.Add(token, state);
                }
            }
            endState = endState || IsEndState;
        }
    }

    public override string ToString() {
        return $"{Id}: {_transitionMap}";
    }
}
