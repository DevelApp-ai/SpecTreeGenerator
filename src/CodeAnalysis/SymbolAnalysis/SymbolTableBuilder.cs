using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using GeneticSpecOptimizer.Common.Models;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace GeneticSpecOptimizer.CodeAnalysis.SymbolAnalysis
{
    /// <summary>
    /// Represents a symbol table for resolving references and understanding types
    /// </summary>
    public class SymbolTable
    {
        private readonly Dictionary<string, ISymbol> _symbols = new Dictionary<string, ISymbol>();
        private readonly Dictionary<SyntaxNode, ISymbol> _nodeToSymbolMap = new Dictionary<SyntaxNode, ISymbol>();
        private readonly Dictionary<ISymbol, string> _symbolToIdMap = new Dictionary<ISymbol, string>(SymbolEqualityComparer.Default);

        /// <summary>
        /// Adds a symbol to the table
        /// </summary>
        public void AddSymbol(string id, ISymbol symbol, SyntaxNode? node = null)
        {
            _symbols[id] = symbol;
            _symbolToIdMap[symbol] = id;

            if (node != null)
            {
                _nodeToSymbolMap[node] = symbol;
            }
        }

        /// <summary>
        /// Gets a symbol by its ID
        /// </summary>
        public ISymbol? GetSymbol(string id)
        {
            return _symbols.TryGetValue(id, out var symbol) ? symbol : null;
        }

        /// <summary>
        /// Gets a symbol for a syntax node
        /// </summary>
        public ISymbol? GetSymbol(SyntaxNode node)
        {
            return _nodeToSymbolMap.TryGetValue(node, out var symbol) ? symbol : null;
        }

        /// <summary>
        /// Gets the ID for a symbol
        /// </summary>
        public string? GetId(ISymbol symbol)
        {
            return _symbolToIdMap.TryGetValue(symbol, out var id) ? id : null;
        }
    }

    /// <summary>
    /// Builds a symbol table from a syntax tree
    /// </summary>
    public class SymbolTableBuilder
    {
        /// <summary>
        /// Builds a symbol table from a syntax tree and compilation
        /// </summary>
        public static async Task<SymbolTable> BuildSymbolTableAsync(SyntaxTree syntaxTree, string filePath)
        {
            var compilation = CSharpCompilation.Create("SymbolAnalysis")
                .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
                .AddSyntaxTrees(syntaxTree);

            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var symbolTable = new SymbolTable();

            // Process all declarations in the syntax tree
            var root = await syntaxTree.GetRootAsync();
            ProcessDeclarations(root, semanticModel, symbolTable, filePath);

            return symbolTable;
        }

        private static void ProcessDeclarations(SyntaxNode node, SemanticModel semanticModel, SymbolTable symbolTable, string filePath)
        {
            // Process namespaces
            if (node is NamespaceDeclarationSyntax namespaceDecl)
            {
                var symbol = semanticModel.GetDeclaredSymbol(namespaceDecl);
                if (symbol != null)
                {
                    var id = $"namespace:{symbol.ToDisplayString()}";
                    symbolTable.AddSymbol(id, symbol, namespaceDecl);
                }
            }
            // Process classes
            else if (node is ClassDeclarationSyntax classDecl)
            {
                var symbol = semanticModel.GetDeclaredSymbol(classDecl);
                if (symbol != null)
                {
                    var id = $"class:{symbol.ToDisplayString()}";
                    symbolTable.AddSymbol(id, symbol, classDecl);
                }
            }
            // Process interfaces
            else if (node is InterfaceDeclarationSyntax interfaceDecl)
            {
                var symbol = semanticModel.GetDeclaredSymbol(interfaceDecl);
                if (symbol != null)
                {
                    var id = $"interface:{symbol.ToDisplayString()}";
                    symbolTable.AddSymbol(id, symbol, interfaceDecl);
                }
            }
            // Process methods
            else if (node is MethodDeclarationSyntax methodDecl)
            {
                var symbol = semanticModel.GetDeclaredSymbol(methodDecl);
                if (symbol != null)
                {
                    var id = $"method:{symbol.ToDisplayString()}";
                    symbolTable.AddSymbol(id, symbol, methodDecl);
                }
            }
            // Process properties
            else if (node is PropertyDeclarationSyntax propertyDecl)
            {
                var symbol = semanticModel.GetDeclaredSymbol(propertyDecl);
                if (symbol != null)
                {
                    var id = $"property:{symbol.ToDisplayString()}";
                    symbolTable.AddSymbol(id, symbol, propertyDecl);
                }
            }
            // Process fields
            else if (node is FieldDeclarationSyntax fieldDecl)
            {
                foreach (var variable in fieldDecl.Declaration.Variables)
                {
                    var symbol = semanticModel.GetDeclaredSymbol(variable);
                    if (symbol != null)
                    {
                        var id = $"field:{symbol.ToDisplayString()}";
                        symbolTable.AddSymbol(id, symbol, variable);
                    }
                }
            }
            // Process enums
            else if (node is EnumDeclarationSyntax enumDecl)
            {
                var symbol = semanticModel.GetDeclaredSymbol(enumDecl);
                if (symbol != null)
                {
                    var id = $"enum:{symbol.ToDisplayString()}";
                    symbolTable.AddSymbol(id, symbol, enumDecl);
                }
            }

            // Recursively process child nodes
            foreach (var child in node.ChildNodes())
            {
                ProcessDeclarations(child, semanticModel, symbolTable, filePath);
            }
        }
    }
}