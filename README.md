# SpecTreeGenerator

SpecTreeGenerator reverse-engineers an existing codebase into an optimized
tree of specification files: it analyzes your source code, extracts relevant
documentation, and uses a genetic algorithm to optimize the specification
hierarchy — removing redundancy and pushing shared content into inherited
sections.

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

## Supported languages and project structures

Code analysis is **backend-driven**: each language is served by an analysis
backend registered for its file extensions. Files whose extension no backend
claims are skipped with a warning (they never crash the run).

| Language | Extension(s) | Analysis backend | Relationship fidelity |
|---|---|---|---|
| C# (up to C# 12) | `.cs` | `RoslynAnalysisBackend` (Roslyn semantic model) | Precise: binder-resolved inheritance, implementation, and method-call edges |

Additional languages (Java 8+, Python 3.11, TypeScript, and the rest of the
[Minotaur-Grammars](https://github.com/DevelApp-ai/Minotaur-Grammars) catalog)
are served by the `MinotaurAnalysisBackend`. Relationship fidelity is
**tiered** (issue
[#27](https://github.com/DevelApp-ai/SpecTreeGenerator/issues/27)): grammars
with Minotaur binding annotations resolve references through the
grammar-driven semantic binder (typed symbol identity, no wrong edges from
same-named types in different namespaces); grammars without annotations
fall back to declaration-name matching, and same-name collisions resolve to
*unresolved* rather than a guessed target. Every reference records its
resolution tier.

There are **no constraints on project structure**: the pipeline scans the
configured `SourceCodeDirectory` recursively for files with backend-claimed
extensions, so it works with any layout (project folders, `src/` + `tests/`
conventions, nested solutions, or loose files). No `.csproj` or `.sln` file is
required for analysis.

Documentation extraction additionally accepts `.md`, `.html`/`.htm`, `.txt`,
and `.docx` files (plus experimental `.pdf`). See the
[Usage Guide](docs/usage.md#supported-languages-and-project-structures) for details.

## Build

Requires the [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0).

```bash
git clone https://github.com/DevelApp-ai/SpecTreeGenerator.git
cd SpecTreeGenerator
dotnet build SpecTreeGenerator.sln -c Release
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
| `SpecTreeGenerator.Common` | Shared models (components, hierarchy, configs) |
| `SpecTreeGenerator.CodeAnalysis` | Source code parsing and analysis |
| `SpecTreeGenerator.DocumentationExtraction` | Document conversion and semantic extraction |
| `SpecTreeGenerator.SourceConsolidation` | Merges code + documentation results |
| `SpecTreeGenerator.GeneticAlgorithm` | Genome encoding, operators, fitness, evolution |
| `SpecTreeGenerator.HierarchyOptimization` | Hierarchy optimization driver |
| `SpecTreeGenerator.Pipeline` | End-to-end pipeline orchestration |
| `Sample` | Runnable sample application |
