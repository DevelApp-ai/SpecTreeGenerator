using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using SpecTreeGenerator.Common.Models;

namespace SpecTreeGenerator.Pipeline
{
    /// <summary>
    /// Machine-readable description of a single specification node in the spec tree.
    /// </summary>
    public class SpecTreeNodeDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string ComponentId { get; set; } = string.Empty;
        public string? ParentSpecificationId { get; set; }
        public string FilePath { get; set; } = string.Empty;
        public string OutputFileName { get; set; } = string.Empty;
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
        public List<SpecTreeNodeDto> Children { get; set; } = new List<SpecTreeNodeDto>();
    }

    /// <summary>
    /// Root document of the spec-tree JSON contract (spectree.json) consumed by
    /// downstream tools such as CitizenCodeLab project import.
    /// </summary>
    public class SpecTreeDocumentDto
    {
        public string SchemaVersion { get; set; } = "1.0";
        public DateTime GeneratedAtUtc { get; set; } = DateTime.UtcNow;
        public string SourceCodeDirectory { get; set; } = string.Empty;
        public List<SpecTreeNodeDto> Roots { get; set; } = new List<SpecTreeNodeDto>();
        public OptimizationMetricsDto Metrics { get; set; } = new OptimizationMetricsDto();
    }

    /// <summary>
    /// Optimization metrics as plain serializable values.
    /// </summary>
    public class OptimizationMetricsDto
    {
        public int OriginalSize { get; set; }
        public int OptimizedSize { get; set; }
        public double SizeReductionPercentage { get; set; }
        public int TotalSections { get; set; }
        public int InheritedSections { get; set; }
        public double InheritancePercentage { get; set; }
        public int HierarchyDepth { get; set; }
    }

    /// <summary>
    /// Writes the spec tree as JSON (spectree.json) next to the markdown output files.
    /// </summary>
    public static class SpecTreeJsonWriter
    {
        public const string DefaultFileName = "spectree.json";

        private static readonly JsonSerializerOptions SerializerOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.Never
        };

        /// <summary>
        /// Builds the tree document from the flat list of optimized specifications.
        /// </summary>
        public static SpecTreeDocumentDto BuildDocument(
            IReadOnlyList<SpecificationFile> specifications,
            OptimizationMetrics metrics,
            string sourceCodeDirectory)
        {
            ArgumentNullException.ThrowIfNull(specifications);
            ArgumentNullException.ThrowIfNull(metrics);

            var nodesById = new Dictionary<string, SpecTreeNodeDto>();
            var roots = new List<SpecTreeNodeDto>();

            foreach (var spec in specifications)
            {
                nodesById[spec.Id] = ToDto(spec);
            }

            foreach (var spec in specifications)
            {
                var node = nodesById[spec.Id];
                if (spec.ParentSpecificationId != null
                    && nodesById.TryGetValue(spec.ParentSpecificationId, out var parent))
                {
                    parent.Children.Add(node);
                }
                else
                {
                    roots.Add(node);
                }
            }

            return new SpecTreeDocumentDto
            {
                SourceCodeDirectory = sourceCodeDirectory ?? string.Empty,
                Roots = roots,
                Metrics = new OptimizationMetricsDto
                {
                    OriginalSize = metrics.OriginalSize,
                    OptimizedSize = metrics.OptimizedSize,
                    SizeReductionPercentage = metrics.SizeReductionPercentage,
                    TotalSections = metrics.TotalSections,
                    InheritedSections = metrics.InheritedSections,
                    InheritancePercentage = metrics.InheritancePercentage,
                    HierarchyDepth = metrics.HierarchyDepth
                }
            };
        }

        /// <summary>
        /// Writes the document to <paramref name="outputDirectory"/>/spectree.json.
        /// </summary>
        public static string Write(SpecTreeDocumentDto document, string outputDirectory)
        {
            ArgumentNullException.ThrowIfNull(document);
            if (string.IsNullOrEmpty(outputDirectory)) throw new ArgumentException("Output directory is required", nameof(outputDirectory));

            Directory.CreateDirectory(outputDirectory);
            var filePath = Path.Combine(outputDirectory, DefaultFileName);
            File.WriteAllText(filePath, JsonSerializer.Serialize(document, SerializerOptions));
            return filePath;
        }

        private static SpecTreeNodeDto ToDto(SpecificationFile spec)
        {
            return new SpecTreeNodeDto
            {
                Id = spec.Id,
                Name = spec.Name,
                ComponentId = spec.ComponentId,
                ParentSpecificationId = spec.ParentSpecificationId,
                FilePath = spec.FilePath,
                OutputFileName = GetOutputFileName(spec),
                Metadata = spec.Metadata
            };
        }

        private static string GetOutputFileName(SpecificationFile spec)
        {
            return spec.Name.Replace(" ", "_").Replace(".", "_") + ".md";
        }
    }
}
