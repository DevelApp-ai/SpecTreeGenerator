using System;
using System.Collections.Generic;
using System.Linq;

namespace GeneticSpecOptimizer.Common.Models
{
    /// <summary>
    /// Represents a specification file
    /// </summary>
    public class SpecificationFile
    {
        /// <summary>
        /// Unique identifier for this specification
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Name of the specification
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Content of the specification
        /// </summary>
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// ID of the component this specification describes
        /// </summary>
        public string ComponentId { get; set; } = string.Empty;

        /// <summary>
        /// ID of the parent specification, if any
        /// </summary>
        public string? ParentSpecificationId { get; set; }

        /// <summary>
        /// File path for the specification
        /// </summary>
        public string FilePath { get; set; } = string.Empty;

        /// <summary>
        /// Path property for backward compatibility
        /// </summary>
        public string Path => FilePath;

        /// <summary>
        /// Additional metadata
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Creates a deep clone of this specification
        /// </summary>
        public SpecificationFile Clone()
        {
            return new SpecificationFile
            {
                Id = this.Id,
                Name = this.Name,
                Content = this.Content,
                ComponentId = this.ComponentId,
                ParentSpecificationId = this.ParentSpecificationId,
                FilePath = this.FilePath,
                Metadata = new Dictionary<string, object>(this.Metadata)
            };
        }

        /// <summary>
        /// Returns a string representation of the specification
        /// </summary>
        public override string ToString()
        {
            return $"Specification: {Name} ({Id})";
        }
    }
}