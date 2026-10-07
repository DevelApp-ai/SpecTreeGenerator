using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using SpecTreeGenerator.Pipeline;

using Xunit;

namespace SpecTreeGenerator.Pipeline.Tests;

/// <summary>
/// End-to-end integration tests for <see cref="OptimizationPipeline"/> using the
/// local fallback path (no AI credentials configured). The sample sources are
/// generated inline so the tests do not depend on repository layout.
/// </summary>
public class OptimizationPipelineIntegrationTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _sourceDir;
    private readonly string _outputDir;

    public OptimizationPipelineIntegrationTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "gso-pipeline-tests-" + Guid.NewGuid().ToString("N"));
        _sourceDir = Path.Combine(_tempDir, "src");
        _outputDir = Path.Combine(_tempDir, "output");
        Directory.CreateDirectory(_sourceDir);

        CreateSampleSources(_sourceDir);
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        Directory.Delete(_tempDir, recursive: true);
    }

    private static void CreateSampleSources(string sourceDir)
    {
        File.WriteAllText(Path.Combine(sourceDir, "GreeterService.cs"), @"using System;

namespace SampleProject
{
    /// <summary>
    /// Greets users by name
    /// </summary>
    public class GreeterService
    {
        /// <summary>
        /// Greets the given user
        /// </summary>
        public string Greet(string name)
        {
            return ""Hello, "" + name;
        }
    }
}
");

        File.WriteAllText(Path.Combine(sourceDir, "CalculatorService.cs"), @"using System;

namespace SampleProject
{
    /// <summary>
    /// Performs basic arithmetic calculations
    /// </summary>
    public class CalculatorService
    {
        /// <summary>
        /// Adds two numbers
        /// </summary>
        public int Add(int a, int b)
        {
            return a + b;
        }

        /// <summary>
        /// Multiplies two numbers
        /// </summary>
        public int Multiply(int a, int b)
        {
            return a * b;
        }
    }
}
");

        File.WriteAllText(Path.Combine(sourceDir, "README.md"), @"# Sample Project

## Greeter Service

The GreeterService greets users by name.

## Calculator Service

The CalculatorService performs basic arithmetic calculations.
");
    }

    private static OptimizationPipelineConfig CreateConfig(string sourceDir, string outputDir) => new()
    {
        SourceCodeDirectory = sourceDir,
        DocumentationFiles = new List<string> { Path.Combine(sourceDir, "README.md") },
        OutputDirectory = outputDir,
        // No AI credentials: forces the deterministic local fallback path
        MistralApiKey = string.Empty,
        AzureAIFoundryApiKey = string.Empty,
        OptimizerConfig = new Common.Models.HierarchyOptimizerConfig
        {
            EvolutionConfig = new Common.Models.EvolutionConfig
            {
                PopulationSize = 10,
                MaxGenerations = 5,
                ElitismCount = 2,
                RandomSeed = 42
            },
            MaxOptimizationTimeMinutes = 1
        }
    };

    [Fact]
    public async Task RunAsync_EndToEnd_LocalFallback_WritesOutputFiles()
    {
        var pipeline = new OptimizationPipeline();
        var result = await pipeline.RunAsync(CreateConfig(_sourceDir, _outputDir));

        Assert.NotNull(result);
        Assert.NotEmpty(result.OptimizedSpecifications);
        Assert.NotEmpty(result.OutputFilePaths);

        Directory.CreateDirectory(_outputDir);
        Assert.All(result.OutputFilePaths, path => Assert.True(File.Exists(path), $"Expected output file '{path}'"));
        Assert.All(
            result.OutputFilePaths,
            path => Assert.EndsWith(".md", Path.GetFileName(path)));
    }

    [Fact]
    public async Task RunAsync_EndToEnd_ReportsSaneMetrics()
    {
        var pipeline = new OptimizationPipeline();
        var result = await pipeline.RunAsync(CreateConfig(_sourceDir, _outputDir));

        var metrics = result.Metrics;
        Assert.NotNull(metrics);
        Assert.True(metrics.OriginalSize > 0);
        Assert.True(metrics.OptimizedSize > 0);
        Assert.True(metrics.OptimizedSize <= metrics.OriginalSize);
        Assert.InRange(metrics.SizeReductionPercentage, 0, 100);
        Assert.True(metrics.HierarchyDepth >= 0);
        Assert.True(metrics.TotalSections >= metrics.InheritedSections);
        Assert.True(metrics.InheritedSections >= 0);
        Assert.True(metrics.OptimizationTime >= TimeSpan.Zero);
    }

    [Fact]
    public async Task RunAsync_EndToEnd_RaisesProgressThroughAllStages()
    {
        var pipeline = new OptimizationPipeline();
        var stages = new List<PipelineStage>();
        pipeline.ProgressUpdated += (_, e) => stages.Add(e.Stage);

        await pipeline.RunAsync(CreateConfig(_sourceDir, _outputDir));

        Assert.Contains(PipelineStage.Initialization, stages);
        Assert.Contains(PipelineStage.CodeAnalysis, stages);
        Assert.Contains(PipelineStage.DocumentationExtraction, stages);
        Assert.Contains(PipelineStage.SourceConsolidation, stages);
        Assert.Contains(PipelineStage.HierarchyOptimization, stages);
        Assert.Contains(PipelineStage.OutputGeneration, stages);
        Assert.Contains(PipelineStage.Completed, stages);
    }

    [Fact]
    public async Task RunAsync_EndToEnd_DocumentationIsIntegratedIntoSpecs()
    {
        var pipeline = new OptimizationPipeline();
        var result = await pipeline.RunAsync(CreateConfig(_sourceDir, _outputDir));

        // The README documents both services, so the consolidated specs must
        // reference the documentation via a Documentation Notes section.
        var combined = string.Join("\n", result.OptimizedSpecifications.Select(s => s.Content));
        Assert.Contains("Documentation Notes", combined);
    }

    [Fact]
    public async Task RunAsync_EndToEnd_IsDeterministicWithFixedSeed()
    {
        var pipeline1 = new OptimizationPipeline();
        var result1 = await pipeline1.RunAsync(CreateConfig(_sourceDir, Path.Combine(_tempDir, "out1")));

        var pipeline2 = new OptimizationPipeline();
        var result2 = await pipeline2.RunAsync(CreateConfig(_sourceDir, Path.Combine(_tempDir, "out2")));

        Assert.Equal(
            result1.OptimizedSpecifications.Select(s => s.Name).OrderBy(n => n),
            result2.OptimizedSpecifications.Select(s => s.Name).OrderBy(n => n));
    }

    [Fact]
    public async Task RunAsync_MissingSourceDirectory_Throws()
    {
        var pipeline = new OptimizationPipeline();
        var config = CreateConfig(_sourceDir, _outputDir);
        config.SourceCodeDirectory = string.Empty;

        await Assert.ThrowsAsync<ArgumentException>(() => pipeline.RunAsync(config));
    }

    [Fact]
    public async Task RunAsync_MissingOutputDirectory_Throws()
    {
        var pipeline = new OptimizationPipeline();
        var config = CreateConfig(_sourceDir, _outputDir);
        config.OutputDirectory = string.Empty;

        await Assert.ThrowsAsync<ArgumentException>(() => pipeline.RunAsync(config));
    }
}