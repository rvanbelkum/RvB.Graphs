namespace RvB.Graphs.FSM;

public sealed class DFA<T> where T : notnull {
    private int _nextId = 0;

    internal DFAState<T>? StartState { get; set; }

    public DFA() { }

    internal DFA(DFAState<T> startState) {
        StartState = startState;
    }

    public void Optimize() {
        StartState?.Optimize();
    }

    public DFAState<T> CreateState(bool isEndState) => new(_nextId++, isEndState);
}
