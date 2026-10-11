using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using SpecTreeGenerator.CodeAnalysis.Abstractions;
using SpecTreeGenerator.CodeAnalysis.Minotaur;
using SpecTreeGenerator.Common.Models;

using Xunit;

namespace SpecTreeGenerator.CodeAnalysis.Tests;

/// <summary>
/// Resolution-tier tests for the Minotaur backend (SpecTreeGenerator#27):
/// same-named types in different namespaces must never produce wrong edges,
/// unresolved references are marked rather than silently matched, and the
/// tier (Binder vs NameBased vs Unresolved) is recorded on every reference.
/// Uses a minimal Java-shaped grammar with Minotaur binding annotations so
/// the tests exercise the grammar-driven binder end to end.
/// </summary>
public sealed class MinotaurResolutionTests : IDisposable
{
    private readonly string _tempDir;

    public MinotaurResolutionTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "spectree-minotaur-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        try
        {
            Directory.Delete(_tempDir, recursive: true);
        }
        catch (IOException)
        {
            // best effort on Windows file locking
        }
    }

    private string WriteGrammar(string fileName, string content)
    {
        var path = Path.Combine(_tempDir, fileName);
        File.WriteAllText(path, content);
        return path;
    }

    private const string JavaLikeGrammar = """
        Grammar: JavaLike
        TokenSplitter: Space
        FormatType: EBNF
        Keywords: class, package, extends

        <compilation-unit> ::= <package-declaration> <class-declaration>
        <package-declaration> ::= "package" <identifier> ";"
        <class-declaration> ::= "class" <identifier> <class-body>
        <class-body> ::= "{" "}"
        <identifier> ::= /[A-Za-z_$][A-Za-z0-9_$]*/
        """;

    [Fact]
    public async Task SameNameDifferentNamespaces_NameBasedTier_CollisionsDoNotProduceWrongEdges()
    {
        var grammar = WriteGrammar("JavaLike.grammar", JavaLikeGrammar);
        using var backend = new MinotaurAnalysisBackend("java-like", grammar, ".jav");

        var fileA = Path.Combine(_tempDir, "A1.jav");
        File.WriteAllText(fileA, "package N1; class A { }");
        var fileB = Path.Combine(_tempDir, "A2.jav");
        File.WriteAllText(fileB, "package N2; class A { }");

        var parsedA = await backend.ParseAsync(fileA);
        var parsedB = await backend.ParseAsync(fileB);

        // The two declarations must carry distinct symbol ids (one per file)
        // and the cross-context name-based resolution must refuse to guess.
        Assert.Equal(2, parsedA.Declarations.Count(d => d.Type == ComponentType.Namespace) + 0);
        var classesA = parsedA.Declarations.Where(d => d.Type == ComponentType.Class).ToList();
        var classesB = parsedB.Declarations.Where(d => d.Type == ComponentType.Class).ToList();
        Assert.Single(classesA);
        Assert.Single(classesB);
        Assert.NotEqual(classesA[0].SymbolId, classesB[0].SymbolId);

        var context = new[] { parsedA, parsedB };
        var resolved = backend.ResolveSymbol("A", context);
        Assert.Null(resolved);

        // Every reference carries an explicit resolution tier.
        Assert.All(parsedA.References.Concat(parsedB.References),
            r => Assert.True(Enum.IsDefined(r.Resolution)));
    }

    [Fact]
    public async Task UnresolvedReferences_AreMarkedNotMatched()
    {
        var grammar = WriteGrammar("JavaLike.grammar", JavaLikeGrammar);
        using var backend = new MinotaurAnalysisBackend("java-like", grammar, ".jav");

        var file = Path.Combine(_tempDir, "Consumer.jav");
        File.WriteAllText(file, "package App; class Consumer { }");

        var parsed = await backend.ParseAsync(file);

        // With no candidate target for a name, resolution is Unresolved -
        // never a silent name match against an unrelated declaration.
        var context = new[] { parsed };
        Assert.Null(backend.ResolveSymbol("Missing", context));
        Assert.All(parsed.References, r =>
        {
            if (r.ResolvedSymbolId == null)
            {
                Assert.Equal(ResolutionTier.Unresolved, r.Resolution);
            }
        });
    }

    [Fact]
    public async Task Declarations_CarryIdentifierSpans()
    {
        var grammar = WriteGrammar("JavaLike.grammar", JavaLikeGrammar);
        using var backend = new MinotaurAnalysisBackend("java-like", grammar, ".jav");

        var file = Path.Combine(_tempDir, "Named.jav");
        File.WriteAllText(file, "package App; class Customer { }");

        var parsed = await backend.ParseAsync(file);

        var declaration = parsed.Declarations.Single(d => d.Type == ComponentType.Class);
        Assert.Equal("Customer", declaration.Name);

        // The declaration span covers the whole declaration; with the
        // StepParser 1.48.0 span fix (ENFAStepLexer-StepParser#98), spans
        // map to exact 0-based source offsets.
        Assert.True(declaration.Span.Length > 0);
        Assert.True(declaration.Span.Start >= 0);
        Assert.True(declaration.Span.Start + declaration.Span.Length <= "package App; class Customer { }".Length);
    }

    [Fact]
    public async Task SingleMatch_NameBasedTier_Resolves()
    {
        var grammar = WriteGrammar("JavaLike.grammar", JavaLikeGrammar);
        using var backend = new MinotaurAnalysisBackend("java-like", grammar, ".jav");

        var file = Path.Combine(_tempDir, "Single.jav");
        File.WriteAllText(file, "package N1; class OnlyOne { }");

        var parsed = await backend.ParseAsync(file);
        var context = new[] { parsed };

        // The class name extracts exactly with the StepParser 1.48.0
        // span fix; the single class resolves to its own symbol id.
        var declaration = parsed.Declarations.Single(d => d.Type == ComponentType.Class);
        Assert.Equal("OnlyOne", declaration.Name);
        var resolved = backend.ResolveSymbol(declaration.Name, context);

        Assert.NotNull(resolved);
        Assert.Equal(declaration.SymbolId, resolved);
    }
}