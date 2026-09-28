using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using GeneticSpecOptimizer.Common.Models;

namespace GeneticSpecOptimizer.DocumentationExtraction.Analyzers
{
    /// <summary>
    /// Represents the structure of a document
    /// </summary>
    public class DocumentStructure
    {
        /// <summary>
        /// Hierarchical sections of the document
        /// </summary>
        public List<StructureSection> Sections { get; set; } = new List<StructureSection>();

        /// <summary>
        /// Code examples found in the document
        /// </summary>
        public List<CodeExample> CodeExamples { get; set; } = new List<CodeExample>();

        /// <summary>
        /// Tables found in the document
        /// </summary>
        public List<DocumentTable> Tables { get; set; } = new List<DocumentTable>();

        /// <summary>
        /// Lists found in the document
        /// </summary>
        public List<DocumentList> Lists { get; set; } = new List<DocumentList>();
    }

    /// <summary>
    /// Represents a section in the document structure
    /// </summary>
    public class StructureSection
    {
        /// <summary>
        /// Title of the section
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Content of the section
        /// </summary>
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// Level of the section (1-6)
        /// </summary>
        public int Level { get; set; }

        /// <summary>
        /// Child sections
        /// </summary>
        public List<StructureSection> Children { get; set; } = new List<StructureSection>();

        /// <summary>
        /// Parent section (if any)
        /// </summary>
        public StructureSection? Parent { get; set; }

        /// <summary>
        /// Start line in the document
        /// </summary>
        public int StartLine { get; set; }

        /// <summary>
        /// End line in the document
        /// </summary>
        public int EndLine { get; set; }
    }

    /// <summary>
    /// Represents a code example in the document
    /// </summary>
    public class CodeExample
    {
        /// <summary>
        /// Language of the code example
        /// </summary>
        public string Language { get; set; } = string.Empty;

        /// <summary>
        /// Content of the code example
        /// </summary>
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// Section containing this code example
        /// </summary>
        public StructureSection? ContainingSection { get; set; }

        /// <summary>
        /// Start line in the document
        /// </summary>
        public int StartLine { get; set; }

        /// <summary>
        /// End line in the document
        /// </summary>
        public int EndLine { get; set; }
    }

    /// <summary>
    /// Represents a table in the document
    /// </summary>
    public class DocumentTable
    {
        /// <summary>
        /// Headers of the table
        /// </summary>
        public List<string> Headers { get; set; } = new List<string>();

        /// <summary>
        /// Rows of the table
        /// </summary>
        public List<List<string>> Rows { get; set; } = new List<List<string>>();

        /// <summary>
        /// Section containing this table
        /// </summary>
        public StructureSection? ContainingSection { get; set; }

        /// <summary>
        /// Start line in the document
        /// </summary>
        public int StartLine { get; set; }

        /// <summary>
        /// End line in the document
        /// </summary>
        public int EndLine { get; set; }
    }

    /// <summary>
    /// Represents a list in the document
    /// </summary>
    public class DocumentList
    {
        /// <summary>
        /// Items in the list
        /// </summary>
        public List<string> Items { get; set; } = new List<string>();

        /// <summary>
        /// Whether the list is ordered
        /// </summary>
        public bool IsOrdered { get; set; }

        /// <summary>
        /// Section containing this list
        /// </summary>
        public StructureSection? ContainingSection { get; set; }

        /// <summary>
        /// Start line in the document
        /// </summary>
        public int StartLine { get; set; }

        /// <summary>
        /// End line in the document
        /// </summary>
        public int EndLine { get; set; }
    }

    /// <summary>
    /// Interface for structure analyzer
    /// </summary>
    public interface IStructureAnalyzer
    {
        DocumentStructure AnalyzeStructure(Parsers.ParsedMarkdownDocument parsedDocument);
    }

    /// <summary>
    /// Implementation of structure analyzer
    /// </summary>
    public class StructureAnalyzer : IStructureAnalyzer
    {
        public DocumentStructure AnalyzeStructure(Parsers.ParsedMarkdownDocument parsedDocument)
        {
            var structure = new DocumentStructure();

            // Convert sections
            structure.Sections = ConvertSections(parsedDocument.Sections);

            // Convert code blocks
            structure.CodeExamples = ConvertCodeBlocks(parsedDocument.CodeBlocks, structure.Sections);

            // Convert tables
            structure.Tables = ConvertTables(parsedDocument.Tables, structure.Sections);

            // Convert lists
            structure.Lists = ConvertLists(parsedDocument.Lists, structure.Sections);

            return structure;
        }

        private static List<StructureSection> ConvertSections(List<Parsers.MarkdownSection> markdownSections)
        {
            var sections = new List<StructureSection>();
            var sectionMap = new Dictionary<Parsers.MarkdownSection, StructureSection>();

            // First pass: Create all sections
            foreach (var mdSection in markdownSections)
            {
                var section = new StructureSection
                {
                    Title = mdSection.Title,
                    Content = mdSection.Content,
                    Level = mdSection.Level,
                    StartLine = mdSection.StartLine,
                    EndLine = mdSection.EndLine
                };

                sectionMap[mdSection] = section;
                sections.Add(section);
            }

            // Second pass: Set up parent-child relationships
            foreach (var mdSection in markdownSections)
            {
                var section = sectionMap[mdSection];

                if (mdSection.Parent != null && sectionMap.TryGetValue(mdSection.Parent, out var parentSection))
                {
                    section.Parent = parentSection;
                    parentSection.Children.Add(section);
                }
            }

            return sections;
        }

        private static List<CodeExample> ConvertCodeBlocks(List<Parsers.MarkdownCodeBlock> codeBlocks, List<StructureSection> sections)
        {
            var codeExamples = new List<CodeExample>();

            foreach (var codeBlock in codeBlocks)
            {
                var codeExample = new CodeExample
                {
                    Language = codeBlock.Language,
                    Content = codeBlock.Content,
                    StartLine = codeBlock.StartLine,
                    EndLine = codeBlock.EndLine,
                    ContainingSection = FindContainingSection(codeBlock.StartLine, codeBlock.EndLine, sections)
                };

                codeExamples.Add(codeExample);
            }

            return codeExamples;
        }

        private static List<DocumentTable> ConvertTables(List<Parsers.MarkdownTable> tables, List<StructureSection> sections)
        {
            var documentTables = new List<DocumentTable>();

            foreach (var table in tables)
            {
                var documentTable = new DocumentTable
                {
                    Headers = new List<string>(table.Headers),
                    Rows = new List<List<string>>(table.Rows.Select(row => new List<string>(row))),
                    StartLine = table.StartLine,
                    EndLine = table.EndLine,
                    ContainingSection = FindContainingSection(table.StartLine, table.EndLine, sections)
                };

                documentTables.Add(documentTable);
            }

            return documentTables;
        }

        private static List<DocumentList> ConvertLists(List<Parsers.MarkdownList> lists, List<StructureSection> sections)
        {
            var documentLists = new List<DocumentList>();

            foreach (var list in lists)
            {
                var documentList = new DocumentList
                {
                    Items = new List<string>(list.Items),
                    IsOrdered = list.IsOrdered,
                    StartLine = list.StartLine,
                    EndLine = list.EndLine,
                    ContainingSection = FindContainingSection(list.StartLine, list.EndLine, sections)
                };

                documentLists.Add(documentList);
            }

            return documentLists;
        }

        private static StructureSection? FindContainingSection(int startLine, int endLine, List<StructureSection> sections)
        {
            // Find the innermost section that contains the given line range
            StructureSection? bestMatch = null;
            int bestMatchLevel = -1;

            foreach (var section in sections)
            {
                if (section.StartLine <= startLine && section.EndLine >= endLine)
                {
                    if (bestMatch == null || section.Level > bestMatchLevel)
                    {
                        bestMatch = section;
                        bestMatchLevel = section.Level;
                    }
                }
            }

            return bestMatch;
        }
    }
}