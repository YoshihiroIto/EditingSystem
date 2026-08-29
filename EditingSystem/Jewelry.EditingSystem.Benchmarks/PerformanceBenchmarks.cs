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
    [Params(16, 256, 4_096, 65_536)]
    public int ActionCount { get; set; }

    [IterationSetup]
    public void Setup()
    {
        _history = new History();
        _history.BeginBatch();
        for (var i = 0; i < ActionCount; ++i)
            _history.Push(NoOp, NoOp);
    }

    [Benchmark]
    public void EndBatch()
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
    [Params(1_000, 100_000, 1_000_000)]
    public int SetSize { get; set; }

    [IterationSetup]
    public void Setup()
    {
        _history = new History();
        _set = new ObservableHashSet<int>(Enumerable.Range(0, SetSize));
    }

    [Benchmark]
    public void UnionOneIntoSet()
    {
        _set.UnionWithEx([SetSize], _history);
    }

    private History _history = null!;
    private ObservableHashSet<int> _set = null!;
}

[MemoryDiagnoser]
public class ObservableCollectionMoveBenchmarks
{
    [IterationSetup(Target = nameof(MoveOneItemWithoutHistory))]
    public void SetupWithoutHistory()
    {
        _history = null;
        _items = new ObservableCollection<int>(Enumerable.Range(0, 1_000));
    }

    [IterationSetup(Target = nameof(MoveOneItemWithHistory))]
    public void SetupWithHistory()
    {
        _history = new History();
        _items = new ObservableCollection<int>(Enumerable.Range(0, 1_000));
        _history.RecordPropertyChange<ObservableCollection<int>>(static _ => { }, default!, _items);
        _history.Clear();
    }

    [Benchmark(Baseline = true)]
    public void MoveOneItemWithoutHistory()
    {
        _items.Move(0, 999);
    }

    [Benchmark]
    public void MoveOneItemWithHistory()
    {
        _items.Move(0, 999);
    }

    private History? _history;
    private ObservableCollection<int> _items = null!;
}
