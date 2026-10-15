using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

using SpecTreeGenerator.Common.Models;
using SpecTreeGenerator.Pipeline;

using Xunit;

namespace SpecTreeGenerator.Pipeline.Tests;

/// <summary>
/// Unit tests for <see cref="SpecTreeJsonWriter"/>: the machine-readable spec-tree
/// JSON contract (spectree.json) consumed by downstream tools such as
/// CitizenCodeLab project import.
/// </summary>
public class SpecTreeJsonWriterTests : IDisposable
{
    private readonly string _tempDir = Path.Combine(Path.GetTempPath(), "spectree-json-tests-" + Guid.NewGuid().ToString("N"));

    public SpecTreeJsonWriterTests()
    {
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        Directory.Delete(_tempDir, recursive: true);
    }

    [Fact]
    public void BuildDocument_WithFlatSpecifications_ProducesRootPerSpec()
    {
        var specs = new List<SpecificationFile>
        {
            new SpecificationFile { Id = "a", Name = "Component A", FilePath = "src/A.cs" },
            new SpecificationFile { Id = "b", Name = "Component B", FilePath = "src/B.cs" }
        };

        var document = SpecTreeJsonWriter.BuildDocument(specs, new OptimizationMetrics(), "/repo");

        Assert.Equal("1.0", document.SchemaVersion);
        Assert.Equal("/repo", document.SourceCodeDirectory);
        Assert.Equal(2, document.Roots.Count);
        Assert.Empty(document.Roots[0].Children);
    }

    [Fact]
    public void BuildDocument_WithParentChildLinks_NestsChildrenUnderParents()
    {
        var specs = new List<SpecificationFile>
        {
            new SpecificationFile { Id = "root", Name = "Root", FilePath = "src/Root.cs" },
            new SpecificationFile { Id = "child", Name = "Child", FilePath = "src/Child.cs", ParentSpecificationId = "root" },
            new SpecificationFile { Id = "grandchild", Name = "Grandchild", FilePath = "src/Grandchild.cs", ParentSpecificationId = "child" }
        };

        var document = SpecTreeJsonWriter.BuildDocument(specs, new OptimizationMetrics(), "/repo");

        var root = Assert.Single(document.Roots);
        Assert.Equal("root", root.Id);
        var child = Assert.Single(root.Children);
        Assert.Equal("child", child.Id);
        var grandchild = Assert.Single(child.Children);
        Assert.Equal("grandchild", grandchild.Id);
    }

    [Fact]
    public void BuildDocument_WithDanglingParentReference_PlacesNodeAtRoot()
    {
        var specs = new List<SpecificationFile>
        {
            new SpecificationFile { Id = "orphan", Name = "Orphan", FilePath = "src/O.cs", ParentSpecificationId = "missing" }
        };

        var document = SpecTreeJsonWriter.BuildDocument(specs, new OptimizationMetrics(), "/repo");

        var orphan = Assert.Single(document.Roots);
        Assert.Equal("orphan", orphan.Id);
    }

    [Fact]
    public void BuildDocument_CopiesMetrics()
    {
        var metrics = new OptimizationMetrics
        {
            OriginalSize = 1000,
            OptimizedSize = 400,
            TotalSections = 20,
            InheritedSections = 8,
            HierarchyDepth = 3
        };

        var document = SpecTreeJsonWriter.BuildDocument(new List<SpecificationFile>(), metrics, "/repo");

        Assert.Equal(1000, document.Metrics.OriginalSize);
        Assert.Equal(400, document.Metrics.OptimizedSize);
        Assert.Equal(60.0, document.Metrics.SizeReductionPercentage, precision: 5);
        Assert.Equal(20, document.Metrics.TotalSections);
        Assert.Equal(8, document.Metrics.InheritedSections);
        Assert.Equal(3, document.Metrics.HierarchyDepth);
    }

    [Fact]
    public void Write_ProducesParseableJsonWithExpectedShape()
    {
        var specs = new List<SpecificationFile>
        {
            new SpecificationFile { Id = "root", Name = "Root Spec", ComponentId = "c1", FilePath = "src/Root.cs" },
            new SpecificationFile { Id = "child", Name = "Child Spec", ParentSpecificationId = "root", FilePath = "src/Child.cs" }
        };

        var document = SpecTreeJsonWriter.BuildDocument(specs, new OptimizationMetrics(), "/repo");
        var path = SpecTreeJsonWriter.Write(document, _tempDir);

        Assert.Equal(Path.Combine(_tempDir, SpecTreeJsonWriter.DefaultFileName), path);
        Assert.True(File.Exists(path));

        using var doc = JsonDocument.Parse(File.ReadAllText(path));
        var root = doc.RootElement;
        Assert.Equal("1.0", root.GetProperty("schemaVersion").GetString());
        Assert.Equal("/repo", root.GetProperty("sourceCodeDirectory").GetString());
        var roots = root.GetProperty("roots");
        Assert.Equal(1, roots.GetArrayLength());
        var rootSpec = roots[0];
        Assert.Equal("Root Spec", rootSpec.GetProperty("name").GetString());
        Assert.Equal("Root_Spec.md", rootSpec.GetProperty("outputFileName").GetString());
        Assert.Equal(1, rootSpec.GetProperty("children").GetArrayLength());
    }

    [Fact]
    public void Write_WithNullDocument_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => SpecTreeJsonWriter.Write(null!, _tempDir));
    }

    [Fact]
    public void Write_WithEmptyOutputDirectory_Throws()
    {
        var document = SpecTreeJsonWriter.BuildDocument(new List<SpecificationFile>(), new OptimizationMetrics(), "/repo");
        Assert.Throws<ArgumentException>(() => SpecTreeJsonWriter.Write(document, string.Empty));
    }
}