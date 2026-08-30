using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Runtime.CompilerServices;
using Xunit;

namespace Jewelry.EditingSystem.Tests.Tests;

public sealed class CollectionChangedWeakEventManagerTests
{
    [Fact]
    public void Registration_is_reference_counted()
    {
        using var manager = new CollectionChangedWeakEventManager();
        var source = new ObservableCollection<int> { 1 };
        var notificationCount = 0;
        NotifyCollectionChangedEventHandler handler = (_, _) => ++notificationCount;

        manager.AddWeakEventListener(source, handler);
        manager.AddWeakEventListener(source, handler);

        manager.RemoveWeakEventListener(source);
        source.Add(2);
        Assert.Equal(1, notificationCount);

        manager.RemoveWeakEventListener(source);
        source.Add(3);
        Assert.Equal(1, notificationCount);
    }

    [Fact]
    public void Removed_registration_is_not_available_for_snapshot_lookup()
    {
        using var manager = new CollectionChangedWeakEventManager();
        var source = new ObservableCollection<int> { 1, 2, 3 };
        NotifyCollectionChangedEventHandler handler = static (_, _) => { };

        manager.AddWeakEventListener(source, handler);
        Assert.Equal(new object?[] { 1, 2, 3 }, manager.GetSnapshot(source));

        manager.RemoveWeakEventListener(source);
        Assert.Throws<InvalidOperationException>(() => manager.GetSnapshot(source));
    }

    [Fact]
    public void Registration_does_not_keep_source_alive()
    {
        using var manager = new CollectionChangedWeakEventManager();
        var sourceReference = RegisterTemporarySource(manager);

        CollectGarbage();

        Assert.False(sourceReference.IsAlive);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference RegisterTemporarySource(CollectionChangedWeakEventManager manager)
    {
        var source = new ObservableCollection<int> { 1 };
        NotifyCollectionChangedEventHandler handler = static (_, _) => { };
        manager.AddWeakEventListener(source, handler);
        return new WeakReference(source);
    }

    private static void CollectGarbage()
    {
        for (var i = 0; i < 3; ++i)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }
    }
}
