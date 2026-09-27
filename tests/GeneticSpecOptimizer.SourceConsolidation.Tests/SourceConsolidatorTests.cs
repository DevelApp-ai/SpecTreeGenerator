using System;
using System.Collections.Generic;
using System.Linq;
using GeneticSpecOptimizer.Common.Models;
using GeneticSpecOptimizer.SourceConsolidation;
using Xunit;

namespace GeneticSpecOptimizer.SourceConsolidation.Tests;

public class SourceConsolidatorTests
{
    private static CodeAnalysisResult CreateCodeAnalysis(params (string ComponentId, string Content)[] specs)
    {
        var result = new CodeAnalysisResult();
        foreach (var (componentId, content) in specs)
        {
            result.InitialSpecifications.Add(new SpecificationFile
            {
                Id = $"spec-{componentId}",
                ComponentId = componentId,
                Name = $"Spec for {componentId}",
                Content = content
            });
        }

        return result;
    }

    private static DocumentationExtractionResult CreateDocExtraction(
        params (string TargetComponentId, string Content, string SourceDocument, double Relevance)[] snippets)
    {
        var result = new DocumentationExtractionResult();
        foreach (var (target, content, source, relevance) in snippets)
        {
            result.Snippets.Add(new DocumentationExtraction.Extractors.DocumentationSnippet
            {
                TargetComponentId = target,
                Content = content,
                SourceDocument = source,
                RelevanceScore = relevance
            });
        }

        return result;
    }

    [Fact]
    public void ConsolidateSources_NullCodeAnalysis_Throws()
    {
        var consolidator = new SourceConsolidator();
        Assert.Throws<ArgumentNullException>(
            () => consolidator.ConsolidateSources(null!, new DocumentationExtractionResult()));
    }

    [Fact]
    public void ConsolidateSources_NullDocExtraction_Throws()
    {
        var consolidator = new SourceConsolidator();
        Assert.Throws<ArgumentNullException>(
            () => consolidator.ConsolidateSources(new CodeAnalysisResult(), null!));
    }

    [Fact]
    public void ConsolidateSources_NoSnippets_ReturnsCodeSpecsUnchanged()
    {
        var consolidator = new SourceConsolidator();
        var codeAnalysis = CreateCodeAnalysis(
            ("ComponentA", "# Component A\n\nDescribed by code.\n"));

        var result = consolidator.ConsolidateSources(codeAnalysis, new DocumentationExtractionResult()).ToList();

        var spec = Assert.Single(result);
        Assert.Equal("ComponentA", spec.ComponentId);
        Assert.DoesNotContain("Documentation Notes", spec.Content);
    }

    [Fact]
    public void ConsolidateSources_MatchingSnippet_IntegratesIntoDocumentationNotes()
    {
        var consolidator = new SourceConsolidator();
        var codeAnalysis = CreateCodeAnalysis(
            ("ComponentA", "# Component A\n\nDescribed by code.\n"));
        var docExtraction = CreateDocExtraction(
            ("ComponentA", "Component A does useful things.", "docs/a.md", 0.9));

        var result = consolidator.ConsolidateSources(codeAnalysis, docExtraction).ToList();

        var spec = Assert.Single(result);
        Assert.Contains("## Documentation Notes", spec.Content);
        Assert.Contains("Component A does useful things.", spec.Content);
        Assert.Contains("docs/a.md", spec.Content);
        // Code-first priority: original code content must be preserved
        Assert.Contains("Described by code.", spec.Content);
    }

    [Fact]
    public void ConsolidateSources_UnknownTarget_DoesNotThrowAndLeavesOtherSpecsIntact()
    {
        var consolidator = new SourceConsolidator();
        var codeAnalysis = CreateCodeAnalysis(
            ("ComponentA", "# Component A\n\nContent A.\n"));
        var docExtraction = CreateDocExtraction(
            ("MissingComponent", "Orphan snippet.", "docs/orphan.md", 0.5));

        var result = consolidator.ConsolidateSources(codeAnalysis, docExtraction).ToList();

        // Unknown target is skipped (with a warning), no orphan content is added
        var spec = Assert.Single(result);
        Assert.DoesNotContain("Orphan snippet.", spec.Content);
    }

    [Fact]
    public void ConsolidateSources_DuplicateComponentIds_FirstSpecWins()
    {
        var consolidator = new SourceConsolidator();
        var codeAnalysis = CreateCodeAnalysis(
            ("ComponentA", "# Component A v1\n\nFirst.\n"));
        codeAnalysis.InitialSpecifications.Add(new SpecificationFile
        {
            Id = "spec-ComponentA-2",
            ComponentId = "ComponentA",
            Name = "Spec for ComponentA (duplicate)",
            Content = "# Component A v2\n\nSecond.\n"
        });

        var docExtraction = CreateDocExtraction(
            ("ComponentA", "Snippet content.", "docs/a.md", 0.8));

        // Must not throw on duplicate ComponentIds
        var result = consolidator.ConsolidateSources(codeAnalysis, docExtraction).ToList();

        Assert.Equal(2, result.Count);
        var withNotes = result.Single(s => s.Content.Contains("Documentation Notes"));
        Assert.Contains("First.", withNotes.Content);
    }

    [Fact]
    public void ConsolidateSources_MultipleSnippets_IntegratedSortedByRelevance()
    {
        var consolidator = new SourceConsolidator();
        var codeAnalysis = CreateCodeAnalysis(
            ("ComponentA", "# Component A\n\nCode content.\n"));
        var docExtraction = CreateDocExtraction(
            ("ComponentA", "Low relevance note.", "docs/low.md", 0.3),
            ("ComponentA", "High relevance note.", "docs/high.md", 0.95),
            ("ComponentA", "Medium relevance note.", "docs/med.md", 0.6));

        var result = consolidator.ConsolidateSources(codeAnalysis, docExtraction).ToList();

        var spec = Assert.Single(result);
        var highIndex = spec.Content.IndexOf("High relevance note.");
        var mediumIndex = spec.Content.IndexOf("Medium relevance note.");
        var lowIndex = spec.Content.IndexOf("Low relevance note.");

        Assert.True(highIndex >= 0 && mediumIndex > highIndex && lowIndex > mediumIndex,
            "Snippets must be integrated in descending relevance order");
    }

    [Fact]
    public void ConsolidateSources_ContentIsWellFormatted()
    {
        var consolidator = new SourceConsolidator();
        var codeAnalysis = CreateCodeAnalysis(
            ("ComponentA", "# Component A\n\nContent.\n\n\nExcess spacing.\n"));
        var docExtraction = CreateDocExtraction(
            ("ComponentA", "Snippet.\n\n\n", "docs/a.md", 0.8));

        var result = consolidator.ConsolidateSources(codeAnalysis, docExtraction).ToList();

        var spec = Assert.Single(result);
        Assert.EndsWith("\n", spec.Content);
        Assert.DoesNotContain("\n\n\n", spec.Content);
        // Documentation Notes must be the last section
        var lastSectionIndex = spec.Content.LastIndexOf("\n## ");
        var docNotesIndex = spec.Content.IndexOf("\n## Documentation Notes\n");
        Assert.Equal(docNotesIndex, lastSectionIndex);
    }

    [Fact]
    public void ConsolidateSources_DuplicateDocumentationNotesSections_Collapsed()
    {
        var consolidator = new SourceConsolidator();
        var spec = new SpecificationFile
        {
            Id = "spec-a",
            ComponentId = "ComponentA",
            Content = "# A\n\n## Documentation Notes\n\nExisting notes.\n\n## Documentation Notes\n\nDup notes.\n"
        };
        var codeAnalysis = new CodeAnalysisResult();
        codeAnalysis.InitialSpecifications.Add(spec);

        var result = consolidator.ConsolidateSources(codeAnalysis, new DocumentationExtractionResult()).ToList();

        var consolidated = Assert.Single(result);
        Assert.Equal(1, consolidated.Content.Split("## Documentation Notes").Length - 1);
    }
}