using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GeneticSpecOptimizer.Common.Models;
using GeneticSpecOptimizer.GeneticAlgorithm.Models;
using GeneticSpecOptimizer.HierarchyOptimization;

namespace GeneticSpecOptimizer.Pipeline
{
    // OptimizationPipelineConfig has been moved to OptimizationPipeline.cs
    
    /// <summary>
    /// Represents the result of running the optimization pipeline
    /// </summary>
    public class OptimizationResult
    {
        /// <summary>
        /// Whether the optimization was successful
        /// </summary>
        public bool Success { get; set; }
        
        /// <summary>
        /// Error message if not successful
        /// </summary>
        public string? ErrorMessage { get; set; }
        
        /// <summary>
        /// Paths to output files
        /// </summary>
        public List<string> OutputFilePaths { get; set; } = new List<string>();
        
        /// <summary>
        /// Result of the evolution process
        /// </summary>
        public EvolutionResult? EvolutionResult { get; set; }
        
        /// <summary>
        /// Optimization metrics
        /// </summary>
        public OptimizationMetrics Metrics { get; set; } = new OptimizationMetrics();
        
        /// <summary>
        /// Additional data
        /// </summary>
        public Dictionary<string, object> AdditionalData { get; set; } = new Dictionary<string, object>();
    }
    
    // OptimizationMetrics has been moved to OptimizationPipeline.cs
}
