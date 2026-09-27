using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GeneticSpecOptimizer.Common.Models;
using Microsoft.SemanticKernel;
using System.IO;
using System.Text;

namespace GeneticSpecOptimizer.DocumentationExtraction.Converters
{
    /// <summary>
    /// Interface for document conversion to Markdown
    /// </summary>
    public interface IMarkitdownConverter
    {
        Task<string> ConvertToMarkdownAsync(string filePath, DocumentType documentType);
    }

    /// <summary>
    /// Types of documents that can be converted
    /// </summary>
    public enum DocumentType
    {
        Markdown,
        Html,
        Word,
        Pdf,
        Text,
        Unknown
    }

    /// <summary>
    /// Implementation of Markitdown converter for document conversion to Markdown
    /// </summary>
    public class MarkitdownConverter : IMarkitdownConverter
    {
        private readonly Kernel _kernel;

        public MarkitdownConverter(Kernel kernel)
        {
            _kernel = kernel ?? throw new ArgumentNullException(nameof(kernel));
        }

        public async Task<string> ConvertToMarkdownAsync(string filePath, DocumentType documentType)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"File not found: {filePath}");
            }

            // If already markdown, just return the content
            if (documentType == DocumentType.Markdown)
            {
                return await File.ReadAllTextAsync(filePath);
            }

            // For other document types, use Markitdown (simulated with Semantic Kernel)
            return await ConvertWithSemanticKernel(filePath, documentType);
        }

        private async Task<string> ConvertWithSemanticKernel(string filePath, DocumentType documentType)
        {
            // In a real implementation, this would use the actual Markitdown library
            // For this implementation, we'll simulate it using Semantic Kernel

            // Read the file content
            string fileContent;
            
            if (documentType == DocumentType.Pdf)
            {
                // In a real implementation, this would use a PDF extraction library
                // For this simulation, we'll assume text extraction from PDF
                fileContent = $"Extracted text from PDF: {filePath}";
            }
            else if (documentType == DocumentType.Word)
            {
                // In a real implementation, this would use a Word document extraction library
                // For this simulation, we'll assume text extraction from Word
                fileContent = $"Extracted text from Word document: {filePath}";
            }
            else if (documentType == DocumentType.Html)
            {
                // Read HTML content
                fileContent = await File.ReadAllTextAsync(filePath);
            }
            else
            {
                // Default to reading as text
                fileContent = await File.ReadAllTextAsync(filePath);
            }

            // Use Semantic Kernel to convert to Markdown
            var function = _kernel.CreateFunctionFromPrompt(
                """
                Convert the following content to well-formatted Markdown:
                
                {{$input}}
                
                Ensure proper Markdown formatting with:
                - Headers using # syntax
                - Lists using - or * syntax
                - Code blocks with ```
                - Tables using | syntax
                - Links using [text](url) syntax
                - Images using ![alt](url) syntax
                
                Return only the converted Markdown without any explanations.
                """
            );

            // Execute the function
            var result = await _kernel.InvokeAsync(function, new() { ["input"] = fileContent });
            return result.GetValue<string>() ?? string.Empty;
        }

        /// <summary>
        /// Detects the document type based on file extension
        /// </summary>
        public static DocumentType DetectDocumentType(string filePath)
        {
            string extension = Path.GetExtension(filePath).ToLowerInvariant();
            
            return extension switch
            {
                ".md" => DocumentType.Markdown,
                ".html" or ".htm" => DocumentType.Html,
                ".docx" or ".doc" => DocumentType.Word,
                ".pdf" => DocumentType.Pdf,
                ".txt" => DocumentType.Text,
                _ => DocumentType.Unknown
            };
        }
    }
}
