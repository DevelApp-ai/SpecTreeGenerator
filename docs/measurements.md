# Measurements (issue #30)

This harness produces the quantitative results required before any number enters the
`spectree-search-based-specification-recovery` article (DevelApp-ai/Research-article-builder,
`articles/spectree-search-based-specification-recovery`).

Ground rules (per the measurement backlog index, Research-article-builder#27): a number stays
out of the article until the corresponding measurement closes with committed data — benchmark
code, raw results, and a reproducible environment. Measure-or-remove applies to every claim.

## Tool

`spectree-measure` (src/Measurements) runs every measurement through the real pipeline
(`OptimizationPipeline`); nothing is mocked:

```
spectree-measure hierarchy     --source <repo> [--output <dir>] [--max-minutes N] [--seed N]
spectree-measure optimizers   --source <repo> [--max-minutes N] [--seed N]
spectree-measure sensitivity  --source <repo> [--seed N]
spectree-measure docmapping   --source <repo> --docs <dir> --ground-truth <file>
spectree-measure languages    --source <repo>
```

All commands print a single JSON object (or array) on stdout; diagnostics go to stderr.

### Commands and the article claims they support

| Command | Measurement (issue #30) | Output fields |
|---|---|---|
| `hierarchy` | Hierarchy quality vs. flat baseline; runtime and memory | `flatLayoutSize`, `optimizedSize`, `sizeReductionPercentage`, `redundancyReductionPercentage`, `hierarchyDepth`, `inheritedSections`/`totalSections`, `stageDurationsMs`, `peakWorkingSetBytes`, `totalWallTimeSeconds` |
| `optimizers` | Genetic optimizer vs. greedy/local-search baseline; convergence curves | `geneticBestFitness`, `greedyBestFitness`, `geneticGenerations`, times, `fitnessHistory` (best fitness per generation = convergence curve) |
| `sensitivity` | Parameter sensitivity (population size, crossover/mutation rates, elitism) | one record per variant: `populationSize`, `crossoverRate`, `mutationRate`, `elitismCount`, `bestFitness`, `generations`, `converged` |
| `docmapping` | Documentation-mapping precision/recall vs. hand labels | `precision`, `recall`, `f1`, `truePositives`, `falsePositives`, `falseNegatives` |
| `languages` | Per-language extraction fidelity of analysis backends | components, relationships, doc-carrying components per backend/language |

### Ground truth format (`docmapping`)

One mapping per line, `section title|component name`:

```
Widget Service|WidgetService
Gadget Service|GadgetService
```

A predicted mapping is a `(SourceSectionTitle, component name)` pair from the documentation
stage; a true positive requires an exact pair match. Report per source format (MD/HTML/Word/txt)
by running the command once per format directory.

### Randomness

Pass `--seed N` for reproducible runs; the seed is forwarded to `EvolutionConfig.RandomSeed`.
Sensitivity variants reuse the same seed so only the varied parameter differs.

## CI (reproducible environment)

`.github/workflows/measurements.yml` runs the battery on three real repositories of this
organization (SpecTreeGenerator itself, CitizenCodeLab, EventStorming), on every push to
`main` and on demand. Each run:

1. builds the tool (`dotnet publish src/Measurements -c Release`),
2. clones the three target repositories at the recorded commits,
3. runs `hierarchy` (per repo), `optimizers` (per repo), `sensitivity` (one repo),
   `docmapping` (sample fixture with hand labels), `languages` (per repo),
4. uploads `results/*.json` and `results/manifest.txt` as the workflow artifact
   `measurements-results`.

The artifact is the raw result data for the article; commit the relevant numbers into the
article only after reviewing them against the measure-or-remove policy.

## Local run

```
dotnet run --project src/Measurements -- hierarchy --source ../CitizenCodeLab --max-minutes 5 --seed 42
```

## Status of the issue #30 checklist

- Hierarchy quality vs. baselines → `hierarchy` command, CI runs it on 3 real repos
- Genetic optimizer behavior (convergence, parameter sensitivity, greedy baseline) → `optimizers` + `sensitivity`
- Documentation-mapping precision → `docmapping` (hand-labeling remains a per-repo manual step; format documented above)
- Per-language fidelity (Minotaur backend) → `languages` (register Minotaur backends via the API; the CI default run covers the Roslyn C# backend)
- Runtime and memory → `hierarchy` (`stageDurationsMs`, `peakWorkingSetBytes`)
- Comprehension study (stretch) → out of scope for this harness; requires human participants
