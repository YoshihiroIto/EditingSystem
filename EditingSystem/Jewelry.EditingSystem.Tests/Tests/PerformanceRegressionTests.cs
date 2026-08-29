using System;
using System.Collections.ObjectModel;
using Xunit;

namespace Jewelry.EditingSystem.Tests.Tests;

public sealed class PerformanceRegressionTests
{
    [Fact]
    public void Observed_ClearEx_allocations_scale_linearly()
    {
        _ = MeasureClearAllocatedBytes(16);

        var small = MeasureClearAllocatedBytes(128);
        var large = MeasureClearAllocatedBytes(1024);

        Assert.True(
            large < small * 20,
            $"Expected near-linear allocation growth, but 128 items allocated {small:N0} bytes " +
            $"and 1,024 items allocated {large:N0} bytes.");
    }

    private static long MeasureClearAllocatedBytes(int count)
    {
        using var history = new History();
        var collection = new ObservableCollection<int>();
        for (var i = 0; i < count; ++i)
            collection.Add(i);

        history.RecordPropertyChange<ObservableCollection<int>>(_ => { }, null!, collection);
        history.Clear();

        var before = GC.GetAllocatedBytesForCurrentThread();
        collection.ClearEx(history);
        return GC.GetAllocatedBytesForCurrentThread() - before;
    }
}
