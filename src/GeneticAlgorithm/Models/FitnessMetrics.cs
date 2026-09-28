using System;
using System.Collections.Generic;

namespace GeneticSpecOptimizer.GeneticAlgorithm.Models
{
    /// <summary>
    /// Represents fitness metrics for a specification genome
    /// </summary>
    public class FitnessMetrics
    {
        /// <summary>
        /// Score for hierarchy optimization (higher is better)
        /// </summary>
        public double HierarchyOptimizationScore { get; set; }

        /// <summary>
        /// Score for size optimization (higher is better)
        /// </summary>
        public double SizeOptimizationScore { get; set; }

        /// <summary>
        /// Score for code alignment (higher is better)
        /// </summary>
        public double CodeAlignmentScore { get; set; }

        /// <summary>
        /// Score for completeness (higher is better)
        /// </summary>
        public double CompletenessScore { get; set; }

        /// <summary>
        /// Creates a new instance of fitness metrics
        /// </summary>
        public FitnessMetrics()
        {
            HierarchyOptimizationScore = 0;
            SizeOptimizationScore = 0;
            CodeAlignmentScore = 0;
            CompletenessScore = 0;
        }

        /// <summary>
        /// Returns a string representation of the metrics
        /// </summary>
        public override string ToString()
        {
            return $"Hierarchy: {HierarchyOptimizationScore:F2}, Size: {SizeOptimizationScore:F2}, Alignment: {CodeAlignmentScore:F2}, Completeness: {CompletenessScore:F2}";
        }
    }
}