using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using GeneticSpecOptimizer.Common.Models;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace GeneticSpecOptimizer.CodeAnalysis.Parsers
{
    /// <summary>
    /// Interface for language parsers
    /// </summary>
    public interface ILanguageParser
    {
        Task<SyntaxTree> ParseFileAsync(string filePath);
    }

    /// <summary>
    /// Factory for creating language parsers based on file extension
    /// </summary>
    public interface ILanguageParserFactory
    {
        ILanguageParser GetParser(string language);
    }

    /// <summary>
    /// Implementation of the language parser factory
    /// </summary>
    public class LanguageParserFactory : ILanguageParserFactory
    {
        private readonly Dictionary<string, ILanguageParser> _parsers = new Dictionary<string, ILanguageParser>();

        public LanguageParserFactory()
        {
            // Register parsers
            _parsers["csharp"] = new CSharpParser();
            // Add more parsers for other languages as needed
        }

        public ILanguageParser GetParser(string language)
        {
            if (_parsers.TryGetValue(language.ToLowerInvariant(), out var parser))
            {
                return parser;
            }

            throw new ArgumentException($"No parser available for language: {language}");
        }
    }

    /// <summary>
    /// C# language parser implementation
    /// </summary>
    public class CSharpParser : ILanguageParser
    {
        public async Task<SyntaxTree> ParseFileAsync(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"File not found: {filePath}");
            }

            string code = await File.ReadAllTextAsync(filePath);
            return CSharpSyntaxTree.ParseText(code, path: filePath);
        }
    }
}