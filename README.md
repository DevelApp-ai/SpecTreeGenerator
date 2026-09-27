# SpecTreeGenerator (GeneticSpecOptimizer)

SpecTreeGenerator reverse-engineers an existing codebase into an optimized
tree of specification files: it analyzes your source code, extracts relevant
documentation, and uses a genetic algorithm to optimize the specification
hierarchy — removing redundancy and pushing shared content into inherited
sections.

The repository is named **SpecTreeGenerator**; the solution, projects, and
namespaces are named **GeneticSpecOptimizer**. The two names refer to the same
tool; the code-level name is `GeneticSpecOptimizer`.

## Features

- **Code analysis** — Roslyn-based extraction of components, relationships, and hierarchy
- **Documentation extraction** — converts Markdown, HTML, Word, and plain text
  docs to Markdown and maps sections to code components
- **AI-backed semantic analysis (optional)** — Mistral AI (default) or Azure AI
  Foundry via Semantic Kernel; falls back to a local deterministic extractor
  when no credentials are configured
- **Genetic algorithm optimization** — evolves the specification hierarchy to
  minimize size and redundancy
- **No credentials required** — the happy path runs fully offline

## Build

Requires the [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0).

```bash
git clone https://github.com/DevelApp-ai/SpecTreeGenerator.git
cd SpecTreeGenerator
dotnet build GeneticSpecOptimizer.sln -c Release
```

## Run the sample

```bash
dotnet run --project src/Sample -c Release
```

The sample generates a small sample project, runs the full pipeline
(code analysis → documentation extraction → source consolidation →
hierarchy optimization → output generation), and writes the optimized
specification files to `output/`.

## Optional: AI-backed semantic analysis

```bash
export MISTRAL_API_KEY=...   # enables Mistral AI
dotnet run --project src/Sample -c Release
```

Without a key, the pipeline uses the local fallback extractor.

## Documentation

- [Architecture Overview](docs/architecture.md)
- [Installation Guide](docs/installation.md)
- [Usage Guide](docs/usage.md)

## Solution layout

| Project | Purpose |
|---------|---------|
| `GeneticSpecOptimizer.Common` | Shared models (components, hierarchy, configs) |
| `GeneticSpecOptimizer.CodeAnalysis` | Source code parsing and analysis |
| `GeneticSpecOptimizer.DocumentationExtraction` | Document conversion and semantic extraction |
| `GeneticSpecOptimizer.SourceConsolidation` | Merges code + documentation results |
| `GeneticSpecOptimizer.GeneticAlgorithm` | Genome encoding, operators, fitness, evolution |
| `GeneticSpecOptimizer.HierarchyOptimization` | Hierarchy optimization driver |
| `GeneticSpecOptimizer.Pipeline` | End-to-end pipeline orchestration |
| `Sample` | Runnable sample application |
