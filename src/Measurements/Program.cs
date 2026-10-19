using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using SpecTreeGenerator.Common.Models;

namespace SpecTreeGenerator.Measurements
{
    /// <summary>
    /// Measurement harness for the research article (issue #30).
    ///
    /// Usage:
    ///   spectree-measure hierarchy --source &lt;dir&gt; [--output &lt;dir&gt;] [--max-minutes N] [--seed N]
    ///   spectree-measure optimizers --source &lt;dir&gt; [--max-minutes N] [--seed N]
    ///   spectree-measure sensitivity --source &lt;dir&gt; [--seed N]
    ///   spectree-measure docmapping --source &lt;dir&gt; --docs &lt;dir&gt; --ground-truth &lt;file&gt;
    ///   spectree-measure languages --source &lt;dir&gt;
    /// </summary>
    public static class Program
    {
        public static async Task<int> Main(string[] args)
        {
            if (args.Length < 2)
            {
                PrintUsage();
                return 2;
            }

            var command = args[0];
            string? source = null;
            var output = Directory.GetCurrentDirectory();
            string? docs = null;
            string? groundTruth = null;
            var maxMinutes = 5;
            int? seed = null;

            for (var i = 1; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "--source":
                        source = Value(args, ref i);
                        break;
                    case "--output":
                        output = Value(args, ref i);
                        break;
                    case "--docs":
                        docs = Value(args, ref i);
                        break;
                    case "--ground-truth":
                        groundTruth = Value(args, ref i);
                        break;
                    case "--max-minutes":
                        maxMinutes = int.TryParse(Value(args, ref i), out var minutes) ? minutes : 5;
                        break;
                    case "--seed":
                        seed = int.TryParse(Value(args, ref i), out var s) ? s : null;
                        break;
                    default:
                        Console.Error.WriteLine($"Unknown argument: {args[i]}");
                        PrintUsage();
                        return 2;
                }
            }

            if (string.IsNullOrEmpty(source) || !Directory.Exists(source))
            {
                Console.Error.WriteLine("Error: --source must point to an existing directory.");
                return 2;
            }

            var optimizerConfig = new HierarchyOptimizerConfig
            {
                MaxOptimizationTimeMinutes = Math.Max(1, maxMinutes),
                EvolutionConfig = new EvolutionConfig { RandomSeed = seed }
            };

            switch (command)
            {
                case "hierarchy":
                {
                    var result = await MeasurementEngine.RunHierarchyBenchmarkAsync(source, output, optimizerConfig);
                    Console.Out.WriteLine(System.Text.Json.JsonSerializer.Serialize(result));
                    return 0;
                }
                case "optimizers":
                {
                    var result = await MeasurementEngine.CompareOptimizersAsync(source, optimizerConfig);
                    Console.Out.WriteLine(System.Text.Json.JsonSerializer.Serialize(result));
                    return 0;
                }
                case "sensitivity":
                {
                    var variants = BuildDefaultSensitivityVariants(seed);
                    var result = await MeasurementEngine.MeasureParameterSensitivityAsync(source, variants);
                    Console.Out.WriteLine(System.Text.Json.JsonSerializer.Serialize(result));
                    return 0;
                }
                case "docmapping":
                {
                    if (string.IsNullOrEmpty(docs) || !Directory.Exists(docs))
                    {
                        Console.Error.WriteLine("Error: docmapping requires --docs pointing to an existing directory.");
                        return 2;
                    }
                    if (string.IsNullOrEmpty(groundTruth) || !File.Exists(groundTruth))
                    {
                        Console.Error.WriteLine("Error: docmapping requires --ground-truth pointing to an existing file.");
                        return 2;
                    }
                    var result = await MeasurementEngine.MeasureDocumentationMappingAsync(source, docs, groundTruth);
                    Console.Out.WriteLine(System.Text.Json.JsonSerializer.Serialize(result));
                    return 0;
                }
                case "languages":
                {
                    var result = await MeasurementEngine.MeasureLanguageFidelityAsync(
                        source, new List<CodeAnalysis.Abstractions.ILanguageAnalysisBackend>());
                    Console.Out.WriteLine(System.Text.Json.JsonSerializer.Serialize(result));
                    return 0;
                }
                default:
                    Console.Error.WriteLine($"Unknown command: {command}");
                    PrintUsage();
                    return 2;
            }
        }

        private static IEnumerable<(string Name, EvolutionConfig Config)> BuildDefaultSensitivityVariants(int? seed)
        {
            yield return ("baseline", new EvolutionConfig { RandomSeed = seed });
            yield return ("population-small", new EvolutionConfig { PopulationSize = 30, RandomSeed = seed });
            yield return ("population-large", new EvolutionConfig { PopulationSize = 200, RandomSeed = seed });
            yield return ("crossover-low", new EvolutionConfig { CrossoverRate = 0.5, RandomSeed = seed });
            yield return ("crossover-high", new EvolutionConfig { CrossoverRate = 0.95, RandomSeed = seed });
            yield return ("mutation-low", new EvolutionConfig { MutationRate = 0.02, RandomSeed = seed });
            yield return ("mutation-high", new EvolutionConfig { MutationRate = 0.3, RandomSeed = seed });
            yield return ("elitism-none", new EvolutionConfig { ElitismCount = 0, RandomSeed = seed });
            yield return ("elitism-high", new EvolutionConfig { ElitismCount = 15, RandomSeed = seed });
        }

        private static string Value(string[] args, ref int i)
        {
            if (i + 1 >= args.Length)
            {
                throw new ArgumentException($"Missing value for {args[i]}");
            }
            i++;
            return args[i];
        }

        private static void PrintUsage()
        {
            Console.Error.WriteLine(
                "Usage: spectree-measure <hierarchy|optimizers|sensitivity|docmapping|languages> --source <dir> [options]");
        }
    }
}
