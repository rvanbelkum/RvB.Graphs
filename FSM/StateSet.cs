using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace RvB.Graphs.FSM;

internal class HashSetEqualityComparer<T> : IEqualityComparer<HashSet<T>> where T : IState {
    public static HashSetEqualityComparer<T> Default = new();

    private HashSetEqualityComparer() { }

    public bool Equals(HashSet<T>? x, HashSet<T>? y) {
        if (x is null || y is null)
            return x is null && y is null;
        return x.Equals(y);
    }

    public int GetHashCode([DisallowNull] HashSet<T> hashSet) {
        var hashCode = 0;
        foreach (var state in hashSet) {
            if (state != null) {
                hashCode ^= state.GetHashCode(); // same hashcode as default comparer
            }
        }
        return hashCode;
    }
}

internal static class HashSetExtensions {
    public static void AddRange<T>(this HashSet<T> hashSet, IEnumerable<T> items) {
        foreach (var item in items)
            hashSet.Add(item);
    }
}

/*
internal sealed class StateSet<T> : IEnumerable<T> where T : IState {
    private readonly HashSet<T> _states;
    private int? _hashcode;

    public StateSet() {
        _states = [];
        _hashcode = null;
    }

    public StateSet(IEnumerable<T> states) {
        _states = [.. states];
        _hashcode = null;
    }

    public int Count => _states.Count;

    /// <summary>Adds the specified element to the <see cref="StateSet{T}"/>.</summary>
    /// <param name="item">The element to add to the set.</param>
    /// <returns>true if the element is added to the <see cref="StateSet{T}"/> object; false if the element is already present.</returns>
    public bool Add(T state) {
        _hashcode = null;
        return _states.Add(state);
    }

    public void AddRange(IEnumerable<T> states) {
        _hashcode = null;
        foreach (var state in states) {
            _states.Add(state);
        }
    }

    public void Clear() {
        _states.Clear();
        _hashcode = null;
    }

    public override bool Equals(object? obj) {
        return obj is StateSet<T> states && Equals(states);
    }

    public override int GetHashCode() {
        if (_hashcode is null) {
            var hashCode = 0;
            foreach (var state in _states) {
                if (state != null) {
                    hashCode ^= state.GetHashCode(); // same hashcode as default comparer
                }
            }
            _hashcode = hashCode;
        }
        return _hashcode.Value;
    }

    public bool Equals(StateSet<T> states) => _states.SetEquals(states);

    public IEnumerator<T> GetEnumerator() => ((IEnumerable<T>)_states).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

internal class StateSetEqualityComparer<T> : IEqualityComparer<StateSet<T>> where T : IState {
    public static StateSetEqualityComparer<T> Default = new();

    private StateSetEqualityComparer() { }

    public bool Equals(StateSet<T>? x, StateSet<T>? y) {
        if (x is null || y is null)
            return x is null && y is null;
        return x.Equals(y);
    }

    public int GetHashCode([DisallowNull] StateSet<T> obj) {
        return obj.GetHashCode();
    }
}
*/
