using System.Runtime.CompilerServices;

namespace AdventureLandSharp.Core;

public class FastPriorityQueue<T>() where T : struct {
    public IReadOnlyList<(float Priority, T Item)> Items => _elements;

    public void Clear() {
        _size = 0;
        _elements.Clear();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public void Enqueue(T item, float priority) {
        if (_size == _elements.Count) {
            _elements.Add((priority, item));
        } else {
            _elements[_size] = (priority, item);
        }

        SiftUp(_size++);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public bool TryDequeue(out T item, out float priority) {
        if (_size == 0) {
            item = default;
            priority = default;
            return false;
        }

        (priority, item) = _elements[0];
        _elements[0] = _elements[--_size];
        SiftDown(0);

        return true;
    }

    private readonly List<(float Priority, T Item)> _elements = [];
    private int _size = 0;

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private void SiftUp(int index) {
        while (index > 0) {
            int parent = (index - 1) / 2;

            if (_elements[index].Priority >= _elements[parent].Priority) {
                break;
            }

            (_elements[index], _elements[parent]) = (_elements[parent], _elements[index]);
            index = parent;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private void SiftDown(int index) {
        while (true) {
            int left = 2 * index + 1;
            int right = 2 * index + 2; 
            int smallest = index;

            if (left < _size && _elements[left].Priority < _elements[smallest].Priority) {
                smallest = left;
            }

            if (right < _size && _elements[right].Priority < _elements[smallest].Priority) {
                smallest = right;
            }

            if (smallest == index) {
                break;
            }

            (_elements[index], _elements[smallest]) = (_elements[smallest], _elements[index]);
            index = smallest;
        }
    }
}
