using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using SpecTreeGenerator.CodeAnalysis.Abstractions;
using SpecTreeGenerator.Common.Models;

namespace SpecTreeGenerator.CodeAnalysis.Roslyn
{
    /// <summary>
    /// Roslyn-based analysis backend for C#. Produces the language-agnostic
    /// intermediate representation from C# source files, preserving the
    /// symbol-id scheme used by the pipeline ("namespace:...", "class:...",
    /// "method:...", "property:...", "field:...", "interface:...", "enum:...").
    /// </summary>
    public class RoslynAnalysisBackend : ILanguageAnalysisBackend
    {
        /// <inheritdoc />
        public string Language => "csharp";

        /// <inheritdoc />
        public IReadOnlyCollection<string> SupportedExtensions { get; } = new[] { ".cs" };

        /// <inheritdoc />
        public async Task<ParsedSource> ParseAsync(string filePath)
        {
            if (!System.IO.File.Exists(filePath))
            {
                throw new System.IO.FileNotFoundException($"File not found: {filePath}");
            }

            var code = await System.IO.File.ReadAllTextAsync(filePath);
            var syntaxTree = CSharpSyntaxTree.ParseText(code, path: filePath);
            var compilation = CSharpCompilation.Create("SymbolAnalysis")
                .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
                .AddSyntaxTrees(syntaxTree);
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var root = await syntaxTree.GetRootAsync();

            var declarations = new List<DeclarationNode>();
            var references = new List<ReferenceNode>();

            CollectDeclarations(root, semanticModel, declarations, filePath);
            CollectReferences(root, semanticModel, references);

            return new ParsedSource
            {
                FilePath = filePath,
                Language = Language,
                Declarations = declarations,
                References = references
            };
        }

        /// <inheritdoc />
        public string? ResolveSymbol(string name, IReadOnlyList<ParsedSource> context)
        {
            // The Roslyn backend resolves references during parsing via its
            // semantic model; name-based resolution after the fact is not
            // needed and not attempted.
            return null;
        }

        private static void CollectDeclarations(
            SyntaxNode node,
            SemanticModel semanticModel,
            List<DeclarationNode> declarations,
            string filePath)
        {
            string? id = null;
            ComponentType type = ComponentType.Unknown;
            string name = string.Empty;
            SyntaxNode? spanNode = node;

            if (node is NamespaceDeclarationSyntax namespaceDecl)
            {
                var symbol = semanticModel.GetDeclaredSymbol(namespaceDecl);
                if (symbol != null)
                {
                    id = $"namespace:{symbol.ToDisplayString()}";
                    type = ComponentType.Namespace;
                    name = symbol.Name;
                }
            }
            else if (node is ClassDeclarationSyntax classDecl)
            {
                var symbol = semanticModel.GetDeclaredSymbol(classDecl);
                if (symbol != null)
                {
                    id = $"class:{symbol.ToDisplayString()}";
                    type = ComponentType.Class;
                    name = symbol.Name;
                }
            }
            else if (node is InterfaceDeclarationSyntax interfaceDecl)
            {
                var symbol = semanticModel.GetDeclaredSymbol(interfaceDecl);
                if (symbol != null)
                {
                    id = $"interface:{symbol.ToDisplayString()}";
                    type = ComponentType.Interface;
                    name = symbol.Name;
                }
            }
            else if (node is MethodDeclarationSyntax methodDecl)
            {
                var symbol = semanticModel.GetDeclaredSymbol(methodDecl);
                if (symbol != null)
                {
                    id = $"method:{symbol.ToDisplayString()}";
                    type = ComponentType.Method;
                    name = symbol.Name;
                }
            }
            else if (node is PropertyDeclarationSyntax propertyDecl)
            {
                var symbol = semanticModel.GetDeclaredSymbol(propertyDecl);
                if (symbol != null)
                {
                    id = $"property:{symbol.ToDisplayString()}";
                    type = ComponentType.Property;
                    name = symbol.Name;
                }
            }
            else if (node is EnumDeclarationSyntax enumDecl)
            {
                var symbol = semanticModel.GetDeclaredSymbol(enumDecl);
                if (symbol != null)
                {
                    id = $"enum:{symbol.ToDisplayString()}";
                    type = ComponentType.Enum;
                    name = symbol.Name;
                }
            }

            if (id != null)
            {
                var parentSymbol = semanticModel.GetEnclosingSymbol(node.SpanStart);
                declarations.Add(new DeclarationNode
                {
                    SymbolId = id,
                    Name = name,
                    FullName = name,
                    Type = type,
                    ParentSymbolId = parentSymbol != null && parentSymbol.Kind != SymbolKind.Namespace && parentSymbol.Kind != SymbolKind.Assembly
                        ? SymbolIdFor(parentSymbol)
                        : null,
                    Span = ToSpan(node.Span)
                });
            }

            foreach (var child in node.ChildNodes())
            {
                CollectDeclarations(child, semanticModel, declarations, filePath);
            }
        }

        private static string SymbolIdFor(ISymbol symbol)
        {
            return symbol.Kind switch
            {
                SymbolKind.NamedType when ((INamedTypeSymbol)symbol).TypeKind == TypeKind.Class => $"class:{symbol.ToDisplayString()}",
                SymbolKind.NamedType when ((INamedTypeSymbol)symbol).TypeKind == TypeKind.Interface => $"interface:{symbol.ToDisplayString()}",
                SymbolKind.NamedType when ((INamedTypeSymbol)symbol).TypeKind == TypeKind.Enum => $"enum:{symbol.ToDisplayString()}",
                SymbolKind.Method => $"method:{symbol.ToDisplayString()}",
                SymbolKind.Property => $"property:{symbol.ToDisplayString()}",
                SymbolKind.Namespace => $"namespace:{symbol.ToDisplayString()}",
                _ => symbol.ToDisplayString()
            };
        }

        private static void CollectReferences(
            SyntaxNode node,
            SemanticModel semanticModel,
            List<ReferenceNode> references)
        {
            // Heritage clauses: base classes and interfaces
            if (node is ClassDeclarationSyntax classDecl && classDecl.BaseList != null)
            {
                var containingSymbol = semanticModel.GetDeclaredSymbol(classDecl);
                foreach (var baseType in classDecl.BaseList.Types)
                {
                    var baseSymbol = semanticModel.GetSymbolInfo(baseType.Type).Symbol as INamedTypeSymbol;
                    if (baseSymbol != null)
                    {
                        references.Add(new ReferenceNode
                        {
                            ResolvedSymbolId = SymbolIdFor(baseSymbol),
                            Name = baseType.Type.ToString(),
                            Kind = baseSymbol.TypeKind == TypeKind.Class ? ReferenceKind.BaseClass : ReferenceKind.InterfaceImplementation,
                            ContainingSymbolId = containingSymbol != null ? SymbolIdFor(containingSymbol) : null,
                            Span = ToSpan(baseType.Span)
                        });
                    }
                }
            }
            else if (node is InvocationExpressionSyntax invocation)
            {
                var containingSymbol = semanticModel.GetEnclosingSymbol(invocation.SpanStart);
                var targetSymbol = semanticModel.GetSymbolInfo(invocation).Symbol;
                if (targetSymbol is IMethodSymbol methodSymbol &&
                    methodSymbol.MethodKind != MethodKind.BuiltinOperator &&
                    methodSymbol.MethodKind != MethodKind.UserDefinedOperator)
                {
                    references.Add(new ReferenceNode
                    {
                        ResolvedSymbolId = SymbolIdFor(methodSymbol),
                        Name = invocation.Expression.ToString(),
                        Kind = ReferenceKind.MethodCall,
                        ContainingSymbolId = containingSymbol != null && containingSymbol.Kind == SymbolKind.Method
                            ? SymbolIdFor(containingSymbol)
                            : null,
                        Span = ToSpan(invocation.Span)
                    });
                }
            }

            foreach (var child in node.ChildNodes())
            {
                CollectReferences(child, semanticModel, references);
            }
        }

        private static TextSpan ToSpan(Microsoft.CodeAnalysis.Text.TextSpan span)
        {
            return new TextSpan(span.Start, span.End);
        }
    }
}