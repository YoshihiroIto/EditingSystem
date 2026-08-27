using System.Collections.Immutable;
using System.Globalization;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Jewelry.EditingSystem.CommunityToolkit.Mvvm.SourceGenerators;

[Generator(LanguageNames.CSharp)]
public sealed class UndoablePropertyGenerator : IIncrementalGenerator
{
    private const string UndoableAttributeName = "Jewelry.EditingSystem.CommunityToolkit.Mvvm.UndoableAttribute";
    private const string EditingHistoryAttributeName = "Jewelry.EditingSystem.CommunityToolkit.Mvvm.EditingHistoryAttribute";
    private const string ObservablePropertyAttributeName = "CommunityToolkit.Mvvm.ComponentModel.ObservablePropertyAttribute";
    private const string HistoryTypeName = "Jewelry.EditingSystem.History";

    private static readonly Version MinimumToolkitVersion = new(8, 4, 0, 0);
    private static readonly Version MaximumToolkitVersion = new(9, 0, 0, 0);

    private static readonly DiagnosticDescriptor MissingObservableProperty = new(
        "JESCT001",
        "Undoable requires ObservableProperty",
        "'{0}' must also be annotated with ObservablePropertyAttribute",
        "Jewelry.EditingSystem.CommunityToolkit.Mvvm",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor UnsupportedTarget = new(
        "JESCT002",
        "Unsupported undoable target",
        "'{0}' cannot be used as an undoable CommunityToolkit.Mvvm property: {1}",
        "Jewelry.EditingSystem.CommunityToolkit.Mvvm",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor MissingHistoryConfiguration = new(
        "JESCT003",
        "EditingHistory configuration is required",
        "Type '{0}' must have exactly one EditingHistoryAttribute",
        "Jewelry.EditingSystem.CommunityToolkit.Mvvm",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor InvalidHistoryMember = new(
        "JESCT004",
        "Invalid editing history member",
        "EditingHistory member '{0}' on type '{1}' is invalid: {2}",
        "Jewelry.EditingSystem.CommunityToolkit.Mvvm",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor TypeMustBePartial = new(
        "JESCT005",
        "Containing types must be partial",
        "Type '{0}' and all containing types must be partial",
        "Jewelry.EditingSystem.CommunityToolkit.Mvvm",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor ReservedHookConflict = new(
        "JESCT006",
        "ObservableProperty hook is reserved",
        "The one-parameter hook '{0}' is reserved by UndoableAttribute; use the two-parameter changing hook or a changed hook",
        "Jewelry.EditingSystem.CommunityToolkit.Mvvm",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor UnsupportedToolkitVersion = new(
        "JESCT007",
        "Unsupported CommunityToolkit.Mvvm version",
        "CommunityToolkit.Mvvm assembly version '{0}' is unsupported; expected a version in [8.4.0, 9.0.0)",
        "Jewelry.EditingSystem.CommunityToolkit.Mvvm",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var candidates = context.SyntaxProvider.ForAttributeWithMetadataName(
            UndoableAttributeName,
            static (node, _) => node is VariableDeclaratorSyntax or PropertyDeclarationSyntax,
            static (generatorContext, _) => new Candidate(
                generatorContext.TargetSymbol,
                generatorContext.TargetSymbol.Locations.FirstOrDefault()));

        context.RegisterSourceOutput(
            candidates.Collect().Combine(context.CompilationProvider),
            static (sourceContext, input) => Execute(sourceContext, input.Left, input.Right));
    }

    private static void Execute(
        SourceProductionContext context,
        ImmutableArray<Candidate> candidates,
        Compilation compilation)
    {
        if (candidates.IsDefaultOrEmpty)
            return;

        var observablePropertyAttribute = compilation.GetTypeByMetadataName(ObservablePropertyAttributeName);
        var historyType = compilation.GetTypeByMetadataName(HistoryTypeName);

        var toolkitVersion = observablePropertyAttribute?.ContainingAssembly.Identity.Version;
        if (toolkitVersion is null || toolkitVersion < MinimumToolkitVersion || toolkitVersion >= MaximumToolkitVersion)
        {
            var displayVersion = toolkitVersion?.ToString() ?? "not referenced";
            foreach (var candidate in candidates)
                context.ReportDiagnostic(Diagnostic.Create(UnsupportedToolkitVersion, candidate.Location, displayVersion));

            return;
        }

        var groupedProperties = new Dictionary<INamedTypeSymbol, List<PropertyModel>>(SymbolEqualityComparer.Default);

        foreach (var candidate in candidates)
        {
            var symbol = candidate.Symbol;
            if (symbol is not IFieldSymbol and not IPropertySymbol)
                continue;

            if (!HasAttribute(symbol, ObservablePropertyAttributeName))
            {
                context.ReportDiagnostic(Diagnostic.Create(MissingObservableProperty, candidate.Location, symbol.Name));
                continue;
            }

            var containingType = symbol.ContainingType;
            if (!AreAllContainingTypesPartial(containingType))
            {
                context.ReportDiagnostic(Diagnostic.Create(TypeMustBePartial, candidate.Location, containingType.ToDisplayString()));
                continue;
            }

            if (!TryGetProperty(symbol, out var propertyName, out var propertyType, out var unsupportedReason))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    UnsupportedTarget,
                    candidate.Location,
                    symbol.Name,
                    unsupportedReason));
                continue;
            }

            var historyAttributes = containingType.GetAttributes()
                .Where(static attribute => attribute.AttributeClass?.ToDisplayString() == EditingHistoryAttributeName)
                .ToArray();

            if (historyAttributes.Length != 1)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    MissingHistoryConfiguration,
                    candidate.Location,
                    containingType.ToDisplayString()));
                continue;
            }

            var configuredName = historyAttributes[0].ConstructorArguments.Length == 1
                ? historyAttributes[0].ConstructorArguments[0].Value as string
                : null;

            if (!TryGetHistoryMember(containingType, historyType, configuredName, out var historyMember, out var historyReason))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    InvalidHistoryMember,
                    candidate.Location,
                    configuredName ?? "<null>",
                    containingType.ToDisplayString(),
                    historyReason));
                continue;
            }

            var hookName = $"On{propertyName}Changing";
            if (HasReservedHookConflict(containingType, hookName, propertyType))
            {
                context.ReportDiagnostic(Diagnostic.Create(ReservedHookConflict, candidate.Location, hookName));
                continue;
            }

            if (!groupedProperties.TryGetValue(containingType, out var properties))
            {
                properties = new List<PropertyModel>();
                groupedProperties.Add(containingType, properties);
            }

            var historyParameterName = historyMember is IParameterSymbol parameter
                ? parameter.Name
                : null;
            var historyAccessorName = historyParameterName is not null
                ? GetUniqueHistoryAccessorName(containingType)
                : null;
            var historyAccessExpression = historyAccessorName is not null
                ? $"this.{EscapeIdentifier(historyAccessorName)}"
                : $"this.{EscapeIdentifier(historyMember!.Name)}";

            properties.Add(new PropertyModel(
                propertyName,
                propertyType,
                historyAccessExpression,
                historyParameterName,
                historyAccessorName));
        }

        foreach (var pair in groupedProperties)
        {
            var source = GenerateSource(pair.Key, pair.Value);
            context.AddSource(GetHintName(pair.Key), SourceText.From(source, Encoding.UTF8));
        }
    }

    private static bool TryGetProperty(
        ISymbol symbol,
        out string propertyName,
        out ITypeSymbol propertyType,
        out string unsupportedReason)
    {
        if (symbol is IFieldSymbol field)
        {
            propertyName = GetGeneratedPropertyName(field.Name);
            propertyType = field.Type;

            if (field.IsStatic)
            {
                unsupportedReason = "static fields are not supported";
                return false;
            }

            if (field.IsReadOnly)
            {
                unsupportedReason = "readonly fields are not supported";
                return false;
            }

            if (propertyName.Length == 0)
            {
                unsupportedReason = "the generated property name is empty";
                return false;
            }

            unsupportedReason = "";
            return true;
        }

        var property = (IPropertySymbol)symbol;
        propertyName = property.Name;
        propertyType = property.Type;

        if (property.IsStatic)
        {
            unsupportedReason = "static properties are not supported";
            return false;
        }

        if (!property.DeclaringSyntaxReferences.Any(static reference =>
                reference.GetSyntax() is PropertyDeclarationSyntax declaration &&
                declaration.Modifiers.Any(SyntaxKind.PartialKeyword)))
        {
            unsupportedReason = "the property must be a partial property";
            return false;
        }

        if (property.SetMethod is null)
        {
            unsupportedReason = "the property must have a setter";
            return false;
        }

        if (property.SetMethod.IsInitOnly)
        {
            unsupportedReason = "init-only properties cannot be changed by undo or redo";
            return false;
        }

        unsupportedReason = "";
        return true;
    }

    private static bool TryGetHistoryMember(
        INamedTypeSymbol containingType,
        INamedTypeSymbol? historyType,
        string? configuredName,
        out ISymbol? historyMember,
        out string reason)
    {
        historyMember = null;

        if (historyType is null)
        {
            reason = "Jewelry.EditingSystem.History is not referenced";
            return false;
        }

        if (string.IsNullOrWhiteSpace(configuredName))
        {
            reason = "the configured member name is empty";
            return false;
        }

        var members = containingType.GetMembers(configuredName!)
            .Where(static member => member is IFieldSymbol or IPropertySymbol)
            .ToArray();

        if (members.Length == 1)
        {
            historyMember = members[0];
        }
        else if (members.Length == 0)
        {
            var parameters = containingType.InstanceConstructors
                .Where(static constructor => constructor.DeclaringSyntaxReferences.Any(static reference =>
                    reference.GetSyntax() is TypeDeclarationSyntax declaration &&
                    declaration.ChildNodes().OfType<ParameterListSyntax>().Any()))
                .SelectMany(static constructor => constructor.Parameters)
                .Where(parameter => parameter.Name == configuredName)
                .ToArray();

            if (parameters.Length == 1)
                historyMember = parameters[0];
        }

        if (historyMember is null)
        {
            reason = "the name does not uniquely identify a field, property, or primary constructor parameter";
            return false;
        }

        var memberType = historyMember switch
        {
            IFieldSymbol field => field.Type,
            IPropertySymbol property => property.Type,
            IParameterSymbol parameter => parameter.Type,
            _ => throw new InvalidOperationException()
        };

        if (historyMember.IsStatic)
        {
            reason = "the member must be an instance member";
            return false;
        }

        if (historyMember is IPropertySymbol { GetMethod: null })
        {
            reason = "the property must have a getter";
            return false;
        }

        if (memberType.NullableAnnotation == NullableAnnotation.Annotated)
        {
            reason = "the member must be non-nullable";
            return false;
        }

        for (var type = memberType as INamedTypeSymbol; type is not null; type = type.BaseType)
        {
            if (SymbolEqualityComparer.Default.Equals(type, historyType))
            {
                reason = "";
                return true;
            }
        }

        reason = $"the member type must derive from {HistoryTypeName}";
        return false;
    }

    private static bool HasReservedHookConflict(
        INamedTypeSymbol containingType,
        string hookName,
        ITypeSymbol propertyType)
    {
        return containingType.GetMembers(hookName)
            .OfType<IMethodSymbol>()
            .Any(method =>
                method.Parameters.Length == 1 &&
                SymbolEqualityComparer.Default.Equals(method.Parameters[0].Type, propertyType) &&
                method.DeclaringSyntaxReferences.Length > 0);
    }

    private static bool AreAllContainingTypesPartial(INamedTypeSymbol type)
    {
        for (var current = type; current is not null; current = current.ContainingType)
        {
            if (current.DeclaringSyntaxReferences.Length == 0 ||
                current.DeclaringSyntaxReferences.Any(static reference =>
                    reference.GetSyntax() is not TypeDeclarationSyntax declaration ||
                    !declaration.Modifiers.Any(SyntaxKind.PartialKeyword)))
            {
                return false;
            }
        }

        return true;
    }

    private static bool HasAttribute(ISymbol symbol, string metadataName)
    {
        return symbol.GetAttributes().Any(attribute => attribute.AttributeClass?.ToDisplayString() == metadataName);
    }

    private static string GetGeneratedPropertyName(string fieldName)
    {
        var name = fieldName.StartsWith("m_", StringComparison.Ordinal)
            ? fieldName.Substring(2)
            : fieldName.TrimStart('_');

        if (name.Length == 0)
            return "";

        return char.ToUpper(name[0], CultureInfo.InvariantCulture) + name.Substring(1);
    }

    private static string GenerateSource(INamedTypeSymbol containingType, List<PropertyModel> properties)
    {
        var builder = new StringBuilder();
        builder.AppendLine("// <auto-generated/>");
        builder.AppendLine("#nullable enable");
        builder.AppendLine();

        if (!containingType.ContainingNamespace.IsGlobalNamespace)
        {
            builder.Append("namespace ");
            builder.Append(containingType.ContainingNamespace.ToDisplayString());
            builder.AppendLine(";");
            builder.AppendLine();
        }

        var hierarchy = new Stack<INamedTypeSymbol>();
        for (var type = containingType; type is not null; type = type.ContainingType)
            hierarchy.Push(type);

        var indent = 0;
        foreach (var type in hierarchy)
        {
            AppendIndent(builder, indent);
            builder.Append("partial ");
            builder.Append(GetTypeKind(type));
            builder.Append(' ');
            builder.Append(EscapeIdentifier(type.Name));

            if (type.TypeParameters.Length > 0)
            {
                builder.Append('<');
                builder.Append(string.Join(", ", type.TypeParameters.Select(parameter => EscapeIdentifier(parameter.Name))));
                builder.Append('>');
            }

            builder.AppendLine();
            AppendIndent(builder, indent);
            builder.AppendLine("{");
            indent++;
        }

        var primaryConstructorHistory = properties.FirstOrDefault(
            static property => property.HistoryParameterName is not null);
        if (primaryConstructorHistory.HistoryParameterName is not null)
        {
            AppendIndent(builder, indent);
            builder.Append("private global::");
            builder.Append(HistoryTypeName);
            builder.Append(' ');
            builder.Append(EscapeIdentifier(primaryConstructorHistory.HistoryAccessorName!));
            builder.Append(" => ");
            builder.Append(EscapeIdentifier(primaryConstructorHistory.HistoryParameterName));
            builder.AppendLine(";");
            builder.AppendLine();
        }

        foreach (var property in properties.OrderBy(static property => property.PropertyName, StringComparer.Ordinal))
        {
            var propertyName = EscapeIdentifier(property.PropertyName);
            var hookName = EscapeIdentifier($"On{property.PropertyName}Changing");
            var propertyType = property.PropertyType.ToDisplayString(TypeDisplayFormat);

            AppendIndent(builder, indent);
            builder.Append("partial void ");
            builder.Append(hookName);
            builder.Append('(');
            builder.Append(propertyType);
            builder.AppendLine(" value)");
            AppendIndent(builder, indent);
            builder.AppendLine("{");
            AppendIndent(builder, indent + 1);
            builder.Append("var editingHistory = ");
            builder.Append(property.HistoryAccessExpression);
            builder.AppendLine(";");
            AppendIndent(builder, indent + 1);
            builder.AppendLine("if (editingHistory.IsInUndoing)");
            AppendIndent(builder, indent + 2);
            builder.AppendLine("return;");
            builder.AppendLine();
            AppendIndent(builder, indent + 1);
            builder.Append("editingHistory.RecordPropertyChange(value => this.");
            builder.Append(propertyName);
            builder.Append(" = value, this.");
            builder.Append(propertyName);
            builder.AppendLine(", value);");
            AppendIndent(builder, indent);
            builder.AppendLine("}");
            builder.AppendLine();
        }

        while (indent > 0)
        {
            indent--;
            AppendIndent(builder, indent);
            builder.AppendLine("}");
        }

        return builder.ToString();
    }

    private static string GetTypeKind(INamedTypeSymbol type)
    {
        if (type.IsRecord)
            return type.TypeKind == TypeKind.Struct ? "record struct" : "record";

        return type.TypeKind switch
        {
            TypeKind.Struct => "struct",
            TypeKind.Interface => "interface",
            _ => "class"
        };
    }

    private static string EscapeIdentifier(string identifier)
    {
        return SyntaxFacts.GetKeywordKind(identifier) != SyntaxKind.None ||
               SyntaxFacts.GetContextualKeywordKind(identifier) != SyntaxKind.None
            ? "@" + identifier
            : identifier;
    }

    private static string GetHintName(INamedTypeSymbol type)
    {
        var name = type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var builder = new StringBuilder(name.Length + 20);

        foreach (var character in name)
            builder.Append(char.IsLetterOrDigit(character) ? character : '_');

        builder.Append(".Undoable.g.cs");
        return builder.ToString();
    }

    private static string GetUniqueHistoryAccessorName(INamedTypeSymbol containingType)
    {
        var name = "__jewelryEditingHistory";
        while (containingType.GetMembers(name).Length > 0)
            name += "_";

        return name;
    }

    private static void AppendIndent(StringBuilder builder, int indent)
    {
        builder.Append(' ', indent * 4);
    }

    private static readonly SymbolDisplayFormat TypeDisplayFormat = new(
        globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Included,
        typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
        genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
        miscellaneousOptions:
            SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers |
            SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier |
            SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

    private readonly struct Candidate
    {
        public Candidate(ISymbol symbol, Location? location)
        {
            Symbol = symbol;
            Location = location;
        }

        public ISymbol Symbol { get; }
        public Location? Location { get; }
    }

    private readonly struct PropertyModel
    {
        public PropertyModel(
            string propertyName,
            ITypeSymbol propertyType,
            string historyAccessExpression,
            string? historyParameterName,
            string? historyAccessorName)
        {
            PropertyName = propertyName;
            PropertyType = propertyType;
            HistoryAccessExpression = historyAccessExpression;
            HistoryParameterName = historyParameterName;
            HistoryAccessorName = historyAccessorName;
        }

        public string PropertyName { get; }
        public ITypeSymbol PropertyType { get; }
        public string HistoryAccessExpression { get; }
        public string? HistoryParameterName { get; }
        public string? HistoryAccessorName { get; }
    }
}
