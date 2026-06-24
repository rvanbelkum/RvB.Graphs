namespace RvB.Graphs.FSM;

public class NFA<T> where T : notnull {
    private int _nextId = 0;

    internal NFAState<T>? StartState { get; private set; }

    public NFA() { }

    internal NFA(NFAState<T> startState) {
        StartState = startState;
    }

    public void Optimize() {
        StartState?.Optimize();
    }

    public NFAState<T> CreateState(bool isEndState) => new(_nextId++, isEndState);

    public NFA<T> Clone() {
        var nfa = new NFA<T>();
        if (StartState != null) {
            nfa.StartState = StartState.Clone(nfa);
        }
        return nfa;
    }

    public DFA<T> ConvertToDFA() {
        var dfa = new DFA<T>();
        if (StartState is not null) {
            dfa.StartState = NFAState<T>.CreateDFAState(dfa, StartState);
        }
        return dfa;
    }
}
