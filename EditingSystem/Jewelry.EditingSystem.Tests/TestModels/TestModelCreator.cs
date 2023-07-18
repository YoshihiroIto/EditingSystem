using System;

namespace Jewelry.EditingSystem.Tests.TestModels;

public static class TestModelCreator
{
    public static ITestModel CreateTestModel(TestModelKinds kind, History history)
    {
        return kind switch
        {
            TestModelKinds.EditableModel => new EditableTestModel(history),
            TestModelKinds.Direct => new DirectTestModel(history),
            TestModelKinds.CommunityToolkitMvvm => new CommunityToolkitMvvmTestModel(history),
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
        };
    }
}