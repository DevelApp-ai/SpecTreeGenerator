using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SpecTreeGenerator.Common.Models;

namespace SpecTreeGenerator.CodeAnalysis.Abstractions
{
    /// <summary>
    /// Language-agnostic intermediate representation of one parsed source file.
    /// Backends produce this; extractors and analyzers consume it. No compiler-
    /// specific types appear here so any language backend (Roslyn, Minotaur/
    /// StepParser, ...) can supply the data.
    /// </summary>
    public class ParsedSource
    {
        /// <summary>Absolute or relative path of the file this was parsed from.</summary>
        public string FilePath { get; set; } = string.Empty;

        /// <summary>Language identifier agreed with the backend (e.g. "csharp").</summary>
        public string Language { get; set; } = string.Empty;

        /// <summary>All declarations found in the file, in source order.</summary>
        public IReadOnlyList<DeclarationNode> Declarations { get; set; } = Array.Empty<DeclarationNode>();

        /// <summary>All references found in the file, in source order.</summary>
        public IReadOnlyList<ReferenceNode> References { get; set; } = Array.Empty<ReferenceNode>();
    }

    /// <summary>
    /// A declaration in the intermediate representation: maps to one
    /// <see cref="ComponentType"/> of the existing component model.
    /// </summary>
    public class DeclarationNode
    {
        /// <summary>Backend-stable identifier (e.g. "class:Namespace.Name"). Must be stable across re-parses of the same source.</summary>
        public string SymbolId { get; set; } = string.Empty;

        /// <summary>Simple (unqualified) name of the declaration.</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>Fully qualified display name (namespace-qualified).</summary>
        public string FullName { get; set; } = string.Empty;

        /// <summary>Component kind (namespace, class, method, ...).</summary>
        public ComponentType Type { get; set; } = ComponentType.Unknown;

        /// <summary>Symbol id of the lexically enclosing declaration, if any.</summary>
        public string? ParentSymbolId { get; set; }

        /// <summary>Raw names of base types (classes) listed on the declaration, as written.</summary>
        public IReadOnlyList<string> BaseClassNames { get; set; } = Array.Empty<string>();

        /// <summary>Raw names of implemented interfaces, as written.</summary>
        public IReadOnlyList<string> InterfaceNames { get; set; } = Array.Empty();

        /// <summary>Source span of the declaration, mapped to the existing TextSpan model.</summary>
        public TextSpan Span { get; set; } = new TextSpan(0, 0);
    }

    /// <summary>
    /// A symbolic reference in the intermediate representation: a use of a
    /// name that may resolve to a declaration. Resolution quality is recorded
    /// so name-based backends can participate with lower fidelity.
    /// </summary>
    public class ReferenceNode
    {
        /// <summary>Symbol id of the referenced declaration when the backend resolved it; otherwise null.</summary>
        public string? ResolvedSymbolId { get; set; }

        /// <summary>The name as it appears in source (e.g. "List", "Foo.Bar").</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>Kind of reference (inheritance, type use, method call, ...).</summary>
        public ReferenceKind Kind { get; set; } = ReferenceKind.TypeUse;

        /// <summary>Symbol id of the declaration containing the reference.</summary>
        public string? ContainingSymbolId { get; set; }

        /// <summary>Source span of the reference.</summary>
        public TextSpan Span { get; set; } = new TextSpan(0, 0);
    }

    /// <summary>Kind of symbolic reference.</summary>
    public enum ReferenceKind
    {
        /// <summary>Base class in a heritage clause.</summary>
        BaseClass,

        /// <summary>Interface in an implements/heritage clause.</summary>
        InterfaceImplementation,

        /// <summary>Type used in a field, property, parameter or return type.</summary>
        TypeUse,

        /// <summary>Method invocation.</summary>
        MethodCall
    }
}
