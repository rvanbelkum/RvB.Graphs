using System.Diagnostics.CodeAnalysis;

namespace RvB.Graphs;

public class UPQ<TElement, TPriority> where TElement : notnull {
    enum AddOrUpdate {
        Added,
        Unchanged,
        Increased,
        Decreased
    }
    class Elements {
        private readonly Dictionary<TElement, int> _elements = [];
        private readonly Dictionary<int, (TElement Element, TPriority Priority)> _values = [];
        private int _nextKeyIndex = 0;
        private readonly Queue<int> _freeKeyIndices = [];

        public Elements() { }
        public bool ContainsElement(TElement element) => _elements.ContainsKey(element);
        public bool TryGetElement(TElement element, out int keyIndex) => _elements.TryGetValue(element, out keyIndex);
        public void Add(TElement element, TPriority priority, int keyIndex) {
            if (_elements.ContainsKey(element) || _values.ContainsKey(keyIndex))
                throw new ArgumentException("Element or keyIndex already in the queue");
            _elements.Add(element, keyIndex);
            _values.Add(keyIndex, (element, priority));
        }
        public int Add(TElement element, TPriority priority) {
            if (_elements.ContainsKey(element))
                throw new ArgumentException("Element already in the queue");
            if (!_freeKeyIndices.TryDequeue(out var keyIndex))
                keyIndex = _nextKeyIndex++;
            _elements.Add(element, keyIndex);
            _values.Add(keyIndex, (element, priority));
            return keyIndex;
        }
        public AddOrUpdate AddOrDecrease(TElement element, TPriority priority, out int keyIndex) {
            AddOrUpdate result;
            if (_elements.TryGetValue(element, out keyIndex)) {
                var oldPriority = _values[keyIndex].Priority;
                if (!Less(oldPriority, priority)) {
                    result = AddOrUpdate.Decreased;
                    _values[keyIndex] = (element, priority);
                } else {
                    result = AddOrUpdate.Unchanged;
                }
            } else {
                Add(element, priority);
                result = AddOrUpdate.Added;
            }
            return result;
        }
        public AddOrUpdate AddOrIncrease(TElement element, TPriority priority, out int keyIndex) {
            AddOrUpdate result;
            if (_elements.TryGetValue(element, out keyIndex)) {
                var oldPriority = _values[keyIndex].Priority;
                if (Less(oldPriority, priority)) {
                    result = AddOrUpdate.Increased;
                    _values[keyIndex] = (element, priority);
                } else {
                    result = AddOrUpdate.Unchanged;
                }
            } else {
                Add(element, priority);
                result = AddOrUpdate.Added;
            }
            return result;
        }
        public int this[TElement element] {
            get => _elements[element];
            set => _elements[element] = value;
        }
        public TElement this[int keyIndex] {
            get => _values[keyIndex].Element;
            set => _values[keyIndex] = (value, _values[keyIndex].Priority);
        }
        public (TElement Element, TPriority Priority) GetElementPriority(int keyIndex) => _values[keyIndex];
        public TPriority GetPriority(int keyIndex) => _values[keyIndex].Priority;
        public void SetPriority(int keyIndex, TPriority priority) {
            _values[keyIndex] = (_values[keyIndex].Element, priority);
        }
        public bool Remove(TElement element) {
            if (TryGetElement(element, out var keyIndex)) {
                _ = _elements.Remove(element);
                _ = _values.Remove(keyIndex);
                _freeKeyIndices.Enqueue(keyIndex);
                return true;
            }
            return false;
        }
        public bool Less(int keyIndex, TPriority priority) {
            return Less(GetPriority(keyIndex), priority);
        }
        public bool Less(TPriority priority, int keyIndex) {
            return Less(priority, GetPriority(keyIndex));
        }
        public bool Less(int keyIndex1, int keyIndex2) {
            return Less(GetPriority(keyIndex1), GetPriority(keyIndex2));
        }
        public static bool Less(TPriority priority1, TPriority priority2) {
            return Comparer<TPriority>.Default.Compare(priority1, priority2) < 0;
        }
    }

    // Current number of elements in the heap.
    private int _elementCount;

    // Maximum number of elements in the heap.
    private int _heapSize;

    // The degree of every node in the heap.
    private readonly int _arity = 4;
    private readonly int _log2Arity = 2;

    // The Position Map (pm) maps Key Indexes (ki) to where the position of that
    // key is represented in the priority queue in the domain [0, sz).
    public int[] pm;

    // The Inverse Map (im) stores the indexes of the keys in the range
    // [0, sz) which make up the priority queue. It should be noted that
    // 'im' and 'pm' are inverses of each other, so: pm[im[i]] = im[pm[i]] = i
    public int[] im;

    // The values associated with the keys. It is very important  to note
    // that this array is indexed by the key indexes (aka 'ki').
    //public TElement[] values;

    // TElement => KeyIndex
    //private Dictionary<TElement, int> _elements;
    // KeyIndex => (TElement, TPriority)
    //private Dictionary<int, (TElement Element, TPriority Priority)> _values;
    readonly Elements _elements = new();

    public UPQ() {
        _heapSize = 0;
        pm = [];
        im = [];
    }

    // Initializes a D-ary heap with a maximum capacity of maxSize.
    public UPQ(int degree, int capacity) {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(capacity);

        _arity = Math.Max(2, degree);
        _log2Arity = (int)Math.Log2(_arity);
        _heapSize = Math.Max(_arity + 1, capacity);

        im = new int[_heapSize];
        pm = new int[_heapSize];
        for (int i = 0; i < _heapSize; i++) {
            pm[i] = im[i] = -1;
        }
    }

    public int Count => _elementCount;

    public bool IsEmpty => _elementCount == 0;

    public bool Contains(TElement element) => _elements.ContainsElement(element);

    public void Enqueue(TElement element, TPriority priority) {
        ArgumentNullException.ThrowIfNull(element);
        var keyIndex = _elements.Add(element, priority);
        if (keyIndex >= _heapSize) {
            IncreaseCapacity();
        }
        pm[keyIndex] = _elementCount;
        im[_elementCount] = keyIndex;
        MoveUp(_elementCount++);
    }

    public void EnqueueOrDecrease(TElement element, TPriority priority) {
        ArgumentNullException.ThrowIfNull(element);
        var result = _elements.AddOrDecrease(element, priority, out var keyIndex);
        if (result == AddOrUpdate.Added) {
            if (keyIndex >= _heapSize) {
                IncreaseCapacity();
            }
            pm[keyIndex] = _elementCount;
            im[_elementCount] = keyIndex;
            MoveUp(_elementCount++);
        } else if (result == AddOrUpdate.Decreased) {
            MoveUp(pm[keyIndex]);
        }
    }

    public void EnqueueOrIncrease(TElement element, TPriority priority) {
        ArgumentNullException.ThrowIfNull(element);
        var result = _elements.AddOrIncrease(element, priority, out var keyIndex);
        if (result == AddOrUpdate.Added) {
            if (keyIndex >= _heapSize) {
                IncreaseCapacity();
            }
            pm[keyIndex] = _elementCount;
            im[_elementCount] = keyIndex;
            MoveUp(_elementCount++);
        } else if (result == AddOrUpdate.Increased) {
            MoveDown(pm[keyIndex]);
        }
    }

    public TElement Dequeue() {
        if (IsEmpty)
            throw new Exception("Priority queue is empty");
        var minValue = _elements[im[0]];
        Delete(im[0]);
        return minValue;
    }

    public TElement Dequeue(out TPriority priority) {
        if (IsEmpty)
            throw new Exception("Priority queue is empty");
        (var element, priority) = _elements.GetElementPriority(im[0]);
        Delete(im[0]);
        return element;
    }

    public bool TryDequeue([MaybeNullWhen(false)] out TElement element) {
        return TryDequeue(out element, out _);
    }

    public bool TryDequeue([MaybeNullWhen(false)] out TElement element, [MaybeNullWhen(false)] out TPriority priority) {
        if (_elementCount == 0) {
            element = default;
            priority = default;
            return false;
        }
        (element, priority) = _elements.GetElementPriority(im[0]);
        Delete(im[0]);
        return true;
    }

    public bool Delete(TElement element) {
        if (_elements.TryGetElement(element, out var keyIndex)) {
            Delete(keyIndex);
            return true;
        }
        return false;
    }

    public bool Update(TElement element, TPriority priority) {
        if (_elements.TryGetElement(element, out var keyIndex)) {
            _elements.SetPriority(keyIndex, priority);
            var i = pm[keyIndex];
            MoveDown(i);
            MoveUp(i);
            return true;
        }
        return false;
    }

    // Strictly decreases the value associated with 'ki' to 'value'
    public bool Decrease(TElement element, TPriority priority) {
        if (_elements.TryGetElement(element, out var keyIndex)) {
            if (_elements.Less(priority, keyIndex)) {
                _elements.SetPriority(keyIndex, priority);
                MoveUp(pm[keyIndex]);
                return true;
            }
            return false;
        }
        throw new ArgumentException("Element not in the queue", nameof(element));
    }

    // Strictly increases the value associated with 'ki' to 'value'
    public bool Increase(TElement element, TPriority priority) {
        if (_elements.TryGetElement(element, out var keyIndex)) {
            if (_elements.Less(keyIndex, priority)) {
                _elements.SetPriority(keyIndex, priority);
                MoveDown(pm[keyIndex]);
                return true;
            }
            return false;
        }
        throw new ArgumentException("Element not in the queue", nameof(element));
    }

    public override string ToString() {
        return string.Join(',', Enumerable.Range(0, _elementCount).Select(i => im[i]));
    }

    /* Helper functions */
    private void IncreaseCapacity(int minCapacity = 0) {
        var newSize = Math.Max(Math.Max(2, _heapSize) << 1, minCapacity);
        Array.Resize(ref pm, newSize);
        Array.Fill(pm, -1, _heapSize, newSize - _heapSize);
        Array.Resize(ref im, newSize);
        Array.Fill(im, -1, _heapSize, newSize - _heapSize);
        _heapSize = newSize;
    }

    private TElement Delete(int ki) {
        int i = pm[ki];
        Swap(i, --_elementCount);
        MoveDown(i);
        MoveUp(i);
        TElement value = _elements[ki];
        _elements.Remove(value);
        pm[ki] = -1;
        im[_elementCount] = -1;
        return value;
    }

    private void MoveDown(int i) {
        for (int j = MinChild(i); j != -1;) {
            Swap(i, j);
            i = j;
            j = MinChild(i);
        }
    }

    private void MoveUp(int i) {
        var parentIndex = GetParentIndex(i);
        while (parentIndex >= 0 && _elements.Less(im[i], im[parentIndex])) {
            Swap(i, parentIndex);
            i = parentIndex;
            parentIndex = GetParentIndex(i);
        }
    }

    // From the parent node at index i find the minimum child below it
    private int MinChild(int i) {
        var index = -1;
        var from = GetFirstChildIndex(i);
        var to = Math.Min(_elementCount, from + _arity);

        for (int j = from; j < to; j++)
            if (_elements.Less(im[j], im[i]))
                index = i = j;
        return index;
    }

    private void Swap(int i, int j) {
        pm[im[j]] = i;
        pm[im[i]] = j;
        (im[i], im[j]) = (im[j], im[i]);
    }

    /// <summary>
    /// Gets the index of an element's parent.
    /// </summary>
    private int GetParentIndex(int index) => (index - 1) >> _log2Arity;

    /// <summary>
    /// Gets the index of the first child of an element.
    /// </summary>
    private int GetFirstChildIndex(int index) => (index << _log2Arity) + 1;

    /* Test functions */

    // Recursively checks if this heap is a min heap. This method is used
    // for testing purposes to validate the heap invariant.
    public bool IsMinHeap() {
        return IsMinHeap(0);
    }

    private bool IsMinHeap(int i) {
        var from = GetFirstChildIndex(i);
        var to = Math.Min(_elementCount, from + _arity);
        for (int j = from; j < to; j++) {
            if (!_elements.Less(im[i], im[j]))
                return false;
            if (!IsMinHeap(j))
                return false;
        }
        return true;
    }
}
