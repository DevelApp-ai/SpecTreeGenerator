using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using SpecTreeGenerator.Common.Models;
using SpecTreeGenerator.Pipeline;

namespace SpecTreeGenerator.Cli
{
    /// <summary>
    /// Command-line entry point for generating an optimized spec tree from an
    /// existing codebase. Designed for embedding by downstream tools such as
    /// CitizenCodeLab project import.
    ///
    /// Usage:
    ///   spectree --source &lt;dir&gt; [--output &lt;dir&gt;] [--docs &lt;dir&gt;] [--json-only] [--max-minutes N] [--mistral-api-key KEY]
    /// </summary>
    public static class Program
    {
        public static async Task<int> Main(string[] args)
        {
            string? source = null;
            var output = Directory.GetCurrentDirectory();
            string? docs = null;
            var jsonOnly = false;
            var maxMinutes = 5;
            var mistralKey = Environment.GetEnvironmentVariable("MISTRAL_API_KEY") ?? string.Empty;

            for (var i = 0; i < args.Length; i++)
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
                    case "--json-only":
                        jsonOnly = true;
                        break;
                    case "--max-minutes":
                        maxMinutes = int.TryParse(Value(args, ref i), out var minutes) ? minutes : 5;
                        break;
                    case "--mistral-api-key":
                        mistralKey = Value(args, ref i);
                        break;
                    case "--help" or "-h":
                        Console.Out.WriteLine("Usage: spectree --source <dir> [--output <dir>] [--docs <dir>] [--json-only] [--max-minutes N] [--mistral-api-key KEY]");
                        return 0;
                    default:
                        Console.Error.WriteLine($"Unknown argument: {args[i]}");
                        Console.Error.WriteLine("Usage: spectree --source <dir> [--output <dir>] [--docs <dir>] [--json-only] [--max-minutes N] [--mistral-api-key KEY]");
                        return 2;
                }
            }

            if (string.IsNullOrEmpty(source) || !Directory.Exists(source))
            {
                Console.Error.WriteLine("Error: --source must point to an existing directory.");
                return 2;
            }

            await RunAsync(source, output, docs, jsonOnly, maxMinutes, mistralKey);
            return 0;
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

        private static async Task RunAsync(
            string source,
            string output,
            string? docs,
            bool jsonOnly,
            int maxMinutes,
            string mistralKey)
        {
            Console.Error.WriteLine($"SpecTreeGenerator: analyzing {source}");

            var config = new OptimizationPipelineConfig
            {
                SourceCodeDirectory = source,
                OutputDirectory = output,
                OptimizerConfig = new HierarchyOptimizerConfig
                {
                    MaxOptimizationTimeMinutes = Math.Max(1, maxMinutes)
                },
                MistralApiKey = mistralKey
            };

            if (docs != null && Directory.Exists(docs))
            {
                foreach (var file in Directory.EnumerateFiles(docs, "*", SearchOption.AllDirectories))
                {
                    var ext = Path.GetExtension(file).ToLowerInvariant();
                    if (ext is ".md" or ".html" or ".htm" or ".txt" or ".docx")
                    {
                        config.DocumentationFiles.Add(file);
                    }
                }
            }

            var pipeline = new OptimizationPipeline();
            pipeline.ProgressUpdated += (_, e) => Console.Error.WriteLine($"[{e.Stage}] {e.Message} ({e.ProgressPercentage:0}%)");

            var result = await pipeline.RunAsync(config);

            if (jsonOnly)
            {
                foreach (var filePath in result.OutputFilePaths)
                {
                    if (File.Exists(filePath))
                    {
                        File.Delete(filePath);
                    }
                }
            }

            Console.Error.WriteLine($"Spec tree written to {result.SpecTreeJsonPath}");
            Console.Out.WriteLine(result.SpecTreeJsonPath);
        }
    }
}
