using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using SpecTreeGenerator.Measurements;
using Xunit;

namespace SpecTreeGenerator.Measurements.Tests;

public class MeasurementEngineTests
{
    private static string CreateFixtureRepo()
    {
        var root = Path.Combine(Path.GetTempPath(), "spectree-measure-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        File.WriteAllText(Path.Combine(root, "WidgetService.cs"),
            "using System.Threading.Tasks;\n" +
            "namespace Fixture\n" +
            "{\n" +
            "    /// <summary>\n" +
            "    /// Provides widget data.\n" +
            "    /// </summary>\n" +
            "    public class WidgetService\n" +
            "    {\n" +
            "        /// <summary>Gets a widget.</summary>\n" +
            "        public async Task<string> GetWidgetAsync() => \"widget\";\n" +
            "    }\n" +
            "}\n");
        File.WriteAllText(Path.Combine(root, "GadgetService.cs"),
            "namespace Fixture\n" +
            "{\n" +
            "    /// <summary>\n" +
            "    /// Provides gadget data.\n" +
            "    /// </summary>\n" +
            "    public class GadgetService\n" +
            "    {\n" +
            "        public string GetGadget() => \"gadget\";\n" +
            "    }\n" +
            "}\n");
        return root;
    }

    [Fact]
    public async Task HierarchyBenchmarkProducesAllMeasuredQuantities()
    {
        var repo = CreateFixtureRepo();
        try
        {
            var result = await MeasurementEngine.RunHierarchyBenchmarkAsync(
                repo,
                Path.Combine(repo, "out"),
                new SpecTreeGenerator.Common.Models.HierarchyOptimizerConfig
                {
                    MaxOptimizationTimeMinutes = 1,
                    EvolutionConfig = new SpecTreeGenerator.Common.Models.EvolutionConfig
                    {
                        MaxGenerations = 3,
                        PopulationSize = 5,
                        RandomSeed = 42
                    }
                });

            Assert.False(string.IsNullOrEmpty(result.Repository));
            Assert.True(result.SourceFileCount >= 2);
            Assert.True(result.ComponentCount > 0);
            Assert.True(result.SpecificationCount > 0);
            Assert.True(result.FlatLayoutSize > 0);
            Assert.True(result.OptimizedSize > 0);
            Assert.True(result.HierarchyDepth >= 0);
            Assert.True(result.TotalWallTimeSeconds > 0);
            Assert.NotEmpty(result.FitnessHistory);
            Assert.NotEmpty(result.StageDurationsMs);
        }
        finally
        {
            Directory.Delete(repo, recursive: true);
        }
    }

    [Fact]
    public async Task OptimizerComparisonReportsGeneticAndGreedyFitness()
    {
        var repo = CreateFixtureRepo();
        try
        {
            var result = await MeasurementEngine.CompareOptimizersAsync(
                repo,
                new SpecTreeGenerator.Common.Models.HierarchyOptimizerConfig
                {
                    MaxOptimizationTimeMinutes = 1,
                    EvolutionConfig = new SpecTreeGenerator.Common.Models.EvolutionConfig
                    {
                        MaxGenerations = 3,
                        PopulationSize = 5,
                        RandomSeed = 42
                    }
                });

            Assert.True(result.GeneticBestFitness >= 0);
            Assert.True(result.GreedyBestFitness >= 0);
            Assert.True(result.GreedyTimeSeconds >= 0);
            Assert.True(result.GeneticTimeSeconds >= 0);
        }
        finally
        {
            Directory.Delete(repo, recursive: true);
        }
    }

    [Fact]
    public async Task ParameterSensitivityProducesOneEntryPerVariant()
    {
        var repo = CreateFixtureRepo();
        try
        {
            var variants = new[]
            {
                ("small", new SpecTreeGenerator.Common.Models.EvolutionConfig
                {
                    PopulationSize = 4, MaxGenerations = 2, RandomSeed = 42
                }),
                ("large", new SpecTreeGenerator.Common.Models.EvolutionConfig
                {
                    PopulationSize = 8, MaxGenerations = 2, RandomSeed = 42
                })
            };
            var results = await MeasurementEngine.MeasureParameterSensitivityAsync(repo, variants);

            Assert.Equal(2, results.Count);
            Assert.All(results, r => Assert.True(r["bestFitness"] >= 0));
            Assert.Equal(4, results[0]["populationSize"]);
            Assert.Equal(8, results[1]["populationSize"]);
        }
        finally
        {
            Directory.Delete(repo, recursive: true);
        }
    }

    [Fact]
    public async Task DocumentationMappingComputesPrecisionRecall()
    {
        var repo = CreateFixtureRepo();
        var docsDir = Path.Combine(repo, "docs");
        Directory.CreateDirectory(docsDir);
        File.WriteAllText(Path.Combine(docsDir, "README.md"),
            "## Widget Service\n\nThe WidgetService provides widget data.\n\n## Gadget Service\n\nThe GadgetService provides gadget data.\n");
        var groundTruth = Path.Combine(repo, "ground-truth.txt");
        File.WriteAllText(groundTruth,
            "Widget Service|WidgetService\n" +
            "Gadget Service|GadgetService\n");

        try
        {
            var result = await MeasurementEngine.MeasureDocumentationMappingAsync(repo, docsDir, groundTruth);

            Assert.Equal(2, result.GroundTruthMappings);
            Assert.Equal(2, result.TruePositives);
            Assert.Equal(0, result.FalsePositives);
            Assert.Equal(0, result.FalseNegatives);
            Assert.Equal(1.0, result.Recall, 6);
            Assert.Equal(1.0, result.Precision, 6);
        }
        finally
        {
            Directory.Delete(repo, recursive: true);
        }
    }

    [Fact]
    public async Task LanguageFidelityReportsCSharpBackend()
    {
        var repo = CreateFixtureRepo();
        try
        {
            var results = await MeasurementEngine.MeasureLanguageFidelityAsync(
                repo, new List<CodeAnalysis.Abstractions.ILanguageAnalysisBackend>());

            Assert.NotEmpty(results);
            var csharp = results.Single(r => r.Language == "csharp");
            Assert.Equal("RoslynAnalysisBackend", csharp.Backend);
            Assert.Equal(2, csharp.SourceFiles);
            Assert.True(csharp.ExtractedComponents > 0);
        }
        finally
        {
            Directory.Delete(repo, recursive: true);
        }
    }

    [Fact]
    public void ReportWriterProducesJsonFile()
    {
        var report = new MeasurementReport
        {
            HierarchyQuality =
            {
                new BenchmarkRunResult
                {
                    Repository = "fixture",
                    SizeReductionPercentage = 12.5,
                    FitnessHistory = new List<double> { 0.1, 0.2 }
                }
            }
        };
        var outputDir = Path.Combine(Path.GetTempPath(), "spectree-measure-tests", Guid.NewGuid().ToString("N"));
        try
        {
            var path = MeasurementEngine.WriteReport(report, outputDir);
            Assert.True(File.Exists(path));
            var json = File.ReadAllText(path);
            Assert.Contains("\"hierarchyQuality\"", json);
            Assert.Contains("\"sizeReductionPercentage\": 12.5", json);
        }
        finally
        {
            Directory.Delete(outputDir, recursive: true);
        }
    }
}
