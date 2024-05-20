namespace Hamdlebot.Core.Collections;

public class LimitedSizeHashSet<TType, TKeyType>
{
    private readonly HashSet<TKeyType> _hashSet = new();
    private readonly Queue<TType> _queue = new();
    private readonly int _maxSize;
    private readonly Func<TType, TKeyType> _keySelector;

    public LimitedSizeHashSet(int maxSize, Func<TType, TKeyType> keySelector)
    {
        _maxSize = maxSize;
        _keySelector = keySelector;
    }

    public void Add(TType item)
    {
        if (_hashSet.Count >= _maxSize)
        {
            var oldestItem = _queue.Dequeue();
            _hashSet.Remove(_keySelector(oldestItem));
        }

        if (_hashSet.Add(_keySelector(item)))
        {
            _queue.Enqueue(item);
        }
    }

    public bool Contains(TKeyType item)
    {
        return _hashSet.Contains(item);
    }
}