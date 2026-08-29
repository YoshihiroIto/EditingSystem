using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Jewelry.Collections;
using Xunit;

namespace Jewelry.EditingSystem.Tests.Tests;

public sealed class CollectionAdapterTests
{
    [Fact]
    public void Adapter_is_reused_for_the_same_collection_instance()
    {
        var collection = new ObservableHashSet<int>();

        var first = CollectionAdapter.Create(collection);
        var second = CollectionAdapter.Create(collection);

        Assert.Same(first, second);
    }

    [Fact]
    public void Dynamic_code_adapter_does_not_allocate_per_operation()
    {
        if (RuntimeFeature.IsDynamicCodeSupported is false)
            return;

        var collection = new HashSet<int>();
        var adapter = CollectionAdapter.Create(collection);
        object item = 42;

        for (var i = 0; i < 128; ++i)
        {
            adapter.Add(item);
            adapter.Remove(item);
        }

        var before = GC.GetAllocatedBytesForCurrentThread();

        for (var i = 0; i < 10_000; ++i)
        {
            adapter.Add(item);
            adapter.Remove(item);
        }

        var allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - before;
        Assert.True(
            allocatedBytes < 1024,
            $"The JIT collection adapter allocated {allocatedBytes:N0} bytes for 10,000 Add/Remove pairs.");
    }
}
