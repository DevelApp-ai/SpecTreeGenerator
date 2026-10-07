using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using SpecTreeGenerator.Common.Models;
namespace SpecTreeGenerator.CodeAnalysis.Extractors
{
    /// <summary>
    /// Extracts code components from a syntax tree
    /// </summary>
    public class ComponentExtractor
    {
        /// <summary>
        /// Extracts code components from a syntax tree using the symbol table
        /// </summary>
        public static List<CodeComponent> ExtractComponents(SyntaxTree syntaxTree, SymbolAnalysis.SymbolTable symbolTable)
        {
            var components = new List<CodeComponent>();
            var root = syntaxTree.GetRoot();
            var filePath = syntaxTree.FilePath;

            // Extract namespaces
            ExtractNamespaces(root, symbolTable, components, filePath);

            // Extract classes
            ExtractClasses(root, symbolTable, components, filePath);

            // Extract interfaces
            ExtractInterfaces(root, symbolTable, components, filePath);

            // Extract methods
            ExtractMethods(root, symbolTable, components, filePath);

            // Extract properties
            ExtractProperties(root, symbolTable, components, filePath);

            // Extract fields
            ExtractFields(root, symbolTable, components, filePath);

            // Extract enums
            ExtractEnums(root, symbolTable, components, filePath);

            return components;
        }

        private static void ExtractNamespaces(SyntaxNode root, SymbolAnalysis.SymbolTable symbolTable, List<CodeComponent> components, string filePath)
        {
            var namespaces = root.DescendantNodes().OfType<NamespaceDeclarationSyntax>();
            foreach (var namespaceDecl in namespaces)
            {
                var symbol = symbolTable.GetSymbol(namespaceDecl);
                if (symbol != null)
                {
                    var id = symbolTable.GetId(symbol);
                    var component = new CodeComponent
                    {
                        Id = id ?? throw new InvalidOperationException("Symbol id unexpectedly null"),
                        Name = namespaceDecl.Name.ToString(),
                        Type = ComponentType.Namespace,
                        Namespace = namespaceDecl.Name.ToString(),
                        ParentId = string.Empty, // Namespaces are top-level
                        SourceFilePath = filePath,
                        StartLine = GetLineNumber(namespaceDecl.SpanStart, syntaxTree: namespaceDecl.SyntaxTree),
                        EndLine = GetLineNumber(namespaceDecl.Span.End, syntaxTree: namespaceDecl.SyntaxTree),
                        Signature = namespaceDecl.ToString(),
                        Documentation = ExtractDocumentation(namespaceDecl)
                    };
                    components.Add(component);
                }
            }
        }

        private static void ExtractClasses(SyntaxNode root, SymbolAnalysis.SymbolTable symbolTable, List<CodeComponent> components, string filePath)
        {
            var classes = root.DescendantNodes().OfType<ClassDeclarationSyntax>();
            foreach (var classDecl in classes)
            {
                var symbol = symbolTable.GetSymbol(classDecl);
                if (symbol != null)
                {
                    var id = symbolTable.GetId(symbol);
                    var parentId = GetParentId(classDecl, symbolTable);

                    var component = new CodeComponent
                    {
                        Id = id ?? throw new InvalidOperationException("Symbol id unexpectedly null"),
                        Name = classDecl.Identifier.Text,
                        Type = ComponentType.Class,
                        Namespace = GetNamespace(classDecl),
                        ParentId = parentId ?? string.Empty,
                        SourceFilePath = filePath,
                        StartLine = GetLineNumber(classDecl.SpanStart, syntaxTree: classDecl.SyntaxTree),
                        EndLine = GetLineNumber(classDecl.Span.End, syntaxTree: classDecl.SyntaxTree),
                        Signature = classDecl.Modifiers.ToString() + " class " + classDecl.Identifier.Text,
                        Documentation = ExtractDocumentation(classDecl)
                    };
                    components.Add(component);
                }
            }
        }

        private static void ExtractInterfaces(SyntaxNode root, SymbolAnalysis.SymbolTable symbolTable, List<CodeComponent> components, string filePath)
        {
            var interfaces = root.DescendantNodes().OfType<InterfaceDeclarationSyntax>();
            foreach (var interfaceDecl in interfaces)
            {
                var symbol = symbolTable.GetSymbol(interfaceDecl);
                if (symbol != null)
                {
                    var id = symbolTable.GetId(symbol);
                    var parentId = GetParentId(interfaceDecl, symbolTable);

                    var component = new CodeComponent
                    {
                        Id = id ?? throw new InvalidOperationException("Symbol id unexpectedly null"),
                        Name = interfaceDecl.Identifier.Text,
                        Type = ComponentType.Interface,
                        Namespace = GetNamespace(interfaceDecl),
                        ParentId = parentId ?? string.Empty,
                        SourceFilePath = filePath,
                        StartLine = GetLineNumber(interfaceDecl.SpanStart, syntaxTree: interfaceDecl.SyntaxTree),
                        EndLine = GetLineNumber(interfaceDecl.Span.End, syntaxTree: interfaceDecl.SyntaxTree),
                        Signature = interfaceDecl.Modifiers.ToString() + " interface " + interfaceDecl.Identifier.Text,
                        Documentation = ExtractDocumentation(interfaceDecl)
                    };
                    components.Add(component);
                }
            }
        }

        private static void ExtractMethods(SyntaxNode root, SymbolAnalysis.SymbolTable symbolTable, List<CodeComponent> components, string filePath)
        {
            var methods = root.DescendantNodes().OfType<MethodDeclarationSyntax>();
            foreach (var methodDecl in methods)
            {
                var symbol = symbolTable.GetSymbol(methodDecl);
                if (symbol != null)
                {
                    var id = symbolTable.GetId(symbol);
                    var parentId = GetParentId(methodDecl, symbolTable);

                    var component = new CodeComponent
                    {
                        Id = id ?? throw new InvalidOperationException("Symbol id unexpectedly null"),
                        Name = methodDecl.Identifier.Text,
                        Type = ComponentType.Method,
                        Namespace = GetNamespace(methodDecl),
                        ParentId = parentId ?? string.Empty,
                        SourceFilePath = filePath,
                        StartLine = GetLineNumber(methodDecl.SpanStart, syntaxTree: methodDecl.SyntaxTree),
                        EndLine = GetLineNumber(methodDecl.Span.End, syntaxTree: methodDecl.SyntaxTree),
                        Signature = methodDecl.Modifiers.ToString() + " " + methodDecl.ReturnType.ToString() + " " + methodDecl.Identifier.Text + methodDecl.ParameterList.ToString(),
                        Documentation = ExtractDocumentation(methodDecl)
                    };
                    components.Add(component);
                }
            }
        }

        private static void ExtractProperties(SyntaxNode root, SymbolAnalysis.SymbolTable symbolTable, List<CodeComponent> components, string filePath)
        {
            var properties = root.DescendantNodes().OfType<PropertyDeclarationSyntax>();
            foreach (var propertyDecl in properties)
            {
                var symbol = symbolTable.GetSymbol(propertyDecl);
                if (symbol != null)
                {
                    var id = symbolTable.GetId(symbol);
                    var parentId = GetParentId(propertyDecl, symbolTable);

                    var component = new CodeComponent
                    {
                        Id = id ?? throw new InvalidOperationException("Symbol id unexpectedly null"),
                        Name = propertyDecl.Identifier.Text,
                        Type = ComponentType.Property,
                        Namespace = GetNamespace(propertyDecl),
                        ParentId = parentId ?? string.Empty,
                        SourceFilePath = filePath,
                        StartLine = GetLineNumber(propertyDecl.SpanStart, syntaxTree: propertyDecl.SyntaxTree),
                        EndLine = GetLineNumber(propertyDecl.Span.End, syntaxTree: propertyDecl.SyntaxTree),
                        Signature = propertyDecl.Modifiers.ToString() + " " + propertyDecl.Type.ToString() + " " + propertyDecl.Identifier.Text,
                        Documentation = ExtractDocumentation(propertyDecl)
                    };
                    components.Add(component);
                }
            }
        }

        private static void ExtractFields(SyntaxNode root, SymbolAnalysis.SymbolTable symbolTable, List<CodeComponent> components, string filePath)
        {
            var fields = root.DescendantNodes().OfType<FieldDeclarationSyntax>();
            foreach (var fieldDecl in fields)
            {
                foreach (var variable in fieldDecl.Declaration.Variables)
                {
                    var symbol = symbolTable.GetSymbol(variable);
                    if (symbol != null)
                    {
                        var id = symbolTable.GetId(symbol);
                        var parentId = GetParentId(fieldDecl, symbolTable);

                        var component = new CodeComponent
                        {
                            Id = id ?? throw new InvalidOperationException("Symbol id unexpectedly null"),
                            Name = variable.Identifier.Text,
                            Type = ComponentType.Field,
                            Namespace = GetNamespace(fieldDecl),
                            ParentId = parentId ?? string.Empty,
                            SourceFilePath = filePath,
                            StartLine = GetLineNumber(fieldDecl.SpanStart, syntaxTree: fieldDecl.SyntaxTree),
                            EndLine = GetLineNumber(fieldDecl.Span.End, syntaxTree: fieldDecl.SyntaxTree),
                            Signature = fieldDecl.Modifiers.ToString() + " " + fieldDecl.Declaration.Type.ToString() + " " + variable.Identifier.Text,
                            Documentation = ExtractDocumentation(fieldDecl)
                        };
                        components.Add(component);
                    }
                }
            }
        }

        private static void ExtractEnums(SyntaxNode root, SymbolAnalysis.SymbolTable symbolTable, List<CodeComponent> components, string filePath)
        {
            var enums = root.DescendantNodes().OfType<EnumDeclarationSyntax>();
            foreach (var enumDecl in enums)
            {
                var symbol = symbolTable.GetSymbol(enumDecl);
                if (symbol != null)
                {
                    var id = symbolTable.GetId(symbol);
                    var parentId = GetParentId(enumDecl, symbolTable);

                    var component = new CodeComponent
                    {
                        Id = id ?? throw new InvalidOperationException("Symbol id unexpectedly null"),
                        Name = enumDecl.Identifier.Text,
                        Type = ComponentType.Enum,
                        Namespace = GetNamespace(enumDecl),
                        ParentId = parentId ?? string.Empty,
                        SourceFilePath = filePath,
                        StartLine = GetLineNumber(enumDecl.SpanStart, syntaxTree: enumDecl.SyntaxTree),
                        EndLine = GetLineNumber(enumDecl.Span.End, syntaxTree: enumDecl.SyntaxTree),
                        Signature = enumDecl.Modifiers.ToString() + " enum " + enumDecl.Identifier.Text,
                        Documentation = ExtractDocumentation(enumDecl)
                    };
                    components.Add(component);
                }
            }
        }

        private static string? GetParentId(SyntaxNode node, SymbolAnalysis.SymbolTable symbolTable)
        {
            var parent = node.Parent;
            while (parent != null)
            {
                var symbol = symbolTable.GetSymbol(parent);
                if (symbol != null)
                {
                    return symbolTable.GetId(symbol);
                }
                parent = parent.Parent;
            }
            return null;
        }

        private static string GetNamespace(SyntaxNode node)
        {
            var parent = node.Parent;
            while (parent != null)
            {
                if (parent is NamespaceDeclarationSyntax namespaceDecl)
                {
                    return namespaceDecl.Name.ToString();
                }
                parent = parent.Parent;
            }
            return string.Empty;
        }

        private static int GetLineNumber(int position, SyntaxTree syntaxTree)
        {
            try
            {
                // Ensure position is within bounds of the source text
                var sourceText = syntaxTree.GetText();
                if (position < 0)
                {
                    position = 0;
                }
                else if (position > sourceText.Length)
                {
                    position = sourceText.Length;
                }

                var linePosition = syntaxTree.GetLineSpan(new TextSpan(position, 0)).StartLinePosition;
                return linePosition.Line + 1; // Line numbers are 0-based in Roslyn
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting line number for position {position}: {ex.Message}");
                return 1; // Default to line 1 if there's an error
            }
        }

        private static string ExtractDocumentation(SyntaxNode node)
        {
            var trivia = node.GetLeadingTrivia();
            var docComments = trivia.Where(t => t.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia) ||
                                               t.IsKind(SyntaxKind.MultiLineDocumentationCommentTrivia));

            if (!docComments.Any())
            {
                return string.Empty;
            }

            return string.Join(Environment.NewLine, docComments.Select(t => t.ToString()));
        }
    }
}