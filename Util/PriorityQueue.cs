namespace AdventureLandSharp.Util;

public class PriorityQueue<T> where T : IComparable<T> {
    private readonly List<(double Priority, T Value)> _heap = [];
    private readonly Dictionary<T, int> _indices = [];

    public int Count => _heap.Count;

    public void Enqueue(T value, double priority) {
        if (_indices.TryGetValue(value, out int index)) {
            _heap[index] = (priority, value);
            BubbleUp(index);
            BubbleDown(index);
        } else {
            _indices[value] = _heap.Count;
            _heap.Add((priority, value));
            BubbleUp(_heap.Count - 1);
        }
    }

    public bool TryDequeue(out T value, out double priority) {
        if (_heap.Count == 0) {
            value = default!;
            priority = default;
            return false;
        }

        (priority, value) = _heap[0];
        _indices.Remove(value);

        if (_heap.Count > 1) {
            _heap[0] = _heap[^1];
            _indices[_heap[0].Value] = 0;
            _heap.RemoveAt(_heap.Count - 1);
            BubbleDown(0);
        } else {
            _heap.RemoveAt(_heap.Count - 1);
        }

        return true;
    }

    public bool TryUpdatePriority(T value, double newPriority) {
        if (_indices.TryGetValue(value, out int index)) {
            (double currentPriority, _) = _heap[index];

            if (currentPriority > newPriority) {
                _heap[index] = (newPriority, value);
                BubbleUp(index);
                return true;
            }
        }
        return false;
    }

    private void BubbleUp(int index) {
        while (index > 0) {
            int parentIndex = (index - 1) / 2;
            if (_heap[index].Priority.CompareTo(_heap[parentIndex].Priority) < 0) {
                Swap(index, parentIndex);
                index = parentIndex;
            } else {
                break;
            }
        }
    }

    private void BubbleDown(int index) {
        int lastIndex = _heap.Count - 1;

        while (true) {
            int leftChildIndex = index * 2 + 1;
            int rightChildIndex = index * 2 + 2;
            int smallestIndex = index;

            if (leftChildIndex <= lastIndex && _heap[leftChildIndex].Priority.CompareTo(_heap[smallestIndex].Priority) < 0) {
                smallestIndex = leftChildIndex;
            }

            if (rightChildIndex <= lastIndex && _heap[rightChildIndex].Priority.CompareTo(_heap[smallestIndex].Priority) < 0) {
                smallestIndex = rightChildIndex;
            }

            if (smallestIndex != index) {
                Swap(index, smallestIndex);
                index = smallestIndex;
            } else {
                break;
            }
        }
    }

    private void Swap(int index1, int index2) {
        (_heap[index1], _heap[index2]) = (_heap[index2], _heap[index1]);
        _indices[_heap[index1].Value] = index1;
        _indices[_heap[index2].Value] = index2;
    }
}
