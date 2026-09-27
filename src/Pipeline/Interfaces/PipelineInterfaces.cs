using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GeneticSpecOptimizer.Common.Models;
using GeneticSpecOptimizer.GeneticAlgorithm.Models;

namespace GeneticSpecOptimizer.Pipeline.Interfaces
{
    /// <summary>
    /// Interface for the optimization pipeline
    /// </summary>
    public interface IOptimizationPipeline
    {
        /// <summary>
        /// Runs the optimization pipeline
        /// </summary>
        Task<OptimizationResult> RunAsync(OptimizationPipelineConfig config, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Event for progress updates
        /// </summary>
        event EventHandler<PipelineProgressEventArgs> ProgressUpdated;
    }
    
    /// <summary>
    /// Event arguments for pipeline progress
    /// </summary>
    public class PipelineProgressEventArgs : EventArgs
    {
        public string Stage { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public double ProgressPercentage { get; set; }
        public TimeSpan ElapsedTime { get; set; }
    }
}
