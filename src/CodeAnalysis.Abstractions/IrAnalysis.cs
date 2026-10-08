using System;
using System.Collections.Generic;
using System.Linq;
using SpecTreeGenerator.Common.Models;

namespace SpecTreeGenerator.CodeAnalysis.Abstractions
{
    /// <summary>
    /// Extracts components from parsed sources in the intermediate
    /// representation. Pure IR: no compiler-specific types. The outputs are
    /// the same <see cref="CodeComponent"/> instances the Roslyn extractor
    /// produced, so downstream stages (hierarchy, specifications) are
    /// unchanged.
    /// </summary>
    public static class IrComponentExtractor
    {
        /// <summary>
        /// Extracts components from the declarations of one parsed source.
        /// The symbol id becomes the component id, preserving the existing
        /// id scheme ("namespace:...", "class:...", ...) so hierarchy and
        /// relationship consumers continue to work.
        /// </summary>
        public static List<CodeComponent> ExtractComponents(ParsedSource source)
        {
            var components = new List<CodeComponent>();

            foreach (var declaration in source.Declarations)
            {
                var component = new CodeComponent
                {
                    Id = declaration.SymbolId,
                    Name = declaration.Name,
                    Type = declaration.Type,
                    FullPath = declaration.FullName,
                    ParentId = declaration.ParentSymbolId ?? string.Empty,
                    SourceFile = source.FilePath,
                    SourceFilePath = source.FilePath
                };
                components.Add(component);
            }

            return components;
        }
    }

    /// <summary>
    /// Analyzes relationships between components from parsed sources in the
    /// intermediate representation. Pure IR: heritage clauses, type uses and
    /// method calls are read from <see cref="ReferenceNode"/> entries, so
    /// name-resolution fidelity is whatever the backend recorded.
    /// </summary>
    public static class IrRelationshipAnalyzer
    {
        /// <summary>
        /// Analyzes relationships within one parsed source. Cross-file
        /// targets resolve through the shared id-to-component lookup built
        /// from all files by the caller (same contract as the Roslyn path).
        /// </summary>
        public static List<CodeRelationship> AnalyzeRelationships(
            ParsedSource source,
            IReadOnlyDictionary<string, CodeComponent> componentLookup)
        {
            var relationships = new List<CodeRelationship>();

            foreach (var reference in source.References)
            {
                var targetId = reference.ResolvedSymbolId;
                if (targetId == null || !componentLookup.ContainsKey(targetId))
                {
                    continue;
                }

                if (reference.ContainingSymbolId == null ||
                    !componentLookup.ContainsKey(reference.ContainingSymbolId))
                {
                    continue;
                }

                var sourceId = reference.ContainingSymbolId;
                RelationshipType type;
                string prefix;
                switch (reference.Kind)
                {
                    case ReferenceKind.BaseClass:
                        type = RelationshipType.Inheritance;
                        prefix = "inheritance";
                        break;
                    case ReferenceKind.InterfaceImplementation:
                        type = RelationshipType.Implementation;
                        prefix = "implementation";
                        break;
                    case ReferenceKind.MethodCall:
                        type = RelationshipType.Usage;
                        prefix = "usage";
                        break;
                    default:
                        type = RelationshipType.Dependency;
                        prefix = "dependency";
                        break;
                }

                relationships.Add(new CodeRelationship
                {
                    Id = $"{prefix}:{sourceId}:{targetId}",
                    Type = type,
                    SourceComponentId = sourceId,
                    TargetComponentId = targetId
                });
            }

            return relationships;
        }
    }
}
