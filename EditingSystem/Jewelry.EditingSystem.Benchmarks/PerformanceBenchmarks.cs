using System.Collections.ObjectModel;
using BenchmarkDotNet.Attributes;
using Jewelry.Collections;

namespace Jewelry.EditingSystem.Benchmarks;

[MemoryDiagnoser]
public class PropertyHistoryBenchmarks
{
    [GlobalSetup]
    public void GlobalSetup()
    {
        _setter = value => _holder.Value = value;
    }

    [IterationSetup(Target = nameof(RecordWithoutRetention))]
    public void SetupWithoutRetention()
    {
        _history = new History { MaxUndoCount = 0 };
        _holder.Value = 0;
    }

    [IterationSetup(Target = nameof(RecordWithHistory))]
    public void SetupWithHistory()
    {
        _history = new History { MaxUndoCount = 10_000 };
        _holder.Value = 0;
    }

    [Benchmark(OperationsPerInvoke = 10_000)]
    public void RecordWithoutRetention()
    {
        RecordChanges();
    }

    [Benchmark(OperationsPerInvoke = 10_000)]
    public void RecordWithHistory()
    {
        RecordChanges();
    }

    private void RecordChanges()
    {
        for (var i = 0; i < 10_000; ++i)
        {
            var oldValue = _holder.Value;
            var newValue = oldValue == 0 ? 1 : 0;
            _holder.Value = newValue;
            _history.RecordAppliedPropertyChange(
                _holder,
                nameof(ValueHolder.Value),
                _setter,
                oldValue,
                newValue);
        }
    }

    private History _history = null!;
    private readonly ValueHolder _holder = new();
    private Action<int> _setter = null!;

    private sealed class ValueHolder
    {
        public int Value { get; set; }
    }
}

[MemoryDiagnoser]
public class BatchHistoryBenchmarks
{
    [IterationSetup]
    public void Setup()
    {
        _history = new History();
        _history.BeginBatch();
        for (var i = 0; i < 4_096; ++i)
            _history.Push(NoOp, NoOp);
    }

    [Benchmark]
    public void EndBatch4096()
    {
        _history.EndBatch();
    }

    private static void NoOp()
    {
    }

    private History _history = null!;
}

[MemoryDiagnoser]
public class SetHistoryBenchmarks
{
    [IterationSetup]
    public void Setup()
    {
        _history = new History();
        _set = new ObservableHashSet<int>(Enumerable.Range(0, 100_000));
    }

    [Benchmark]
    public void UnionOneInto100K()
    {
        _set.UnionWithEx([100_000], _history);
    }

    private History _history = null!;
    private ObservableHashSet<int> _set = null!;
}

[MemoryDiagnoser]
public class ObservableCollectionMoveBenchmarks
{
    [IterationSetup]
    public void Setup()
    {
        _history = new History();
        _items = new ObservableCollection<int>(Enumerable.Range(0, 1_000));
        _history.RecordPropertyChange<ObservableCollection<int>>(_ => { }, default!, _items);
        _history.Clear();
    }

    [Benchmark]
    public void MoveOneItem()
    {
        _items.Move(0, 999);
    }

    private History _history = null!;
    private ObservableCollection<int> _items = null!;
}
