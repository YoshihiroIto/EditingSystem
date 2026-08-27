using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.SourceGenerators;
using Jewelry.EditingSystem;
using Jewelry.EditingSystem.CommunityToolkit.Mvvm;
using Jewelry.EditingSystem.CommunityToolkit.Mvvm.SourceGenerators;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace Jewelry.EditingSystem.CommunityToolkit.Mvvm.SourceGenerators.Tests;

public sealed class UndoablePropertyGeneratorTests
{
    [Fact]
    public void GeneratesHooksForFieldPartialPropertyNestedGenericAndNamingConventions()
    {
        const string source = """
using CommunityToolkit.Mvvm.ComponentModel;
using Jewelry.EditingSystem;
using Jewelry.EditingSystem.CommunityToolkit.Mvvm;

public partial class Outer<T>
{
    [EditingHistory(nameof(_history))]
    public partial class Model<U> : ObservableObject
    {
        private readonly History _history = new();

        [Undoable, ObservableProperty]
        private int m_first;

        [Undoable, ObservableProperty]
        private string? _second;

        [Undoable, ObservableProperty]
        private int third;

        [Undoable, ObservableProperty]
        public partial U? Fourth { get; set; }
    }
}
""";

        var result = Run(source);
        var generated = Assert.Single(result.Results).GeneratedSources;
        var text = Assert.Single(generated).SourceText.ToString();

        Assert.Empty(result.Diagnostics);
        Assert.Contains("partial class Outer<T>", text);
        Assert.Contains("partial class Model<U>", text);
        Assert.Contains("partial void OnFirstChanging(int value)", text);
        Assert.Contains("partial void OnSecondChanging(string? value)", text);
        Assert.Contains("partial void OnThirdChanging(int value)", text);
        Assert.Contains("partial void OnFourthChanging(U? value)", text);
        Assert.Contains("value => this.Fourth = value", text);
        Assert.DoesNotContain("__Internals", text);
    }

    [Fact]
    public void ReportsMissingObservableProperty()
    {
        AssertDiagnostic("""
using Jewelry.EditingSystem;
using Jewelry.EditingSystem.CommunityToolkit.Mvvm;

[EditingHistory(nameof(_history))]
public partial class Model
{
    private readonly History _history = new();
    [Undoable] private int value;
}
""", "JESCT001");
    }

    [Fact]
    public void GeneratesHookUsingPrimaryConstructorHistoryParameter()
    {
        const string source = """
using CommunityToolkit.Mvvm.ComponentModel;
using Jewelry.EditingSystem;
using Jewelry.EditingSystem.CommunityToolkit.Mvvm;

[EditingHistory(nameof(history))]
public partial class Model(History history) : ObservableObject
{
    [Undoable, ObservableProperty]
    public partial int Value { get; set; }
}
""";

        var result = Run(source);
        var generated = Assert.Single(result.Results).GeneratedSources;
        var text = Assert.Single(generated).SourceText.ToString();

        Assert.Empty(result.Diagnostics);
        Assert.Contains("private global::Jewelry.EditingSystem.History __jewelryEditingHistory => history;", text);
        Assert.Contains("var editingHistory = this.__jewelryEditingHistory;", text);
    }

    [Fact]
    public void CompilesWithBothGeneratorsWhenHistoryParameterWouldShadowNewValue()
    {
        const string source = """
using CommunityToolkit.Mvvm.ComponentModel;
using Jewelry.EditingSystem;
using Jewelry.EditingSystem.CommunityToolkit.Mvvm;

[EditingHistory(nameof(newValue))]
public partial class Model(History newValue) : ObservableObject
{
    [Undoable, ObservableProperty]
    public partial int Value { get; set; }
}
""";

        var (result, outputCompilation) = RunAndCompileWithBothGenerators(source);
        var generated = Assert.Single(
            result.Results.SelectMany(static generator => generator.GeneratedSources),
            static source => source.HintName.EndsWith(".Undoable.g.cs", StringComparison.Ordinal));
        var text = generated.SourceText.ToString();

        Assert.Contains("partial void OnValueChanging(int value)", text);
        Assert.Contains("private global::Jewelry.EditingSystem.History __jewelryEditingHistory => newValue;", text);
        Assert.Contains("var editingHistory = this.__jewelryEditingHistory;", text);
        Assert.DoesNotContain(
            outputCompilation.GetDiagnostics(),
            static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
    }

    [Fact]
    public void ReportsMissingHistoryConfiguration()
    {
        AssertDiagnostic("""
using CommunityToolkit.Mvvm.ComponentModel;
using Jewelry.EditingSystem.CommunityToolkit.Mvvm;

public partial class Model : ObservableObject
{
    [Undoable, ObservableProperty] private int value;
}
""", "JESCT003");
    }

    [Fact]
    public void ReportsInvalidHistoryMember()
    {
        AssertDiagnostic("""
using CommunityToolkit.Mvvm.ComponentModel;
using Jewelry.EditingSystem.CommunityToolkit.Mvvm;

[EditingHistory("missing")]
public partial class Model : ObservableObject
{
    [Undoable, ObservableProperty] private int value;
}
""", "JESCT004");
    }

    [Fact]
    public void ReportsNullableHistoryMember()
    {
        AssertDiagnostic("""
using CommunityToolkit.Mvvm.ComponentModel;
using Jewelry.EditingSystem;
using Jewelry.EditingSystem.CommunityToolkit.Mvvm;

[EditingHistory(nameof(_history))]
public partial class Model : ObservableObject
{
    private readonly History? _history;
    [Undoable, ObservableProperty] private int value;
}
""", "JESCT004");
    }

    [Fact]
    public void ReportsNonPartialContainingType()
    {
        AssertDiagnostic("""
using CommunityToolkit.Mvvm.ComponentModel;
using Jewelry.EditingSystem;
using Jewelry.EditingSystem.CommunityToolkit.Mvvm;

[EditingHistory(nameof(_history))]
public class Model : ObservableObject
{
    private readonly History _history = new();
    [Undoable, ObservableProperty] private int value;
}
""", "JESCT005");
    }

    [Fact]
    public void ReportsReservedOneParameterChangingHook()
    {
        AssertDiagnostic("""
using CommunityToolkit.Mvvm.ComponentModel;
using Jewelry.EditingSystem;
using Jewelry.EditingSystem.CommunityToolkit.Mvvm;

[EditingHistory(nameof(_history))]
public partial class Model : ObservableObject
{
    private readonly History _history = new();
    [Undoable, ObservableProperty] private int value;
    partial void OnValueChanging(int value) { }
}
""", "JESCT006");
    }

    [Fact]
    public void ReportsUnsupportedTarget()
    {
        AssertDiagnostic("""
using CommunityToolkit.Mvvm.ComponentModel;
using Jewelry.EditingSystem;
using Jewelry.EditingSystem.CommunityToolkit.Mvvm;

[EditingHistory(nameof(_history))]
public partial class Model : ObservableObject
{
    private readonly History _history = new();
    [Undoable, ObservableProperty] private readonly int value;
}
""", "JESCT002");
    }

    [Fact]
    public void ReportsInitOnlyPartialProperty()
    {
        AssertDiagnostic("""
using CommunityToolkit.Mvvm.ComponentModel;
using Jewelry.EditingSystem;
using Jewelry.EditingSystem.CommunityToolkit.Mvvm;

[EditingHistory(nameof(_history))]
public partial class Model : ObservableObject
{
    private readonly History _history = new();
    [Undoable, ObservableProperty] public partial int Value { get; init; }
}
""", "JESCT002");
    }

    [Fact]
    public void ReportsUnsupportedToolkitAssemblyVersion()
    {
        const string source = """
using System;

namespace CommunityToolkit.Mvvm.ComponentModel
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public sealed class ObservablePropertyAttribute : Attribute { }
}

namespace Jewelry.EditingSystem
{
    public class History { }
}

namespace Jewelry.EditingSystem.CommunityToolkit.Mvvm
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public sealed class UndoableAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Class)]
    public sealed class EditingHistoryAttribute : Attribute
    {
        public EditingHistoryAttribute(string memberName) { }
    }
}

namespace Test
{
    using CommunityToolkit.Mvvm.ComponentModel;
    using Jewelry.EditingSystem;
    using Jewelry.EditingSystem.CommunityToolkit.Mvvm;

    [EditingHistory(nameof(_history))]
    public partial class Model
    {
        private readonly History _history = new();
        [Undoable, ObservableProperty] private int value;
    }
}
""";

        var result = Run(source, includeProductReferences: false);
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Id == "JESCT007");
    }

    private static void AssertDiagnostic(string source, string diagnosticId)
    {
        var result = Run(source);
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Id == diagnosticId);
    }

    private static GeneratorDriverRunResult Run(string source, bool includeProductReferences = true)
    {
        var parseOptions = new CSharpParseOptions(LanguageVersion.Preview);
        var syntaxTree = CSharpSyntaxTree.ParseText(source, parseOptions);
        var references = GetFrameworkReferences().ToList();

        if (includeProductReferences)
        {
            references.Add(MetadataReference.CreateFromFile(typeof(ObservablePropertyAttribute).Assembly.Location));
            references.Add(MetadataReference.CreateFromFile(typeof(UndoableAttribute).Assembly.Location));
            references.Add(MetadataReference.CreateFromFile(typeof(History).Assembly.Location));
        }

        var compilation = CSharpCompilation.Create(
            "GeneratorTests",
            [syntaxTree],
            references.DistinctBy(reference => reference.Display),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));

        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            [new UndoablePropertyGenerator().AsSourceGenerator()],
            parseOptions: parseOptions);

        driver = driver.RunGenerators(compilation);
        return driver.GetRunResult();
    }

    private static (GeneratorDriverRunResult Result, Compilation OutputCompilation) RunAndCompileWithBothGenerators(
        string source)
    {
        var parseOptions = new CSharpParseOptions(LanguageVersion.Preview);
        var syntaxTree = CSharpSyntaxTree.ParseText(source, parseOptions);
        var references = GetFrameworkReferences()
            .Append(MetadataReference.CreateFromFile(typeof(ObservablePropertyAttribute).Assembly.Location))
            .Append(MetadataReference.CreateFromFile(typeof(UndoableAttribute).Assembly.Location))
            .Append(MetadataReference.CreateFromFile(typeof(History).Assembly.Location))
            .DistinctBy(reference => reference.Display);
        var compilation = CSharpCompilation.Create(
            "GeneratorCompilationTests",
            [syntaxTree],
            references,
            new CSharpCompilationOptions(
                OutputKind.DynamicallyLinkedLibrary,
                nullableContextOptions: NullableContextOptions.Enable));

        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            [
                new ObservablePropertyGenerator().AsSourceGenerator(),
                new UndoablePropertyGenerator().AsSourceGenerator()
            ],
            parseOptions: parseOptions);

        driver = driver.RunGeneratorsAndUpdateCompilation(
            compilation,
            out var outputCompilation,
            out var generatorDiagnostics);

        Assert.DoesNotContain(
            generatorDiagnostics,
            static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        return (driver.GetRunResult(), outputCompilation);
    }

    private static IEnumerable<MetadataReference> GetFrameworkReferences()
    {
        var trustedPlatformAssemblies = (string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")
            ?? throw new InvalidOperationException("Trusted platform assemblies are unavailable.");

        return trustedPlatformAssemblies
            .Split(Path.PathSeparator)
            .Select(path => MetadataReference.CreateFromFile(path));
    }
}
