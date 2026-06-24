using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace RvB.Graphs.FSM;

internal sealed class DFATransitionMap<T> : IEnumerable<(T Token, DFAState<T> State)> where T : notnull {
    // Transitions: Token => States
    private readonly Dictionary<T, DFAState<T>> _transitions = [];

    public IEnumerator<(T Token, DFAState<T> State)> GetEnumerator() {
        foreach (var (token, state) in _transitions)
            yield return (token, state);
    }

    public DFAState<T> this[T token] {
        get => _transitions[token];
        set => _transitions[token] = value;
    }

    public void Set(T token, DFAState<T> state) {
        _transitions[token] = state;
    }

    public bool TryGetValue(T token, [MaybeNullWhen(false)] out DFAState<T> state) {
        return _transitions.TryGetValue(token, out state);
    }

    public Dictionary<DFAState<T>, List<T>> GetNextStatesByState() {
        var mapByState = new Dictionary<DFAState<T>, List<T>>();
        foreach (var (token, state) in _transitions) {
            if (mapByState.TryGetValue(state, out var tokenList)) {
                tokenList.Add(token);
            } else {
                mapByState[state] = [token];
            }
        }
        foreach (var state in mapByState) {
            state.Value.Sort();
        }
        return mapByState;
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public override string ToString() {
        var result = string.Empty;
        if (_transitions.Count > 0) {
            var e = _transitions.Select(t => $"{t.Key} => [{t.Value.Id}]");
            result = string.Join(", ", e);
        }
        return result;
    }
}
