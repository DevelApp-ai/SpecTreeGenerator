using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GeneticSpecOptimizer.Common.Models;

namespace GeneticSpecOptimizer.DocumentationExtraction.Interfaces
{
    /// <summary>
    /// Interface for documentation converters
    /// </summary>
    public interface IDocumentationConverter
    {
        /// <summary>
        /// Converts documentation files to markdown
        /// </summary>
        Task<IEnumerable<string>> ConvertToMarkdownAsync(IEnumerable<string> documentationFiles);
    }
    
    /// <summary>
    /// Interface for markdown parsers
    /// </summary>
    public interface IMarkdownParser
    {
        /// <summary>
        /// Parses markdown content
        /// </summary>
        Task<IEnumerable<DocumentationSection>> ParseMarkdownAsync(IEnumerable<string> markdownFiles);
    }
    
    /// <summary>
    /// Interface for structure analyzers
    /// </summary>
    public interface IStructureAnalyzer
    {
        /// <summary>
        /// Analyzes the structure of documentation sections
        /// </summary>
        Task<DocumentationStructure> AnalyzeStructureAsync(IEnumerable<DocumentationSection> sections);
    }
    
    /// <summary>
    /// Interface for semantic extractors
    /// </summary>
    public interface ISemanticExtractor
    {
        /// <summary>
        /// Extracts semantic information from documentation
        /// </summary>
        Task<IEnumerable<SemanticComponent>> ExtractSemanticComponentsAsync(DocumentationStructure structure, CodeHierarchy codeHierarchy);
    }
    
    /// <summary>
    /// Represents a section of documentation
    /// </summary>
    public class DocumentationSection
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public int Level { get; set; }
        public string SourceFile { get; set; } = string.Empty;
        public List<DocumentationSection> Subsections { get; set; } = new List<DocumentationSection>();
    }
    
    /// <summary>
    /// Represents the structure of documentation
    /// </summary>
    public class DocumentationStructure
    {
        public List<DocumentationSection> RootSections { get; set; } = new List<DocumentationSection>();
        public Dictionary<string, DocumentationSection> SectionLookup { get; set; } = new Dictionary<string, DocumentationSection>();
    }
    
    /// <summary>
    /// Represents a semantic component extracted from documentation
    /// </summary>
    public class SemanticComponent
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string SourceSectionId { get; set; } = string.Empty;
        public string? RelatedCodeComponentId { get; set; }
    }
}
