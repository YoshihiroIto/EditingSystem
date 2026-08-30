using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Jewelry.EditingSystem.SourceGenerators;

[Generator(LanguageNames.CSharp)]
public sealed class UndoablePropertyGenerator : IIncrementalGenerator
{
    private const string UndoableAttributeName = "Jewelry.EditingSystem.Annotations.UndoableAttribute";
    private const string EditingHistoryAttributeName = "Jewelry.EditingSystem.Annotations.EditingHistoryAttribute";
    private const string HistoryTypeName = "Jewelry.EditingSystem.History";
    private const string NotifyPropertyChangedTypeName = "System.ComponentModel.INotifyPropertyChanged";
    private const string PropertyChangedEventHandlerTypeName = "System.ComponentModel.PropertyChangedEventHandler";
    private const string PropertyChangedEventArgsTypeName = "System.ComponentModel.PropertyChangedEventArgs";

    private static readonly DiagnosticDescriptor UnsupportedTarget = new(
        "JES001",
        "Unsupported undoable property",
        "'{0}' cannot be generated as an undoable property: {1}",
        "Jewelry.EditingSystem",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor MissingHistoryConfiguration = new(
        "JES002",
        "EditingHistory configuration is required",
        "Type '{0}' must have exactly one EditingHistoryAttribute",
        "Jewelry.EditingSystem",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor InvalidHistoryMember = new(
        "JES003",
        "Invalid editing history member",
        "EditingHistory member '{0}' on type '{1}' is invalid: {2}",
        "Jewelry.EditingSystem",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor TypeMustBePartial = new(
        "JES004",
        "Containing types must be partial",
        "Type '{0}' and all containing types must be partial",
        "Jewelry.EditingSystem",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor NotificationUnavailable = new(
        "JES005",
        "PropertyChanged notification cannot be raised",
        "Type '{0}' implements INotifyPropertyChanged, but no accessible RaisePropertyChanged/OnPropertyChanged method or locally declared PropertyChanged event was found; '{1}' remains undoable but will not raise PropertyChanged",
        "Jewelry.EditingSystem",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var candidates = context.SyntaxProvider.ForAttributeWithMetadataName(
            UndoableAttributeName,
            static (node, _) => node is PropertyDeclarationSyntax,
            static (generatorContext, _) => new Candidate(
                (IPropertySymbol)generatorContext.TargetSymbol,
                generatorContext.TargetNode.GetLocation()));

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

        var historyType = compilation.GetTypeByMetadataName(HistoryTypeName);
        var notifyPropertyChangedType = compilation.GetTypeByMetadataName(NotifyPropertyChangedTypeName);
        var propertyChangedEventHandlerType = compilation.GetTypeByMetadataName(PropertyChangedEventHandlerTypeName);
        var propertyChangedEventArgsType = compilation.GetTypeByMetadataName(PropertyChangedEventArgsTypeName);

        var groupedProperties = new Dictionary<INamedTypeSymbol, List<PropertyModel>>(SymbolEqualityComparer.Default);

        foreach (var candidate in candidates)
        {
            var property = candidate.Property;
            var containingType = property.ContainingType;

            if (!AreAllContainingTypesPartial(containingType))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    TypeMustBePartial,
                    candidate.Location,
                    containingType.ToDisplayString()));
                continue;
            }

            if (containingType.TypeKind != TypeKind.Class)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    UnsupportedTarget,
                    candidate.Location,
                    property.Name,
                    "the containing type must be a class or record class"));
                continue;
            }

            if (!TryValidateProperty(property, out var declaration, out var unsupportedReason))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    UnsupportedTarget,
                    candidate.Location,
                    property.Name,
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

            if (!TryGetHistoryMember(
                    containingType,
                    historyType,
                    configuredName,
                    out var historyMember,
                    out var historyReason))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    InvalidHistoryMember,
                    candidate.Location,
                    configuredName ?? "<null>",
                    containingType.ToDisplayString(),
                    historyReason));
                continue;
            }

            var notification = ResolveNotification(
                compilation,
                containingType,
                notifyPropertyChangedType,
                propertyChangedEventHandlerType,
                propertyChangedEventArgsType);

            if (notification.Kind == NotificationKind.Unavailable)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    NotificationUnavailable,
                    candidate.Location,
                    containingType.ToDisplayString(),
                    property.Name));
                notification = default;
            }

            if (!groupedProperties.TryGetValue(containingType, out var properties))
            {
                properties = new List<PropertyModel>();
                groupedProperties.Add(containingType, properties);
            }

            var historyParameterName = historyMember is IParameterSymbol parameter
                ? parameter.Name
                : null;
            var historyAccessorName = historyParameterName is { }
                ? GetUniqueMemberName(containingType, "__jewelryEditingHistory")
                : null;
            var historyAccessExpression = historyAccessorName is { }
                ? $"this.{EscapeIdentifier(historyAccessorName)}"
                : $"this.{EscapeIdentifier(historyMember!.Name)}";

            properties.Add(new PropertyModel(
                property.Name,
                property.Type,
                string.Join(" ", declaration.Modifiers.Select(static modifier => modifier.Text)),
                historyAccessExpression,
                historyParameterName,
                historyAccessorName,
                GetUniqueMemberName(containingType, $"__jewelryEditingValue{property.Name}"),
                GetUniqueMemberName(containingType, $"__jewelryEditingSetter{property.Name}"),
                GetUniqueMemberName(containingType, $"__jewelryEditingPropertyChangedArgs{property.Name}"),
                GetAccessorModifiers(declaration, SyntaxKind.GetAccessorDeclaration),
                GetAccessorModifiers(declaration, SyntaxKind.SetAccessorDeclaration),
                notification));
        }

        foreach (var pair in groupedProperties)
            context.AddSource(GetHintName(pair.Key), SourceText.From(GenerateSource(pair.Key, pair.Value), Encoding.UTF8));
    }

    private static bool TryValidateProperty(
        IPropertySymbol property,
        out PropertyDeclarationSyntax declaration,
        out string unsupportedReason)
    {
        declaration = null!;

        if (property.IsStatic)
        {
            unsupportedReason = "static properties are not supported";
            return false;
        }

        if (property.IsIndexer)
        {
            unsupportedReason = "indexers are not supported";
            return false;
        }

        if (property.IsAbstract)
        {
            unsupportedReason = "abstract properties are not supported";
            return false;
        }

        if (property.RefKind != RefKind.None)
        {
            unsupportedReason = "ref-returning properties are not supported";
            return false;
        }

        declaration = property.DeclaringSyntaxReferences
            .Select(static reference => reference.GetSyntax())
            .OfType<PropertyDeclarationSyntax>()
            .FirstOrDefault(static syntax => syntax.Modifiers.Any(SyntaxKind.PartialKeyword))!;

        if (declaration is null)
        {
            unsupportedReason = "the property must be declared partial";
            return false;
        }

        if (property.GetMethod is null || property.SetMethod is null)
        {
            unsupportedReason = "the property must have both get and set accessors";
            return false;
        }

        if (property.SetMethod.IsInitOnly)
        {
            unsupportedReason = "init-only properties cannot be changed by undo or redo";
            return false;
        }

        if (declaration.Initializer is not null ||
            declaration.ExpressionBody is not null ||
            declaration.AccessorList is null ||
            declaration.AccessorList.Accessors.Any(static accessor =>
                accessor.Body is not null || accessor.ExpressionBody is not null))
        {
            unsupportedReason = "the defining partial property must not provide an implementation or initializer";
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

        for (var type = memberType as INamedTypeSymbol; type is { }; type = type.BaseType)
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

    private static NotificationModel ResolveNotification(
        Compilation compilation,
        INamedTypeSymbol containingType,
        INamedTypeSymbol? notifyPropertyChangedType,
        INamedTypeSymbol? propertyChangedEventHandlerType,
        INamedTypeSymbol? propertyChangedEventArgsType)
    {
        if (notifyPropertyChangedType is null ||
            !containingType.AllInterfaces.Any(
                implemented => SymbolEqualityComparer.Default.Equals(implemented, notifyPropertyChangedType)))
        {
            return default;
        }

        var stringType = compilation.GetSpecialType(SpecialType.System_String);

        var method = FindNotificationMethod(compilation, containingType, "RaisePropertyChanged", stringType);
        if (method is not null)
            return new NotificationModel(NotificationKind.StringMethod, method.Name);

        method = FindNotificationMethod(compilation, containingType, "OnPropertyChanged", stringType);
        if (method is not null)
            return new NotificationModel(NotificationKind.StringMethod, method.Name);

        if (propertyChangedEventArgsType is not null)
        {
            method = FindNotificationMethod(compilation, containingType, "RaisePropertyChanged", propertyChangedEventArgsType);
            if (method is not null)
                return new NotificationModel(NotificationKind.EventArgsMethod, method.Name);

            method = FindNotificationMethod(compilation, containingType, "OnPropertyChanged", propertyChangedEventArgsType);
            if (method is not null)
                return new NotificationModel(NotificationKind.EventArgsMethod, method.Name);
        }

        if (propertyChangedEventHandlerType is not null)
        {
            var localEvent = containingType.GetMembers("PropertyChanged")
                .OfType<IEventSymbol>()
                .FirstOrDefault(@event =>
                    SymbolEqualityComparer.Default.Equals(@event.ContainingType, containingType) &&
                    SymbolEqualityComparer.Default.Equals(@event.Type, propertyChangedEventHandlerType) &&
                    @event.DeclaringSyntaxReferences.Any(static reference =>
                        reference.GetSyntax() is VariableDeclaratorSyntax));

            if (localEvent is not null)
                return new NotificationModel(NotificationKind.DirectEvent, localEvent.Name);
        }

        return new NotificationModel(NotificationKind.Unavailable, null);
    }

    private static IMethodSymbol? FindNotificationMethod(
        Compilation compilation,
        INamedTypeSymbol containingType,
        string methodName,
        ITypeSymbol parameterType)
    {
        for (INamedTypeSymbol? type = containingType; type is not null; type = type.BaseType)
        {
            var method = type.GetMembers(methodName)
                .OfType<IMethodSymbol>()
                .FirstOrDefault(candidate =>
                    IsNotificationMethod(compilation, containingType, candidate, parameterType));

            if (method is not null)
                return method;
        }

        return null;
    }

    private static bool IsNotificationMethod(
        Compilation compilation,
        INamedTypeSymbol containingType,
        IMethodSymbol method,
        ITypeSymbol parameterType)
    {
        return !method.IsStatic &&
               !method.IsGenericMethod &&
               method.MethodKind == MethodKind.Ordinary &&
               method.ReturnsVoid &&
               method.Parameters.Length == 1 &&
               method.Parameters[0].RefKind == RefKind.None &&
               SymbolEqualityComparer.Default.Equals(method.Parameters[0].Type, parameterType) &&
               compilation.IsSymbolAccessibleWithin(method, containingType);
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
            ++indent;
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
            var propertyType = property.PropertyType.ToDisplayString(TypeDisplayFormat);

            AppendIndent(builder, indent);
            builder.Append("private ");
            builder.Append(propertyType);
            builder.Append(' ');
            builder.Append(EscapeIdentifier(property.BackingFieldName));
            builder.AppendLine(" = default!;");

            AppendIndent(builder, indent);
            builder.Append("private global::System.Action<");
            builder.Append(propertyType);
            builder.Append(">? ");
            builder.Append(EscapeIdentifier(property.SetterFieldName));
            builder.AppendLine(";");

            if (property.Notification.RequiresEventArgs)
            {
                AppendIndent(builder, indent);
                builder.Append("private static readonly global::System.ComponentModel.PropertyChangedEventArgs ");
                builder.Append(EscapeIdentifier(property.EventArgsFieldName));
                builder.Append(" = new(nameof(");
                builder.Append(EscapeIdentifier(property.PropertyName));
                builder.AppendLine("));");
            }

            builder.AppendLine();
        }

        foreach (var property in properties.OrderBy(static property => property.PropertyName, StringComparer.Ordinal))
            AppendProperty(builder, indent, property);

        while (indent > 0)
        {
            --indent;
            AppendIndent(builder, indent);
            builder.AppendLine("}");
        }

        return builder.ToString();
    }

    private static void AppendProperty(StringBuilder builder, int indent, PropertyModel property)
    {
        var propertyName = EscapeIdentifier(property.PropertyName);
        var backingFieldName = EscapeIdentifier(property.BackingFieldName);
        var setterFieldName = EscapeIdentifier(property.SetterFieldName);
        var propertyType = property.PropertyType.ToDisplayString(TypeDisplayFormat);

        AppendIndent(builder, indent);
        builder.Append(property.Modifiers);
        builder.Append(' ');
        builder.Append(propertyType);
        builder.Append(' ');
        builder.Append(propertyName);
        builder.AppendLine();
        AppendIndent(builder, indent);
        builder.AppendLine("{");

        AppendIndent(builder, indent + 1);
        if (property.GetAccessorModifiers.Length > 0)
        {
            builder.Append(property.GetAccessorModifiers);
            builder.Append(' ');
        }
        builder.Append("get => this.");
        builder.Append(backingFieldName);
        builder.AppendLine(";");

        AppendIndent(builder, indent + 1);
        if (property.SetAccessorModifiers.Length > 0)
        {
            builder.Append(property.SetAccessorModifiers);
            builder.Append(' ');
        }
        builder.AppendLine("set");
        AppendIndent(builder, indent + 1);
        builder.AppendLine("{");

        AppendIndent(builder, indent + 2);
        builder.Append("if (global::System.Collections.Generic.EqualityComparer<");
        builder.Append(propertyType);
        builder.Append(">.Default.Equals(this.");
        builder.Append(backingFieldName);
        builder.AppendLine(", value))");
        AppendIndent(builder, indent + 3);
        builder.AppendLine("return;");
        builder.AppendLine();

        AppendIndent(builder, indent + 2);
        builder.Append("var editingHistory = ");
        builder.Append(property.HistoryAccessExpression);
        builder.AppendLine(";");
        AppendIndent(builder, indent + 2);
        builder.Append("var oldValue = this.");
        builder.Append(backingFieldName);
        builder.AppendLine(";");
        AppendIndent(builder, indent + 2);
        builder.Append("this.");
        builder.Append(backingFieldName);
        builder.AppendLine(" = value;");

        AppendNotification(builder, indent + 2, property);

        builder.AppendLine();
        AppendIndent(builder, indent + 2);
        builder.AppendLine("editingHistory.RecordAppliedPropertyChange(");
        AppendIndent(builder, indent + 3);
        builder.AppendLine("this,");
        AppendIndent(builder, indent + 3);
        builder.Append("nameof(");
        builder.Append(propertyName);
        builder.AppendLine("),");
        AppendIndent(builder, indent + 3);
        builder.Append("(this.");
        builder.Append(setterFieldName);
        builder.Append(" ??= value => this.");
        builder.Append(propertyName);
        builder.AppendLine(" = value),");
        AppendIndent(builder, indent + 3);
        builder.AppendLine("oldValue,");
        AppendIndent(builder, indent + 3);
        builder.AppendLine("value);");

        AppendIndent(builder, indent + 1);
        builder.AppendLine("}");
        AppendIndent(builder, indent);
        builder.AppendLine("}");
        builder.AppendLine();
    }

    private static void AppendNotification(StringBuilder builder, int indent, PropertyModel property)
    {
        switch (property.Notification.Kind)
        {
            case NotificationKind.None:
                return;

            case NotificationKind.StringMethod:
                builder.AppendLine();
                AppendIndent(builder, indent);
                builder.Append("this.");
                builder.Append(EscapeIdentifier(property.Notification.MemberName!));
                builder.Append("(nameof(");
                builder.Append(EscapeIdentifier(property.PropertyName));
                builder.AppendLine("));");
                return;

            case NotificationKind.EventArgsMethod:
                builder.AppendLine();
                AppendIndent(builder, indent);
                builder.Append("this.");
                builder.Append(EscapeIdentifier(property.Notification.MemberName!));
                builder.Append('(');
                builder.Append(EscapeIdentifier(property.EventArgsFieldName));
                builder.AppendLine(");");
                return;

            case NotificationKind.DirectEvent:
                builder.AppendLine();
                AppendIndent(builder, indent);
                builder.Append("this.");
                builder.Append(EscapeIdentifier(property.Notification.MemberName!));
                builder.Append("?.Invoke(this, ");
                builder.Append(EscapeIdentifier(property.EventArgsFieldName));
                builder.AppendLine(");");
                return;
        }
    }

    private static string GetAccessorModifiers(PropertyDeclarationSyntax declaration, SyntaxKind accessorKind)
    {
        var accessor = declaration.AccessorList?.Accessors.FirstOrDefault(accessor => accessor.IsKind(accessorKind));
        return accessor is null
            ? ""
            : string.Join(" ", accessor.Modifiers.Select(static modifier => modifier.Text));
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

    private static string GetUniqueMemberName(INamedTypeSymbol containingType, string baseName)
    {
        var name = baseName;
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
        public Candidate(IPropertySymbol property, Location? location)
        {
            Property = property;
            Location = location;
        }

        public IPropertySymbol Property { get; }
        public Location? Location { get; }
    }

    private readonly struct NotificationModel
    {
        public NotificationModel(NotificationKind kind, string? memberName)
        {
            Kind = kind;
            MemberName = memberName;
        }

        public NotificationKind Kind { get; }
        public string? MemberName { get; }
        public bool RequiresEventArgs => Kind is NotificationKind.EventArgsMethod or NotificationKind.DirectEvent;
    }

    private enum NotificationKind
    {
        None,
        StringMethod,
        EventArgsMethod,
        DirectEvent,
        Unavailable
    }

    private readonly struct PropertyModel
    {
        public PropertyModel(
            string propertyName,
            ITypeSymbol propertyType,
            string modifiers,
            string historyAccessExpression,
            string? historyParameterName,
            string? historyAccessorName,
            string backingFieldName,
            string setterFieldName,
            string eventArgsFieldName,
            string getAccessorModifiers,
            string setAccessorModifiers,
            NotificationModel notification)
        {
            PropertyName = propertyName;
            PropertyType = propertyType;
            Modifiers = modifiers;
            HistoryAccessExpression = historyAccessExpression;
            HistoryParameterName = historyParameterName;
            HistoryAccessorName = historyAccessorName;
            BackingFieldName = backingFieldName;
            SetterFieldName = setterFieldName;
            EventArgsFieldName = eventArgsFieldName;
            GetAccessorModifiers = getAccessorModifiers;
            SetAccessorModifiers = setAccessorModifiers;
            Notification = notification;
        }

        public string PropertyName { get; }
        public ITypeSymbol PropertyType { get; }
        public string Modifiers { get; }
        public string HistoryAccessExpression { get; }
        public string? HistoryParameterName { get; }
        public string? HistoryAccessorName { get; }
        public string BackingFieldName { get; }
        public string SetterFieldName { get; }
        public string EventArgsFieldName { get; }
        public string GetAccessorModifiers { get; }
        public string SetAccessorModifiers { get; }
        public NotificationModel Notification { get; }
    }
}
