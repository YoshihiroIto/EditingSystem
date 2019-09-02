namespace EditingSystem
{
    public interface ICollectionItem
    {
        void Changed(in CollectionItemChangedInfo info);
    }

    public readonly struct CollectionItemChangedInfo
    {
        public readonly CollectionItemChangedType Type;

        public CollectionItemChangedInfo(in CollectionItemChangedType type)
        {
            Type = type;
        }
    }

    public enum CollectionItemChangedType
    {
        Add,
        Remove,
        Move
    }
}