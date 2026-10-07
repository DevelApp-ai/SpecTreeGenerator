using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using GeneticSpecOptimizer.Common.Models;

using Microsoft.CodeAnalysis;

namespace GeneticSpecOptimizer.CodeAnalysis.Interfaces
{
    /// <summary>
    /// Interface for language parsers
    /// </summary>
    public interface ILanguageParser
    {
        /// <summary>
        /// Parses source code files in a directory
        /// </summary>
        Task<IEnumerable<SyntaxTree>> ParseDirectoryAsync(string directory, IEnumerable<string> includeExtensions, IEnumerable<string> excludeDirectories);

        /// <summary>
        /// Parses a single source code file
        /// </summary>
        Task<SyntaxTree> ParseFileAsync(string filePath);
    }

    /// <summary>
    /// Interface for symbol table builders
    /// </summary>
    public interface ISymbolTableBuilder
    {
        /// <summary>
        /// Builds a symbol table from syntax trees
        /// </summary>
        Task<Compilation> BuildSymbolTableAsync(IEnumerable<SyntaxTree> syntaxTrees);
    }

    /// <summary>
    /// Interface for component extractors
    /// </summary>
    public interface IComponentExtractor
    {
        /// <summary>
        /// Extracts components from a compilation
        /// </summary>
        Task<IEnumerable<CodeComponent>> ExtractComponentsAsync(Compilation compilation, bool includePrivateMembers);
    }

    /// <summary>
    /// Interface for relationship analyzers
    /// </summary>
    public interface IRelationshipAnalyzer
    {
        /// <summary>
        /// Analyzes relationships between components
        /// </summary>
        Task<IEnumerable<CodeRelationship>> AnalyzeRelationshipsAsync(IEnumerable<CodeComponent> components, Compilation compilation);
    }

    /// <summary>
    /// Interface for hierarchy builders
    /// </summary>
    public interface IHierarchyBuilder
    {
        /// <summary>
        /// Builds a code hierarchy from components and relationships
        /// </summary>
        Task<CodeHierarchy> BuildHierarchyAsync(IEnumerable<CodeComponent> components, IEnumerable<CodeRelationship> relationships);
    }

    /// <summary>
    /// Interface for specification generators
    /// </summary>
    public interface ISpecificationGenerator
    {
        /// <summary>
        /// Generates specifications from a code hierarchy
        /// </summary>
        Task<IEnumerable<SpecificationFile>> GenerateSpecificationsAsync(CodeHierarchy hierarchy, bool includeImplementationDetails);
    }
}