using Jewelry.EditingSystem.Tests.TestModels;
using Xunit;
using static Jewelry.EditingSystem.Tests.TestModels.TestModelCreator;

namespace Jewelry.EditingSystem.Tests;

public sealed class SetEditablePropertyTests
{
    [Theory]
    [ClassData(typeof(TestModelKindsTestData))]
    public void Basic(TestModelKinds testModelKind)
    {
        using var history = new History();
        var model = CreateBasicTestModel(testModelKind, history);

        model.IntValue = 123;
        Assert.Equal(1, model.ChangingCount);

        model.IntValue = 456;
        Assert.Equal(2, model.ChangingCount);

        model.IntValue = 456;
        Assert.Equal(2, model.ChangingCount);

        model.IntValue = 123;
        Assert.Equal(3, model.ChangingCount);
    }
}