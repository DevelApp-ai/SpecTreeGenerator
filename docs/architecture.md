# GeneticSpecOptimizer Architecture Overview

This document provides an overview of the GeneticSpecOptimizer system architecture, explaining how the different components work together to optimize specifications using genetic algorithms.

## System Architecture

The GeneticSpecOptimizer system is composed of several interconnected components that work together to analyze code, extract documentation, and optimize specifications using genetic algorithms.

```
┌─────────────────┐     ┌─────────────────┐     ┌─────────────────┐
│  Code Analysis  │     │  Documentation  │     │     Source      │
│                 │────▶│   Extraction    │────▶│ Consolidation   │
└─────────────────┘     └─────────────────┘     └─────────────────┘
                                                        │
                                                        ▼
┌─────────────────┐     ┌─────────────────┐     ┌─────────────────┐
│     Output      │     │    Hierarchy    │     │    Genetic      │
│   Generation    │◀────│  Optimization   │◀────│   Algorithm     │
└─────────────────┘     └─────────────────┘     └─────────────────┘
```

### Pipeline Flow

1. **Code Analysis**: Analyzes source code to extract components, relationships, and hierarchy
2. **Documentation Extraction**: Extracts relevant information from documentation files
3. **Source Consolidation**: Combines code analysis and documentation extraction results
4. **Genetic Algorithm**: Encodes specifications as genomes and evolves them
5. **Hierarchy Optimization**: Optimizes the specification hierarchy using genetic algorithms
6. **Output Generation**: Generates optimized specification files

## Component Details

### Common Models

The `GeneticSpecOptimizer.Common` project contains shared models used throughout the system:

- `CodeComponent`: Represents a component in the code (class, method, property, etc.)
- `CodeRelationship`: Represents a relationship between components
- `CodeHierarchy`: Represents the hierarchical structure of components
- `SpecificationFile`: Represents a specification file
- `TextSpan`: Represents a span of text in a source file

### Code Analysis

The `GeneticSpecOptimizer.CodeAnalysis` project is responsible for analyzing source code:

- `LanguageParser`: Parses source code files
- `SymbolTableBuilder`: Builds a symbol table from parsed code
- `ComponentExtractor`: Extracts components from parsed code
- `RelationshipAnalyzer`: Analyzes relationships between components
- `HierarchyBuilder`: Builds a hierarchy of components
- `SpecificationGenerator`: Generates initial specifications from code

### Documentation Extraction

The `GeneticSpecOptimizer.DocumentationExtraction` project extracts information from documentation:

- `MarkitdownConverter`: Converts various document formats to Markdown
- `MarkdownParser`: Parses Markdown documents
- `StructureAnalyzer`: Analyzes the structure of parsed documents
- `SemanticExtractor`: Extracts semantically relevant snippets

### Genetic Algorithm

The `GeneticSpecOptimizer.GeneticAlgorithm` project implements the genetic algorithm:

- `SpecificationGenomeEncoder`: Encodes specifications as genomes
- `GeneticOperators`: Implements genetic operators (mutation, crossover)
- `FitnessEvaluator`: Evaluates the fitness of genomes
- `EvolutionEngine`: Runs the evolution process

### Hierarchy Optimization

The `GeneticSpecOptimizer.HierarchyOptimization` project optimizes the specification hierarchy:

- `HierarchyOptimizer`: Optimizes the hierarchy using genetic algorithms
- `HierarchyOptimizerConfig`: Configuration for the optimizer

### Pipeline

The `GeneticSpecOptimizer.Pipeline` project ties everything together:

- `OptimizationPipeline`: Main pipeline that orchestrates the optimization process
- `OptimizationPipelineConfig`: Configuration for the pipeline

## Data Flow

1. Source code files are parsed and analyzed to extract components, relationships, and hierarchy
2. Documentation files are parsed and analyzed to extract relevant information
3. Code analysis and documentation extraction results are consolidated
4. Specifications are encoded as genomes
5. Genomes are evolved using genetic algorithms to optimize the hierarchy
6. Optimized specifications are generated as output files

## Integration Points

### AI-Backed Semantic Analysis (Semantic Kernel)

The semantic analysis stage can use an LLM to map documentation sections to code
components. The AI backend is configured through `OptimizationPipelineConfig`
(see [Usage Guide](usage.md)) and is created by `SemanticKernelFactory`:

- **Mistral AI** (default when `MistralApiKey` is set): uses the official
  Semantic Kernel `MistralAI` connector (experimental package) against the
  Mistral AI API. `SemanticKernelMistralService` extracts relevance-scored
  snippets per section.
- **Azure AI Foundry** (when `AzureAIFoundryEndpoint` + `AzureAIFoundryApiKey`
  are set): uses the Azure OpenAI connector with the configured deployment.
- **Local fallback** (default, no credentials required): when no AI credentials
  are configured, `LocalSemanticKernelService` maps sections to components using
  deterministic keyword matching. The happy path never requires credentials.

### Document Conversion (MarkitdownConverter)

`MarkitdownConverter` converts documentation files to Markdown without any AI
dependency:

- `.md`, `.txt` — returned as-is
- `.html` — converted to Markdown (headings, lists, links, emphasis, code blocks)
- `.docx` — text extracted from the Office Open XML document body
- `.pdf` — **experimental**: best-effort text extraction from FlateDecode
  content streams; scanned or CID-encoded PDFs are not supported and raise a
  clear `NotSupportedException`

## Extension Points

The system is designed to be extensible in several ways:

1. **Language Support**: Add new language parsers to support additional programming languages
2. **Document Formats**: Add support for additional document formats
3. **Fitness Functions**: Customize fitness functions for different optimization goals
4. **Genetic Operators**: Implement custom genetic operators for specific needs
5. **Output Formats**: Add support for additional output formats

## Performance Considerations

- **Memory Usage**: Processing large codebases may require significant memory
- **Computation Time**: Genetic algorithm evolution can be computationally intensive
- **Parallelization**: Many operations are parallelized for better performance
- **Caching**: Results are cached where appropriate to avoid redundant computation

## Security Considerations

- **Code Access**: The system needs read access to source code files
- **API Keys**: Azure AI Foundry integration requires API keys
- **Output Files**: Output files are written to the specified output directory

## Next Steps

For more detailed information, refer to:

- [Installation Guide](installation.md)
- [Usage Guide](usage.md)
