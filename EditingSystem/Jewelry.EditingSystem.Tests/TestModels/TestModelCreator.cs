using System;

namespace Jewelry.EditingSystem.Tests.TestModels;

public static class TestModelCreator
{
    public static IBasicTestModel CreateBasicTestModel(TestModelKinds kind, History history)
    {
        return kind switch
        {
            TestModelKinds.EditableModel => new EditableBasicTestModel(history),
            TestModelKinds.Direct => new DirectBasicTestModel(history),
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
        };
    }
    
    public static IFlagTestModel CreateFlagTestModel(TestModelKinds kind, History history)
    {
        return kind switch
        {
            TestModelKinds.EditableModel => new EditableFlagTestModel(history),
            TestModelKinds.Direct => new DirectFlagTestModel(history),
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
        };
    }
}