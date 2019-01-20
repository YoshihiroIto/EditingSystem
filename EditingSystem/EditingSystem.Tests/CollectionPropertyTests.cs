using System;
using System.Collections.ObjectModel;
using System.Linq;
using Xunit;

namespace EditingSystem.Tests
{
    public class CollectionPropertyTests
    {
        [Fact]
        public void Add()
        {
            var history = new History();
            var model = new TestModel(history);

            model.IntCollection = new ObservableCollection<int>();

            model.IntCollection.Add(1);
            Assert.Equal(1, model.IntCollection.Count);

            model.IntCollection.Add(2);
            Assert.Equal(2, model.IntCollection.Count);

            model.IntCollection.Add(3);
            Assert.Equal(3, model.IntCollection.Count);

            history.Undo();
            Assert.Equal(2, model.IntCollection.Count);
            Assert.True(model.IntCollection.SequenceEqual(new[] {1, 2}));

            history.Undo();
            Assert.Equal(1, model.IntCollection.Count);
            Assert.True(model.IntCollection.SequenceEqual(new[] {1}));

            history.Redo();
            Assert.Equal(2, model.IntCollection.Count);
            Assert.True(model.IntCollection.SequenceEqual(new[] {1, 2}));

            history.Redo();
            Assert.Equal(3, model.IntCollection.Count);
            Assert.True(model.IntCollection.SequenceEqual(new[] {1, 2, 3}));
        }

        [Fact]
        public void Move_Ascending()
        {
            var history = new History();
            var model = new TestModel(history);

            model.IntCollection = new ObservableCollection<int>();

            model.IntCollection.Add(0);
            model.IntCollection.Add(1);
            model.IntCollection.Add(2);
            model.IntCollection.Add(3);

            Assert.True(model.IntCollection.SequenceEqual(new[] {0, 1, 2, 3}));

            model.IntCollection.Move(0, 3);
            Assert.True(model.IntCollection.SequenceEqual(new[] {1, 2, 3, 0}));

            history.Undo();
            Assert.True(model.IntCollection.SequenceEqual(new[] {0, 1, 2, 3}));

            history.Redo();
            Assert.True(model.IntCollection.SequenceEqual(new[] {1, 2, 3, 0}));
        }

        [Fact]
        public void Move_Descending()
        {
            var history = new History();
            var model = new TestModel(history);

            model.IntCollection = new ObservableCollection<int>();

            model.IntCollection.Add(0);
            model.IntCollection.Add(1);
            model.IntCollection.Add(2);
            model.IntCollection.Add(3);

            Assert.True(model.IntCollection.SequenceEqual(new[] {0, 1, 2, 3}));

            model.IntCollection.Move(3, 0);
            Assert.True(model.IntCollection.SequenceEqual(new[] {3, 0, 1, 2}));

            history.Undo();
            Assert.True(model.IntCollection.SequenceEqual(new[] {0, 1, 2, 3}));

            history.Redo();
            Assert.True(model.IntCollection.SequenceEqual(new[] {3, 0, 1, 2}));
        }

        [Fact]
        public void Remove()
        {
            var history = new History();
            var model = new TestModel(history);

            model.IntCollection = new ObservableCollection<int>();

            model.IntCollection.Add(100);
            model.IntCollection.Add(101);
            model.IntCollection.Add(102);
            model.IntCollection.Add(103);

            Assert.True(model.IntCollection.SequenceEqual(new[] {100, 101, 102, 103}));

            model.IntCollection.Remove(103);
            Assert.True(model.IntCollection.SequenceEqual(new[] {100, 101, 102}));

            history.Undo();
            Assert.True(model.IntCollection.SequenceEqual(new[] {100, 101, 102, 103}));

            history.Redo();
            Assert.True(model.IntCollection.SequenceEqual(new[] {100, 101, 102}));
        }

        [Fact]
        public void RemoveAt()
        {
            var history = new History();
            var model = new TestModel(history);

            model.IntCollection = new ObservableCollection<int>();

            model.IntCollection.Add(100);
            model.IntCollection.Add(101);
            model.IntCollection.Add(102);
            model.IntCollection.Add(103);

            Assert.True(model.IntCollection.SequenceEqual(new[] {100, 101, 102, 103}));

            model.IntCollection.RemoveAt(3);
            Assert.True(model.IntCollection.SequenceEqual(new[] {100, 101, 102}));

            history.Undo();
            Assert.True(model.IntCollection.SequenceEqual(new[] {100, 101, 102, 103}));

            history.Redo();
            Assert.True(model.IntCollection.SequenceEqual(new[] {100, 101, 102}));
        }

        [Fact]
        public void Insert()
        {
            var history = new History();
            var model = new TestModel(history);

            model.IntCollection = new ObservableCollection<int>();

            model.IntCollection.Add(100);
            model.IntCollection.Add(101);
            model.IntCollection.Add(102);
            model.IntCollection.Add(103);

            Assert.True(model.IntCollection.SequenceEqual(new[] {100, 101, 102, 103}));

            model.IntCollection.Insert(2, 999);
            Assert.True(model.IntCollection.SequenceEqual(new[] {100, 101, 999, 102, 103}));

            history.Undo();
            Assert.True(model.IntCollection.SequenceEqual(new[] {100, 101, 102, 103}));

            history.Redo();
            Assert.True(model.IntCollection.SequenceEqual(new[] {100, 101, 999, 102, 103}));
        }

        [Fact]
        public void Clear()
        {
            var history = new History();
            var model = new TestModel(history);

            model.IntCollection = new ObservableCollection<int>();

            model.IntCollection.Add(100);
            model.IntCollection.Add(101);
            model.IntCollection.Add(102);
            model.IntCollection.Add(103);

            Assert.True(model.IntCollection.SequenceEqual(new[] {100, 101, 102, 103}));

            Assert.Throws<NotSupportedException>(() =>
                model.IntCollection.Clear()
            );
        }

        [Fact]
        public void ClearEx()
        {
            var history = new History();
            var model = new TestModel(history);

            model.IntCollection = new ObservableCollection<int>();

            model.IntCollection.Add(100);
            model.IntCollection.Add(101);
            model.IntCollection.Add(102);
            model.IntCollection.Add(103);

            Assert.True(model.IntCollection.SequenceEqual(new[] {100, 101, 102, 103}));

            model.IntCollection.ClearEx(history);

            Assert.True(model.IntCollection.SequenceEqual(new int[] {}));

            history.Undo();
            Assert.True(model.IntCollection.SequenceEqual(new[] {100, 101, 102, 103}));

            history.Redo();
            Assert.True(model.IntCollection.SequenceEqual(new int[] {}));
        }

        [Fact]
        public void Replace()
        {
            var history = new History();
            var model = new TestModel(history);

            model.IntCollection = new ObservableCollection<int>();

            model.IntCollection.Add(100);
            model.IntCollection.Add(101);
            model.IntCollection.Add(102);
            model.IntCollection.Add(103);

            Assert.True(model.IntCollection.SequenceEqual(new[] {100, 101, 102, 103}));

            model.IntCollection[2] = 999;
            Assert.True(model.IntCollection.SequenceEqual(new[] {100, 101, 999, 103}));

            history.Undo();
            Assert.True(model.IntCollection.SequenceEqual(new[] {100, 101, 102, 103}));

            history.Redo();
            Assert.True(model.IntCollection.SequenceEqual(new[] {100, 101, 999, 103}));
        }

        public class TestModel : EditableModelBase
        {
            public TestModel(History history)
            {
                SetupEditingSystem(history);
            }

            #region IntCollection

            private ObservableCollection<int> _IntCollection;

            public ObservableCollection<int> IntCollection
            {
                get => _IntCollection;
                set => SetEditableProperty(v => _IntCollection = v, _IntCollection, value);
            }

            #endregion
        }
    }
}