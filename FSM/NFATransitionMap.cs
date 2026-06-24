using System.Diagnostics.CodeAnalysis;

namespace RvB.Graphs.FSM;

internal sealed class NFATransitionMap<T> where T : notnull {
    // Epsilon Transitions: States
    private HashSet<NFAState<T>> _εTransitions = [];

    // Transitions: Token => States
    private Dictionary<T, HashSet<NFAState<T>>> _transitions = [];

    public void ClearE() => _εTransitions.Clear();

    public void Add(NFAState<T> state) {
        _εTransitions.Add(state);
    }

    public void Add(T token, NFAState<T> state) {
        if (_transitions.TryGetValue(token, out var states)) {
            states.Add(state);
        } else {
            _transitions[token] = [state];
        }
    }

    public NFATransitionMap<T> Clone(NFA<T> nfa, Dictionary<NFAState<T>, NFAState<T>> map) {
        var εTransitions = new HashSet<NFAState<T>>();
        foreach (var state in _εTransitions) {
            εTransitions.Add(state.Clone(nfa, map));
        }
        var transitions = new Dictionary<T, HashSet<NFAState<T>>>();
        foreach (var (token, states) in _transitions) {
            var clonedStates = new HashSet<NFAState<T>>();
            foreach (var state in states) {
                clonedStates.Add(state.Clone(nfa, map));
            }
            transitions.Add(token, clonedStates);
        }
        return new() {
            _εTransitions = εTransitions,
            _transitions = transitions
        };
    }

    public HashSet<NFAState<T>> this[T token] {
        get => _transitions[token];
        set => _transitions[token] = value;
    }

    public bool TryGetValue(T token, [MaybeNullWhen(false)] out HashSet<NFAState<T>> states) {
        return _transitions.TryGetValue(token, out states);
    }

    public IEnumerable<(T Token, HashSet<NFAState<T>> StateSet)> GetNonΕTransitions() {
        foreach (var (token, nextStates) in _transitions)
            yield return (token, nextStates);
    }

    public IEnumerable<NFAState<T>> GetΕStates() {
        foreach (var nextState in _εTransitions)
            yield return nextState;
    }

    public IEnumerable<NFAState<T>> GetPossibleNextStates() {
        var nextStates = new HashSet<NFAState<T>>();
        foreach (var state in _εTransitions) {
            if (nextStates.Add(state)) {
                yield return state;
            }
        }
        foreach (var (token, states) in _transitions) {
            foreach (var state in states) {
                if (nextStates.Add(state)) {
                    yield return state;
                }
            }
        }
    }

    public IEnumerable<(NFAState<T> State, string Label)> GetTransitionsByState() {
        const string epsilon = "ε";
        var edges = new Dictionary<NFAState<T>, List<string>>();
        foreach (var state in _εTransitions) {
            if (!edges.TryGetValue(state, out var value)) {
                edges[state] = [epsilon];
            } else {
                value.Add(epsilon);
            }
        }
        foreach (var (token, states) in _transitions) {
            foreach (var state in states) {
                var tokenStr = token.ToString()!;
                if (!edges.TryGetValue(state, out var value)) {
                    edges[state] = [tokenStr];
                } else {
                    value.Add(tokenStr);
                }
            }
        }
        foreach (var e in edges) {
            yield return (e.Key, string.Join(',', e.Value));
        }
    }

    public override string ToString() {
        var result = string.Empty;
        if (_transitions.Count > 0) {
            var e = _transitions.Select(t => $"{t.Key} => [{string.Join(", ", t.Value.Select(s => s.Id))}]");
            result = string.Join(", ", e);
        }
        if (_εTransitions.Count > 0) {
            var s = string.Join(", ", _εTransitions.Select(s => s.Id));
            s = $"ε => [{s}]";
            if (result.Length > 0)
                s += ", ";
            result = s + result;
        }
        return result;
    }
}
