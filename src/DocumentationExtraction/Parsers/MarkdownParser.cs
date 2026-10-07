using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using SpecTreeGenerator.Common.Models;

namespace SpecTreeGenerator.DocumentationExtraction.Parsers
{
    /// <summary>
    /// Represents a parsed Markdown document
    /// </summary>
    public class ParsedMarkdownDocument
    {
        /// <summary>
        /// Full text of the Markdown document
        /// </summary>
        public string FullText { get; set; } = string.Empty;

        /// <summary>
        /// Sections of the document
        /// </summary>
        public List<MarkdownSection> Sections { get; set; } = new List<MarkdownSection>();

        /// <summary>
        /// Code blocks in the document
        /// </summary>
        public List<MarkdownCodeBlock> CodeBlocks { get; set; } = new List<MarkdownCodeBlock>();

        /// <summary>
        /// Lists in the document
        /// </summary>
        public List<MarkdownList> Lists { get; set; } = new List<MarkdownList>();

        /// <summary>
        /// Tables in the document
        /// </summary>
        public List<MarkdownTable> Tables { get; set; } = new List<MarkdownTable>();
    }

    /// <summary>
    /// Represents a section in a Markdown document
    /// </summary>
    public class MarkdownSection
    {
        /// <summary>
        /// Level of the section (1-6)
        /// </summary>
        public int Level { get; set; }

        /// <summary>
        /// Title of the section
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Content of the section
        /// </summary>
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// Start line of the section
        /// </summary>
        public int StartLine { get; set; }

        /// <summary>
        /// End line of the section
        /// </summary>
        public int EndLine { get; set; }

        /// <summary>
        /// Parent section (if any)
        /// </summary>
        public MarkdownSection? Parent { get; set; }

        /// <summary>
        /// Child sections
        /// </summary>
        public List<MarkdownSection> Children { get; set; } = new List<MarkdownSection>();
    }

    /// <summary>
    /// Represents a code block in a Markdown document
    /// </summary>
    public class MarkdownCodeBlock
    {
        /// <summary>
        /// Language of the code block
        /// </summary>
        public string Language { get; set; } = string.Empty;

        /// <summary>
        /// Content of the code block
        /// </summary>
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// Start line of the code block
        /// </summary>
        public int StartLine { get; set; }

        /// <summary>
        /// End line of the code block
        /// </summary>
        public int EndLine { get; set; }
    }

    /// <summary>
    /// Represents a list in a Markdown document
    /// </summary>
    public class MarkdownList
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
        /// Start line of the list
        /// </summary>
        public int StartLine { get; set; }

        /// <summary>
        /// End line of the list
        /// </summary>
        public int EndLine { get; set; }
    }

    /// <summary>
    /// Represents a table in a Markdown document
    /// </summary>
    public class MarkdownTable
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
        /// Start line of the table
        /// </summary>
        public int StartLine { get; set; }

        /// <summary>
        /// End line of the table
        /// </summary>
        public int EndLine { get; set; }
    }

    /// <summary>
    /// Interface for Markdown parser
    /// </summary>
    public interface IMarkdownParser
    {
        ParsedMarkdownDocument Parse(string markdownContent);
    }

    /// <summary>
    /// Implementation of Markdown parser
    /// </summary>
    public class MarkdownParser : IMarkdownParser
    {
        public ParsedMarkdownDocument Parse(string markdownContent)
        {
            var document = new ParsedMarkdownDocument
            {
                FullText = markdownContent
            };

            // Split content into lines
            var lines = markdownContent.Split(LineSeparators, StringSplitOptions.None);

            // Parse sections
            document.Sections = ParseSections(lines);

            // Parse code blocks
            document.CodeBlocks = ParseCodeBlocks(lines);

            // Parse lists
            document.Lists = ParseLists(lines);

            // Parse tables
            document.Tables = ParseTables(lines);

            return document;
        }

        private static readonly string[] LineSeparators = { "\r\n", "\r", "\n" };

        private static List<MarkdownSection> ParseSections(string[] lines)
        {
            var sections = new List<MarkdownSection>();
            MarkdownSection? currentSection = null;

            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                var headerMatch = Regex.Match(line, @"^(#{1,6})\s+(.+)$");

                if (headerMatch.Success)
                {
                    // Found a header
                    var level = headerMatch.Groups[1].Value.Length;
                    var title = headerMatch.Groups[2].Value.Trim();

                    // Create a new section
                    var newSection = new MarkdownSection
                    {
                        Level = level,
                        Title = title,
                        StartLine = i,
                        EndLine = i
                    };

                    // Find parent section
                    if (currentSection != null)
                    {
                        if (level > currentSection.Level)
                        {
                            // This is a child of the current section
                            newSection.Parent = currentSection;
                            currentSection.Children.Add(newSection);
                        }
                        else
                        {
                            // Find appropriate parent
                            var parent = currentSection.Parent;
                            while (parent != null && level <= parent.Level)
                            {
                                parent = parent.Parent;
                            }

                            if (parent != null)
                            {
                                newSection.Parent = parent;
                                parent.Children.Add(newSection);
                            }
                        }
                    }

                    // Update current section
                    currentSection = newSection;
                    sections.Add(newSection);
                }
                else if (currentSection != null)
                {
                    // Add content to current section
                    currentSection.Content += line + "\n";
                    currentSection.EndLine = i;
                }
            }

            return sections;
        }

        private static List<MarkdownCodeBlock> ParseCodeBlocks(string[] lines)
        {
            var codeBlocks = new List<MarkdownCodeBlock>();
            bool inCodeBlock = false;
            MarkdownCodeBlock? currentBlock = null;

            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];

                if (line.StartsWith("```", StringComparison.Ordinal))
                {
                    if (!inCodeBlock)
                    {
                        // Start of code block
                        inCodeBlock = true;
                        currentBlock = new MarkdownCodeBlock
                        {
                            Language = line.Length > 3 ? line.Substring(3).Trim() : string.Empty,
                            StartLine = i
                        };
                    }
                    else
                    {
                        // End of code block
                        inCodeBlock = false;
                        currentBlock!.EndLine = i;
                        codeBlocks.Add(currentBlock);
                        currentBlock = null;
                    }
                }
                else if (inCodeBlock && currentBlock != null)
                {
                    // Add content to current code block
                    currentBlock.Content += line + "\n";
                }
            }

            return codeBlocks;
        }

        private static List<MarkdownList> ParseLists(string[] lines)
        {
            var lists = new List<MarkdownList>();
            MarkdownList? currentList = null;

            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i].Trim();
                var unorderedMatch = Regex.Match(line, @"^[-*+]\s+(.+)$");
                var orderedMatch = Regex.Match(line, @"^\d+\.\s+(.+)$");

                if (unorderedMatch.Success || orderedMatch.Success)
                {
                    var itemContent = unorderedMatch.Success
                        ? unorderedMatch.Groups[1].Value
                        : orderedMatch.Groups[1].Value;

                    if (currentList == null)
                    {
                        // Start a new list
                        currentList = new MarkdownList
                        {
                            IsOrdered = orderedMatch.Success,
                            StartLine = i
                        };
                        lists.Add(currentList);
                    }

                    // Add item to current list
                    currentList.Items.Add(itemContent);
                    currentList.EndLine = i;
                }
                else if (string.IsNullOrWhiteSpace(line) && currentList != null)
                {
                    // End of list
                    currentList = null;
                }
            }

            return lists;
        }

        private static List<MarkdownTable> ParseTables(string[] lines)
        {
            var tables = new List<MarkdownTable>();
            MarkdownTable? currentTable = null;

            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i].Trim();

                if (line.StartsWith('|') && line.EndsWith('|'))
                {
                    if (currentTable == null)
                    {
                        // Start a new table
                        currentTable = new MarkdownTable
                        {
                            StartLine = i
                        };
                        tables.Add(currentTable);

                        // Parse headers
                        var headerCells = line.Split('|')
                            .Skip(1) // Skip the first empty cell
                            .Take(line.Count(c => c == '|') - 1) // Skip the last empty cell
                            .Select(cell => cell.Trim())
                            .ToList();

                        currentTable.Headers.AddRange(headerCells);
                    }
                    else if (line.Replace("|", "").Trim().All(c => c == '-' || c == ':'))
                    {
                        // This is the separator line, skip it
                        continue;
                    }
                    else
                    {
                        // Parse row
                        var cells = line.Split('|')
                            .Skip(1) // Skip the first empty cell
                            .Take(line.Count(c => c == '|') - 1) // Skip the last empty cell
                            .Select(cell => cell.Trim())
                            .ToList();

                        currentTable.Rows.Add(cells);
                        currentTable.EndLine = i;
                    }
                }
                else if (currentTable != null && !string.IsNullOrWhiteSpace(line))
                {
                    // End of table
                    currentTable = null;
                }
            }

            return tables;
        }
    }
}