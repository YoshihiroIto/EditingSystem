using System.Collections.ObjectModel;
using Xunit;

// ReSharper disable UseObjectOrCollectionInitializer

namespace EditingSystem.Tests
{
    public class CollectionItemTests
    {
        [Fact]
        public void Add()
        {
            var history = new History();
            var model = new TestModel(history);

            model.Collection = new ObservableCollection<CollectionItem>();

            var item0 = new CollectionItem();
            var item1 = new CollectionItem();
            var item2 = new CollectionItem();

            Assert.Equal(0, item0.CollectionChangedCount);
            Assert.Equal(0, item1.CollectionChangedCount);
            Assert.Equal(0, item2.CollectionChangedCount);

            model.Collection.Add(item0);
            model.Collection.Add(item1);
            model.Collection.Add(item2);

            Assert.Equal(1, item0.CollectionChangedCount);
            Assert.Equal(1, item1.CollectionChangedCount);
            Assert.Equal(1, item2.CollectionChangedCount);

            history.Undo();
            Assert.Equal(1, item0.CollectionChangedCount);
            Assert.Equal(1, item1.CollectionChangedCount);
            Assert.Equal(2, item2.CollectionChangedCount);

            history.Undo();
            Assert.Equal(1, item0.CollectionChangedCount);
            Assert.Equal(2, item1.CollectionChangedCount);
            Assert.Equal(2, item2.CollectionChangedCount);

            history.Undo();
            Assert.Equal(2, item0.CollectionChangedCount);
            Assert.Equal(2, item1.CollectionChangedCount);
            Assert.Equal(2, item2.CollectionChangedCount);

            history.Redo();
            Assert.Equal(3, item0.CollectionChangedCount);
            Assert.Equal(2, item1.CollectionChangedCount);
            Assert.Equal(2, item2.CollectionChangedCount);

            history.Redo();
            Assert.Equal(3, item0.CollectionChangedCount);
            Assert.Equal(3, item1.CollectionChangedCount);
            Assert.Equal(2, item2.CollectionChangedCount);

            history.Redo();
            Assert.Equal(3, item0.CollectionChangedCount);
            Assert.Equal(3, item1.CollectionChangedCount);
            Assert.Equal(3, item2.CollectionChangedCount);
        }

        [Fact]
        public void Move_Ascending()
        {
            var history = new History();
            var model = new TestModel(history);

            model.Collection = new ObservableCollection<CollectionItem>();

            var item0 = new CollectionItem();
            var item1 = new CollectionItem();
            var item2 = new CollectionItem();
            var item3 = new CollectionItem();

            model.Collection.Add(item0);
            model.Collection.Add(item1);
            model.Collection.Add(item2);
            model.Collection.Add(item3);

            Assert.Equal(1, item0.CollectionChangedCount);
            Assert.Equal(1, item1.CollectionChangedCount);
            Assert.Equal(1, item2.CollectionChangedCount);
            Assert.Equal(1, item3.CollectionChangedCount);

            model.Collection.Move(0, 3);

            Assert.Equal(2, item0.CollectionChangedCount);
            Assert.Equal(1, item1.CollectionChangedCount);
            Assert.Equal(1, item2.CollectionChangedCount);
            Assert.Equal(1, item3.CollectionChangedCount);

            history.Undo();
            Assert.Equal(3, item0.CollectionChangedCount);
            Assert.Equal(1, item1.CollectionChangedCount);
            Assert.Equal(1, item2.CollectionChangedCount);
            Assert.Equal(1, item3.CollectionChangedCount);

            history.Redo();
            Assert.Equal(4, item0.CollectionChangedCount);
            Assert.Equal(1, item1.CollectionChangedCount);
            Assert.Equal(1, item2.CollectionChangedCount);
            Assert.Equal(1, item3.CollectionChangedCount);
        }

        [Fact]
        public void Move_Descending()
        {
            var history = new History();
            var model = new TestModel(history);

            model.Collection = new ObservableCollection<CollectionItem>();

            var item0 = new CollectionItem();
            var item1 = new CollectionItem();
            var item2 = new CollectionItem();
            var item3 = new CollectionItem();

            model.Collection.Add(item0);
            model.Collection.Add(item1);
            model.Collection.Add(item2);
            model.Collection.Add(item3);

            Assert.Equal(1, item0.CollectionChangedCount);
            Assert.Equal(1, item1.CollectionChangedCount);
            Assert.Equal(1, item2.CollectionChangedCount);
            Assert.Equal(1, item3.CollectionChangedCount);

            model.Collection.Move(3, 0);

            Assert.Equal(1, item0.CollectionChangedCount);
            Assert.Equal(1, item1.CollectionChangedCount);
            Assert.Equal(1, item2.CollectionChangedCount);
            Assert.Equal(2, item3.CollectionChangedCount);

            history.Undo();
            Assert.Equal(1, item0.CollectionChangedCount);
            Assert.Equal(1, item1.CollectionChangedCount);
            Assert.Equal(1, item2.CollectionChangedCount);
            Assert.Equal(3, item3.CollectionChangedCount);

            history.Redo();
            Assert.Equal(1, item0.CollectionChangedCount);
            Assert.Equal(1, item1.CollectionChangedCount);
            Assert.Equal(1, item2.CollectionChangedCount);
            Assert.Equal(4, item3.CollectionChangedCount);
        }

        [Fact]
        public void Remove()
        {
            var history = new History();
            var model = new TestModel(history);

            model.Collection = new ObservableCollection<CollectionItem>();

            var item0 = new CollectionItem();
            var item1 = new CollectionItem();
            var item2 = new CollectionItem();
            var item3 = new CollectionItem();

            model.Collection.Add(item0);
            model.Collection.Add(item1);
            model.Collection.Add(item2);
            model.Collection.Add(item3);

            model.Collection.Remove(item3);

            Assert.Equal(1, item0.CollectionChangedCount);
            Assert.Equal(1, item1.CollectionChangedCount);
            Assert.Equal(1, item2.CollectionChangedCount);
            Assert.Equal(2, item3.CollectionChangedCount);

            history.Undo();
            Assert.Equal(1, item0.CollectionChangedCount);
            Assert.Equal(1, item1.CollectionChangedCount);
            Assert.Equal(1, item2.CollectionChangedCount);
            Assert.Equal(3, item3.CollectionChangedCount);

            history.Redo();
            Assert.Equal(1, item0.CollectionChangedCount);
            Assert.Equal(1, item1.CollectionChangedCount);
            Assert.Equal(1, item2.CollectionChangedCount);
            Assert.Equal(4, item3.CollectionChangedCount);
        }

        [Fact]
        public void RemoveAt()
        {
            var history = new History();
            var model = new TestModel(history);

            model.Collection = new ObservableCollection<CollectionItem>();

            var item0 = new CollectionItem();
            var item1 = new CollectionItem();
            var item2 = new CollectionItem();
            var item3 = new CollectionItem();

            model.Collection.Add(item0);
            model.Collection.Add(item1);
            model.Collection.Add(item2);
            model.Collection.Add(item3);

            model.Collection.RemoveAt(3);

            Assert.Equal(1, item0.CollectionChangedCount);
            Assert.Equal(1, item1.CollectionChangedCount);
            Assert.Equal(1, item2.CollectionChangedCount);
            Assert.Equal(2, item3.CollectionChangedCount);

            history.Undo();
            Assert.Equal(1, item0.CollectionChangedCount);
            Assert.Equal(1, item1.CollectionChangedCount);
            Assert.Equal(1, item2.CollectionChangedCount);
            Assert.Equal(3, item3.CollectionChangedCount);

            history.Redo();
            Assert.Equal(1, item0.CollectionChangedCount);
            Assert.Equal(1, item1.CollectionChangedCount);
            Assert.Equal(1, item2.CollectionChangedCount);
            Assert.Equal(4, item3.CollectionChangedCount);
        }

        [Fact]
        public void Insert()
        {
            var history = new History();
            var model = new TestModel(history);

            model.Collection = new ObservableCollection<CollectionItem>();

            var item0 = new CollectionItem();
            var item1 = new CollectionItem();
            var item2 = new CollectionItem();
            var item3 = new CollectionItem();

            model.Collection.Add(item0);
            model.Collection.Add(item1);
            model.Collection.Add(item2);
            model.Collection.Add(item3);

            Assert.Equal(1, item0.CollectionChangedCount);
            Assert.Equal(1, item1.CollectionChangedCount);
            Assert.Equal(1, item2.CollectionChangedCount);
            Assert.Equal(1, item3.CollectionChangedCount);

            var itemX = new CollectionItem();
            Assert.Equal(0, itemX.CollectionChangedCount);

            model.Collection.Insert(2, itemX);

            Assert.Equal(1, item0.CollectionChangedCount);
            Assert.Equal(1, item1.CollectionChangedCount);
            Assert.Equal(1, itemX.CollectionChangedCount);
            Assert.Equal(1, item2.CollectionChangedCount);
            Assert.Equal(1, item3.CollectionChangedCount);

            history.Undo();
            Assert.Equal(2, itemX.CollectionChangedCount);

            history.Redo();
            Assert.Equal(3, itemX.CollectionChangedCount);
        }

        [Fact]
        public void ClearEx()
        {
            var history = new History();
            var model = new TestModel(history);

            model.Collection = new ObservableCollection<CollectionItem>();

            var item0 = new CollectionItem();
            var item1 = new CollectionItem();
            var item2 = new CollectionItem();
            var item3 = new CollectionItem();

            model.Collection.Add(item0);
            model.Collection.Add(item1);
            model.Collection.Add(item2);
            model.Collection.Add(item3);

            Assert.Equal(1, item0.CollectionChangedCount);
            Assert.Equal(1, item1.CollectionChangedCount);
            Assert.Equal(1, item2.CollectionChangedCount);
            Assert.Equal(1, item3.CollectionChangedCount);

            model.Collection.ClearEx(history);

            Assert.Equal(2, item0.CollectionChangedCount);
            Assert.Equal(2, item1.CollectionChangedCount);
            Assert.Equal(2, item2.CollectionChangedCount);
            Assert.Equal(2, item3.CollectionChangedCount);

            history.Undo();
            Assert.Equal(3, item0.CollectionChangedCount);
            Assert.Equal(3, item1.CollectionChangedCount);
            Assert.Equal(3, item2.CollectionChangedCount);
            Assert.Equal(3, item3.CollectionChangedCount);

            history.Redo();
            Assert.Equal(4, item0.CollectionChangedCount);
            Assert.Equal(4, item1.CollectionChangedCount);
            Assert.Equal(4, item2.CollectionChangedCount);
            Assert.Equal(4, item3.CollectionChangedCount);
        }

        [Fact]
        public void Replace()
        {
            var history = new History();
            var model = new TestModel(history);

            model.Collection = new ObservableCollection<CollectionItem>();

            var item0 = new CollectionItem();
            var item1 = new CollectionItem();
            var item2 = new CollectionItem();
            var item3 = new CollectionItem();

            model.Collection.Add(item0);
            model.Collection.Add(item1);
            model.Collection.Add(item2);
            model.Collection.Add(item3);

            Assert.Equal(1, item0.CollectionChangedCount);
            Assert.Equal(1, item1.CollectionChangedCount);
            Assert.Equal(1, item2.CollectionChangedCount);
            Assert.Equal(1, item3.CollectionChangedCount);

            var itemX = new CollectionItem();

            model.Collection[2] = itemX;

            Assert.Equal(1, item0.CollectionChangedCount);
            Assert.Equal(1, item1.CollectionChangedCount);
            Assert.Equal(2, item2.CollectionChangedCount);
            Assert.Equal(1, item3.CollectionChangedCount);
            Assert.Equal(1, itemX.CollectionChangedCount);

            history.Undo();
            Assert.Equal(1, item0.CollectionChangedCount);
            Assert.Equal(1, item1.CollectionChangedCount);
            Assert.Equal(3, item2.CollectionChangedCount);
            Assert.Equal(1, item3.CollectionChangedCount);
            Assert.Equal(2, itemX.CollectionChangedCount);

            history.Redo();
            Assert.Equal(1, item0.CollectionChangedCount);
            Assert.Equal(1, item1.CollectionChangedCount);
            Assert.Equal(4, item2.CollectionChangedCount);
            Assert.Equal(1, item3.CollectionChangedCount);
            Assert.Equal(3, itemX.CollectionChangedCount);
        } 

        public class TestModel : EditableModelBase
        {
            public TestModel(History history)
            {
                SetupEditingSystem(history);
            }

            #region Collection

            private ObservableCollection<CollectionItem> _Collection;

            public ObservableCollection<CollectionItem> Collection
            {
                get => _Collection;
                set => SetEditableProperty(v => _Collection = v, _Collection, value);
            }

            #endregion
        }


        public class CollectionItem : ICollectionItem
        {
            public int CollectionChangedCount { get; private set; }

            public void Changed()
            {
                ++ CollectionChangedCount;
            }
        }
    }
}
