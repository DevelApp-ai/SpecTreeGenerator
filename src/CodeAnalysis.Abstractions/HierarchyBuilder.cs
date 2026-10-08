using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using SpecTreeGenerator.Common.Models;

namespace SpecTreeGenerator.CodeAnalysis.Abstractions
{
    /// <summary>
    /// Builds a code hierarchy from extracted components
    /// </summary>
    public class HierarchyBuilder
    {
        /// <summary>
        /// Builds a code hierarchy from the extracted components
        /// </summary>
        public static CodeHierarchy BuildHierarchy(List<CodeComponent> components)
        {
            var hierarchy = new CodeHierarchy();

            // First pass: Add all components to the hierarchy
            foreach (var component in components)
            {
                hierarchy.AddComponent(component);
            }

            // Second pass: Reorganize to ensure proper parent-child relationships
            hierarchy.ReorganizeHierarchy();

            return hierarchy;
        }

        /// <summary>
        /// Updates the code hierarchy with component relationships
        /// </summary>
        public static void UpdateHierarchyWithRelationships(CodeHierarchy hierarchy, List<CodeRelationship> relationships)
        {
            // This method could be used to enhance the hierarchy with relationship information
            // For example, adding metadata to nodes based on their relationships

            foreach (var relationship in relationships)
            {
                if (hierarchy.NodeLookup.TryGetValue(relationship.SourceComponentId, out var sourceNode) &&
                    hierarchy.NodeLookup.TryGetValue(relationship.TargetComponentId, out var targetNode))
                {
                    // Add relationship metadata to the nodes
                    string relationshipKey = $"relationship:{relationship.Type}";

                    // For the source node, add outgoing relationship
                    if (!sourceNode.Metadata.TryGetValue(relationshipKey, out var outList))
                    {
                        outList = new List<string>();
                        sourceNode.Metadata[relationshipKey] = outList;
                    }
                    ((List<string>)outList).Add(targetNode.ComponentId);

                    // For the target node, add incoming relationship
                    string incomingKey = $"incoming:{relationship.Type}";
                    if (!targetNode.Metadata.TryGetValue(incomingKey, out var inList))
                    {
                        inList = new List<string>();
                        targetNode.Metadata[incomingKey] = inList;
                    }
                    ((List<string>)inList).Add(sourceNode.ComponentId);
                }
            }
        }
    }

    /// <summary>
    /// Extension of HierarchyNode to include metadata
    /// </summary>
    public static class HierarchyNodeExtensions
    {
        /// <summary>
        /// Adds metadata to a hierarchy node
        /// </summary>
        public static void AddMetadata(this HierarchyNode node, string key, object value)
        {
            if (node.Metadata == null)
            {
                node.Metadata = new Dictionary<string, object>();
            }

            node.Metadata[key] = value;
        }

        /// <summary>
        /// Gets metadata from a hierarchy node
        /// </summary>
        public static T GetMetadata<T>(this HierarchyNode node, string key, T? defaultValue = default)
        {
            if (node.Metadata == null || !node.Metadata.TryGetValue(key, out var raw))
            {
                return defaultValue!;
            }

            return (T)raw!;
        }
    }
}