using System;
using System.Collections.Generic;
using System.Linq;

namespace GeneticSpecOptimizer.Common.Models
{
    /// <summary>
    /// Type of code component
    /// </summary>
    public enum ComponentType
    {
        Namespace,
        Class,
        Interface,
        Struct,
        Enum,
        Method,
        Property,
        Field,
        Event,
        Delegate,
        Unknown
    }

    /// <summary>
    /// Represents a code component
    /// </summary>
    public class CodeComponent
    {
        /// <summary>
        /// Unique identifier for this component
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Name of the component
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Type of the component
        /// </summary>
        public ComponentType Type { get; set; }

        /// <summary>
        /// Full path of the component (e.g., namespace:class:method)
        /// </summary>
        public string FullPath { get; set; } = string.Empty;

        /// <summary>
        /// Namespace containing this component
        /// </summary>
        public string Namespace { get; set; } = string.Empty;

        /// <summary>
        /// Parent component ID (if any)
        /// </summary>
        public string ParentId { get; set; } = string.Empty;

        /// <summary>
        /// Source file path
        /// </summary>
        public string SourceFile { get; set; } = string.Empty;

        /// <summary>
        /// Absolute path to the source file
        /// </summary>
        public string SourceFilePath { get; set; } = string.Empty;

        /// <summary>
        /// Source code of the component
        /// </summary>
        public string SourceCode { get; set; } = string.Empty;

        /// <summary>
        /// Documentation comments
        /// </summary>
        public string Documentation { get; set; } = string.Empty;

        /// <summary>
        /// Method or type signature (if applicable)
        /// </summary>
        public string Signature { get; set; } = string.Empty;

        /// <summary>
        /// Starting line number in source file
        /// </summary>
        public int StartLine { get; set; }

        /// <summary>
        /// Ending line number in source file
        /// </summary>
        public int EndLine { get; set; }

        /// <summary>
        /// Additional metadata
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Creates a deep clone of this component
        /// </summary>
        public CodeComponent Clone()
        {
            return new CodeComponent
            {
                Id = this.Id,
                Name = this.Name,
                Type = this.Type,
                FullPath = this.FullPath,
                Namespace = this.Namespace,
                ParentId = this.ParentId,
                SourceFile = this.SourceFile,
                SourceFilePath = this.SourceFilePath,
                SourceCode = this.SourceCode,
                Documentation = this.Documentation,
                Signature = this.Signature,
                StartLine = this.StartLine,
                EndLine = this.EndLine,
                Metadata = new Dictionary<string, object>(this.Metadata)
            };
        }

        /// <summary>
        /// Returns a string representation of the component
        /// </summary>
        public override string ToString()
        {
            return $"{Type}: {Name} ({FullPath})";
        }
    }
}