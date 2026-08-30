using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using Jewelry.EditingSystem.Tests.TestModels;
using Xunit;

namespace Jewelry.EditingSystem.Tests.Tests;

public sealed class MultiMoveHistoryTests
{
    [Fact]
    public void Range_move_toward_start_preserves_order_and_only_reports_move_notifications()
    {
        using var history = new History();
        var item0 = new CollectionItem();
        var item1 = new CollectionItem();
        var item2 = new CollectionItem();
        var item3 = new CollectionItem();
        var item4 = new CollectionItem();
        var collection = new RangeObservableCollection<CollectionItem>
        {
            item0,
            item1,
            item2,
            item3,
            item4,
        };

        history.RecordPropertyChange<RangeObservableCollection<CollectionItem>>(
            static _ => { },
            null!,
            collection);
        history.Clear();

        collection.MoveRange(3, 0, 2);
        Assert.Equal(new[] { item3, item4, item0, item1, item2 }, collection);

        history.Undo();
        Assert.Equal(new[] { item0, item1, item2, item3, item4 }, collection);

        history.Redo();
        Assert.Equal(new[] { item3, item4, item0, item1, item2 }, collection);

        Assert.Equal(0, item0.CollectionChangedMoveCount);
        Assert.Equal(0, item1.CollectionChangedMoveCount);
        Assert.Equal(0, item2.CollectionChangedMoveCount);
        Assert.Equal(3, item3.CollectionChangedMoveCount);
        Assert.Equal(3, item4.CollectionChangedMoveCount);

        Assert.Equal(0, item3.CollectionChangedAddCount);
        Assert.Equal(0, item4.CollectionChangedAddCount);
        Assert.Equal(0, item3.CollectionChangedRemoveCount);
        Assert.Equal(0, item4.CollectionChangedRemoveCount);
    }

    private sealed class RangeObservableCollection<T> : ObservableCollection<T>
    {
        public void MoveRange(int oldIndex, int newIndex, int count)
        {
            var items = new List<T>(count);
            for (var i = 0; i < count; ++i)
            {
                items.Add(Items[oldIndex]);
                Items.RemoveAt(oldIndex);
            }

            for (var i = 0; i < count; ++i)
                Items.Insert(newIndex + i, items[i]);

            OnCollectionChanged(new NotifyCollectionChangedEventArgs(
                NotifyCollectionChangedAction.Move,
                (IList)items,
                newIndex,
                oldIndex));
        }
    }
}
