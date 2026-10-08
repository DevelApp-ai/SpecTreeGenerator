using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.SemanticKernel;

using SpecTreeGenerator.CodeAnalysis.Abstractions;
using SpecTreeGenerator.CodeAnalysis.Analyzers;
using SpecTreeGenerator.CodeAnalysis.Builders;
using SpecTreeGenerator.CodeAnalysis.Extractors;
using SpecTreeGenerator.CodeAnalysis.Roslyn;
using SpecTreeGenerator.CodeAnalysis.Generators;
using SpecTreeGenerator.CodeAnalysis.Parsers;
using SpecTreeGenerator.CodeAnalysis.SymbolAnalysis;
using SpecTreeGenerator.Common.Models;
using SpecTreeGenerator.DocumentationExtraction.Analyzers;
using SpecTreeGenerator.DocumentationExtraction.Converters;
using SpecTreeGenerator.DocumentationExtraction.Extractors;
using SpecTreeGenerator.DocumentationExtraction.Parsers;
using SpecTreeGenerator.GeneticAlgorithm.Encoding;
using SpecTreeGenerator.GeneticAlgorithm.Evolution;
using SpecTreeGenerator.GeneticAlgorithm.Fitness;
using SpecTreeGenerator.HierarchyOptimization;
using SpecTreeGenerator.SourceConsolidation;
namespace SpecTreeGenerator.Pipeline
{
    /// <summary>
    /// Configuration for the optimization pipeline
    /// </summary>
    public class OptimizationPipelineConfig
    {
        /// <summary>
        /// Path to the source code directory
        /// </summary>
        public string SourceCodeDirectory { get; set; } = string.Empty;

        /// <summary>
        /// Paths to documentation files
        /// </summary>
        public List<string> DocumentationFiles { get; set; } = new List<string>();

        /// <summary>
        /// Output directory for optimized specifications
        /// </summary>
        public string OutputDirectory { get; set; } = string.Empty;

        /// <summary>
        /// Configuration for hierarchy optimization
        /// </summary>
        public HierarchyOptimizerConfig OptimizerConfig { get; set; } = new HierarchyOptimizerConfig();

        /// <summary>
        /// Azure AI Foundry endpoint for Semantic Kernel
        /// </summary>
        public string AzureAIFoundryEndpoint { get; set; } = string.Empty;

        /// <summary>
        /// Azure AI Foundry API key
        /// </summary>
        public string AzureAIFoundryApiKey { get; set; } = string.Empty;

        /// <summary>
        /// Azure AI Foundry deployment name
        /// </summary>
        public string AzureAIFoundryDeploymentName { get; set; } = "mistral";

        /// <summary>
        /// Mistral AI API key (https://console.mistral.ai). When set, the pipeline
        /// uses the Mistral AI API for semantic analysis. This takes priority over
        /// the Azure AI Foundry settings.
        /// </summary>
        public string MistralApiKey { get; set; } = string.Empty;

        /// <summary>
        /// Mistral AI model to use (e.g. "mistral-large-latest")
        /// </summary>
        public string MistralModel { get; set; } = "mistral-large-latest";

        /// <summary>
        /// Optional custom Mistral-compatible endpoint (defaults to the Mistral AI API)
        /// </summary>
        public string MistralEndpoint { get; set; } = string.Empty;
    }

    /// <summary>
    /// Optimization metrics for the pipeline
    /// </summary>
    public class OptimizationMetrics
    {
        /// <summary>
        /// Original size of specifications in characters
        /// </summary>
        public int OriginalSize { get; set; }

        /// <summary>
        /// Optimized size of specifications in characters
        /// </summary>
        public int OptimizedSize { get; set; }

        /// <summary>
        /// Percentage reduction in size
        /// </summary>
        public double SizeReductionPercentage => OriginalSize > 0 ? 100.0 * (1.0 - (double)OptimizedSize / OriginalSize) : 0;

        /// <summary>
        /// Total number of sections in specifications
        /// </summary>
        public int TotalSections { get; set; }

        /// <summary>
        /// Number of sections that are inherited
        /// </summary>
        public int InheritedSections { get; set; }

        /// <summary>
        /// Percentage of sections that are inherited
        /// </summary>
        public double InheritancePercentage => TotalSections > 0 ? 100.0 * (double)InheritedSections / TotalSections : 0;

        /// <summary>
        /// Depth of the hierarchy
        /// </summary>
        public int HierarchyDepth { get; set; }

        /// <summary>
        /// Time taken for optimization
        /// </summary>
        public TimeSpan OptimizationTime { get; set; }

        /// <summary>
        /// Convert from HierarchyOptimization.OptimizationMetrics
        /// </summary>
        public static OptimizationMetrics FromHierarchyOptimizationMetrics(HierarchyOptimization.OptimizationMetrics metrics)
        {
            return new OptimizationMetrics
            {
                OriginalSize = metrics.OriginalSize,
                OptimizedSize = metrics.OptimizedSize,
                TotalSections = metrics.TotalSections,
                InheritedSections = metrics.InheritedSections,
                HierarchyDepth = metrics.HierarchyDepth,
                OptimizationTime = metrics.OptimizationTime
            };
        }
    }

    /// <summary>
    /// Result of the optimization pipeline
    /// </summary>
    public class OptimizationPipelineResult
    {
        /// <summary>
        /// Optimized specifications
        /// </summary>
        public List<SpecificationFile> OptimizedSpecifications { get; set; } = new List<SpecificationFile>();

        /// <summary>
        /// Optimization metrics
        /// </summary>
        public OptimizationMetrics Metrics { get; set; } = new OptimizationMetrics();

        /// <summary>
        /// Output file paths
        /// </summary>
        public List<string> OutputFilePaths { get; set; } = new List<string>();
    }

    /// <summary>
    /// Interface for optimization pipeline
    /// </summary>
    public interface IOptimizationPipeline
    {
        /// <summary>
        /// Runs the optimization pipeline
        /// </summary>
        Task<OptimizationPipelineResult> RunAsync(OptimizationPipelineConfig config);

        /// <summary>
        /// Event raised when pipeline progress is updated
        /// </summary>
        event EventHandler<PipelineProgressEventArgs>? ProgressUpdated;
    }

    /// <summary>
    /// Event arguments for pipeline progress
    /// </summary>
    public class PipelineProgressEventArgs : EventArgs
    {
        /// <summary>
        /// Current stage of the pipeline
        /// </summary>
        public PipelineStage Stage { get; set; }

        /// <summary>
        /// Progress message
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Progress percentage
        /// </summary>
        public double ProgressPercentage { get; set; }
    }

    /// <summary>
    /// Stages of the pipeline
    /// </summary>
    public enum PipelineStage
    {
        Initialization,
        CodeAnalysis,
        DocumentationExtraction,
        SourceConsolidation,
        HierarchyOptimization,
        OutputGeneration,
        Completed
    }

    /// <summary>
    /// Implementation of optimization pipeline
    /// </summary>
    public class OptimizationPipeline : IOptimizationPipeline
    {
        public event EventHandler<PipelineProgressEventArgs>? ProgressUpdated;

        public async Task<OptimizationPipelineResult> RunAsync(OptimizationPipelineConfig config)
        {
            // Validate configuration
            if (string.IsNullOrEmpty(config.SourceCodeDirectory))
            {
                throw new ArgumentException("Source code directory is required");
            }

            if (string.IsNullOrEmpty(config.OutputDirectory))
            {
                throw new ArgumentException("Output directory is required");
            }

            // Create output directory if it doesn't exist
            Directory.CreateDirectory(config.OutputDirectory);

            // Initialize Semantic Kernel (null when no AI credentials are configured)
            var kernel = SemanticKernelFactory.TryCreate(config);
            var kernelMode = SemanticKernelFactory.GetMode(config);

            // Report progress
            ReportProgress(PipelineStage.Initialization,
                kernelMode == SemanticKernelMode.LocalFallback
                    ? "Pipeline initialized (no AI credentials configured; using local semantic extraction)"
                    : $"Pipeline initialized (semantic analysis via {kernelMode})", 0);

            // Step 1: Code Analysis
            ReportProgress(PipelineStage.CodeAnalysis, "Starting code analysis", 0);
            var codeAnalysisResult = await AnalyzeCodeAsync(config.SourceCodeDirectory);
            ReportProgress(PipelineStage.CodeAnalysis, "Code analysis completed", 100);

            // Step 2: Documentation Extraction
            ReportProgress(PipelineStage.DocumentationExtraction, "Starting documentation extraction", 0);
            var docExtractionResult = await ExtractDocumentationAsync(config.DocumentationFiles, kernel, codeAnalysisResult.CodeHierarchy);
            ReportProgress(PipelineStage.DocumentationExtraction, "Documentation extraction completed", 100);

            // Step 3: Source Consolidation
            ReportProgress(PipelineStage.SourceConsolidation, "Starting source consolidation", 0);
            var sourceConsolidator = new SourceConsolidation.SourceConsolidator();
            var consolidatedSpecs = sourceConsolidator
                .ConsolidateSources(codeAnalysisResult, docExtractionResult)
                .ToList();
            ReportProgress(PipelineStage.SourceConsolidation, "Source consolidation completed", 100);

            // Step 4: Hierarchy Optimization
            ReportProgress(PipelineStage.HierarchyOptimization, "Starting hierarchy optimization", 0);
            var optimizationResult = await OptimizeHierarchyAsync(consolidatedSpecs, config.OptimizerConfig);
            ReportProgress(PipelineStage.HierarchyOptimization, "Hierarchy optimization completed", 100);

            // Step 5: Output Generation
            ReportProgress(PipelineStage.OutputGeneration, "Generating output files", 0);
            var outputFilePaths = GenerateOutputFiles(optimizationResult.OptimizedSpecifications, config.OutputDirectory);
            ReportProgress(PipelineStage.OutputGeneration, "Output files generated", 100);

            // Report completion
            ReportProgress(PipelineStage.Completed, "Pipeline completed", 100);

            // Return result
            return new OptimizationPipelineResult
            {
                OptimizedSpecifications = optimizationResult.OptimizedSpecifications,
                Metrics = OptimizationMetrics.FromHierarchyOptimizationMetrics(optimizationResult.Metrics),
                OutputFilePaths = outputFilePaths
            };
        }

        private static async Task<CodeAnalysisResult> AnalyzeCodeAsync(string sourceCodeDirectory)
        {
            // Discover source files through the registered analysis backends
            // (extension-driven; unknown extensions are skipped).
            var registry = new AnalysisBackendRegistry()
                .Register(new RoslynAnalysisBackend());
            var sourceFiles = Directory.GetFiles(sourceCodeDirectory, "*", SearchOption.AllDirectories)
                .Where(f => registry.ForFile(f) != null)
                .ToList();

            // Parse source files into the language-agnostic IR
            var components = new List<CodeComponent>();
            var relationships = new List<CodeRelationship>();
            var parsedSources = new List<ParsedSource>();

            foreach (var sourceFile in sourceFiles)
            {
                var backend = registry.ForFile(sourceFile)!;
                var parsed = await backend.ParseAsync(sourceFile);
                parsedSources.Add(parsed);
                components.AddRange(IrComponentExtractor.ExtractComponents(parsed));
            }

            // Analyze relationships against the full component lookup
            // (cross-file references need all components known up front).
            var componentLookup = components.ToDictionary(c => c.Id);
            foreach (var parsed in parsedSources)
            {
                relationships.AddRange(IrRelationshipAnalyzer.AnalyzeRelationships(parsed, componentLookup));
            }

            // Build code hierarchy
            var codeHierarchy = HierarchyBuilder.BuildHierarchy(components);
            HierarchyBuilder.UpdateHierarchyWithRelationships(codeHierarchy, relationships);

            // Generate initial specifications
            var specificationGenerator = new SpecificationGenerator();
            var initialSpecifications = (await specificationGenerator
                .GenerateSpecificationsAsync(codeHierarchy, includeImplementationDetails: false)).ToList();

            // Return result
            return new CodeAnalysisResult
            {
                Components = components,
                Relationships = relationships,
                CodeHierarchy = codeHierarchy,
                InitialSpecifications = initialSpecifications
            };
        }

        private static async Task<DocumentationExtractionResult> ExtractDocumentationAsync(
            List<string> documentationFiles,
            Kernel? kernel,
            CodeHierarchy codeHierarchy)
        {
            var snippets = new List<DocumentationExtraction.Extractors.DocumentationSnippet>();

            // Create Markitdown converter (no AI dependency; converts files directly)
            var markitdownConverter = new MarkitdownConverter();

            // Use the AI-backed extractor when a kernel is configured,
            // otherwise fall back to the local, deterministic extractor.
            DocumentationExtraction.Extractors.ISemanticKernelService semanticExtractor =
                kernel is not null
                    ? new DocumentationExtraction.Extractors.SemanticKernelMistralService(kernel)
                    : new DocumentationExtraction.Extractors.LocalSemanticKernelService();

            foreach (var docFile in documentationFiles)
            {
                // Detect document type
                var documentType = MarkitdownConverter.DetectDocumentType(docFile);

                // Convert to Markdown
                var markdownContent = await markitdownConverter.ConvertToMarkdownAsync(docFile, documentType);

                // Parse Markdown
                var markdownParser = new MarkdownParser();
                var parsedDocument = markdownParser.Parse(markdownContent);

                // Analyze structure
                var structureAnalyzer = new StructureAnalyzer();
                var documentStructure = structureAnalyzer.AnalyzeStructure(parsedDocument);

                // Extract relevant snippets (AI-backed when configured)
                var docSnippets = await semanticExtractor.ExtractRelevantSnippetsAsync(
                    parsedDocument, documentStructure, codeHierarchy);

                // Set source document
                foreach (var snippet in docSnippets)
                {
                    snippet.SourceDocument = docFile;
                }

                snippets.AddRange(docSnippets);
            }

            // Return result
            return new DocumentationExtractionResult
            {
                Snippets = snippets
            };
        }

        private static List<SpecificationFile> ConsolidateSources(
            CodeAnalysisResult codeAnalysis,
            DocumentationExtractionResult docExtraction)
        {
            // Delegates to the SourceConsolidation component (single source of truth)
            SourceConsolidation.SourceConsolidator consolidator = new();
            return consolidator.ConsolidateSources(codeAnalysis, docExtraction).ToList();
        }

        private async Task<HierarchyOptimizationResult> OptimizeHierarchyAsync(
            List<SpecificationFile> specifications,
            HierarchyOptimizerConfig config)
        {
            // Create genome encoder
            var genomeEncoder = new SpecificationGenomeEncoder();

            // Create hierarchy optimizer
            var hierarchyOptimizer = new HierarchyOptimizer(genomeEncoder);

            // Subscribe to progress updates
            hierarchyOptimizer.OptimizationProgressUpdated += (sender, e) =>
            {
                double progressPercentage = e.ProgressPercentage;
                ReportProgress(
                    PipelineStage.HierarchyOptimization,
                    $"Optimizing hierarchy: Generation {e.CurrentGeneration}/{e.MaxGenerations}, Best fitness: {e.BestFitness:F4}",
                    progressPercentage);
            };

            // Run optimization
            return await hierarchyOptimizer.OptimizeHierarchyAsync(specifications, config);
        }

        private static List<string> GenerateOutputFiles(List<SpecificationFile> specifications, string outputDirectory)
        {
            var outputFilePaths = new List<string>();

            foreach (var spec in specifications)
            {
                // Generate file name
                var fileName = spec.Name.Replace(" ", "_").Replace(".", "_") + ".md";
                var filePath = Path.Combine(outputDirectory, fileName);

                // Write file
                File.WriteAllText(filePath, spec.Content);

                // Add to output paths
                outputFilePaths.Add(filePath);
            }

            return outputFilePaths;
        }

        private void ReportProgress(PipelineStage stage, string message, double progressPercentage)
        {
            ProgressUpdated?.Invoke(this, new PipelineProgressEventArgs
            {
                Stage = stage,
                Message = message,
                ProgressPercentage = progressPercentage
            });
        }
    }

}