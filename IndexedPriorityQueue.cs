using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace RvB.Graphs;

public sealed class IndexedPriorityQueue<TElement, TPriority> where TElement : IComparable<TElement> {
    #region public
    public int Count {
        get { return _count; }
    }

    public TElement this[int index] {
        get {
            Debug.Assert(index < _objects.Length && index >= 0, $"IndexedPriorityQueue.[]: Index '{index}' out of range");
            return _objects[index];
        }

        set {
            Debug.Assert(index < _objects.Length && index >= 0, $"IndexedPriorityQueue.[]: Index '{index}' out of range");
            Set(index, value);
        }
    }

    public IndexedPriorityQueue(int maxSize) {
        Resize(maxSize);
    }

    /// <summary>
    /// Inserts a new value with the given index
    /// </summary>
    /// <param name="index">index to insert at</param>
    /// <param name="value">value to insert</param>
    public void Insert(int index, TElement value) {
        Debug.Assert(index < _objects.Length && index >= 0, $"IndexedPriorityQueue.Insert: Index '{index}' out of range");

        ++_count;
        // add object
        _objects[index] = value;

        // add to heap
        _heapInverse[index] = _count;
        _heap[_count] = index;

        // update heap
        SortHeapUpward(_count);
    }

    /// <summary>
    /// Gets the top element of the queue
    /// </summary>
    /// <returns>The top element</returns>
    public TElement Top() {
        // top of heap [first element is 1, not 0]
        return _objects[_heap[1]];
    }

    /// <summary>
    /// Removes the top element from the queue
    /// </summary>
    /// <returns>The removed element</returns>
    public TElement? Pop() {
        Debug.Assert(_count > 0, "IndexedPriorityQueue.Pop: Queue is empty");

        if (_count == 0) {
            return default;
        }

        // swap front to back for removal
        Swap(1, _count--);

        // re-sort heap
        SortHeapDownward(1);

        // return popped object
        return _objects[_heap[_count + 1]];
    }

    /// <summary>
    /// Updates the value at the given index. Note that this function is not
    /// as efficient as the DecreaseIndex/IncreaseIndex methods, but is
    /// best when the value at the index is not known
    /// </summary>
    /// <param name="index">index of the value to set</param>
    /// <param name="obj">new value</param>
    public void Set(int index, TElement obj) {
        if (obj.CompareTo(_objects[index]) <= 0) {
            DecreaseIndex(index, obj);
        } else {
            IncreaseIndex(index, obj);
        }
    }

    /// <summary>
    /// Decrease the value at the current index
    /// </summary>
    /// <param name="index">index to decrease value of</param>
    /// <param name="obj">new value</param>
    public void DecreaseIndex(int index, TElement obj) {
        Debug.Assert(index < _objects.Length && index >= 0, $"IndexedPriorityQueue.DecreaseIndex: Index '{index}' out of range");
        Debug.Assert(obj.CompareTo(_objects[index]) <= 0, $"IndexedPriorityQueue.DecreaseIndex: object '{obj}' isn't less than current value '{_objects[index]}'");

        _objects[index] = obj;
        SortUpward(index);
    }

    /// <summary>
    /// Increase the value at the current index
    /// </summary>
    /// <param name="index">index to increase value of</param>
    /// <param name="obj">new value</param>
    public void IncreaseIndex(int index, TElement obj) {
        Debug.Assert(index < _objects.Length && index >= 0, $"IndexedPriorityQueue.DecreaseIndex: Index '{index}' out of range");
        Debug.Assert(obj.CompareTo(_objects[index]) >= 0, $"IndexedPriorityQueue.DecreaseIndex: object '{obj}' isn't greater than current value '{_objects[index]}'");

        _objects[index] = obj;
        SortDownward(index);
    }

    public void Clear() {
        _count = 0;
    }

    /// <summary>
    /// Set the maximum capacity of the queue
    /// </summary>
    /// <param name="maxSize">new maximum capacity</param>
    [MemberNotNull(nameof(_objects), nameof(_heap), nameof(_heapInverse))]
    public void Resize(int maxSize) {
        Debug.Assert(maxSize >= 0, $"IndexedPriorityQueue.Resize: Invalid size '{maxSize}'");

        _objects = new TElement[maxSize];
        _heap = new int[maxSize + 1];
        _heapInverse = new int[maxSize];
        _count = 0;
    }
    #endregion // public

    #region private
    private TElement[] _objects;
    private int[] _heap;
    private int[] _heapInverse;
    private int _count;

    private void SortUpward(int index) {
        SortHeapUpward(_heapInverse[index]);
    }

    private void SortDownward(int index) {
        SortHeapDownward(_heapInverse[index]);
    }

    private void SortHeapUpward(int heapIndex) {
        // move toward top if better than parent
        while (heapIndex > 1 && _objects[_heap[heapIndex]].CompareTo(_objects[_heap[ParentIndex(heapIndex)]]) < 0) {
            // swap this node with its parent
            Swap(heapIndex, ParentIndex(heapIndex));

            // reset iterator to be at parents old position
            // (child's new position)
            heapIndex = ParentIndex(heapIndex);
        }
    }

    private void SortHeapDownward(int heapIndex) {
        // move node downward if less than children
        while (FirstChildIndex(heapIndex) <= _count) {
            int child = FirstChildIndex(heapIndex);

            // find smallest of two children (if 2 exist)
            if (child < _count && _objects[_heap[child + 1]].CompareTo(_objects[_heap[child]]) < 0) {
                ++child;
            }

            // swap with child if less
            if (_objects[_heap[child]].CompareTo(_objects[_heap[heapIndex]]) < 0) {
                Swap(child, heapIndex);
                heapIndex = child;
            }
            // no swap necessary
            else {
                break;
            }
        }
    }

    private void Swap(int i, int j) {
        // swap elements in heap
        (_heap[i], _heap[j]) = (_heap[j], _heap[i]);
        // reset inverses
        _heapInverse[_heap[i]] = i;
        _heapInverse[_heap[j]] = j;
    }

    private static int ParentIndex(int heapIndex) {
        return (heapIndex / 2);
    }

    private static int FirstChildIndex(int heapIndex) {
        return (heapIndex * 2);
    }
    #endregion // private
}
