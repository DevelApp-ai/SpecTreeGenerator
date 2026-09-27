using System;
using System.Collections.Generic;
using System.Linq;

namespace GeneticSpecOptimizer.Common.Models
{
    /// <summary>
    /// Type of relationship between components
    /// </summary>
    public enum RelationshipType
    {
        Inheritance,
        Implementation,
        Dependency,
        Composition,
        Aggregation,
        Usage,
        Call,
        Unknown
    }

    /// <summary>
    /// Represents a relationship between code components
    /// </summary>
    public class CodeRelationship
    {
        /// <summary>
        /// Unique identifier for this relationship
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();
        
        /// <summary>
        /// Type of relationship
        /// </summary>
        public RelationshipType Type { get; set; }
        
        /// <summary>
        /// Source component ID
        /// </summary>
        public string SourceComponentId { get; set; } = string.Empty;
        
        /// <summary>
        /// Target component ID
        /// </summary>
        public string TargetComponentId { get; set; } = string.Empty;
        
        /// <summary>
        /// Additional metadata
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
        
        /// <summary>
        /// Creates a deep clone of this relationship
        /// </summary>
        public CodeRelationship Clone()
        {
            return new CodeRelationship
            {
                Id = this.Id,
                Type = this.Type,
                SourceComponentId = this.SourceComponentId,
                TargetComponentId = this.TargetComponentId,
                Metadata = new Dictionary<string, object>(this.Metadata)
            };
        }
        
        /// <summary>
        /// Returns a string representation of the relationship
        /// </summary>
        public override string ToString()
        {
            return $"{Type}: {SourceComponentId} -> {TargetComponentId}";
        }
    }
}
