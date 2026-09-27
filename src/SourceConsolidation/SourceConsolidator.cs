using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GeneticSpecOptimizer.Common.Models;
using System.Linq;

namespace GeneticSpecOptimizer.SourceConsolidation
{
    /// <summary>
    /// Interface for source consolidator
    /// </summary>
    public interface ISourceConsolidator
    {
        IEnumerable<SpecificationFile> ConsolidateSources(
            CodeAnalysisResult codeAnalysis,
            DocumentationExtractionResult docExtraction);
    }

    /// <summary>
    /// Result of code analysis
    /// </summary>
    public class CodeAnalysisResult
    {
        /// <summary>
        /// Components extracted from code
        /// </summary>
        public List<CodeComponent> Components { get; set; }
        
        /// <summary>
        /// Relationships between components
        /// </summary>
        public List<CodeRelationship> Relationships { get; set; }
        
        /// <summary>
        /// Code hierarchy
        /// </summary>
        public CodeHierarchy CodeHierarchy { get; set; }
        
        /// <summary>
        /// Initial specifications generated from code
        /// </summary>
        public List<SpecificationFile> InitialSpecifications { get; set; }
    }

    /// <summary>
    /// Result of documentation extraction
    /// </summary>
    public class DocumentationExtractionResult
    {
        /// <summary>
        /// Snippets extracted from documentation
        /// </summary>
        public List<DocumentationExtraction.Extractors.DocumentationSnippet> Snippets { get; set; }
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
            var consolidatedSpecs = codeAnalysis.InitialSpecifications.ToList();
            var specMap = consolidatedSpecs.ToDictionary(s => s.ComponentId);
            
            // Sort snippets by relevance score (highest first) to prioritize more relevant snippets
            var sortedSnippets = docExtraction.Snippets
                .OrderByDescending(s => s.RelevanceScore)
                .ToList();
            
            foreach (var snippet in sortedSnippets)
            {
                if (specMap.TryGetValue(snippet.TargetComponentId, out var targetSpec))
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
            // Append or insert snippet content into appropriate section
            // Example: Add to a 'Description' or 'Documentation Notes' section
            // Handle potential conflicts based on priority rules (code > docs)
            
            if (!spec.Content.Contains("\n## Documentation Notes\n"))
            {
                spec.Content += "\n\n## Documentation Notes\n";
            }
            
            spec.Content += $"\n* **Source:** {snippet.SourceDocument}\n* **Content:** {snippet.Content.Trim()}\n";
        }
        
        private void RefineConsolidatedSpecs(List<SpecificationFile> specs)
        {
            // Post-processing: formatting, consistency checks, etc.
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
            // This is a simplified implementation
            // A more sophisticated implementation would use semantic similarity
            // to identify and remove duplicate content
            
            // For now, we'll just ensure there are no duplicate Documentation Notes sections
            if (spec.Content.Contains("\n## Documentation Notes\n"))
            {
                var firstIndex = spec.Content.IndexOf("\n## Documentation Notes\n");
                var lastIndex = spec.Content.LastIndexOf("\n## Documentation Notes\n");
                
                if (firstIndex != lastIndex)
                {
                    // There are multiple Documentation Notes sections
                    // Keep only the first one
                    spec.Content = spec.Content.Substring(0, lastIndex);
                }
            }
        }
        
        private void ReorderSections(SpecificationFile spec)
        {
            // This is a simplified implementation
            // A more sophisticated implementation would parse the Markdown
            // and reorder sections based on a predefined order
            
            // For now, we'll just ensure Documentation Notes is the last section
            if (spec.Content.Contains("\n## Documentation Notes\n"))
            {
                var index = spec.Content.IndexOf("\n## Documentation Notes\n");
                var docNotes = spec.Content.Substring(index);
                var restOfContent = spec.Content.Substring(0, index);
                
                // Check if there are any sections after Documentation Notes
                var nextSectionIndex = docNotes.IndexOf("\n## ", 20); // Skip the "## Documentation Notes" part
                
                if (nextSectionIndex != -1)
                {
                    // There are sections after Documentation Notes
                    // Move Documentation Notes to the end
                    var afterDocNotes = docNotes.Substring(nextSectionIndex);
                    docNotes = docNotes.Substring(0, nextSectionIndex);
                    
                    spec.Content = restOfContent + afterDocNotes + docNotes;
                }
            }
        }
    }
}
