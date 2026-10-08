using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using SpecTreeGenerator.Common.Models;

namespace SpecTreeGenerator.CodeAnalysis.Abstractions
{
    /// <summary>
    /// Language-agnostic analysis backend. Implementations parse source files
    /// into the intermediate representation (<see cref="ParsedSource"/>) and
    /// resolve symbol references. The Roslyn implementation serves C#; a
    /// Minotaur/StepParser implementation (issue #21) will serve the grammar
    /// languages.
    /// </summary>
    public interface ILanguageAnalysisBackend
    {
        /// <summary>Language identifier this backend handles (e.g. "csharp").</summary>
        string Language { get; }

        /// <summary>File extensions (lowercase, with dot) this backend can parse.</summary>
        IReadOnlyCollection<string> SupportedExtensions { get; }

        /// <summary>
        /// Parses one source file into the intermediate representation.
        /// </summary>
        Task<ParsedSource> ParseAsync(string filePath);

        /// <summary>
        /// Resolves a reference name to a symbol id within the given parse
        /// results. Backends with a semantic binder resolve precisely; others
        /// may fall back to name matching and return null when ambiguous.
        /// </summary>
        string? ResolveSymbol(string name, IReadOnlyList<ParsedSource> context);
    }

    /// <summary>
    /// Registry of available backends keyed by language and file extension.
    /// The pipeline asks this which backend handles a file; unknown
    /// extensions are skipped by the caller.
    /// </summary>
    public interface IAnalysisBackendRegistry
    {
        /// <summary>All registered backends.</summary>
        IEnumerable<ILanguageAnalysisBackend> All { get; }

        /// <summary>Returns the backend for a file path, or null when unsupported.</summary>
        ILanguageAnalysisBackend? ForFile(string filePath);
    }

    /// <summary>
    /// Default in-memory registry. Backends register themselves on
    /// construction; the pipeline composes one registry per run.
    /// </summary>
    public class AnalysisBackendRegistry : IAnalysisBackendRegistry
    {
        private readonly List<ILanguageAnalysisBackend> _backends = new();

        /// <summary>Registers a backend. Last registration wins per language.</summary>
        public AnalysisBackendRegistry Register(ILanguageAnalysisBackend backend)
        {
            _backends.RemoveAll(b => string.Equals(b.Language, backend.Language, StringComparison.OrdinalIgnoreCase));
            _backends.Add(backend);
            return this;
        }

        /// <inheritdoc />
        public IEnumerable<ILanguageAnalysisBackend> All => _backends;

        /// <inheritdoc />
        public ILanguageAnalysisBackend? ForFile(string filePath)
        {
            var extension = System.IO.Path.GetExtension(filePath).ToLowerInvariant();
            foreach (var backend in _backends)
            {
                foreach (var supported in backend.SupportedExtensions)
                {
                    if (string.Equals(supported, extension, StringComparison.OrdinalIgnoreCase))
                    {
                        return backend;
                    }
                }
            }

            return null;
        }
    }
}
