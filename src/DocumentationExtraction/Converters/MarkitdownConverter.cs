using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace GeneticSpecOptimizer.DocumentationExtraction.Converters
{
    /// <summary>
    /// Interface for document conversion to Markdown
    /// </summary>
    public interface IMarkitdownConverter
    {
        /// <summary>
        /// Converts the file at <paramref name="filePath"/> to Markdown.
        /// </summary>
        /// <param name="filePath">Path to the document to convert.</param>
        /// <param name="documentType">Detected document type (see <see cref="DetectDocumentType"/>).</param>
        /// <returns>The Markdown representation of the document.</returns>
        /// <exception cref="FileNotFoundException">The file does not exist.</exception>
        /// <exception cref="NotSupportedException">The document type cannot be converted.</exception>
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
    /// Converts documents to Markdown without any AI dependency.
    ///
    /// Supported conversions:
    /// - Markdown / Text: returned as-is.
    /// - HTML: converted to Markdown (headings, lists, links, emphasis, code blocks).
    /// - Word (.docx): text extracted from the Office Open XML document body.
    /// - PDF: best-effort extraction of text from uncompressed and FlateDecode
    ///   content streams (simple text-based PDFs). This is experimental.
    /// </summary>
    public class MarkitdownConverter : IMarkitdownConverter
    {
        /// <inheritdoc />
        public async Task<string> ConvertToMarkdownAsync(string filePath, DocumentType documentType)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"File not found: {filePath}");
            }

            switch (documentType)
            {
                case DocumentType.Markdown:
                case DocumentType.Text:
                    return await File.ReadAllTextAsync(filePath);

                case DocumentType.Html:
                    return ConvertHtmlToMarkdown(await File.ReadAllTextAsync(filePath));

                case DocumentType.Word:
                    return ConvertWordToMarkdown(filePath);

                case DocumentType.Pdf:
                    return ConvertPdfToMarkdown(filePath);

                default:
                    throw new NotSupportedException(
                        $"Document type '{documentType}' of file '{filePath}' is not supported. " +
                        "Supported types: .md, .txt, .html, .docx, .pdf (experimental).");
            }
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
                ".docx" => DocumentType.Word,
                ".pdf" => DocumentType.Pdf,
                ".txt" => DocumentType.Text,
                _ => DocumentType.Unknown
            };
        }

        /// <summary>
        /// Converts a minimal, commonly-used subset of HTML to Markdown.
        /// Handles headings, paragraphs, lists, links, emphasis and code blocks.
        /// </summary>
        internal static string ConvertHtmlToMarkdown(string html)
        {
            // Extract body if present
            var bodyMatch = Regex.Match(html, @"<body[^>]*>(.*?)</body>", RegexOptions.Singleline | RegexOptions.IgnoreCase);
            if (bodyMatch.Success)
            {
                html = bodyMatch.Groups[1].Value;
            }

            // Remove script/style blocks
            html = Regex.Replace(html, @"<(script|style)[^>]*>.*?</\1>", string.Empty,
                RegexOptions.Singleline | RegexOptions.IgnoreCase);

            // Code blocks
            html = Regex.Replace(html, @"<pre[^>]*>(.*?)</pre>",
                m => "\n```\n" + DecodeEntities(m.Groups[1].Value).Trim() + "\n```\n",
                RegexOptions.Singleline | RegexOptions.IgnoreCase);

            // Headings
            for (int level = 1; level <= 6; level++)
            {
                var hashes = new string('#', level);
                html = Regex.Replace(html, $"<h{level}[^>]*>(.*?)</h{level}>",
                    m => $"\n{hashes} {m.Groups[1].Value.Trim()}\n", RegexOptions.Singleline | RegexOptions.IgnoreCase);
            }

            // Lists (unordered and ordered)
            html = Regex.Replace(html, @"<li[^>]*>(.*?)</li>", m => "- " + m.Groups[1].Value.Trim() + "\n",
                RegexOptions.Singleline | RegexOptions.IgnoreCase);
            html = Regex.Replace(html, @"</(ul|ol)>", "\n", RegexOptions.IgnoreCase);
            html = Regex.Replace(html, @"<(ul|ol)[^>]*>", "\n", RegexOptions.IgnoreCase);

            // Links and images
            html = Regex.Replace(html, @"<a[^>]*href=[""']([^""']*)[""'][^>]*>(.*?)</a>",
                m => $"[{m.Groups[2].Value.Trim()}]({m.Groups[1].Value})", RegexOptions.Singleline | RegexOptions.IgnoreCase);
            html = Regex.Replace(html, @"<img[^>]*src=[""']([^""']*)[""'][^>]*alt=[""']([^""']*)[""'][^>]*/?>",
                m => $"![{m.Groups[2].Value}]({m.Groups[1].Value})", RegexOptions.IgnoreCase);
            html = Regex.Replace(html, @"<img[^>]*src=[""']([^""']*)[""'][^>]*/?>",
                m => $"![]({m.Groups[1].Value})", RegexOptions.IgnoreCase);

            // Emphasis
            html = Regex.Replace(html, @"<(strong|b)[^>]*>(.*?)</\1>", m => "**" + m.Groups[2].Value.Trim() + "**",
                RegexOptions.Singleline | RegexOptions.IgnoreCase);
            html = Regex.Replace(html, @"<(em|i)[^>]*>(.*?)</\1>", m => "*" + m.Groups[2].Value.Trim() + "*",
                RegexOptions.Singleline | RegexOptions.IgnoreCase);

            // Paragraph and line breaks
            html = Regex.Replace(html, @"<br\s*/?>", "\n", RegexOptions.IgnoreCase);
            html = Regex.Replace(html, @"</p>", "\n\n", RegexOptions.IgnoreCase);
            html = Regex.Replace(html, @"<p[^>]*>", string.Empty, RegexOptions.IgnoreCase);

            // Strip all remaining tags
            html = Regex.Replace(html, @"<[^>]+>", string.Empty);
            html = DecodeEntities(html);

            // Collapse whitespace
            html = Regex.Replace(html, @"\n{3,}", "\n\n");

            return html.Trim() + "\n";
        }

        /// <summary>
        /// Extracts text from a Word (.docx) document by reading the Office Open XML
        /// body (word/document.xml) and mapping paragraphs to Markdown lines.
        /// </summary>
        internal static string ConvertWordToMarkdown(string filePath)
        {
            using var archive = ZipFile.OpenRead(filePath);

            var entry = archive.GetEntry("word/document.xml");
            if (entry is null)
            {
                throw new NotSupportedException(
                    $"'{filePath}' does not look like a valid .docx file (word/document.xml is missing).");
            }

            using var stream = entry.Open();
            using var reader = new StreamReader(stream, Encoding.UTF8);
            var documentXml = reader.ReadToEnd();

            // Split into paragraphs and extract run text within each
            var paragraphs = Regex.Matches(documentXml, @"<w:p\b.*?</w:p>", RegexOptions.Singleline);
            var builder = new StringBuilder();

            foreach (Match paragraph in paragraphs)
            {
                var text = string.Concat(
                    Regex.Matches(paragraph.Value, @"<w:t[^>]*>(.*?)</w:t>", RegexOptions.Singleline)
                        .Select(m => m.Groups[1].Value));

                // Map heading styles to Markdown headings
                var styleMatch = Regex.Match(paragraph.Value, @"<w:pStyle w:val=""(Heading(\d))""", RegexOptions.IgnoreCase);
                if (styleMatch.Success && int.TryParse(styleMatch.Groups[2].Value, out var level) && level is >= 1 and <= 6)
                {
                    builder.AppendLine(new string('#', level) + " " + DecodeEntities(text).Trim());
                }
                else
                {
                    var decoded = DecodeEntities(text).Trim();
                    if (decoded.Length > 0)
                    {
                        builder.AppendLine(decoded);
                    }
                }
            }

            return builder.ToString().TrimEnd() + "\n";
        }

        /// <summary>
        /// Best-effort, experimental PDF text extraction. Decompresses FlateDecode
        /// content streams and pulls text out of the Tj/TJ show-text operators.
        /// Only works for text-based PDFs that use simple font encodings; complex
        /// PDFs (scanned images, CID-keyed fonts) will yield little or no text.
        /// </summary>
        internal static string ConvertPdfToMarkdown(string filePath)
        {
            var bytes = File.ReadAllBytes(filePath);
            var builder = new StringBuilder();

            // Locate and decompress all FlateDecode streams
            var offsets = new List<int>();
            for (int i = 0; i + 5 < bytes.Length; i++)
            {
                if (bytes[i] == 's' && bytes[i + 1] == 't' && bytes[i + 2] == 'r' && bytes[i + 3] == 'e' && bytes[i + 4] == 'a' && bytes[i + 5] == 'm')
                {
                    offsets.Add(i + 6);
                }
            }

            foreach (var offset in offsets)
            {
                var chunk = DecompressStream(bytes, offset);
                if (chunk is null)
                {
                    continue;
                }

                // Collect text from (...) Tj  and  [...] TJ operators
                var content = Encoding.Latin1.GetString(chunk);
                foreach (Match m in Regex.Matches(content, @"\(((?:[^()\\]|\\.)*)\)\s*Tj|\[((?:[^\[\]]|\\.)*)\]\s*TJ"))
                {
                    var raw = m.Groups[1].Success ? m.Groups[1].Value : Regex.Replace(m.Groups[2].Value, @"\((?:[^()\\]|\\.)*\)|\\[^0-9]", string.Empty);
                    var text = UnescapePdfText(raw);
                    if (text.Trim().Length > 0)
                    {
                        builder.Append(text);
                    }
                }
                if (builder.Length > 0)
                {
                    builder.Append('\n');
                }
            }

            var result = builder.ToString().Trim();
            if (result.Length == 0)
            {
                throw new NotSupportedException(
                    $"No text could be extracted from PDF '{filePath}'. PDF text extraction is experimental and " +
                    "does not support scanned/image-based or CID-encoded PDFs.");
            }

            return result + "\n";
        }

        private static byte[]? DecompressStream(byte[] bytes, int offset)
        {
            // Skip whitespace/EOL after 'stream'
            while (offset < bytes.Length && (bytes[offset] is (byte)' ' or (byte)'\r' or (byte)'\n'))
            {
                offset++;
            }

            // Find 'endstream'
            var end = IndexOfBytes(bytes, "endstream"u8, offset);
            if (end < 0)
            {
                return null;
            }

            var compressed = new byte[end - offset];
            Array.Copy(bytes, offset, compressed, 0, compressed.Length);

            try
            {
                using var input = new MemoryStream(compressed);
                using var zlib = new System.IO.Compression.ZLibStream(input, CompressionMode.Decompress);
                using var output = new MemoryStream();
                zlib.CopyTo(output);
                return output.ToArray();
            }
            catch (InvalidDataException)
            {
                // Stream was not zlib-compressed; use raw bytes
                return compressed;
            }
            catch
            {
                return null;
            }
        }

        private static int IndexOfBytes(byte[] haystack, ReadOnlySpan<byte> needle, int start)
        {
            for (var i = start; i <= haystack.Length - needle.Length; i++)
            {
                var matched = true;
                for (var j = 0; j < needle.Length; j++)
                {
                    if (haystack[i + j] != needle[j])
                    {
                        matched = false;
                        break;
                    }
                }

                if (matched)
                {
                    return i;
                }
            }

            return -1;
        }

        private static string UnescapePdfText(string text)
        {
            return text.Replace("\\(", "(").Replace("\\)", ")").Replace("\\\\", "\\");
        }

        private static string DecodeEntities(string text)
        {
            return text
                .Replace("&nbsp;", " ")
                .Replace("&amp;", "&")
                .Replace("&lt;", "<")
                .Replace("&gt;", ">")
                .Replace("&quot;", "\"")
                .Replace("&#39;", "'");
        }
    }
}