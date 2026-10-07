using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using SpecTreeGenerator.Common.Models;
namespace SpecTreeGenerator.CodeAnalysis.Analyzers
{
    /// <summary>
    /// Analyzes relationships between code components
    /// </summary>
    public class RelationshipAnalyzer
    {
        /// <summary>
        /// Analyzes relationships between components using the symbol table
        /// </summary>
        public static List<CodeRelationship> AnalyzeRelationships(List<CodeComponent> components, SymbolAnalysis.SymbolTable symbolTable, SyntaxTree syntaxTree)
        {
            var relationships = new List<CodeRelationship>();
            var root = syntaxTree.GetRoot();

            // Create a lookup for quick access to components by ID
            var componentLookup = components.ToDictionary(c => c.Id);

            // Analyze inheritance relationships
            AnalyzeInheritanceRelationships(root, symbolTable, relationships, componentLookup);

            // Analyze implementation relationships
            AnalyzeImplementationRelationships(root, symbolTable, relationships, componentLookup);

            // Analyze dependency relationships
            AnalyzeDependencyRelationships(root, symbolTable, relationships, componentLookup);

            // Analyze method call relationships
            AnalyzeMethodCallRelationships(root, symbolTable, relationships, componentLookup);

            return relationships;
        }

        private static void AnalyzeInheritanceRelationships(SyntaxNode root, SymbolAnalysis.SymbolTable symbolTable, List<CodeRelationship> relationships, Dictionary<string, CodeComponent> componentLookup)
        {
            var classes = root.DescendantNodes().OfType<ClassDeclarationSyntax>();
            foreach (var classDecl in classes)
            {
                if (classDecl.BaseList != null)
                {
                    foreach (var baseType in classDecl.BaseList.Types)
                    {
                        // Check if this is a class (inheritance) or interface (implementation)
                        var baseTypeSymbol = symbolTable.GetSymbol(baseType);
                        if (baseTypeSymbol != null && baseTypeSymbol is INamedTypeSymbol namedTypeSymbol)
                        {
                            if (namedTypeSymbol.TypeKind == TypeKind.Class)
                            {
                                var sourceId = symbolTable.GetId(symbolTable.GetSymbol(classDecl)!);
                                var targetId = symbolTable.GetId(baseTypeSymbol);

                                if (sourceId != null && targetId != null &&
                                    componentLookup.ContainsKey(sourceId) && componentLookup.ContainsKey(targetId))
                                {
                                    var relationship = new CodeRelationship
                                    {
                                        Id = $"inheritance:{sourceId}:{targetId}",
                                        Type = RelationshipType.Inheritance,
                                        SourceComponentId = sourceId,
                                        TargetComponentId = targetId
                                    };
                                    relationships.Add(relationship);
                                }
                            }
                        }
                    }
                }
            }
        }

        private static void AnalyzeImplementationRelationships(SyntaxNode root, SymbolAnalysis.SymbolTable symbolTable, List<CodeRelationship> relationships, Dictionary<string, CodeComponent> componentLookup)
        {
            var classes = root.DescendantNodes().OfType<ClassDeclarationSyntax>();
            foreach (var classDecl in classes)
            {
                if (classDecl.BaseList != null)
                {
                    foreach (var baseType in classDecl.BaseList.Types)
                    {
                        // Check if this is a class (inheritance) or interface (implementation)
                        var baseTypeSymbol = symbolTable.GetSymbol(baseType);
                        if (baseTypeSymbol != null && baseTypeSymbol is INamedTypeSymbol namedTypeSymbol)
                        {
                            if (namedTypeSymbol.TypeKind == TypeKind.Interface)
                            {
                                var sourceId = symbolTable.GetId(symbolTable.GetSymbol(classDecl)!);
                                var targetId = symbolTable.GetId(baseTypeSymbol);

                                if (sourceId != null && targetId != null &&
                                    componentLookup.ContainsKey(sourceId) && componentLookup.ContainsKey(targetId))
                                {
                                    var relationship = new CodeRelationship
                                    {
                                        Id = $"implementation:{sourceId}:{targetId}",
                                        Type = RelationshipType.Implementation,
                                        SourceComponentId = sourceId,
                                        TargetComponentId = targetId
                                    };
                                    relationships.Add(relationship);
                                }
                            }
                        }
                    }
                }
            }
        }

        private static void AnalyzeDependencyRelationships(SyntaxNode root, SymbolAnalysis.SymbolTable symbolTable, List<CodeRelationship> relationships, Dictionary<string, CodeComponent> componentLookup)
        {
            // Analyze field declarations for dependencies
            var fields = root.DescendantNodes().OfType<FieldDeclarationSyntax>();
            foreach (var fieldDecl in fields)
            {
                var fieldType = fieldDecl.Declaration.Type;
                var fieldTypeSymbol = symbolTable.GetSymbol(fieldType);
                if (fieldTypeSymbol != null)
                {
                    var parentId = GetParentId(fieldDecl, symbolTable);
                    var targetId = symbolTable.GetId(fieldTypeSymbol);

                    if (parentId != null && targetId != null &&
                        componentLookup.ContainsKey(parentId) && componentLookup.ContainsKey(targetId))
                    {
                        var relationship = new CodeRelationship
                        {
                            Id = $"dependency:{parentId}:{targetId}",
                            Type = RelationshipType.Dependency,
                            SourceComponentId = parentId,
                            TargetComponentId = targetId
                        };
                        relationships.Add(relationship);
                    }
                }
            }

            // Analyze property declarations for dependencies
            var properties = root.DescendantNodes().OfType<PropertyDeclarationSyntax>();
            foreach (var propertyDecl in properties)
            {
                var propertyType = propertyDecl.Type;
                var propertyTypeSymbol = symbolTable.GetSymbol(propertyType);
                if (propertyTypeSymbol != null)
                {
                    var parentId = GetParentId(propertyDecl, symbolTable);
                    var targetId = symbolTable.GetId(propertyTypeSymbol);

                    if (parentId != null && targetId != null &&
                        componentLookup.ContainsKey(parentId) && componentLookup.ContainsKey(targetId))
                    {
                        var relationship = new CodeRelationship
                        {
                            Id = $"dependency:{parentId}:{targetId}",
                            Type = RelationshipType.Dependency,
                            SourceComponentId = parentId,
                            TargetComponentId = targetId
                        };
                        relationships.Add(relationship);
                    }
                }
            }
        }

        private static void AnalyzeMethodCallRelationships(SyntaxNode root, SymbolAnalysis.SymbolTable symbolTable, List<CodeRelationship> relationships, Dictionary<string, CodeComponent> componentLookup)
        {
            var methodCalls = root.DescendantNodes().OfType<InvocationExpressionSyntax>();
            foreach (var methodCall in methodCalls)
            {
                var methodSymbol = symbolTable.GetSymbol(methodCall);
                if (methodSymbol != null)
                {
                    var containingMethod = methodCall.Ancestors().OfType<MethodDeclarationSyntax>().FirstOrDefault();
                    if (containingMethod != null)
                    {
                        var sourceId = symbolTable.GetId(symbolTable.GetSymbol(containingMethod)!);
                        var targetId = symbolTable.GetId(methodSymbol);

                        if (sourceId != null && targetId != null &&
                            componentLookup.ContainsKey(sourceId) && componentLookup.ContainsKey(targetId))
                        {
                            var relationship = new CodeRelationship
                            {
                                Id = $"call:{sourceId}:{targetId}",
                                Type = RelationshipType.Call,
                                SourceComponentId = sourceId,
                                TargetComponentId = targetId
                            };
                            relationships.Add(relationship);
                        }
                    }
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
    }
}