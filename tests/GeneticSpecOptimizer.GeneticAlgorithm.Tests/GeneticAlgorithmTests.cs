using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using GeneticSpecOptimizer.Common.Models;
using GeneticSpecOptimizer.GeneticAlgorithm.Evolution;
using GeneticSpecOptimizer.GeneticAlgorithm.Fitness;
using GeneticSpecOptimizer.GeneticAlgorithm.Models;
using GeneticSpecOptimizer.GeneticAlgorithm.Operators;

using Xunit;

namespace GeneticSpecOptimizer.GeneticAlgorithm.Tests;

public class GeneticOperatorsTests
{
    private static SpecificationGenome CreateTestGenome(int specCount = 3)
    {
        var genome = new SpecificationGenome();
        for (var i = 0; i < specCount; i++)
        {
            genome.SpecificationFiles.Add(new SpecificationFile
            {
                Id = $"spec-{i}",
                Name = $"Spec{i}",
                ComponentId = $"component-{i}",
                Content = $"# Spec{i}\n\n## Overview\n\nContent for spec {i}.\n"
            });
        }

        for (var i = 1; i < specCount; i++)
        {
            genome.ParentChildRelationships[$"spec-{i}"] = "spec-0";
        }

        return genome;
    }

    [Fact]
    public async Task MutateAsync_DoesNotModifyOriginalGenome()
    {
        var operators = new GeneticOperators(seed: 42);
        var genome = CreateTestGenome();
        var originalContent = genome.SpecificationFiles.Select(s => s.Content).ToList();

        await operators.MutateAsync(genome, mutationRate: 1.0);

        // The original genome must be untouched (mutation works on a clone)
        Assert.Equal(originalContent, genome.SpecificationFiles.Select(s => s.Content));
    }

    [Fact]
    public async Task CrossoverAsync_IsDeterministicGivenSameSeed()
    {
        var p1 = CreateTestGenome();
        var p2 = CreateTestGenome();

        var ops1 = new GeneticOperators(seed: 123);
        var (a1, b1) = await ops1.CrossoverAsync(p1, p2, crossoverRate: 1.0);

        var ops2 = new GeneticOperators(seed: 123);
        var p3 = CreateTestGenome();
        var p4 = CreateTestGenome();
        var (a2, b2) = await ops2.CrossoverAsync(p3, p4, crossoverRate: 1.0);

        Assert.Equal(
            a1.SpecificationFiles.Select(s => (s.Name, s.Content)),
            a2.SpecificationFiles.Select(s => (s.Name, s.Content)));
        Assert.Equal(
            b1.SpecificationFiles.Select(s => (s.Name, s.Content)),
            b2.SpecificationFiles.Select(s => (s.Name, s.Content)));
    }

    [Fact]
    public async Task CrossoverAsync_WithRateZero_ReturnsClonesOfParents()
    {
        var operators = new GeneticOperators(seed: 7);
        var p1 = CreateTestGenome();
        var p2 = CreateTestGenome();

        var (o1, o2) = await operators.CrossoverAsync(p1, p2, crossoverRate: 0.0);

        // Names carried through on clones
        Assert.Equal(p1.SpecificationFiles.Select(s => s.Name).OrderBy(n => n),
            o1.SpecificationFiles.Select(s => s.Name).OrderBy(n => n));
        Assert.Equal(p2.SpecificationFiles.Select(s => s.Name).OrderBy(n => n),
            o2.SpecificationFiles.Select(s => s.Name).OrderBy(n => n));
        // Offspring are clones, not the same instances
        Assert.NotSame(p1, o1);
        Assert.NotSame(p2, o2);
        // And they carry the same content as their parents
        Assert.Equal(p1.SpecificationFiles.Select(s => s.Content), o1.SpecificationFiles.Select(s => s.Content));
    }

    [Fact]
    public async Task SelectAsync_ReturnsRequestedCountFromPopulation()
    {
        var operators = new GeneticOperators(seed: 99);
        var population = Enumerable.Range(0, 10)
            .Select(i =>
            {
                var g = CreateTestGenome();
                g.FitnessScore = i;
                return g;
            })
            .ToList();

        var selected = await operators.SelectAsync(population, count: 4, tournamentSize: 3);

        Assert.Equal(4, selected.Count);
        Assert.All(selected, s => Assert.Contains(s, population));
    }

    [Fact]
    public async Task SelectAsync_TournamentPrefersHigherFitness()
    {
        // With a large tournament size the fittest individual should dominate
        var operators = new GeneticOperators(seed: 5);
        var population = Enumerable.Range(0, 10)
            .Select(i =>
            {
                var g = CreateTestGenome(1);
                g.FitnessScore = i;
                return g;
            })
            .ToList();
        var best = population.OrderByDescending(g => g.FitnessScore).First();

        var selected = await operators.SelectAsync(population, count: 20, tournamentSize: 10);

        // Max-of-10-draws tournament: winners must come from the fitter half
        // on average (statistical property of tournament selection).
        Assert.All(selected, s => Assert.InRange(s.FitnessScore, 0, 9));
        Assert.True(selected.Average(s => s.FitnessScore) > 4.5,
            $"Tournament selection should favor higher fitness, mean was {selected.Average(s => s.FitnessScore)}");
        Assert.Contains(selected, s => s.FitnessScore == best.FitnessScore);
    }
}

public class FitnessEvaluatorTests
{
    private static SpecificationGenome CreateTestGenome()
    {
        var genome = new SpecificationGenome();
        genome.SpecificationFiles.Add(new SpecificationFile
        {
            Id = "spec-0",
            Name = "Root",
            Content = "# Root\n\n## Overview\n\nRoot content.\n"
        });
        return genome;
    }

    [Fact]
    public async Task EvaluateAsync_ReturnsFiniteNonNegativeFitness()
    {
        var evaluator = new FitnessEvaluator();

        var fitness = await evaluator.EvaluateAsync(CreateTestGenome(), new CodeHierarchy(), null);

        Assert.True(double.IsFinite(fitness), $"Fitness was {fitness}");
        Assert.True(fitness >= 0, $"Fitness was negative: {fitness}");
    }

    [Fact]
    public async Task EvaluateAsync_InheritanceImprovesFitness()
    {
        // A genome with a parent/child relationship deduplicates content via
        // inheritance, which the fitness function rewards.
        var evaluator = new FitnessEvaluator();

        var plain = new SpecificationGenome();
        plain.SpecificationFiles.Add(new SpecificationFile
        {
            Id = "spec-0",
            Name = "A",
            Content = "# A\n\nShared section content.\n"
        });
        plain.SpecificationFiles.Add(new SpecificationFile
        {
            Id = "spec-1",
            Name = "B",
            Content = "# B\n\nShared section content.\n"
        });

        var inherited = plain.Clone();
        inherited.ParentChildRelationships["spec-1"] = "spec-0";

        var fitnessPlain = await evaluator.EvaluateAsync(plain, new CodeHierarchy(), null);
        var fitnessInherited = await evaluator.EvaluateAsync(inherited, new CodeHierarchy(), null);

        Assert.True(fitnessInherited >= fitnessPlain,
            $"Inherited genome fitness ({fitnessInherited}) should not be lower than plain ({fitnessPlain})");
    }

    [Fact]
    public async Task CalculateMetricsAsync_ReturnsMetricsForGenome()
    {
        var evaluator = new FitnessEvaluator();

        var metrics = await evaluator.CalculateMetricsAsync(CreateTestGenome(), new CodeHierarchy());

        Assert.NotNull(metrics);
    }
}

public class EvolutionEngineTests
{
    private static SpecificationGenome CreateSeedGenome(int specCount = 3)
    {
        var genome = new SpecificationGenome();
        for (var i = 0; i < specCount; i++)
        {
            genome.SpecificationFiles.Add(new SpecificationFile
            {
                Id = $"spec-{i}",
                Name = $"Spec{i}",
                Content = $"# Spec{i}\n\n## Overview\n\nContent {i} with some words to mutate.\n"
            });
        }

        for (var i = 1; i < specCount; i++)
        {
            genome.ParentChildRelationships[$"spec-{i}"] = "spec-0";
        }

        return genome;
    }

    private static Common.Models.EvolutionConfig CreateConfig(int? seed = 42, int maxGenerations = 5) => new()
    {
        PopulationSize = 10,
        MaxGenerations = maxGenerations,
        ElitismCount = 2,
        TournamentSize = 3,
        RandomSeed = seed
    };

    [Fact]
    public async Task EvolveAsync_StopsAtGenerationLimit()
    {
        var engine = new EvolutionEngine(new GeneticOperators(seed: 1), new FitnessEvaluator());
        var config = CreateConfig(maxGenerations: 5);

        var result = await engine.EvolveAsync(CreateSeedGenome(), new CodeHierarchy(), config);

        Assert.True(result.Generations <= 5);
        Assert.NotNull(result.BestGenome);
        Assert.NotEmpty(result.FitnessHistory);
    }

    [Fact]
    public async Task EvolveAsync_IsDeterministicGivenRandomSeed()
    {
        var r1 = await new EvolutionEngine(new GeneticOperators(seed: 7), new FitnessEvaluator())
            .EvolveAsync(CreateSeedGenome(), new CodeHierarchy(), CreateConfig(seed: 7, maxGenerations: 3));
        var r2 = await new EvolutionEngine(new GeneticOperators(seed: 7), new FitnessEvaluator())
            .EvolveAsync(CreateSeedGenome(), new CodeHierarchy(), CreateConfig(seed: 7, maxGenerations: 3));

        Assert.Equal(r1.FitnessHistory, r2.FitnessHistory);
        Assert.Equal(r1.Generations, r2.Generations);
    }

    [Fact]
    public async Task EvolveAsync_ElitismPreservesBestFitnessMonotonically()
    {
        var engine = new EvolutionEngine(new GeneticOperators(seed: 3), new FitnessEvaluator());
        var config = CreateConfig(maxGenerations: 8);

        var result = await engine.EvolveAsync(CreateSeedGenome(), new CodeHierarchy(), config);

        // With elitism, best fitness must never decrease across generations
        for (var i = 1; i < result.FitnessHistory.Count; i++)
        {
            Assert.True(result.FitnessHistory[i] >= result.FitnessHistory[i - 1] - 1e-12,
                $"Fitness dropped at generation {i}: {result.FitnessHistory[i - 1]} -> {result.FitnessHistory[i]}");
        }
    }

    [Fact]
    public async Task EvolveAsync_HonoursCancellation()
    {
        var engine = new EvolutionEngine(new GeneticOperators(seed: 11), new FitnessEvaluator());
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            engine.EvolveAsync(CreateSeedGenome(), new CodeHierarchy(), CreateConfig(maxGenerations: 50), cts.Token));
    }

    [Fact]
    public async Task EvolveAsync_RaisesProgressEvents()
    {
        var engine = new EvolutionEngine(new GeneticOperators(seed: 13), new FitnessEvaluator());
        var generations = new List<int>();
        engine.ProgressUpdated += (_, e) => generations.Add(e.Generation);

        await engine.EvolveAsync(CreateSeedGenome(), new CodeHierarchy(), CreateConfig(maxGenerations: 3));

        Assert.NotEmpty(generations);
    }
}