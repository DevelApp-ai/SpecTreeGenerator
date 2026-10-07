using System;
using System.Collections.Generic;
using System.Linq;

namespace GeneticSpecOptimizer.Common.Models
{
    /// <summary>
    /// Represents a node in a code hierarchy
    /// </summary>
    public class HierarchyNode
    {
        /// <summary>
        /// Unique identifier for this node
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Name of the node
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Type of the node (namespace, class, method, etc.)
        /// </summary>
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// Full path of the node (e.g., namespace:class:method)
        /// </summary>
        public string FullPath { get; set; } = string.Empty;

        /// <summary>
        /// Component ID this node represents
        /// </summary>
        public string ComponentId { get; set; } = string.Empty;

        /// <summary>
        /// Parent node (if any)
        /// </summary>
        public HierarchyNode? Parent { get; set; }

        /// <summary>
        /// Child nodes
        /// </summary>
        public List<HierarchyNode> Children { get; set; } = new List<HierarchyNode>();

        /// <summary>
        /// Additional metadata for the node
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Creates a deep clone of this node (without parent or children)
        /// </summary>
        public HierarchyNode Clone()
        {
            var clone = new HierarchyNode
            {
                Id = this.Id,
                Name = this.Name,
                Type = this.Type,
                FullPath = this.FullPath,
                ComponentId = this.ComponentId,
                Metadata = new Dictionary<string, object>(this.Metadata)
            };

            return clone;
        }

        /// <summary>
        /// Creates a deep clone of this node and its descendants
        /// </summary>
        public HierarchyNode CloneWithDescendants()
        {
            var clone = this.Clone();

            foreach (var child in this.Children)
            {
                var childClone = child.CloneWithDescendants();
                childClone.Parent = clone;
                clone.Children.Add(childClone);
            }

            return clone;
        }

        /// <summary>
        /// Returns a string representation of the node
        /// </summary>
        public override string ToString()
        {
            return $"{Type}: {Name} ({FullPath})";
        }
    }

    /// <summary>
    /// Represents a code hierarchy
    /// </summary>
    public class CodeHierarchy
    {
        /// <summary>
        /// Root nodes of the hierarchy
        /// </summary>
        public List<HierarchyNode> RootNodes { get; set; } = new List<HierarchyNode>();

        /// <summary>
        /// Lookup dictionary for quick node access by ID
        /// </summary>
        public Dictionary<string, HierarchyNode> NodeLookup { get; set; } = new Dictionary<string, HierarchyNode>();

        /// <summary>
        /// List of all code components in the hierarchy
        /// </summary>
        public List<CodeComponent> Components { get; set; } = new List<CodeComponent>();

        /// <summary>
        /// List of all relationships between components
        /// </summary>
        public List<CodeRelationship> Relationships { get; set; } = new List<CodeRelationship>();

        /// <summary>
        /// Creates a deep clone of this hierarchy
        /// </summary>
        public CodeHierarchy Clone()
        {
            var clone = new CodeHierarchy();

            foreach (var rootNode in this.RootNodes)
            {
                clone.RootNodes.Add(rootNode.CloneWithDescendants());
            }

            // Rebuild node lookup
            clone.RebuildNodeLookup();

            return clone;
        }

        /// <summary>
        /// Finds a node by its full path
        /// </summary>
        public HierarchyNode? FindNodeByPath(string fullPath)
        {
            foreach (var rootNode in RootNodes)
            {
                var node = FindNodeByPathRecursive(rootNode, fullPath);
                if (node != null)
                {
                    return node;
                }
            }

            return null;
        }

        /// <summary>
        /// Adds a component to the hierarchy
        /// </summary>
        public void AddComponent(CodeComponent component)
        {
            // Create a node for the component
            var node = new HierarchyNode
            {
                Id = component.Id,
                Name = component.Name,
                Type = component.Type.ToString(),
                FullPath = component.FullPath,
                ComponentId = component.Id
            };

            // Add metadata
            node.Metadata["Component"] = component;

            // Add to lookup
            NodeLookup[node.Id] = node;

            // If this is a root component, add to root nodes
            if (string.IsNullOrEmpty(component.ParentId))
            {
                RootNodes.Add(node);
            }
            else
            {
                // Find parent node
                if (NodeLookup.TryGetValue(component.ParentId, out var parentNode))
                {
                    // Add as child
                    parentNode.Children.Add(node);
                    node.Parent = parentNode;
                }
                else
                {
                    // Parent not found, add as root
                    RootNodes.Add(node);
                }
            }
        }

        /// <summary>
        /// Reorganizes the hierarchy based on component relationships
        /// </summary>
        public void ReorganizeHierarchy()
        {
            // Clear existing parent-child relationships
            foreach (var node in NodeLookup.Values)
            {
                node.Parent = null;
                node.Children.Clear();
            }

            // Rebuild root nodes
            RootNodes.Clear();

            // Rebuild parent-child relationships
            foreach (var node in NodeLookup.Values)
            {
                if (node.Metadata.TryGetValue("Component", out var componentObj) &&
                    componentObj is CodeComponent component)
                {
                    if (string.IsNullOrEmpty(component.ParentId))
                    {
                        // This is a root node
                        RootNodes.Add(node);
                    }
                    else if (NodeLookup.TryGetValue(component.ParentId, out var parentNode))
                    {
                        // Add as child to parent
                        parentNode.Children.Add(node);
                        node.Parent = parentNode;
                    }
                    else
                    {
                        // Parent not found, add as root
                        RootNodes.Add(node);
                    }
                }
            }
        }

        /// <summary>
        /// Rebuilds the node lookup dictionary
        /// </summary>
        public void RebuildNodeLookup()
        {
            NodeLookup.Clear();

            foreach (var rootNode in RootNodes)
            {
                AddToLookupRecursive(rootNode);
            }
        }

        private void AddToLookupRecursive(HierarchyNode node)
        {
            NodeLookup[node.Id] = node;

            foreach (var child in node.Children)
            {
                AddToLookupRecursive(child);
            }
        }

        private static HierarchyNode? FindNodeByPathRecursive(HierarchyNode node, string fullPath)
        {
            if (node.FullPath == fullPath)
            {
                return node;
            }

            foreach (var child in node.Children)
            {
                var result = FindNodeByPathRecursive(child, fullPath);
                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }
    }
}