using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using SpecTreeGenerator.Common.Models;

namespace SpecTreeGenerator.DocumentationExtraction.Extractors
{
    /// <summary>
    /// Local, non-AI implementation of the semantic extraction service.
    ///
    /// Used as the fallback when no AI credentials are configured: it maps
    /// documentation sections to code components by keyword overlap between
    /// the section text and the component names. It requires no network
    /// access and produces deterministic results.
    /// </summary>
    public class LocalSemanticKernelService : ISemanticKernelService
    {
        private const double RelevanceThreshold = 0.7;

        public Task<List<DocumentationSnippet>> ExtractRelevantSnippetsAsync(
            Parsers.ParsedMarkdownDocument document,
            Analyzers.DocumentStructure documentStructure,
            CodeHierarchy codeHierarchy)
        {
            ArgumentNullException.ThrowIfNull(documentStructure);
            ArgumentNullException.ThrowIfNull(codeHierarchy);

            var snippets = new List<DocumentationSnippet>();

            var components = codeHierarchy.NodeLookup.Values
                .Where(n => !string.IsNullOrEmpty(n.ComponentId))
                .Select(n => new { n.ComponentId, Tokens = Tokenize(n.Name) })
                .Where(c => c.Tokens.Count > 0)
                .ToList();

            foreach (var section in documentStructure.Sections)
            {
                if (string.IsNullOrWhiteSpace(section.Content))
                {
                    continue;
                }

                var sectionText = $"{section.Title} {section.Content}";
                var sectionTokens = Tokenize(sectionText);
                if (sectionTokens.Count == 0)
                {
                    continue;
                }

                foreach (var component in components)
                {
                    var overlap = component.Tokens.Intersect(sectionTokens, StringComparer.OrdinalIgnoreCase).Count();
                    var score = (double)overlap / component.Tokens.Count;

                    if (score >= RelevanceThreshold)
                    {
                        snippets.Add(new DocumentationSnippet
                        {
                            TargetComponentId = component.ComponentId!,
                            Content = section.Content.Trim(),
                            RelevanceScore = Math.Round(score, 2)
                        });
                    }
                }
            }

            return Task.FromResult(snippets);
        }

        /// <summary>
        /// Splits an identifier into lowercase word tokens, splitting on
        /// word boundaries and camelCase humps ("WeatherService" → "weather", "service").
        /// </summary>
        private static List<string> Tokenize(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return new List<string>();
            }

            return Regex.Split(text, @"[^a-zA-Z0-9]+")
                .Where(t => t.Length > 1)
                .SelectMany(SplitCamelCase)
                .Where(t => !StopWords.Contains(t.ToLowerInvariant()))
                .Select(t => t.ToLowerInvariant())
                .Distinct()
                .ToList();
        }

        private static IEnumerable<string> SplitCamelCase(string word)
        {
            var parts = Regex.Split(word, @"(?<=[a-z])(?=[A-Z])|(?<=[A-Z])(?=[A-Z][a-z])");
            foreach (var part in parts)
            {
                if (part.Length > 1)
                {
                    yield return part;
                }
            }
        }

        private static readonly HashSet<string> StopWords = new(StringComparer.OrdinalIgnoreCase)
        {
            "the", "and", "for", "with", "this", "that", "from", "into", "are", "was",
            "its", "has", "have", "can", "will", "shall", "may", "new", "get", "set",
            "class", "interface", "struct", "enum", "namespace", "public", "private",
            "static", "void", "using", "service", "data", "provider"
        };
    }
}