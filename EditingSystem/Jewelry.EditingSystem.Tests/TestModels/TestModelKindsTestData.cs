using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Jewelry.EditingSystem.Tests.TestModels;

public sealed class TestModelKindsTestData : IEnumerable<object[]>
{
    public IEnumerator<object[]> GetEnumerator() =>
        Enum.GetValues<TestModelKinds>()
            .Select(x => new object[] { x })
            .GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}