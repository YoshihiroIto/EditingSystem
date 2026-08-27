using System;

namespace Jewelry.EditingSystem.Tests.TestModels
{
    public sealed class CollectionItem : ICollectionItem
    {
        public int CollectionChangedAddCount { get; private set; }
        public int CollectionChangedRemoveCount { get; private set; }
        public int CollectionChangedMoveCount { get; private set; }

        public void Changed(in CollectionItemChangedInfo info)
        {
            switch (info.Type)
            {
                case CollectionItemChangedType.Add:
                    ++ CollectionChangedAddCount;
                    break;

                case CollectionItemChangedType.Remove:
                    ++ CollectionChangedRemoveCount;
                    break;

                case CollectionItemChangedType.Move:
                    ++ CollectionChangedMoveCount;
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(info.Type), info.Type, null);
            }
        }
    }
}