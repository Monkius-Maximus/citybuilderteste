namespace CityBuilder.Pathfinding;

/// <summary>
/// Binary min-heap priority queue keyed by a float priority. Hand-rolled because
/// netstandard2.1 (the Unity-friendly target) has no <c>System.Collections.Generic.PriorityQueue</c>.
/// Reused across searches (call <see cref="Clear"/>) to avoid per-query allocation.
/// Duplicate entries for a node are allowed; consumers use "lazy deletion" (skip stale pops).
/// </summary>
public sealed class MinHeap
{
    private struct Entry
    {
        public float Priority;
        public int Node;
    }

    private Entry[] _items;
    private int _count;

    public MinHeap(int capacity = 64) => _items = new Entry[Math.Max(1, capacity)];

    public int Count => _count;

    public void Clear() => _count = 0;

    public void Push(int node, float priority)
    {
        if (_count == _items.Length)
        {
            Array.Resize(ref _items, _items.Length * 2);
        }

        int i = _count++;
        _items[i].Priority = priority;
        _items[i].Node = node;

        // Sift up.
        while (i > 0)
        {
            int parent = (i - 1) >> 1;
            if (_items[parent].Priority <= _items[i].Priority)
            {
                break;
            }

            Swap(i, parent);
            i = parent;
        }
    }

    public bool TryPop(out int node, out float priority)
    {
        if (_count == 0)
        {
            node = -1;
            priority = 0f;
            return false;
        }

        node = _items[0].Node;
        priority = _items[0].Priority;

        _count--;
        if (_count > 0)
        {
            _items[0] = _items[_count];
            SiftDown(0);
        }

        return true;
    }

    private void SiftDown(int i)
    {
        while (true)
        {
            int left = 2 * i + 1;
            int right = 2 * i + 2;
            int smallest = i;

            if (left < _count && _items[left].Priority < _items[smallest].Priority)
            {
                smallest = left;
            }

            if (right < _count && _items[right].Priority < _items[smallest].Priority)
            {
                smallest = right;
            }

            if (smallest == i)
            {
                break;
            }

            Swap(i, smallest);
            i = smallest;
        }
    }

    private void Swap(int a, int b)
    {
        Entry tmp = _items[a];
        _items[a] = _items[b];
        _items[b] = tmp;
    }
}
