﻿using Microsoft.CodeAnalysis;
using INamedTypeSymbol = Microsoft.CodeAnalysis.INamedTypeSymbol;

namespace Jewelry.EditingSystem.CommunityToolkit.Mvvm;

[Generator(LanguageNames.CSharp)]
public class HistoryGenerator : IIncrementalGenerator
{
    private const string HistoryAttributeName = "HistoryAttribute";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(static context =>
        {
            context.AddSource("HistoryAttribute.cs", """
using System;
namespace Jewelry.EditingSystem.CommunityToolkit.Mvvm;

[AttributeUsage(AttributeTargets.Field)]
public sealed class HistoryAttribute : Attribute
{
}
""");
        });

        var source = context.SyntaxProvider.ForAttributeWithMetadataName(
            $"Jewelry.EditingSystem.CommunityToolkit.Mvvm.HistoryAttribute",
            predicate: static (node, _) => true,
            transform: static (context, token) => context);

        context.RegisterSourceOutput(source, Emit);
    }

    private static void Emit(SourceProductionContext context,
        GeneratorAttributeSyntaxContext source)
    {
        if (source.TargetSymbol is not IFieldSymbol fieldSymbol)
            return;

        var attribute = source.Attributes.FirstOrDefault(x => x.AttributeClass?.Name == HistoryAttributeName);
        if (attribute is null)
            return;

        // var data = ParseEnumExtensionsAttribute(attribute);
        //
        // var enumUnit = EnumUnit.Create(
        //     enumSymbol,
        //     extensionClassNamespace: data.ExtensionClassNamespace,
        //     extensionClassName: data.ExtensionClassName);

        context.AddSource("AAA.g.cs", "//AAA");
    }
}