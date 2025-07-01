using System.Collections.Generic;

public sealed class CollectionDiffChecker<T>
{
    private HashSet<T> _currentSet;
    private HashSet<T> _lastSet;

    private List<T> _added;
    private List<T> _removed;

    public IReadOnlyList<T> Added => _added;
    public IReadOnlyList<T> Removed => _removed;

    #region Ctor
    public CollectionDiffChecker()
    {
        _currentSet = new HashSet<T>();
        _lastSet = new HashSet<T>();

        _added = new List<T>();
        _removed = new List<T>();
    }

    public CollectionDiffChecker(int capacity)
    {
        _currentSet = new HashSet<T>(capacity);
        _lastSet = new HashSet<T>(capacity);

        _added = new List<T>(capacity);
        _removed = new List<T>(capacity);
    }

    public CollectionDiffChecker(IEqualityComparer<T> comparer)
    {
        _currentSet = new HashSet<T>(comparer);
        _lastSet = new HashSet<T>(comparer);

        _added = new List<T>();
        _removed = new List<T>();
    }

    public CollectionDiffChecker(int capacity, IEqualityComparer<T> comparer)
    {
        _currentSet = new HashSet<T>(capacity, comparer);
        _lastSet = new HashSet<T>(capacity, comparer);

        _added = new List<T>(capacity);
        _removed = new List<T>(capacity);
    }
    #endregion

    public void Execute(params T[] args)
    {
        Execute(args as IReadOnlyList<T>);
    }

    public void Execute(IReadOnlyList<T> current)
    {
        _currentSet.Clear();

        for (int i = 0, length = current.Count; i < length; i++)
        {
            var item = current[i];

            _currentSet.Add(item);
        }

        _removed.Clear();

        foreach (var item in _lastSet)
        {
            if (!_currentSet.Contains(item))
            {
                _removed.Add(item);
            }
        }

        _added.Clear();

        foreach (var item in _currentSet)
        {
            if (!_lastSet.Contains(item))
            {
                _added.Add(item);
            }
        }

        var temp = _lastSet;
        _lastSet = _currentSet;
        _currentSet = temp;
    }
}
