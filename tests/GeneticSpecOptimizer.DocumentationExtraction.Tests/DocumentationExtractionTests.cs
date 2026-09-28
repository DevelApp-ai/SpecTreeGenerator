using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using GeneticSpecOptimizer.Common.Models;
using GeneticSpecOptimizer.DocumentationExtraction.Converters;
using GeneticSpecOptimizer.DocumentationExtraction.Extractors;
using GeneticSpecOptimizer.DocumentationExtraction.Parsers;

using Xunit;

namespace GeneticSpecOptimizer.DocumentationExtraction.Tests;

public class MarkdownParserTests
{
    private static readonly string[] ExpectedSectionTitles = { "Title", "Section One", "Subsection", "Section Two" };
    private static readonly string[] ExpectedListItems = { "Item 1", "Item 2", "Item 3" };
    private static readonly string[] ExpectedTableHeaders = { "Column A", "Column B" };
    private const string SampleMarkdown = @"# Title

Intro paragraph.

## Section One

Content of section one.

```csharp
var x = 1;
```

### Subsection

Subsection content.

## Section Two

- Item 1
- Item 2
- Item 3

| Column A | Column B |
|----------|----------|
| a1       | b1       |
| a2       | b2       |
";

    [Fact]
    public void Parse_ReturnsFullText()
    {
        var parser = new MarkdownParser();
        var document = parser.Parse(SampleMarkdown);

        Assert.Equal(SampleMarkdown, document.FullText);
    }

    [Fact]
    public void Parse_ExtractsSectionsWithCorrectLevels()
    {
        var parser = new MarkdownParser();
        var document = parser.Parse(SampleMarkdown);

        var titles = document.Sections.Select(s => s.Title).ToList();
        Assert.Equal(ExpectedSectionTitles, titles);
        Assert.Equal(1, document.Sections[0].Level);
        Assert.Equal(2, document.Sections[1].Level);
        Assert.Equal(3, document.Sections[2].Level);
    }

    [Fact]
    public void Parse_BuildsSectionHierarchy()
    {
        var parser = new MarkdownParser();
        var document = parser.Parse(SampleMarkdown);

        var sectionOne = document.Sections.Single(s => s.Title == "Section One");
        var subsection = document.Sections.Single(s => s.Title == "Subsection");

        Assert.Same(sectionOne, subsection.Parent);
        Assert.Contains(subsection, sectionOne.Children);
    }

    [Fact]
    public void Parse_ExtractsCodeBlockWithLanguage()
    {
        var parser = new MarkdownParser();
        var document = parser.Parse(SampleMarkdown);

        var block = Assert.Single(document.CodeBlocks);
        Assert.Equal("csharp", block.Language);
        Assert.Contains("var x = 1;", block.Content);
    }

    [Fact]
    public void Parse_ExtractsListItems()
    {
        var parser = new MarkdownParser();
        var document = parser.Parse(SampleMarkdown);

        var list = Assert.Single(document.Lists);
        Assert.False(list.IsOrdered);
        Assert.Equal(ExpectedListItems, list.Items);
    }

    [Fact]
    public void Parse_ExtractsTableWithHeadersAndRows()
    {
        var parser = new MarkdownParser();
        var document = parser.Parse(SampleMarkdown);

        var table = Assert.Single(document.Tables);
        Assert.Equal(ExpectedTableHeaders, table.Headers);
        Assert.Equal(2, table.Rows.Count);
        Assert.Equal("a1", table.Rows[0][0]);
        Assert.Equal("b2", table.Rows[1][1]);
    }

    [Fact]
    public void Parse_EmptyContent_ReturnsEmptyCollections()
    {
        var parser = new MarkdownParser();
        var document = parser.Parse(string.Empty);

        Assert.Empty(document.Sections);
        Assert.Empty(document.CodeBlocks);
        Assert.Empty(document.Lists);
        Assert.Empty(document.Tables);
    }
}

public class MarkitdownConverterTests : IDisposable
{
    private readonly string _tempDir;

    public MarkitdownConverterTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "gso-docex-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        Directory.Delete(_tempDir, recursive: true);
        GC.SuppressFinalize(this);
    }

    [Fact]
    public void DetectDocumentType_MapsExtensions()
    {
        Assert.Equal(DocumentType.Markdown, MarkitdownConverter.DetectDocumentType("readme.md"));
        Assert.Equal(DocumentType.Markdown, MarkitdownConverter.DetectDocumentType("readme.MD"));
        Assert.Equal(DocumentType.Html, MarkitdownConverter.DetectDocumentType("page.html"));
        Assert.Equal(DocumentType.Html, MarkitdownConverter.DetectDocumentType("page.htm"));
        Assert.Equal(DocumentType.Word, MarkitdownConverter.DetectDocumentType("doc.docx"));
        Assert.Equal(DocumentType.Pdf, MarkitdownConverter.DetectDocumentType("doc.pdf"));
        Assert.Equal(DocumentType.Text, MarkitdownConverter.DetectDocumentType("notes.txt"));
        Assert.Equal(DocumentType.Unknown, MarkitdownConverter.DetectDocumentType("image.png"));
    }

    [Fact]
    public async Task ConvertToMarkdownAsync_MarkdownPassthrough()
    {
        var path = Path.Combine(_tempDir, "readme.md");
        const string content = "# Hello\n\nWorld\n";
        await File.WriteAllTextAsync(path, content);

        var converter = new MarkitdownConverter();
        var result = await converter.ConvertToMarkdownAsync(path, DocumentType.Markdown);

        Assert.Equal(content, result);
    }

    [Fact]
    public async Task ConvertToMarkdownAsync_TextPassthrough()
    {
        var path = Path.Combine(_tempDir, "notes.txt");
        const string content = "plain text notes";
        await File.WriteAllTextAsync(path, content);

        var converter = new MarkitdownConverter();
        var result = await converter.ConvertToMarkdownAsync(path, DocumentType.Text);

        Assert.Equal(content, result);
    }

    [Fact]
    public async Task ConvertToMarkdownAsync_MissingFile_Throws()
    {
        var converter = new MarkitdownConverter();
        await Assert.ThrowsAsync<FileNotFoundException>(
            () => converter.ConvertToMarkdownAsync(
                Path.Combine(_tempDir, "missing.md"), DocumentType.Markdown));
    }

    [Fact]
    public async Task ConvertToMarkdownAsync_UnknownType_ThrowsNotSupported()
    {
        var path = Path.Combine(_tempDir, "file.xyz");
        await File.WriteAllTextAsync(path, "data");

        var converter = new MarkitdownConverter();
        await Assert.ThrowsAsync<NotSupportedException>(
            () => converter.ConvertToMarkdownAsync(path, DocumentType.Unknown));
    }

    [Fact]
    public async Task ConvertToMarkdownAsync_Html_ConvertedToMarkdown()
    {
        var path = Path.Combine(_tempDir, "page.html");
        await File.WriteAllTextAsync(path, @"<html><head><style>body{}</style></head><body>
<h1>Heading One</h1>
<p>This is <strong>bold</strong> and <em>italic</em>.</p>
<ul><li>First</li><li>Second</li></ul>
<p>See <a href=""https://example.com"">the docs</a>.</p>
<script>alert('nope');</script>
</body></html>");

        var converter = new MarkitdownConverter();
        var result = await converter.ConvertToMarkdownAsync(path, DocumentType.Html);

        Assert.Contains("# Heading One", result);
        Assert.Contains("**bold**", result);
        Assert.Contains("*italic*", result);
        Assert.Contains("- First", result);
        Assert.Contains("- Second", result);
        Assert.Contains("[the docs](https://example.com)", result);
        // script/style content must be stripped
        Assert.DoesNotContain("alert", result);
        Assert.DoesNotContain("body{}", result);
    }
}

public class LocalSemanticKernelServiceTests
{
    private static CodeHierarchy CreateHierarchy(params (string Id, string Name)[] components)
    {
        var hierarchy = new CodeHierarchy();
        foreach (var (id, name) in components)
        {
            hierarchy.AddComponent(new CodeComponent
            {
                Id = id,
                Name = name,
                Type = ComponentType.Class
            });
        }

        return hierarchy;
    }

    private static Analyzers.DocumentStructure CreateStructure(params (string Title, string Content)[] sections)
    {
        var structure = new Analyzers.DocumentStructure();
        foreach (var (title, content) in sections)
        {
            structure.Sections.Add(new Analyzers.StructureSection
            {
                Title = title,
                Content = content,
                Level = 1
            });
        }

        return structure;
    }

    [Fact]
    public async Task ExtractRelevantSnippetsAsync_MapsSectionToComponentByKeywordOverlap()
    {
        var service = new LocalSemanticKernelService();
        var hierarchy = CreateHierarchy(("comp-weather", "WeatherService"));
        var structure = CreateStructure(
            ("Weather Service", "The WeatherService provides weather data for locations."));

        var snippets = await service.ExtractRelevantSnippetsAsync(
            new ParsedMarkdownDocument(), structure, hierarchy);

        var snippet = Assert.Single(snippets);
        Assert.Equal("comp-weather", snippet.TargetComponentId);
        Assert.Contains("weather data", snippet.Content);
        Assert.True(snippet.RelevanceScore >= 0.7);
    }

    [Fact]
    public async Task ExtractRelevantSnippetsAsync_NoOverlap_ReturnsNoSnippets()
    {
        var service = new LocalSemanticKernelService();
        var hierarchy = CreateHierarchy(("comp-weather", "WeatherService"));
        var structure = CreateStructure(
            ("Payments", "Handles invoices, refunds and billing cycles."));

        var snippets = await service.ExtractRelevantSnippetsAsync(
            new ParsedMarkdownDocument(), structure, hierarchy);

        Assert.Empty(snippets);
    }

    [Fact]
    public async Task ExtractRelevantSnippetsAsync_IsDeterministic()
    {
        var hierarchy = CreateHierarchy(("comp-a", "TemperatureConverter"));
        var structure = CreateStructure(
            ("Temperature Converter", "TemperatureConverter converts temperature units."));

        var service1 = new LocalSemanticKernelService();
        var service2 = new LocalSemanticKernelService();

        var r1 = await service1.ExtractRelevantSnippetsAsync(new ParsedMarkdownDocument(), structure, hierarchy);
        var r2 = await service2.ExtractRelevantSnippetsAsync(new ParsedMarkdownDocument(), structure, hierarchy);

        Assert.Equal(
            r1.Select(s => (s.TargetComponentId, s.Content, s.RelevanceScore)),
            r2.Select(s => (s.TargetComponentId, s.Content, s.RelevanceScore)));
    }

    [Fact]
    public async Task ExtractRelevantSnippetsAsync_NullArguments_Throw()
    {
        var service = new LocalSemanticKernelService();
        var hierarchy = CreateHierarchy(("comp-a", "ComponentA"));

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => service.ExtractRelevantSnippetsAsync(null!, null!, null!));
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => service.ExtractRelevantSnippetsAsync(
                new ParsedMarkdownDocument(),
                new Analyzers.DocumentStructure(),
                null!));
    }

    [Fact]
    public async Task ExtractRelevantSnippetsAsync_EmptySectionContent_Skipped()
    {
        var service = new LocalSemanticKernelService();
        var hierarchy = CreateHierarchy(("comp-a", "ComponentA"));
        var structure = CreateStructure(("Empty", ""));

        var snippets = await service.ExtractRelevantSnippetsAsync(
            new ParsedMarkdownDocument(), structure, hierarchy);

        Assert.Empty(snippets);
    }
}