using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GeneticSpecOptimizer.Common.Models;
using GeneticSpecOptimizer.GeneticAlgorithm.Encoding;
using GeneticSpecOptimizer.GeneticAlgorithm.Models;
using GeneticSpecOptimizer.HierarchyOptimization;
using Xunit;

namespace GeneticSpecOptimizer.HierarchyOptimization.Tests;

public class HierarchyOptimizerTests
{
    private static List<SpecificationFile> CreateSpecifications(int count = 3)
    {
        var specs = new List<SpecificationFile>();
        for (var i = 0; i < count; i++)
        {
            specs.Add(new SpecificationFile
            {
                Id = $"spec-{i}",
                Name = $"Spec{i}",
                ComponentId = $"component-{i}",
                Content = $"# Spec{i}\n\n## Overview\n\nContent for spec {i}.\n\n## Details\n\nMore content {i}.\n"
            });
        }

        return specs;
    }

    private static HierarchyOptimizerConfig CreateConfig(int? seed = 42) => new()
    {
        EvolutionConfig = new EvolutionConfig
        {
            PopulationSize = 10,
            MaxGenerations = 5,
            ElitismCount = 2,
            RandomSeed = seed
        }
    };

    [Fact]
    public void Constructor_ThrowsOnNullEncoder()
    {
        Assert.Throws<ArgumentNullException>(() => new HierarchyOptimizer(null!));
    }

    [Fact]
    public async Task OptimizeHierarchyAsync_ReturnsResultWithSaneMetrics()
    {
        var optimizer = new HierarchyOptimizer(new SpecificationGenomeEncoder());
        var specs = CreateSpecifications();

        var result = await optimizer.OptimizeHierarchyAsync(specs, CreateConfig());

        Assert.NotNull(result);
        Assert.NotNull(result.OptimizedSpecifications);
        Assert.NotNull(result.EvolutionResult);
        Assert.NotNull(result.Metrics);

        // Metrics must be consistent
        Assert.True(result.Metrics.OriginalSize > 0);
        Assert.True(result.Metrics.OptimizedSize > 0);
        Assert.True(result.Metrics.OptimizedSize <= result.Metrics.OriginalSize + 0);
        Assert.True(result.Metrics.SizeReductionPercentage >= 0 && result.Metrics.SizeReductionPercentage <= 100);
        Assert.True(result.Metrics.HierarchyDepth >= 0);
        Assert.True(result.Metrics.TotalSections >= 0);
        Assert.True(result.Metrics.InheritedSections >= 0);
        Assert.True(result.Metrics.InheritedSections <= result.Metrics.TotalSections);
        Assert.True(result.Metrics.OptimizationTime >= TimeSpan.Zero);
    }

    [Fact]
    public async Task OptimizeHierarchyAsync_ReturnsNonEmptyOutputWithUniqueIds()
    {
        var optimizer = new HierarchyOptimizer(new SpecificationGenomeEncoder());
        var specs = CreateSpecifications(count: 4);

        var result = await optimizer.OptimizeHierarchyAsync(specs, CreateConfig());

        // The GA may merge or drop specifications during optimization, but the
        // output must be non-empty and each specification must have a unique id.
        Assert.NotEmpty(result.OptimizedSpecifications);
        Assert.Equal(
            result.OptimizedSpecifications.Count,
            result.OptimizedSpecifications.Select(s => s.Id).Distinct().Count());
    }

    [Fact]
    public async Task OptimizeHierarchyAsync_EveryOutputHasContent()
    {
        var optimizer = new HierarchyOptimizer(new SpecificationGenomeEncoder());
        var specs = CreateSpecifications();

        var result = await optimizer.OptimizeHierarchyAsync(specs, CreateConfig());

        Assert.All(result.OptimizedSpecifications, s =>
        {
            Assert.False(string.IsNullOrWhiteSpace(s.Name));
            Assert.False(string.IsNullOrWhiteSpace(s.Content));
        });
    }

    [Fact]
    public async Task OptimizeHierarchyAsync_IsDeterministicWithSameSeed()
    {
        var specs1 = CreateSpecifications();
        var specs2 = CreateSpecifications();

        var optimizer1 = new HierarchyOptimizer(new SpecificationGenomeEncoder());
        var optimizer2 = new HierarchyOptimizer(new SpecificationGenomeEncoder());

        var result1 = await optimizer1.OptimizeHierarchyAsync(specs1, CreateConfig(seed: 99));
        var result2 = await optimizer2.OptimizeHierarchyAsync(specs2, CreateConfig(seed: 99));

        Assert.Equal(
            result1.OptimizedSpecifications.Select(s => s.Content),
            result2.OptimizedSpecifications.Select(s => s.Content));
    }

    [Fact]
    public async Task OptimizeHierarchyAsync_DoesNotModifyInputSpecifications()
    {
        var optimizer = new HierarchyOptimizer(new SpecificationGenomeEncoder());
        var specs = CreateSpecifications();
        var originalContents = specs.Select(s => s.Content).ToList();

        await optimizer.OptimizeHierarchyAsync(specs, CreateConfig());

        Assert.Equal(originalContents, specs.Select(s => s.Content));
    }

    [Fact]
    public async Task OptimizeHierarchyAsync_RaisesProgressEvents()
    {
        var optimizer = new HierarchyOptimizer(new SpecificationGenomeEncoder());
        var events = new List<OptimizationProgressEventArgs>();
        optimizer.OptimizationProgressUpdated += (_, e) => events.Add(e);

        var config = CreateConfig();
        await optimizer.OptimizeHierarchyAsync(CreateSpecifications(), config);

        Assert.NotEmpty(events);
        Assert.All(events, e => Assert.True(e.CurrentGeneration >= 0));
        Assert.All(events, e => Assert.Equal(config.EvolutionConfig.MaxGenerations, e.MaxGenerations));
        Assert.All(events, e => Assert.InRange(e.ProgressPercentage, 0, 100));
    }

    [Fact]
    public async Task OptimizeHierarchyAsync_UsesDefaultsWhenConfigIsNull()
    {
        var optimizer = new HierarchyOptimizer(new SpecificationGenomeEncoder());

        // Default config has PopulationSize 100 / MaxGenerations 50, which is slow;
        // with 1 tiny spec the run is still bounded by convergence, so just assert it completes.
        var result = await optimizer.OptimizeHierarchyAsync(
            CreateSpecifications(count: 1),
            config: null);

        Assert.NotNull(result);
    }
}