using Jewelry.EditingSystem.Tests.TestModels;
using Xunit;
using static Jewelry.EditingSystem.Tests.TestModels.TestModelCreator;

namespace Jewelry.EditingSystem.Tests.Tests;

public sealed class FlagPropertyCoalescingTests
{
    [Theory]
    [InlineData(TestModelKinds.EditableModel)]
    [InlineData(TestModelKinds.Direct)]
    public void Coalescing_batch_drops_net_no_op_flag_change(TestModelKinds testModelKind)
    {
        using var history = new History();
        var model = CreateFlagTestModel(testModelKind, history);

        history.BeginCoalescingBatch();
        model.IsA = true;
        model.IsA = false;
        history.EndCoalescingBatch();

        Assert.False(model.IsA);
        Assert.False(history.CanUndo);
        Assert.False(history.CanRedo);
    }
}
