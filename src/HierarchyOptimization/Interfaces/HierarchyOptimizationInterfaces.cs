using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using SpecTreeGenerator.Common.Models;
using SpecTreeGenerator.GeneticAlgorithm.Models;

namespace SpecTreeGenerator.HierarchyOptimization.Interfaces
{
    /// <summary>
    /// Interface for hierarchy optimizers
    /// </summary>
    public interface IHierarchyOptimizer
    {
        /// <summary>
        /// Optimizes a hierarchy of specifications
        /// </summary>
        Task<OptimizationResult> OptimizeAsync(CodeHierarchy codeHierarchy, IEnumerable<SpecificationFile> initialSpecifications, HierarchyOptimizerConfig config, CancellationToken cancellationToken = default);

        /// <summary>
        /// Event for progress updates
        /// </summary>
        event EventHandler<OptimizationProgressEventArgs> ProgressUpdated;
    }

    /// <summary>
    /// Event arguments for optimization progress
    /// </summary>
    public class OptimizationProgressEventArgs : EventArgs
    {
        public string Stage { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public double ProgressPercentage { get; set; }
        public TimeSpan ElapsedTime { get; set; }
    }

    /// <summary>
    /// Result of the optimization process
    /// </summary>
    public class OptimizationResult
    {
        /// <summary>
        /// Whether the optimization was successful
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Optimized specifications
        /// </summary>
        public IEnumerable<SpecificationFile> OptimizedSpecifications { get; set; } = null!;

        /// <summary>
        /// Result of the evolution process
        /// </summary>
        public EvolutionResult EvolutionResult { get; set; } = null!;

        /// <summary>
        /// Optimization metrics
        /// </summary>
        public OptimizationMetrics Metrics { get; set; } = null!;
    }

    /// <summary>
    /// Metrics for the optimization process
    /// </summary>
    public class OptimizationMetrics
    {
        /// <summary>
        /// Original size of specifications in characters
        /// </summary>
        public int OriginalSize { get; set; }

        /// <summary>
        /// Optimized size of specifications in characters
        /// </summary>
        public int OptimizedSize { get; set; }

        /// <summary>
        /// Percentage reduction in size
        /// </summary>
        public double SizeReductionPercentage => OriginalSize > 0 ? 100.0 * (1.0 - (double)OptimizedSize / OriginalSize) : 0;

        /// <summary>
        /// Total number of sections in specifications
        /// </summary>
        public int TotalSections { get; set; }

        /// <summary>
        /// Number of sections that are inherited
        /// </summary>
        public int InheritedSections { get; set; }

        /// <summary>
        /// Percentage of sections that are inherited
        /// </summary>
        public double InheritancePercentage => TotalSections > 0 ? 100.0 * (double)InheritedSections / TotalSections : 0;

        /// <summary>
        /// Depth of the hierarchy
        /// </summary>
        public int HierarchyDepth { get; set; }

        /// <summary>
        /// Time taken for optimization
        /// </summary>
        public TimeSpan OptimizationTime { get; set; }
    }
}