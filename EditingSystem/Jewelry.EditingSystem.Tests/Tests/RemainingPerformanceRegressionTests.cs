using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Jewelry.Collections;
using Xunit;

namespace Jewelry.EditingSystem.Tests.Tests;

public sealed class RemainingPerformanceRegressionTests
{
    [Fact]
    public void RemoveWhereEx_does_not_copy_the_entire_set_twice()
    {
        using var history = new History();
        var set = new CountingSet<int>(Enumerable.Range(0, 1_000));

        set.RemoveWhereEx(static value => value == 500, history);

        Assert.Equal(999, set.Count);
        Assert.InRange(set.CopyToCount, 0, 1);
    }

    [Fact]
    public void ICollection_ClearEx_does_not_copy_the_entire_collection_twice()
    {
        using var history = new History();
        var collection = new CountingCollection<int>(Enumerable.Range(0, 1_000));

        collection.ClearEx(history);

        Assert.Empty(collection);
        Assert.Equal(1, collection.CopyToCount);
    }

    [Fact]
    public void Ending_a_large_batch_does_not_allocate_storage_proportional_to_action_count()
    {
        using var history = new History();
        history.BeginBatch();
        for (var i = 0; i < 4_096; ++i)
            history.Push(NoOp, NoOp);

        var before = GC.GetAllocatedBytesForCurrentThread();
        history.EndBatch();
        var allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.True(
            allocatedBytes < 32_768,
            $"Ending a 4,096-action batch allocated {allocatedBytes:N0} bytes.");
        Assert.Equal(1, history.UndoCount);
    }

    [Fact]
    public void ObservableHashSet_small_union_does_not_allocate_a_full_set_snapshot()
    {
        using var warmupHistory = new History();
        var warmup = new ObservableHashSet<int>([1, 2, 3]);
        warmup.UnionWithEx([4], warmupHistory);

        using var history = new History();
        var set = new ObservableHashSet<int>(Enumerable.Range(0, 100_000));

        var before = GC.GetAllocatedBytesForCurrentThread();
        set.UnionWithEx([100_000], history);
        var allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.True(
            allocatedBytes < 100_000,
            $"Adding one item to a 100,000-item set allocated {allocatedBytes:N0} bytes.");
        Assert.Equal(100_001, set.Count);
        Assert.Equal(1, history.UndoCount);
    }

    [Fact]
    public void Recording_property_changes_does_not_allocate_an_internal_closure_per_change()
    {
        using var history = new History { MaxUndoCount = 0 };
        var target = new ValueHolder();
        Action<int> setter = value => target.Value = value;

        for (var i = 0; i < 128; ++i)
        {
            var oldValue = target.Value;
            var newValue = oldValue == 0 ? 1 : 0;
            target.Value = newValue;
            history.RecordAppliedPropertyChange(target, nameof(ValueHolder.Value), setter, oldValue, newValue);
        }

        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 10_000; ++i)
        {
            var oldValue = target.Value;
            var newValue = oldValue == 0 ? 1 : 0;
            target.Value = newValue;
            history.RecordAppliedPropertyChange(target, nameof(ValueHolder.Value), setter, oldValue, newValue);
        }
        var allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.True(
            allocatedBytes < 800_000,
            $"Recording 10,000 property changes allocated {allocatedBytes:N0} bytes.");
    }

    private static void NoOp()
    {
    }

    private sealed class ValueHolder
    {
        public int Value { get; set; }
    }

    private sealed class CountingCollection<T> : ICollection<T>
    {
        public CountingCollection(IEnumerable<T> items)
        {
            _items = new HashSet<T>(items);
        }

        public int Count => _items.Count;
        public bool IsReadOnly => false;
        public int CopyToCount { get; private set; }

        public void Add(T item) => _items.Add(item);
        public void Clear() => _items.Clear();
        public bool Contains(T item) => _items.Contains(item);
        public bool Remove(T item) => _items.Remove(item);
        public IEnumerator<T> GetEnumerator() => _items.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public void CopyTo(T[] array, int arrayIndex)
        {
            ++CopyToCount;
            _items.CopyTo(array, arrayIndex);
        }

        private readonly HashSet<T> _items;
    }

    private sealed class CountingSet<T> : ISet<T>
    {
        public CountingSet(IEnumerable<T> items)
        {
            _items = new HashSet<T>(items);
        }

        public int Count => _items.Count;
        public bool IsReadOnly => false;
        public int CopyToCount { get; private set; }

        public bool Add(T item) => _items.Add(item);
        void ICollection<T>.Add(T item) => _items.Add(item);
        public void ExceptWith(IEnumerable<T> other) => _items.ExceptWith(other);
        public void IntersectWith(IEnumerable<T> other) => _items.IntersectWith(other);
        public bool IsProperSubsetOf(IEnumerable<T> other) => _items.IsProperSubsetOf(other);
        public bool IsProperSupersetOf(IEnumerable<T> other) => _items.IsProperSupersetOf(other);
        public bool IsSubsetOf(IEnumerable<T> other) => _items.IsSubsetOf(other);
        public bool IsSupersetOf(IEnumerable<T> other) => _items.IsSupersetOf(other);
        public bool Overlaps(IEnumerable<T> other) => _items.Overlaps(other);
        public bool SetEquals(IEnumerable<T> other) => _items.SetEquals(other);
        public void SymmetricExceptWith(IEnumerable<T> other) => _items.SymmetricExceptWith(other);
        public void UnionWith(IEnumerable<T> other) => _items.UnionWith(other);
        public void Clear() => _items.Clear();
        public bool Contains(T item) => _items.Contains(item);
        public bool Remove(T item) => _items.Remove(item);
        public IEnumerator<T> GetEnumerator() => _items.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public void CopyTo(T[] array, int arrayIndex)
        {
            ++CopyToCount;
            _items.CopyTo(array, arrayIndex);
        }

        private readonly HashSet<T> _items;
    }
}
