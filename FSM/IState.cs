namespace RvB.Graphs.FSM;

public interface IState {
    public int Id { get; }

    public bool IsEndState { get; }
}
