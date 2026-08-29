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
}
