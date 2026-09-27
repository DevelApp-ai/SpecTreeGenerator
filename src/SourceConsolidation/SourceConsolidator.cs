using System;
using System.Collections.Generic;
using System.Linq;
using GeneticSpecOptimizer.Common.Models;

namespace GeneticSpecOptimizer.SourceConsolidation
{
    /// <summary>
    /// Result of the code analysis stage. Single source of truth for this contract;
    /// the pipeline and any other consumers reference it from here.
    /// </summary>
    public class CodeAnalysisResult
    {
        /// <summary>
        /// Components extracted from code
        /// </summary>
        public List<CodeComponent> Components { get; set; } = new List<CodeComponent>();

        /// <summary>
        /// Relationships between components
        /// </summary>
        public List<CodeRelationship> Relationships { get; set; } = new List<CodeRelationship>();

        /// <summary>
        /// Code hierarchy
        /// </summary>
        public CodeHierarchy CodeHierarchy { get; set; } = new CodeHierarchy();

        /// <summary>
        /// Initial specifications generated from code
        /// </summary>
        public List<SpecificationFile> InitialSpecifications { get; set; } = new List<SpecificationFile>();
    }

    /// <summary>
    /// Result of the documentation extraction stage. Single source of truth for this contract.
    /// </summary>
    public class DocumentationExtractionResult
    {
        /// <summary>
        /// Snippets extracted from documentation
        /// </summary>
        public List<DocumentationExtraction.Extractors.DocumentationSnippet> Snippets { get; set; } =
            new List<DocumentationExtraction.Extractors.DocumentationSnippet>();
    }

    /// <summary>
    /// Interface for source consolidator
    /// </summary>
    public interface ISourceConsolidator
    {
        /// <summary>
        /// Combines code analysis and documentation extraction results into
        /// a single, consistent set of specification files.
        /// </summary>
        IEnumerable<SpecificationFile> ConsolidateSources(
            CodeAnalysisResult codeAnalysis,
            DocumentationExtractionResult docExtraction);
    }

    /// <summary>
    /// Implementation of source consolidator
    /// </summary>
    public class SourceConsolidator : ISourceConsolidator
    {
        public IEnumerable<SpecificationFile> ConsolidateSources(
            CodeAnalysisResult codeAnalysis,
            DocumentationExtractionResult docExtraction)
        {
            ArgumentNullException.ThrowIfNull(codeAnalysis);
            ArgumentNullException.ThrowIfNull(docExtraction);

            var consolidatedSpecs = codeAnalysis.InitialSpecifications.ToList();
            var specMap = consolidatedSpecs
                .Where(s => !string.IsNullOrEmpty(s.ComponentId))
                .GroupBy(s => s.ComponentId)
                .ToDictionary(g => g.Key, g => g.First());

            // Sort snippets by relevance score (highest first) to prioritize more relevant snippets
            var sortedSnippets = docExtraction.Snippets
                .OrderByDescending(s => s.RelevanceScore)
                .ToList();

            foreach (var snippet in sortedSnippets)
            {
                if (!string.IsNullOrEmpty(snippet.TargetComponentId) &&
                    specMap.TryGetValue(snippet.TargetComponentId, out var targetSpec))
                {
                    // Integrate snippet into the specification file
                    IntegrateSnippet(targetSpec, snippet);
                }
                else
                {
                    // Log warning: Documentation snippet found for non-existent code component
                    Console.WriteLine($"Warning: Documentation snippet for '{snippet.TargetComponentId}' does not match any code component.");
                }
            }

            // Further refinement: Ensure consistency, remove orphaned doc snippets
            RefineConsolidatedSpecs(consolidatedSpecs);

            return consolidatedSpecs;
        }

        private void IntegrateSnippet(SpecificationFile spec, DocumentationExtraction.Extractors.DocumentationSnippet snippet)
        {
            // Append or insert snippet content into the 'Documentation Notes' section.
            // Conflict priority is code-first: documentation only augments, never replaces.

            if (!spec.Content.Contains("\n## Documentation Notes\n"))
            {
                spec.Content += "\n\n## Documentation Notes\n";
            }

            spec.Content += $"\n* **Source:** {snippet.SourceDocument}\n* **Content:** {snippet.Content.Trim()}\n";
        }

        private void RefineConsolidatedSpecs(List<SpecificationFile> specs)
        {
            foreach (var spec in specs)
            {
                // Ensure proper Markdown formatting
                EnsureProperFormatting(spec);

                // Remove duplicate content
                RemoveDuplicateContent(spec);

                // Ensure sections are in a consistent order
                ReorderSections(spec);
            }
        }

        private void EnsureProperFormatting(SpecificationFile spec)
        {
            // Ensure the content ends with a newline
            if (!spec.Content.EndsWith("\n"))
            {
                spec.Content += "\n";
            }

            // Ensure there are no triple newlines (excessive spacing)
            spec.Content = spec.Content.Replace("\n\n\n", "\n\n");
        }

        private void RemoveDuplicateContent(SpecificationFile spec)
        {
            // Ensure there are no duplicate Documentation Notes sections
            // (a more sophisticated implementation would also detect near-duplicate
            // snippet content using semantic similarity)
            if (spec.Content.Contains("\n## Documentation Notes\n"))
            {
                var firstIndex = spec.Content.IndexOf("\n## Documentation Notes\n");
                var lastIndex = spec.Content.LastIndexOf("\n## Documentation Notes\n");

                if (firstIndex != lastIndex)
                {
                    // Keep only the first Documentation Notes section
                    spec.Content = spec.Content.Substring(0, lastIndex);
                }
            }
        }

        private void ReorderSections(SpecificationFile spec)
        {
            // Ensure Documentation Notes is the last section
            if (spec.Content.Contains("\n## Documentation Notes\n"))
            {
                var index = spec.Content.IndexOf("\n## Documentation Notes\n");
                var docNotes = spec.Content.Substring(index);
                var restOfContent = spec.Content.Substring(0, index);

                // Check if there are any sections after Documentation Notes
                var nextSectionIndex = docNotes.IndexOf("\n## ", 20); // Skip the "## Documentation Notes" part

                if (nextSectionIndex != -1)
                {
                    // Move Documentation Notes to the end
                    var afterDocNotes = docNotes.Substring(nextSectionIndex);
                    docNotes = docNotes.Substring(0, nextSectionIndex);

                    spec.Content = restOfContent + afterDocNotes + docNotes;
                }
            }
        }
    }
}
