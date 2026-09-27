# GeneticSpecOptimizer Usage Guide

This document provides detailed instructions for using the GeneticSpecOptimizer system to optimize specifications based on code analysis and genetic algorithms.

## Overview

GeneticSpecOptimizer is a tool that analyzes your codebase and documentation, then uses genetic algorithms to optimize the specification hierarchy, reducing redundancy and improving maintainability.

## Basic Usage

### Running the Pipeline

The main entry point for the system is the `OptimizationPipeline` class. Here's a basic example of how to use it:

```csharp
// Create configuration
var config = new OptimizationPipelineConfig
{
    SourceCodeDirectory = "/path/to/your/code",
    DocumentationFiles = new List<string> { "/path/to/docs/README.md" },
    OutputDirectory = "/path/to/output",
    OptimizerConfig = new HierarchyOptimizerConfig
    {
        MaxOptimizationTimeMinutes = 5,
        EvolutionConfig = new EvolutionConfig
        {
            PopulationSize = 50,
            MaxGenerations = 100,
            ElitismCount = 5
        }
    }
};

// Create pipeline
var pipeline = new OptimizationPipeline();

// Subscribe to progress updates (optional)
pipeline.ProgressUpdated += (sender, e) =>
{
    Console.WriteLine($"[{e.Stage}] {e.Message} - {e.ProgressPercentage:F1}%");
};

// Run pipeline
var result = await pipeline.RunAsync(config);

// Process results
Console.WriteLine($"Optimization completed with {result.OptimizedSpecifications.Count} specifications");
Console.WriteLine($"Size reduction: {result.Metrics.SizeReductionPercentage:F1}%");
```

### Configuration Options

#### OptimizationPipelineConfig

| Property | Description | Default |
|----------|-------------|---------|
| SourceCodeDirectory | Path to the source code directory | (Required) |
| DocumentationFiles | List of paths to documentation files | Empty list |
| OutputDirectory | Path to the output directory | (Required) |
| OptimizerConfig | Configuration for the hierarchy optimizer | Default config |
| AzureAIFoundryEndpoint | Azure AI Foundry endpoint for Semantic Kernel (used only when Mistral API key is not set) | Empty string |
| AzureAIFoundryApiKey | Azure AI Foundry API key | Empty string |
| AzureAIFoundryDeploymentName | Azure AI Foundry deployment name | "mistral" |
| MistralApiKey | Mistral AI API key — enables Mistral-backed semantic analysis (takes priority over Azure settings) | Empty string |
| MistralModel | Mistral AI model to use | "mistral-large-latest" |
| MistralEndpoint | Optional custom Mistral-compatible endpoint | Empty string |

#### HierarchyOptimizerConfig

| Property | Description | Default |
|----------|-------------|---------|
| MaxOptimizationTimeMinutes | Maximum time to spend optimizing (in minutes) | 5 |
| SizeReductionWeight | Weight for size reduction in fitness calculation | 0.4 |
| RedundancyReductionWeight | Weight for redundancy reduction in fitness calculation | 0.3 |
| InheritanceWeight | Weight for inheritance in fitness calculation | 0.2 |
| CodeAlignmentWeight | Weight for code alignment in fitness calculation | 0.1 |
| MaxHierarchyDepth | Maximum hierarchy depth to allow | 5 |
| EvolutionConfig | Configuration for the genetic algorithm | Default config |

#### EvolutionConfig

| Property | Description | Default |
|----------|-------------|---------|
| PopulationSize | Size of the population | 100 |
| MaxGenerations | Maximum number of generations to run | 50 |
| MutationRate | Probability of mutation | 0.1 |
| CrossoverRate | Probability of crossover | 0.8 |
| ElitismCount | Number of elite individuals to preserve | 5 |
| TournamentSize | Tournament size for selection | 3 |
| ConvergenceThreshold | Convergence threshold | 0.001 |
| ConvergenceGenerations | Number of generations with no improvement to consider converged | 10 |
| RandomSeed | Random seed for reproducibility | null |

## Advanced Usage

### Custom Fitness Functions

You can customize the fitness function by modifying the weights in the `HierarchyOptimizerConfig`:

```csharp
var config = new OptimizationPipelineConfig
{
    // ... other settings ...
    OptimizerConfig = new HierarchyOptimizerConfig
    {
        // Prioritize inheritance over size reduction
        SizeReductionWeight = 0.2,
        RedundancyReductionWeight = 0.2,
        InheritanceWeight = 0.5,
        CodeAlignmentWeight = 0.1
    }
};
```

### AI-backed semantic analysis (optional)

The pipeline runs **without any credentials** by default, using a local,
deterministic extractor. To enable LLM-backed semantic analysis, configure
either backend:

**Mistral AI (preferred):**

```csharp
var config = new OptimizationPipelineConfig
{
    // ... other settings ...
    MistralApiKey = Environment.GetEnvironmentVariable("MISTRAL_API_KEY"),
    MistralModel = "mistral-large-latest"
};
```

**Azure AI Foundry:**

```csharp
var config = new OptimizationPipelineConfig
{
    // ... other settings ...
    AzureAIFoundryEndpoint = "https://your-endpoint.openai.azure.com/",
    AzureAIFoundryApiKey = "your-api-key",
    AzureAIFoundryDeploymentName = "your-deployment-name"
};
```

When no credentials are configured, the pipeline uses
`LocalSemanticKernelService` (keyword matching) — no network access needed.

### Processing Results

The `OptimizationPipelineResult` contains:

- `OptimizedSpecifications`: List of optimized specification files
- `Metrics`: Optimization metrics
- `OutputFilePaths`: Paths to the generated output files

Example of processing results:

```csharp
var result = await pipeline.RunAsync(config);

// Display metrics
Console.WriteLine($"Original size: {result.Metrics.OriginalSize} characters");
Console.WriteLine($"Optimized size: {result.Metrics.OptimizedSize} characters");
Console.WriteLine($"Size reduction: {result.Metrics.SizeReductionPercentage:F1}%");
Console.WriteLine($"Inherited sections: {result.Metrics.InheritedSections}/{result.Metrics.TotalSections}");
Console.WriteLine($"Hierarchy depth: {result.Metrics.HierarchyDepth}");
Console.WriteLine($"Optimization time: {result.Metrics.OptimizationTime}");

// Process output files
foreach (var file in result.OutputFilePaths)
{
    Console.WriteLine($"Generated: {file}");
}
```

## Best Practices

1. **Start Small**: Begin with a small codebase to understand how the system works before applying it to larger projects.

2. **Tune Weights**: Experiment with different weight configurations to achieve the desired balance between size reduction, redundancy reduction, and inheritance.

3. **Provide Quality Documentation**: The better your documentation, the more effective the optimization will be.

4. **Review Results**: Always review the optimized specifications to ensure they meet your requirements.

5. **Iterative Approach**: Use an iterative approach, applying the optimizer multiple times with different configurations.

## Troubleshooting

### Common Issues

#### Long Optimization Times

If optimization is taking too long:

- Reduce the `MaxGenerations` in `EvolutionConfig`
- Reduce the `PopulationSize` in `EvolutionConfig`
- Set a lower `MaxOptimizationTimeMinutes` in `HierarchyOptimizerConfig`

#### Poor Optimization Results

If optimization results are not satisfactory:

- Increase the `PopulationSize` and `MaxGenerations` in `EvolutionConfig`
- Adjust the weights in `HierarchyOptimizerConfig`
- Provide more detailed documentation files

#### Memory Issues

If you encounter memory issues:

- Process smaller portions of your codebase at a time
- Reduce the `PopulationSize` in `EvolutionConfig`

## Next Steps

For more detailed information about specific components, refer to:

- [Architecture Overview](architecture.md)
- [API Reference](api_reference.md)
- [Genetic Algorithm Details](genetic_algorithm.md)
