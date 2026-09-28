using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using GeneticSpecOptimizer.Common.Models;
using GeneticSpecOptimizer.GeneticAlgorithm.Interfaces;
using GeneticSpecOptimizer.GeneticAlgorithm.Models;

namespace GeneticSpecOptimizer.GeneticAlgorithm.Fitness
{
    /// <summary>
    /// Implementation of the fitness evaluator
    /// </summary>
    public class FitnessEvaluator : IFitnessEvaluator
    {
        private static readonly string[] ParagraphSeparators = { "\r\n\r\n", "\n\n" };
        /// <summary>
        /// Evaluates the fitness of a genome
        /// </summary>
        public async Task<double> EvaluateAsync(SpecificationGenome genome, CodeHierarchy codeHierarchy, HierarchyOptimizerConfig? config)
        {
            // Calculate metrics
            var metrics = await CalculateMetricsAsync(genome, codeHierarchy);

            // Calculate fitness score
            double fitnessScore = 0;

            // Hierarchy optimization score (higher is better)
            fitnessScore += metrics.HierarchyOptimizationScore * 0.4;

            // Size optimization score (higher is better)
            fitnessScore += metrics.SizeOptimizationScore * 0.3;

            // Code alignment score (higher is better)
            fitnessScore += metrics.CodeAlignmentScore * 0.2;

            // Completeness score (higher is better)
            fitnessScore += metrics.CompletenessScore * 0.1;

            return fitnessScore;
        }

        /// <summary>
        /// Calculates detailed fitness metrics for a genome
        /// </summary>
        public async Task<FitnessMetrics> CalculateMetricsAsync(SpecificationGenome genome, CodeHierarchy codeHierarchy)
        {
            await Task.CompletedTask;
            var metrics = new FitnessMetrics();

            // Calculate hierarchy optimization score
            metrics.HierarchyOptimizationScore = CalculateHierarchyOptimizationScore(genome);

            // Calculate size optimization score
            metrics.SizeOptimizationScore = CalculateSizeOptimizationScore(genome);

            // Calculate code alignment score
            metrics.CodeAlignmentScore = CalculateCodeAlignmentScore(genome, codeHierarchy);

            // Calculate completeness score
            metrics.CompletenessScore = CalculateCompletenessScore(genome, codeHierarchy);

            return metrics;
        }

        private static double CalculateHierarchyOptimizationScore(SpecificationGenome genome)
        {
            // Count specifications with parents
            int specificationsWithParents = genome.ParentChildRelationships.Count;

            // Count total specifications
            int totalSpecifications = genome.SpecificationFiles.Count;

            // Calculate hierarchy depth
            int maxDepth = CalculateMaxHierarchyDepth(genome);

            // Calculate hierarchy breadth
            double averageBreadth = CalculateAverageHierarchyBreadth(genome);

            // Calculate score
            double parentRatio = totalSpecifications > 0 ? (double)specificationsWithParents / totalSpecifications : 0;
            double depthFactor = Math.Min(maxDepth / 5.0, 1.0); // Normalize depth, cap at 5 levels
            double breadthFactor = Math.Min(1.0, 5.0 / Math.Max(1.0, averageBreadth)); // Prefer smaller breadth, optimal around 5

            // Combine factors
            return parentRatio * 0.5 + depthFactor * 0.3 + breadthFactor * 0.2;
        }

        private static double CalculateSizeOptimizationScore(SpecificationGenome genome)
        {
            // Calculate total content size
            int totalContentSize = genome.SpecificationFiles.Sum(s => s.Content.Length);

            // Calculate unique content size (approximate by counting unique paragraphs)
            var allParagraphs = new HashSet<string>();
            foreach (var spec in genome.SpecificationFiles)
            {
                var paragraphs = spec.Content.Split(ParagraphSeparators, StringSplitOptions.RemoveEmptyEntries);
                foreach (var paragraph in paragraphs)
                {
                    allParagraphs.Add(paragraph.Trim());
                }
            }
            int uniqueContentSize = allParagraphs.Sum(p => p.Length);

            // Calculate duplication ratio
            double duplicationRatio = uniqueContentSize > 0 ? (double)uniqueContentSize / totalContentSize : 0;

            // Calculate size efficiency
            double sizeEfficiency = CalculateSizeEfficiency(genome);

            // Combine factors
            return duplicationRatio * 0.7 + sizeEfficiency * 0.3;
        }

        private static double CalculateCodeAlignmentScore(SpecificationGenome genome, CodeHierarchy codeHierarchy)
        {
            // Check if code hierarchy is available
            if (codeHierarchy == null || codeHierarchy.Components.Count == 0)
            {
                return 0.5; // Neutral score if no code hierarchy
            }

            // Count specifications with matching components
            int matchingComponents = 0;
            foreach (var spec in genome.SpecificationFiles)
            {
                if (!string.IsNullOrEmpty(spec.ComponentId) &&
                    codeHierarchy.Components.Any(c => c.Id == spec.ComponentId))
                {
                    matchingComponents++;
                }
            }

            // Calculate component coverage
            double componentCoverage = (double)matchingComponents / genome.SpecificationFiles.Count;

            // Calculate relationship alignment
            double relationshipAlignment = CalculateRelationshipAlignment(genome, codeHierarchy);

            // Combine factors
            return componentCoverage * 0.6 + relationshipAlignment * 0.4;
        }

        private static double CalculateCompletenessScore(SpecificationGenome genome, CodeHierarchy codeHierarchy)
        {
            // Check if code hierarchy is available
            if (codeHierarchy == null || codeHierarchy.Components.Count == 0)
            {
                return 0.5; // Neutral score if no code hierarchy
            }

            // Count covered components
            var coveredComponentIds = genome.SpecificationFiles
                .Where(s => !string.IsNullOrEmpty(s.ComponentId))
                .Select(s => s.ComponentId)
                .Distinct()
                .ToHashSet();

            // Calculate component coverage
            double componentCoverage = (double)coveredComponentIds.Count / codeHierarchy.Components.Count;

            // Calculate content completeness
            double contentCompleteness = CalculateContentCompleteness(genome);

            // Combine factors
            return componentCoverage * 0.7 + contentCompleteness * 0.3;
        }

        private static int CalculateMaxHierarchyDepth(SpecificationGenome genome)
        {
            // Calculate maximum depth of hierarchy
            int maxDepth = 0;

            foreach (var spec in genome.SpecificationFiles)
            {
                int depth = CalculateDepth(genome, spec.Id);
                maxDepth = Math.Max(maxDepth, depth);
            }

            return maxDepth;
        }

        private static int CalculateDepth(SpecificationGenome genome, string specId)
        {
            // Calculate depth of specification in hierarchy
            int depth = 0;
            string currentId = specId;

            while (genome.ParentChildRelationships.TryGetValue(currentId, out string? parentId))
            {
                depth++;
                currentId = parentId!;

                // Prevent infinite loops
                if (depth > 100)
                {
                    break;
                }
            }

            return depth;
        }

        private static double CalculateAverageHierarchyBreadth(SpecificationGenome genome)
        {
            // Calculate average number of children per parent
            var parentCounts = new Dictionary<string, int>();

            foreach (var parentId in genome.ParentChildRelationships.Values)
            {
                if (parentCounts.TryGetValue(parentId, out var count))
                {
                    parentCounts[parentId] = count + 1;
                }
                else
                {
                    parentCounts[parentId] = 1;
                }
            }

            // Calculate average
            return parentCounts.Count > 0 ? parentCounts.Values.Average() : 0;
        }

        private static double CalculateSizeEfficiency(SpecificationGenome genome)
        {
            // Calculate size efficiency based on content reuse
            double totalContentSize = genome.SpecificationFiles.Sum(s => s.Content.Length);

            // Calculate content that could be inherited
            double inheritableContent = 0;
            foreach (var spec in genome.SpecificationFiles)
            {
                if (genome.ParentChildRelationships.TryGetValue(spec.Id, out string? parentId))
                {
                    var parent = genome.SpecificationFiles.FirstOrDefault(s => s.Id == parentId);
                    if (parent != null)
                    {
                        // Estimate inheritable content by comparing paragraphs
                        var specParagraphs = spec.Content.Split(ParagraphSeparators, StringSplitOptions.RemoveEmptyEntries);
                        var parentParagraphs = parent.Content.Split(ParagraphSeparators, StringSplitOptions.RemoveEmptyEntries);

                        foreach (var paragraph in specParagraphs)
                        {
                            if (parentParagraphs.Any(p => p.Contains(paragraph)))
                            {
                                inheritableContent += paragraph.Length;
                            }
                        }
                    }
                }
            }

            // Calculate efficiency ratio
            return totalContentSize > 0 ? inheritableContent / totalContentSize : 0;
        }

        private static double CalculateRelationshipAlignment(SpecificationGenome genome, CodeHierarchy codeHierarchy)
        {
            // Calculate alignment between specification relationships and code relationships
            int alignedRelationships = 0;
            int totalRelationships = genome.ParentChildRelationships.Count;

            foreach (var relationship in genome.ParentChildRelationships)
            {
                string childId = relationship.Key;
                string parentId = relationship.Value;

                var childSpec = genome.SpecificationFiles.FirstOrDefault(s => s.Id == childId);
                var parentSpec = genome.SpecificationFiles.FirstOrDefault(s => s.Id == parentId);

                if (childSpec != null && parentSpec != null &&
                    !string.IsNullOrEmpty(childSpec.ComponentId) && !string.IsNullOrEmpty(parentSpec.ComponentId))
                {
                    // Check if there's a corresponding relationship in the code
                    var childComponent = codeHierarchy.Components.FirstOrDefault(c => c.Id == childSpec.ComponentId);
                    var parentComponent = codeHierarchy.Components.FirstOrDefault(c => c.Id == parentSpec.ComponentId);

                    if (childComponent != null && parentComponent != null)
                    {
                        // Check for direct relationship
                        var codeRelationship = codeHierarchy.Relationships.FirstOrDefault(r =>
                            r.SourceComponentId == childComponent.Id && r.TargetComponentId == parentComponent.Id);

                        if (codeRelationship != null)
                        {
                            alignedRelationships++;
                        }
                    }
                }
            }

            // Calculate alignment ratio
            return totalRelationships > 0 ? (double)alignedRelationships / totalRelationships : 0;
        }

        private static double CalculateContentCompleteness(SpecificationGenome genome)
        {
            // Calculate content completeness based on content size and structure
            double averageContentSize = genome.SpecificationFiles.Average(s => s.Content.Length);
            double targetSize = 1000; // Target size in characters

            // Calculate size factor
            double sizeFactor = Math.Min(averageContentSize / targetSize, 1.0);

            // Calculate structure factor
            double structureFactor = CalculateStructureFactor(genome);

            // Combine factors
            return sizeFactor * 0.6 + structureFactor * 0.4;
        }

        private static double CalculateStructureFactor(SpecificationGenome genome)
        {
            // Calculate structure factor based on presence of sections
            int totalSections = 0;
            int totalSpecifications = genome.SpecificationFiles.Count;

            foreach (var spec in genome.SpecificationFiles)
            {
                // Count headings
                var headings = System.Text.RegularExpressions.Regex.Matches(spec.Content, @"^#+\s+.+$", System.Text.RegularExpressions.RegexOptions.Multiline);
                totalSections += headings.Count;
            }

            // Calculate average sections per specification
            double averageSections = totalSpecifications > 0 ? (double)totalSections / totalSpecifications : 0;

            // Normalize
            return Math.Min(averageSections / 5.0, 1.0); // Target of 5 sections per specification
        }
    }
}