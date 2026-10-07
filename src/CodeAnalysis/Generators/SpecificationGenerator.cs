using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using SpecTreeGenerator.Common.Models;

namespace SpecTreeGenerator.CodeAnalysis.Generators
{
    /// <summary>
    /// Implementation of the specification generator
    /// </summary>
    public class SpecificationGenerator : Interfaces.ISpecificationGenerator
    {
        /// <summary>
        /// Generates specifications from a code hierarchy
        /// </summary>
        public async Task<IEnumerable<SpecificationFile>> GenerateSpecificationsAsync(CodeHierarchy hierarchy, bool includeImplementationDetails)
        {
            var specifications = new List<SpecificationFile>();

            // Process each root node
            foreach (var rootNode in hierarchy.RootNodes)
            {
                await ProcessNodeAsync(rootNode, null, specifications, includeImplementationDetails);
            }

            return specifications;
        }

        private static async Task ProcessNodeAsync(HierarchyNode node, SpecificationFile? parentSpec, List<SpecificationFile> specifications, bool includeImplementationDetails)
        {
            // Skip if not a component we want to generate a specification for
            if (string.IsNullOrEmpty(node.ComponentId) || !ShouldGenerateSpecification(node))
            {
                // Process children
                foreach (var child in node.Children)
                {
                    await ProcessNodeAsync(child, parentSpec, specifications, includeImplementationDetails);
                }

                return;
            }

            // Create specification
            var spec = new SpecificationFile
            {
                Name = GetSpecificationName(node),
                ComponentId = node.ComponentId,
                Content = await GenerateSpecificationContentAsync(node, includeImplementationDetails),
                FilePath = GetSpecificationFilePath(node)
            };

            // Set parent specification if any
            if (parentSpec != null)
            {
                spec.ParentSpecificationId = parentSpec.Id;
            }

            // Add to list
            specifications.Add(spec);

            // Process children
            foreach (var child in node.Children)
            {
                await ProcessNodeAsync(child, spec, specifications, includeImplementationDetails);
            }
        }

        private static bool ShouldGenerateSpecification(HierarchyNode node)
        {
            // Generate specifications for classes, interfaces, and structs
            return node.Type == "Class" || node.Type == "Interface" || node.Type == "Struct";
        }

        private static string GetSpecificationName(HierarchyNode node)
        {
            return $"{node.Name}Specification";
        }

        private static string GetSpecificationFilePath(HierarchyNode node)
        {
            // Get component
            if (node.Metadata.TryGetValue("Component", out var componentObj) &&
                componentObj is CodeComponent component)
            {
                // Create path based on namespace and name
                var namespaceParts = component.Namespace.Split('.');
                var path = string.Join("/", namespaceParts);

                return $"{path}/{component.Name}.ai";
            }

            // Fallback
            return $"{node.Name}.ai";
        }

        private static async Task<string> GenerateSpecificationContentAsync(HierarchyNode node, bool includeImplementationDetails)
        {
            await Task.CompletedTask;
            // Get component
            if (!node.Metadata.TryGetValue("Component", out var componentObj) ||
                !(componentObj is CodeComponent component))
            {
                return $"# {node.Name}\n\nNo component information available.";
            }

            // Build content
            var content = new System.Text.StringBuilder();

            // Add header
            content.AppendLine(CultureInfo.InvariantCulture, $"# {component.Name}");
            content.AppendLine();

            // Add documentation
            if (!string.IsNullOrEmpty(component.Documentation))
            {
                content.AppendLine(component.Documentation);
                content.AppendLine();
            }

            // Add signature
            if (!string.IsNullOrEmpty(component.Signature))
            {
                content.AppendLine("## Signature");
                content.AppendLine();
                content.AppendLine("```csharp");
                content.AppendLine(component.Signature);
                content.AppendLine("```");
                content.AppendLine();
            }

            // Add implementation details if requested
            if (includeImplementationDetails && !string.IsNullOrEmpty(component.SourceCode))
            {
                content.AppendLine("## Implementation");
                content.AppendLine();
                content.AppendLine("```csharp");
                content.AppendLine(component.SourceCode);
                content.AppendLine("```");
                content.AppendLine();
            }

            // Add child components
            if (node.Children.Count > 0)
            {
                content.AppendLine("## Members");
                content.AppendLine();

                foreach (var child in node.Children)
                {
                    if (child.Metadata.TryGetValue("Component", out var childComponentObj) &&
                        childComponentObj is CodeComponent childComponent)
                    {
                        content.AppendLine(CultureInfo.InvariantCulture, $"### {childComponent.Name}");
                        content.AppendLine();

                        if (!string.IsNullOrEmpty(childComponent.Documentation))
                        {
                            content.AppendLine(childComponent.Documentation);
                            content.AppendLine();
                        }

                        if (!string.IsNullOrEmpty(childComponent.Signature))
                        {
                            content.AppendLine("```csharp");
                            content.AppendLine(childComponent.Signature);
                            content.AppendLine("```");
                            content.AppendLine();
                        }
                    }
                }
            }

            return content.ToString();
        }
    }
}