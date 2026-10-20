using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

using Microsoft.SemanticKernel;

using SpecTreeGenerator.Common.Models;
namespace SpecTreeGenerator.DocumentationExtraction.Extractors
{
    /// <summary>
    /// Represents a documentation snippet extracted from a document
    /// </summary>
    public class DocumentationSnippet
    {
        /// <summary>
        /// ID of the target code component this snippet relates to
        /// </summary>
        public string TargetComponentId { get; set; } = string.Empty;

        /// <summary>
        /// Content of the snippet
        /// </summary>
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// Source document path
        /// </summary>
        public string SourceDocument { get; set; } = string.Empty;

        /// <summary>
        /// Relevance score (0.0 to 1.0)
        /// </summary>
        public double RelevanceScore { get; set; }

        /// <summary>
        /// Title of the documentation section the snippet was extracted from
        /// </summary>
        public string SourceSectionTitle { get; set; } = string.Empty;
    }

    /// <summary>
    /// Interface for Semantic Kernel service
    /// </summary>
    public interface ISemanticKernelService
    {
        Task<List<DocumentationSnippet>> ExtractRelevantSnippetsAsync(
            Parsers.ParsedMarkdownDocument document,
            Analyzers.DocumentStructure documentStructure,
            CodeHierarchy codeHierarchy);
    }

    /// <summary>
    /// Implementation of Semantic Kernel service using Mistral LLM
    /// </summary>
    public class SemanticKernelMistralService : ISemanticKernelService
    {
        private readonly Kernel _kernel;

        public SemanticKernelMistralService(Kernel kernel)
        {
            _kernel = kernel ?? throw new ArgumentNullException(nameof(kernel));
        }

        public async Task<List<DocumentationSnippet>> ExtractRelevantSnippetsAsync(
            Parsers.ParsedMarkdownDocument document,
            Analyzers.DocumentStructure documentStructure,
            CodeHierarchy codeHierarchy)
        {
            var snippets = new List<DocumentationSnippet>();

            // Prepare code component information for the LLM
            var codeComponentInfo = SerializeCodeHierarchyForPrompt(codeHierarchy);

            // Process each section to find relevant snippets
            foreach (var section in documentStructure.Sections)
            {
                var sectionSnippets = await ExtractSnippetsFromSectionAsync(section, codeComponentInfo);
                snippets.AddRange(sectionSnippets);
            }

            return snippets;
        }

        private async Task<List<DocumentationSnippet>> ExtractSnippetsFromSectionAsync(
            Analyzers.StructureSection section,
            string codeComponentInfo)
        {
            // Skip empty sections
            if (string.IsNullOrWhiteSpace(section.Content))
            {
                return new List<DocumentationSnippet>();
            }

            // Create a function to extract snippets
            var function = _kernel.CreateFunctionFromPrompt(
                """
                You are an expert documentation analyzer. Your task is to extract relevant documentation snippets 
                from the provided section and map them to code components.

                Code Components:
                {{$codeComponents}}

                Section Title: {{$sectionTitle}}
                Section Content:
                {{$sectionContent}}

                For each relevant piece of information in the section, determine which code component it relates to.
                Return a JSON array of snippets, where each snippet has:
                - targetComponentId: The ID of the code component this snippet relates to
                - content: The relevant text from the section
                - relevanceScore: A score from 0.0 to 1.0 indicating how relevant this snippet is to the component

                Only include snippets with relevance score >= 0.7.
                Return valid JSON only, with no additional text.
                """,
                new Microsoft.SemanticKernel.PromptExecutionSettings
                {
                    ExtensionData = new Dictionary<string, object>
                    {
                        ["response_format"] = new { type = "json_object" }
                    }
                }
            );

            // Execute the function
            var result = await _kernel.InvokeAsync(function, new()
            {
                ["codeComponents"] = codeComponentInfo,
                ["sectionTitle"] = section.Title,
                ["sectionContent"] = section.Content
            });

            // Parse the result
            var snippets = ParseSnippetResult(result.GetValue<string>() ?? "[]");
            foreach (var snippet in snippets)
            {
                snippet.SourceSectionTitle = section.Title;
            }

            // Process child sections recursively
            foreach (var childSection in section.Children)
            {
                var childSnippets = await ExtractSnippetsFromSectionAsync(childSection, codeComponentInfo);
                snippets.AddRange(childSnippets);
            }

            return snippets;
        }

        private static readonly JsonSerializerOptions CachedJsonOptions = new() { WriteIndented = true };

        private static string SerializeCodeHierarchyForPrompt(CodeHierarchy hierarchy)
        {
            var components = new List<object>();

            foreach (var node in hierarchy.NodeLookup.Values)
            {
                components.Add(new
                {
                    id = node.ComponentId,
                    name = node.Name,
                    type = node.Type.ToString(),
                    parentId = node.Parent?.ComponentId
                });
            }

            return JsonSerializer.Serialize(components, CachedJsonOptions);
        }

        private static List<DocumentationSnippet> ParseSnippetResult(string jsonResult)
        {
            try
            {
                // Parse the JSON result
                using JsonDocument doc = JsonDocument.Parse(jsonResult);
                JsonElement root = doc.RootElement;

                // Check if the result has a "snippets" property (some models might wrap the array)
                JsonElement snippetsElement;
                if (root.TryGetProperty("snippets", out snippetsElement))
                {
                    return ParseSnippetsArray(snippetsElement);
                }
                else
                {
                    // Assume the root itself is the array or contains the array
                    foreach (var prop in root.EnumerateObject())
                    {
                        if (prop.Value.ValueKind == JsonValueKind.Array)
                        {
                            return ParseSnippetsArray(prop.Value);
                        }
                    }

                    // If we get here, try parsing the root as an array
                    if (root.ValueKind == JsonValueKind.Array)
                    {
                        return ParseSnippetsArray(root);
                    }
                }

                // If we couldn't find an array, return an empty list
                return new List<DocumentationSnippet>();
            }
            catch (JsonException)
            {
                // Handle JSON parsing errors
                Console.WriteLine($"Error parsing JSON result: {jsonResult}");
                return new List<DocumentationSnippet>();
            }
        }

        private static List<DocumentationSnippet> ParseSnippetsArray(JsonElement snippetsArray)
        {
            var snippets = new List<DocumentationSnippet>();

            foreach (var element in snippetsArray.EnumerateArray())
            {
                try
                {
                    var snippet = new DocumentationSnippet
                    {
                        TargetComponentId = element.GetProperty("targetComponentId").GetString() ?? string.Empty,
                        Content = element.GetProperty("content").GetString() ?? string.Empty,
                        RelevanceScore = element.GetProperty("relevanceScore").GetDouble()
                    };

                    // Only include snippets with relevance score >= 0.7
                    if (snippet.RelevanceScore >= 0.7)
                    {
                        snippets.Add(snippet);
                    }
                }
                catch (JsonException)
                {
                    // Skip malformed entries
                    continue;
                }
            }

            return snippets;
        }
    }
}