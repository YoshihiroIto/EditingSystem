using System;
using System.Collections.Generic;
using System.Linq;
using Jewelry.Collections;
using Xunit;

namespace Jewelry.EditingSystem.Tests.Tests;

public sealed class CustomComparerCollectionHistoryTests
{
    [Fact]
    public void ObservableHashSet_remove_undo_restores_the_actual_stored_instance()
    {
        using var history = new History();
        var original = new SetItem(1, "original");
        var equivalent = new SetItem(1, "equivalent");
        var set = Observe(
            history,
            new ObservableHashSet<SetItem>([original], SetItemComparer.Instance));

        Assert.True(set.Remove(equivalent));
        Assert.Empty(set);

        history.Undo();

        Assert.True(set.TryGetValue(equivalent, out var restored));
        Assert.Same(original, restored);
    }

    [Fact]
    public void ObservableHashSet_remove_undo_restores_the_actual_instance_when_Equals_matches()
    {
        using var history = new History();
        var original = new EquatableSetItem(1, "original");
        var equivalent = new EquatableSetItem(1, "equivalent");
        var set = Observe(history, new ObservableHashSet<EquatableSetItem>([original]));

        Assert.True(set.Remove(equivalent));
        Assert.Empty(set);

        history.Undo();

        Assert.True(set.TryGetValue(equivalent, out var restored));
        Assert.Same(original, restored);
    }

    [Fact]
    public void ObservableDictionary_remove_undo_restores_the_actual_stored_key()
    {
        using var history = new History();
        var dictionary = Observe(
            history,
            new ObservableDictionary<string, int>(StringComparer.OrdinalIgnoreCase)
            {
                ["Original"] = 1
            });

        history.Clear();
        Assert.True(dictionary.Remove("original"));
        Assert.Empty(dictionary);

        history.Undo();

        var restored = Assert.Single(dictionary);
        Assert.Equal("Original", restored.Key);
        Assert.Equal(1, restored.Value);
    }

    [Fact]
    public void ObservableDictionary_replace_undo_redo_preserves_the_actual_stored_key()
    {
        using var history = new History();
        var dictionary = Observe(
            history,
            new ObservableDictionary<string, int>(StringComparer.OrdinalIgnoreCase)
            {
                ["Original"] = 1
            });

        history.Clear();
        dictionary["original"] = 2;

        var changed = Assert.Single(dictionary);
        Assert.Equal("Original", changed.Key);
        Assert.Equal(2, changed.Value);

        history.Undo();

        var undone = Assert.Single(dictionary);
        Assert.Equal("Original", undone.Key);
        Assert.Equal(1, undone.Value);

        history.Redo();

        var redone = Assert.Single(dictionary);
        Assert.Equal("Original", redone.Key);
        Assert.Equal(2, redone.Value);
    }

    private static T Observe<T>(History history, T collection)
    {
        history.RecordPropertyChange<T>(_ => { }, default!, collection);
        history.Clear();
        return collection;
    }

    private sealed class SetItem
    {
        public SetItem(int key, string name)
        {
            Key = key;
            Name = name;
        }

        public int Key { get; }
        public string Name { get; }
    }

    private sealed class SetItemComparer : IEqualityComparer<SetItem>
    {
        public static SetItemComparer Instance { get; } = new();

        public bool Equals(SetItem? x, SetItem? y)
        {
            return ReferenceEquals(x, y) || x is { } && y is { } && x.Key == y.Key;
        }

        public int GetHashCode(SetItem obj)
        {
            return obj.Key;
        }
    }

    private sealed class EquatableSetItem : IEquatable<EquatableSetItem>
    {
        public EquatableSetItem(int key, string name)
        {
            Key = key;
            Name = name;
        }

        public int Key { get; }
        public string Name { get; }

        public bool Equals(EquatableSetItem? other)
        {
            return other is { } && Key == other.Key;
        }

        public override bool Equals(object? obj)
        {
            return obj is EquatableSetItem other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Key;
        }
    }
}
