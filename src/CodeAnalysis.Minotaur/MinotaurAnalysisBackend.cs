using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CognitiveGraph;
using CognitiveGraph.Accessors;
using DevelApp.StepParser;
using SpecTreeGenerator.CodeAnalysis.Abstractions;
using SpecTreeGenerator.Common.Models;

namespace SpecTreeGenerator.CodeAnalysis.Minotaur
{
    /// <summary>
    /// Multi-language analysis backend built on Minotaur's StepParser and
    /// the Minotaur-Grammars catalog. Any language with a grammar in the
    /// catalog (Java 8, Python 3.11, TypeScript, ...) is supported by
    /// pointing the constructor at the grammar file.
    /// </summary>
    /// <remarks>
    /// Relationship fidelity is name-based in this tier: references are
    /// recorded with their source-level names and resolved by matching
    /// declaration names across the parsed sources. Precise binder-level
    /// resolution arrives with the Minotaur semantic binding engine
    /// (DevelApp-ai/Minotaur#121).
    /// </remarks>
    public class MinotaurAnalysisBackend : ILanguageAnalysisBackend
    {
        private readonly string _grammarFilePath;
        private readonly StepParserEngine _engine;

        /// <summary>
        /// Creates a backend for one language, backed by one grammar file
        /// (e.g. from a Minotaur-Grammars repository checkout).
        /// </summary>
        public MinotaurAnalysisBackend(string language, string grammarFilePath, string extension)
        {
            Language = language;
            SupportedExtensions = new[] { extension };
            _grammarFilePath = grammarFilePath;
            _engine = new StepParserEngine();
            _engine.LoadGrammar(grammarFilePath);
        }

        /// <inheritdoc />
        public string Language { get; }

        /// <inheritdoc />
        public IReadOnlyCollection<string> SupportedExtensions { get; }

        /// <inheritdoc />
        public Task<ParsedSource> ParseAsync(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"File not found: {filePath}");
            }

            var code = File.ReadAllText(filePath);
            var result = _engine.Parse(code, Path.GetFileName(filePath));
            if (!result.Success || result.CognitiveGraph == null)
            {
                var errors = string.Join("; ", result.Errors);
                throw new InvalidOperationException(
                    $"Minotaur parse failed for '{filePath}' with grammar '{_grammarFilePath}': {errors}");
            }

            return Task.FromResult(BuildParsedSource(filePath, code, result.CognitiveGraph));
        }

        /// <inheritdoc />
        public string? ResolveSymbol(string name, IReadOnlyList<ParsedSource> context)
        {
            // Name-based tier: the first declaration whose Name matches wins;
            // ambiguity (multiple distinct matches) returns null.
            string? single = null;
            foreach (var parsed in context)
            {
                foreach (var declaration in parsed.Declarations)
                {
                    if (string.Equals(declaration.Name, name, StringComparison.Ordinal))
                    {
                        if (single != null && !string.Equals(single, declaration.SymbolId, StringComparison.Ordinal))
                        {
                            return null;
                        }

                        single = declaration.SymbolId;
                    }
                }
            }

            return single;
        }

        private ParsedSource BuildParsedSource(string filePath, string sourceText, CognitiveGraph graph)
        {
            var declarations = new List<DeclarationNode>();
            var references = new List<ReferenceNode>();

            var root = graph.GetRootNode();
            Walk(root, graph, sourceText, filePath, declarations, references, parentSymbolId: null);

            // Resolve name-based references now that all declarations of
            // this file are known (cross-file targets resolve at the
            // pipeline level through ResolveSymbol).
            var byName = declarations
                .GroupBy(d => d.Name, StringComparer.Ordinal)
                .ToDictionary(g => g.Key, g => g.Count(), StringComparer.Ordinal);
            foreach (var reference in references)
            {
                if (byName.TryGetValue(reference.Name, out var count) && count == 1)
                {
                    reference.ResolvedSymbolId = declarations
                        .Single(d => string.Equals(d.Name, reference.Name, StringComparison.Ordinal))
                        .SymbolId;
                }
            }

            return new ParsedSource
            {
                FilePath = filePath,
                Language = Language,
                Declarations = declarations,
                References = references
            };
        }

        private void Walk(
            SymbolNode node,
            CognitiveGraph graph,
            string sourceText,
            string filePath,
            List<DeclarationNode> declarations,
            List<ReferenceNode> references,
            string? parentSymbolId)
        {
            var ruleName = GetRuleName(node);
            var nodeText = GetNodeSourceText(node, sourceText);

            var declarationType = MapRuleNameToComponentType(ruleName);
            if (declarationType != ComponentType.Unknown)
            {
                var symbolId = FormSymbolId(declarationType, nodeText, filePath);
                var name = ExtractName(nodeText, ruleName);
                declarations.Add(new DeclarationNode
                {
                    SymbolId = symbolId,
                    Name = name,
                    FullName = name,
                    Type = declarationType,
                    ParentSymbolId = parentSymbolId,
                    Span = new TextSpan((int)node.SourceStart, (int)node.SourceEnd)
                });
                parentSymbolId = symbolId;
            }
            else if (MapRuleNameToReferenceKind(ruleName, out var referenceKind))
            {
                references.Add(new ReferenceNode
                {
                    ResolvedSymbolId = null,
                    Name = ExtractReferenceName(nodeText),
                    Kind = referenceKind,
                    ContainingSymbolId = parentSymbolId,
                    Span = new TextSpan((int)node.SourceStart, (int)node.SourceEnd)
                });
            }

            foreach (var childOffset in CollectChildOffsets(node))
            {
                var child = graph.GetNodeAt(childOffset);
                Walk(child, graph, sourceText, filePath, declarations, references, parentSymbolId);
            }
        }

        private static string GetRuleName(SymbolNode node)
        {
            var properties = node.GetProperties();
            for (var i = 0; i < properties.Count; i++)
            {
                var property = properties[i];
                if (string.Equals(property.GetKey(), "RuleName", StringComparison.Ordinal))
                {
                    return property.GetValue().AsString();
                }
            }

            return string.Empty;
        }

        private static string GetNodeSourceText(SymbolNode node, string sourceText)
        {
            var start = (int)Math.Min(node.SourceStart, (uint)sourceText.Length);
            var length = (int)Math.Min(node.SourceLength, (uint)(sourceText.Length - start));
            return sourceText.Substring(start, length);
        }

        private static List<uint> CollectChildOffsets(SymbolNode node)
        {
            var offsets = new List<uint>();
            var packedNodes = node.GetPackedNodes();
            for (var p = 0; p < packedNodes.Count; p++)
            {
                var packed = packedNodes[p];
                var children = packed.GetChildNodes();
                for (var c = 0; c < children.Count; c++)
                {
                    offsets.Add(children[c].Offset);
                }
            }

            return offsets;
        }

        private static ComponentType MapRuleNameToComponentType(string ruleName) => ruleName switch
        {
            // Java 8 grammar (Minotaur-Grammars)
            "package-declaration" => ComponentType.Namespace,
            "class-declaration" or "normal-class-declaration" => ComponentType.Class,
            "interface-declaration" => ComponentType.Interface,
            "enum-declaration" => ComponentType.Enum,
            "method-declaration" or "generic-method-declaration" or "abstract-method-declaration" or "default-method-declaration" or "static-method-declaration" => ComponentType.Method,
            "field-declaration" => ComponentType.Field,
            // Python 3.11 grammar
            "classdef" => ComponentType.Class,
            "funcdef" => ComponentType.Method,
            "decorated" => ComponentType.Class,
            // TypeScript grammar
            "class-declaration-ts" or "namespace-declaration" => ComponentType.Class,
            "interface-declaration" => ComponentType.Interface,
            "enum-declaration" => ComponentType.Enum,
            "type-alias-declaration" => ComponentType.Class,
            // Legacy unversioned Java grammar (folder names)
            "class declaration" => ComponentType.Class,
            "interface declaration" => ComponentType.Interface,
            "method declaration" => ComponentType.Method,
            "field declaration" => ComponentType.Field,
            _ => ComponentType.Unknown
        };

        private static bool MapRuleNameToReferenceKind(string ruleName, out ReferenceKind kind)
        {
            switch (ruleName)
            {
                case "superclass":
                case "extends":
                case "extends-clause":
                case "extends-types":
                    kind = ReferenceKind.BaseClass;
                    return true;
                case "superinterfaces":
                case "implements":
                case "implements-clause":
                    kind = ReferenceKind.InterfaceImplementation;
                    return true;
                case "method-invocation":
                case "method_invocation":
                case "call":
                    kind = ReferenceKind.MethodCall;
                    return true;
                default:
                    kind = ReferenceKind.TypeUse;
                    return false;
            }
        }

        private static string FormSymbolId(ComponentType type, string nodeText, string filePath)
        {
            var name = ExtractName(nodeText, nodeText);
            return $"{type.ToString().ToLowerInvariant()}:{name}@{filePath}";
        }

        private static string ExtractName(string nodeText, string ruleName)
        {
            // Declaration nodes in the supported grammars start with their
            // keyword(s); the declared name is the first identifier-looking
            // token after the leading keyword(s) and modifiers.
            var keywords = new HashSet<string>(StringComparer.Ordinal)
            {
                "namespace", "package", "class", "interface", "enum", "struct",
                "record", "abstract", "sealed", "public", "private", "protected",
                "internal", "static", "final", "partial", "method", "property",
                "field", "func", "function", "def", "fun", "type", "declare",
                "export", "default", "async", "const", "var", "let"
            };
            var tokens = nodeText.Split(' ', '\t', '\n', '\r', '(', '{', ':', ';', '{', StringSplitOptions.RemoveEmptyEntries);
            foreach (var token in tokens)
            {
                if (token.Length > 0 && char.IsLetter(token[0]) && !keywords.Contains(token))
                {
                    return token;
                }
            }

            return tokens.Length > 0 ? tokens[0] : ruleName;
        }

        private static string ExtractReferenceName(string nodeText)
        {
            var tokens = nodeText.Split(' ', '\t', '\n', '\r', '(', ':', ';', '{', ',', StringSplitOptions.RemoveEmptyEntries);
            foreach (var token in tokens)
            {
                if (token.Length > 0 && (char.IsLetter(token[0]) || token[0] == '_'))
                {
                    return token;
                }
            }

            return nodeText;
        }
    }
}