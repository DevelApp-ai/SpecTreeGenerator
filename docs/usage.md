# SpecTreeGenerator Usage Guide

This document provides detailed instructions for using the SpecTreeGenerator system to optimize specifications based on code analysis and genetic algorithms.

## Overview

SpecTreeGenerator is a tool that analyzes your codebase and documentation, then uses genetic algorithms to optimize the specification hierarchy, reducing redundancy and improving maintainability.

## Supported languages and project structures

### Programming languages

Code analysis is **backend-driven**: each language is served by an analysis
backend (`ILanguageAnalysisBackend`) registered for its file extensions, and
the pipeline routes every discovered file to the backend that claims its
extension (`AnalysisBackendRegistry.ForFile`).

| Language | Extension(s) | Analysis backend | Relationship fidelity |
|---|---|---|---|
| C# (up to C# 12) | `.cs` | `RoslynAnalysisBackend` (Roslyn semantic model) | Precise: binder-resolved inheritance, implementation, and method-call edges |

Files whose extension no backend claims are **skipped with a warning**
(`no analysis backend for '<path>'; file skipped`) — the run continues.

Additional languages (Java 8+, Python 3.11, TypeScript, and the rest of the
[Minotaur-Grammars](https://github.com/DevelApp-ai/Minotaur-Grammars)
catalog) are served by the `MinotaurAnalysisBackend`. Relationship fidelity
is **tiered** (issue
[#27](https://github.com/DevelApp-ai/SpecTreeGenerator/issues/27)): when
the grammar carries Minotaur binding annotations, references resolve
through the grammar-driven semantic binder with typed symbol identity
(`ResolutionTier.Binder` — no wrong edges from same-named types in
different namespaces); without annotations the backend falls back to
declaration-name matching (`ResolutionTier.NameBased`), and same-name
collisions resolve to *unresolved* rather than a guessed target. Every
reference records its tier. See the
[language version matrix](https://github.com/DevelApp-ai/Minotaur-Grammars/blob/main/docs/language-versions.md).

### Project structures

There are **no constraints on project structure**:

- The pipeline recursively scans the configured `SourceCodeDirectory` for
  files with backend-claimed extensions (see
  `OptimizationPipeline.AnalyzeCodeAsync`).
- No `.csproj` or `.sln` file is required for analysis; loose files work too.
- Any layout works, including `src/` + `tests/` conventions, nested solutions,
  monorepos (point at the sub-directory of interest), or single-file projects.

### Documentation formats

Documentation extraction supports Markdown (`.md`), HTML (`.html`, `.htm`),
plain text (`.txt`), and Word (`.docx`); PDF (`.pdf`) is experimental. Files are
converted to Markdown and mapped to the analyzed code components.

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

## Machine-readable output: the spectree.json contract

Every pipeline run writes a machine-readable description of the optimized
spec tree next to the `.md` files: `spectree.json`. It is the stable
contract consumed by downstream tools such as **CitizenCodeLab project
import** (which maps the tree onto its virtual structure and generates
initial `.ai` specification files).

### Schema (version 1.0)

```json
{
  "schemaVersion": "1.0",
  "generatedAtUtc": "2026-10-15T19:40:00Z",
  "sourceCodeDirectory": "/path/to/your/code",
  "roots": [
    {
      "id": "<specification id>",
      "name": "<specification name>",
      "componentId": "<analyzed component id>",
      "parentSpecificationId": null,
      "filePath": "<original source file path>",
      "outputFileName": "<Name_With_Underscores>.md",
      "metadata": { },
      "children": [ { "..." : "nested the same way" } ]
    }
  ],
  "metrics": {
    "originalSize": 0,
    "optimizedSize": 0,
    "sizeReductionPercentage": 0.0,
    "totalSections": 0,
    "inheritedSections": 0,
    "inheritancePercentage": 0.0,
    "hierarchyDepth": 0
  }
}
```

Notes for consumers:

- `roots` is the top level of the optimized hierarchy; each node's `children`
  nest the same node shape recursively.
- A specification whose `parentSpecificationId` refers to a specification not
  present in the tree appears at the root level (never dropped).
- `outputFileName` matches the `.md` file written by the pipeline, relative to
  the output directory.
- `metadata` is an open dictionary; keys are added by the consolidation and
  optimization stages.

### CLI

The `SpecTreeGenerator.Cli` project (assembly `spectree`) exposes the pipeline
as a command-line tool for headless/embedded use:

```bash
dotnet run --project src/Cli -- \
  --source /path/to/your/code \
  --output /path/to/output \
  [--docs /path/to/docs] \
  [--json-only] \
  [--max-minutes 5]
```

Options:

| Option | Description | Default |
|--------|-------------|---------|
| `--source` | Directory containing the source code to reverse-engineer | (required) |
| `--output` | Directory for the `.md` files and `spectree.json` | current directory |
| `--docs` | Directory scanned for documentation (`.md`, `.html`, `.txt`, `.docx`) | none |
| `--json-only` | Write only `spectree.json`, skip the `.md` files | false |
| `--max-minutes` | Maximum time in minutes for hierarchy optimization | 5 |
| `--mistral-api-key` | Mistral AI API key (falls back to the local deterministic extractor) | `MISTRAL_API_KEY` env |

The absolute path of the written `spectree.json` is printed to **stdout**
(only that path, for easy scripting); progress and diagnostics go to stderr.

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
