using System;

namespace ResonanceTools.Utility;

public class Seq<T>
{
    private readonly List<T> _items;
    private readonly bool _fixed;

    public Seq(int capacity, bool fixedLength = false)
    {
        _items = new List<T>(capacity);
        _fixed = fixedLength;

        if (fixedLength)
        {
            // Pre-fill with default(T) to make sure indexer works immediately
            for (int i = 0; i < capacity; i++)
                _items.Add(default!);
        }
    }

    public T this[int index]
    {
        get => _items[index];
        set => _items[index] = value;
    }

    public int Count => _items.Count;
}
