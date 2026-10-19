using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using SpecTreeGenerator.Common.Models;
using SpecTreeGenerator.GeneticAlgorithm.Encoding;
using SpecTreeGenerator.GeneticAlgorithm.Fitness;
using SpecTreeGenerator.Pipeline;

namespace SpecTreeGenerator.Measurements
{
    /// <summary>
    /// Result of one benchmark run (issue #30 measurement items).
    /// </summary>
    public class BenchmarkRunResult
    {
        public string Repository { get; set; } = string.Empty;
        public int SourceFileCount { get; set; }
        public int ComponentCount { get; set; }
        public int SpecificationCount { get; set; }
        public double FlatLayoutSize { get; set; }
        public double OptimizedSize { get; set; }
        public double SizeReductionPercentage { get; set; }
        public double RedundancyReductionPercentage { get; set; }
        public int HierarchyDepth { get; set; }
        public int InheritedSections { get; set; }
        public int TotalSections { get; set; }
        public List<double> FitnessHistory { get; set; } = new();
        public bool Converged { get; set; }
        public int Generations { get; set; }
        public double BestFitness { get; set; }
        public Dictionary<string, double> StageDurationsMs { get; set; } = new();
        public long PeakWorkingSetBytes { get; set; }
        public double TotalWallTimeSeconds { get; set; }
    }

    /// <summary>
    /// Result of the genetic optimizer vs. greedy baseline comparison.
    /// </summary>
    public class OptimizerComparisonResult
    {
        public string Repository { get; set; } = string.Empty;
        public double GeneticBestFitness { get; set; }
        public double GreedyBestFitness { get; set; }
        public int GeneticGenerations { get; set; }
        public double GeneticTimeSeconds { get; set; }
        public double GreedyTimeSeconds { get; set; }
        public List<double> FitnessHistory { get; set; } = new();
    }

    /// <summary>
    /// Documentation-mapping precision against a hand-labeled ground truth file.
    /// Ground truth format: one mapping per line, "section title|component name".
    /// </summary>
    public class DocumentationMappingResult
    {
        public string Repository { get; set; } = string.Empty;
        public int ExtractedSnippets { get; set; }
        public int GroundTruthMappings { get; set; }
        public int TruePositives { get; set; }
        public int FalsePositives { get; set; }
        public int FalseNegatives { get; set; }
        public double Precision => TruePositives + FalsePositives > 0
            ? (double)TruePositives / (TruePositives + FalsePositives)
            : 0;
        public double Recall => TruePositives + FalseNegatives > 0
            ? (double)TruePositives / (TruePositives + FalseNegatives)
            : 0;
        public double F1 => Precision + Recall > 0
            ? 2 * Precision * Recall / (Precision + Recall)
            : 0;
    }

    /// <summary>
    /// Per-language extraction fidelity of the analysis backends.
    /// </summary>
    public class LanguageFidelityResult
    {
        public string Repository { get; set; } = string.Empty;
        public string Language { get; set; } = string.Empty;
        public string Backend { get; set; } = string.Empty;
        public int SourceFiles { get; set; }
        public int ExtractedComponents { get; set; }
        public int ExtractedRelationships { get; set; }
        public int DocumentationCarryingComponents { get; set; }
        public int SpecificationFiles { get; set; }
    }

    /// <summary>
    /// Aggregated benchmark report for one repository set (issue #30).
    /// </summary>
    public class MeasurementReport
    {
        public string GeneratedAtUtc { get; set; } = DateTime.UtcNow.ToString("o");
        public List<BenchmarkRunResult> HierarchyQuality { get; set; } = new();
        public List<OptimizerComparisonResult> OptimizerBehavior { get; set; } = new();
        public List<Dictionary<string, double>> ParameterSensitivity { get; set; } = new();
        public List<DocumentationMappingResult> DocumentationMapping { get; set; } = new();
        public List<LanguageFidelityResult> LanguageFidelity { get; set; } = new();
    }

    /// <summary>
    /// Runs the measurement battery for the research article (issue #30).
    /// Every measurement goes through the real pipeline; nothing is mocked.
    /// </summary>
    public static class MeasurementEngine
    {
        public static async Task<BenchmarkRunResult> RunHierarchyBenchmarkAsync(
            string repositoryPath,
            string outputDirectory,
            HierarchyOptimizerConfig optimizerConfig)
        {
            var repository = Path.GetFileName(repositoryPath.TrimEnd(Path.DirectorySeparatorChar));
            var pipeline = new OptimizationPipeline();
            var config = new OptimizationPipelineConfig
            {
                SourceCodeDirectory = repositoryPath,
                OutputDirectory = outputDirectory,
                OptimizerConfig = optimizerConfig
            };

            var totalStopwatch = System.Diagnostics.Stopwatch.StartNew();
            var result = await pipeline.RunAsync(config);
            totalStopwatch.Stop();

            var flatLayoutSize = (double)result.ConsolidatedSpecifications.Sum(s => s.Content.Length);
            var optimizedSize = (double)result.OptimizedSpecifications.Sum(s => s.Content.Length);

            var process = System.Diagnostics.Process.GetCurrentProcess();
            return new BenchmarkRunResult
            {
                Repository = repository,
                SourceFileCount = Directory.GetFiles(repositoryPath, "*.cs", SearchOption.AllDirectories).Length,
                ComponentCount = result.CodeComponents.Count,
                SpecificationCount = result.OptimizedSpecifications.Count,
                FlatLayoutSize = flatLayoutSize,
                OptimizedSize = optimizedSize,
                SizeReductionPercentage = flatLayoutSize > 0 ? 100.0 * (flatLayoutSize - optimizedSize) / flatLayoutSize : 0,
                RedundancyReductionPercentage = result.Metrics.SizeReductionPercentage,
                HierarchyDepth = result.Metrics.HierarchyDepth,
                InheritedSections = result.Metrics.InheritedSections,
                TotalSections = result.Metrics.TotalSections,
                FitnessHistory = result.FitnessHistory,
                Converged = result.Converged,
                Generations = result.Generations,
                BestFitness = result.FitnessHistory.Count > 0 ? result.FitnessHistory.Max() : 0,
                StageDurationsMs = result.StageDurations.ToDictionary(
                    kv => kv.Key.ToString(), kv => kv.Value.TotalMilliseconds),
                PeakWorkingSetBytes = process.PeakWorkingSet64,
                TotalWallTimeSeconds = totalStopwatch.Elapsed.TotalSeconds
            };
        }

        /// <summary>
        /// Greedy/local-search baseline: iteratively re-parents each specification
        /// to the parent that maximizes fitness, one pass, no backtracking.
        /// </summary>
        public static async Task<OptimizerComparisonResult> CompareOptimizersAsync(
            string repositoryPath,
            HierarchyOptimizerConfig optimizerConfig)
        {
            var repository = Path.GetFileName(repositoryPath.TrimEnd(Path.DirectorySeparatorChar));
            var pipeline = new OptimizationPipeline();
            var config = new OptimizationPipelineConfig
            {
                SourceCodeDirectory = repositoryPath,
                OutputDirectory = Path.Combine(Path.GetTempPath(), "spectree-measure-greedy"),
                OptimizerConfig = optimizerConfig
            };
            var result = await pipeline.RunAsync(config);

            var geneticHistory = result.FitnessHistory;
            var geneticBest = geneticHistory.Count > 0 ? geneticHistory.Max() : 0;
            var geneticTime = result.StageDurations.TryGetValue(PipelineStage.HierarchyOptimization, out var gaTime)
                ? gaTime.TotalSeconds
                : 0;

            var encoder = new SpecificationGenomeEncoder();
            var evaluator = new FitnessEvaluator();
            var greedyStopwatch = System.Diagnostics.Stopwatch.StartNew();
            var greedyBest = await RunGreedyBaselineAsync(
                result.ConsolidatedSpecifications, encoder, evaluator);
            greedyStopwatch.Stop();

            return new OptimizerComparisonResult
            {
                Repository = repository,
                GeneticBestFitness = geneticBest,
                GreedyBestFitness = greedyBest,
                GeneticGenerations = result.Generations,
                GeneticTimeSeconds = geneticTime,
                GreedyTimeSeconds = greedyStopwatch.Elapsed.TotalSeconds,
                FitnessHistory = geneticHistory
            };
        }

        private static async Task<double> RunGreedyBaselineAsync(
            List<SpecificationFile> specifications,
            SpecificationGenomeEncoder encoder,
            FitnessEvaluator evaluator)
        {
            var genome = await encoder.EncodeAsync(specifications);
            var hierarchy = new CodeHierarchy();
            var baselineConfig = new HierarchyOptimizerConfig();

            var currentFitness = await evaluator.EvaluateAsync(genome, hierarchy, baselineConfig);
            var improved = true;
            while (improved)
            {
                improved = false;
                foreach (var spec in genome.SpecificationFiles)
                {
                    var originalParent = genome.ParentChildRelationships.TryGetValue(spec.Id, out var parent)
                        ? parent
                        : null;

                    double bestLocalFitness = currentFitness;
                    string? bestParent = originalParent;
                    var hasParent = originalParent != null;
                    var candidateParents = genome.SpecificationFiles
                        .Where(s => s.Id != spec.Id)
                        .Select(s => s.Id)
                        .ToList();
                    if (hasParent)
                    {
                        candidateParents.Add(string.Empty);
                    }

                    foreach (var candidate in candidateParents)
                    {
                        if (string.IsNullOrEmpty(candidate))
                        {
                            genome.ParentChildRelationships.Remove(spec.Id);
                        }
                        else
                        {
                            genome.ParentChildRelationships[spec.Id] = candidate;
                        }
                        var fitness = await evaluator.EvaluateAsync(genome, hierarchy, baselineConfig);
                        if (fitness > bestLocalFitness + 1e-9)
                        {
                            bestLocalFitness = fitness;
                            bestParent = string.IsNullOrEmpty(candidate) ? null : candidate;
                        }
                    }

                    if (bestParent == null)
                    {
                        genome.ParentChildRelationships.Remove(spec.Id);
                    }
                    else
                    {
                        genome.ParentChildRelationships[spec.Id] = bestParent;
                    }
                    if (bestLocalFitness > currentFitness + 1e-9)
                    {
                        currentFitness = bestLocalFitness;
                        improved = true;
                    }
                }
            }
            return currentFitness;
        }

        /// <summary>
        /// Parameter sensitivity: vary population size, crossover/mutation rates and
        /// elitism, and record the best fitness reached for each configuration.
        /// </summary>
        public static async Task<List<Dictionary<string, double>>> MeasureParameterSensitivityAsync(
            string repositoryPath,
            IEnumerable<(string Name, EvolutionConfig Config)> variants)
        {
            var results = new List<Dictionary<string, double>>();
            var repository = Path.GetFileName(repositoryPath.TrimEnd(Path.DirectorySeparatorChar));
            var pipeline = new OptimizationPipeline();
            foreach (var (name, evolutionConfig) in variants)
            {
                var config = new OptimizationPipelineConfig
                {
                    SourceCodeDirectory = repositoryPath,
                    OutputDirectory = Path.Combine(Path.GetTempPath(), "spectree-measure-sensitivity"),
                    OptimizerConfig = new HierarchyOptimizerConfig
                    {
                        EvolutionConfig = evolutionConfig
                    }
                };
                var result = await pipeline.RunAsync(config);
                results.Add(new Dictionary<string, double>
                {
                    ["bestFitness"] = result.FitnessHistory.Count > 0 ? result.FitnessHistory.Max() : 0,
                    ["generations"] = result.Generations,
                    ["converged"] = result.Converged ? 1 : 0,
                    ["populationSize"] = evolutionConfig.PopulationSize,
                    ["crossoverRate"] = evolutionConfig.CrossoverRate,
                    ["mutationRate"] = evolutionConfig.MutationRate,
                    ["elitismCount"] = evolutionConfig.ElitismCount
                });
            }
            return results;
        }

        /// <summary>
        /// Documentation-mapping precision/recall against a hand-labeled ground
        /// truth file ("section title|component name" per line).
        /// </summary>
        public static async Task<DocumentationMappingResult> MeasureDocumentationMappingAsync(
            string repositoryPath,
            string documentationDirectory,
            string groundTruthPath)
        {
            var repository = Path.GetFileName(repositoryPath.TrimEnd(Path.DirectorySeparatorChar));
            var groundTruth = File.ReadAllLines(groundTruthPath)
                .Select(l => l.Split('|', 2))
                .Where(p => p.Length == 2)
                .Select(p => (Section: p[0].Trim(), Component: p[1].Trim()))
                .ToList();

            var pipeline = new OptimizationPipeline();
            var config = new OptimizationPipelineConfig
            {
                SourceCodeDirectory = repositoryPath,
                OutputDirectory = Path.Combine(Path.GetTempPath(), "spectree-measure-docmapping"),
                DocumentationFiles = Directory
                    .EnumerateFiles(documentationDirectory, "*", SearchOption.AllDirectories)
                    .Where(f => Path.GetExtension(f).ToLowerInvariant() is ".md" or ".html" or ".htm" or ".txt" or ".docx")
                    .ToList()
            };
            var result = await pipeline.RunAsync(config);

            var componentNameById = result.CodeComponents
                .GroupBy(c => c.Id)
                .ToDictionary(g => g.Key, g => g.First().Name);
            var extracted = result.DocumentationSnippets
                .Where(s => componentNameById.TryGetValue(s.TargetComponentId, out _))
                .Select(s => (Section: s.SourceSectionTitle.Trim(), Component: componentNameById[s.TargetComponentId]))
                .ToHashSet();

            int truePositives = 0;
            var predicted = extracted.ToHashSet();
            foreach (var (section, component) in groundTruth)
            {
                if (predicted.Remove((section, component)))
                {
                    truePositives++;
                }
            }
            var falsePositives = predicted.Count;
            var falseNegatives = groundTruth.Count - truePositives;

            return new DocumentationMappingResult
            {
                Repository = repository,
                ExtractedSnippets = result.DocumentationSnippets.Count,
                GroundTruthMappings = groundTruth.Count,
                TruePositives = truePositives,
                FalsePositives = falsePositives,
                FalseNegatives = falseNegatives
            };
        }

        /// <summary>
        /// Per-language extraction fidelity: runs the pipeline with the given
        /// backends and records what each backend extracted.
        /// </summary>
        public static async Task<List<LanguageFidelityResult>> MeasureLanguageFidelityAsync(
            string repositoryPath,
            IEnumerable<CodeAnalysis.Abstractions.ILanguageAnalysisBackend> backends)
        {
            var registry = new CodeAnalysis.Abstractions.AnalysisBackendRegistry()
                .Register(new CodeAnalysis.Roslyn.RoslynAnalysisBackend());
            foreach (var backend in backends)
            {
                registry.Register(backend);
            }

            var results = new List<LanguageFidelityResult>();
            foreach (var backend in registry.All)
            {
                var files = new List<string>();
                foreach (var extension in backend.SupportedExtensions)
                {
                    files.AddRange(Directory.GetFiles(repositoryPath, "*" + extension, SearchOption.AllDirectories));
                }
                if (files.Count == 0)
                {
                    continue;
                }

                var components = new List<CodeComponent>();
                var relationships = new List<CodeRelationship>();
                foreach (var file in files)
                {
                    var parsed = await backend.ParseAsync(file);
                    components.AddRange(CodeAnalysis.Abstractions.IrComponentExtractor.ExtractComponents(parsed));
                }
                var componentLookup = components
                    .GroupBy(c => c.Id)
                    .ToDictionary(g => g.Key, g => g.First());
                foreach (var file in files)
                {
                    var parsed = await backend.ParseAsync(file);
                    relationships.AddRange(CodeAnalysis.Abstractions.IrRelationshipAnalyzer.AnalyzeRelationships(parsed, componentLookup));
                }

                results.Add(new LanguageFidelityResult
                {
                    Repository = Path.GetFileName(repositoryPath.TrimEnd(Path.DirectorySeparatorChar)),
                    Language = backend.Language,
                    Backend = backend.GetType().Name,
                    SourceFiles = files.Count,
                    ExtractedComponents = components.Count,
                    ExtractedRelationships = relationships.Count,
                    DocumentationCarryingComponents = components.Count(c => !string.IsNullOrEmpty(c.Documentation)),
                    SpecificationFiles = components.Count
                });
            }
            return results;
        }

        private static readonly JsonSerializerOptions ReportSerializerOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters = { new JsonStringEnumConverter() }
        };

        public static string WriteReport(MeasurementReport report, string outputDirectory)
        {
            Directory.CreateDirectory(outputDirectory);
            var path = Path.Combine(outputDirectory, "measurements.json");
            File.WriteAllText(path, JsonSerializer.Serialize(report, ReportSerializerOptions));
            return path;
        }
    }
}
