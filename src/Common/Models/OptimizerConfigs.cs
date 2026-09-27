using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace GeneticSpecOptimizer.Common.Models
{
    /// <summary>
    /// Configuration settings for the hierarchy optimizer
    /// </summary>
    public class HierarchyOptimizerConfig
    {
        /// <summary>
        /// Maximum time to spend optimizing (in minutes)
        /// </summary>
        public int MaxOptimizationTimeMinutes { get; set; } = 5;
        
        /// <summary>
        /// Weight for size reduction in fitness calculation
        /// </summary>
        public double SizeReductionWeight { get; set; } = 0.4;
        
        /// <summary>
        /// Weight for redundancy reduction in fitness calculation
        /// </summary>
        public double RedundancyReductionWeight { get; set; } = 0.3;
        
        /// <summary>
        /// Weight for inheritance in fitness calculation
        /// </summary>
        public double InheritanceWeight { get; set; } = 0.2;
        
        /// <summary>
        /// Weight for code alignment in fitness calculation
        /// </summary>
        public double CodeAlignmentWeight { get; set; } = 0.1;
        
        /// <summary>
        /// Maximum hierarchy depth to allow
        /// </summary>
        public int MaxHierarchyDepth { get; set; } = 5;
        
        /// <summary>
        /// Additional settings
        /// </summary>
        public Dictionary<string, object> AdditionalSettings { get; set; } = new Dictionary<string, object>();
        
        /// <summary>
        /// Evolution configuration settings
        /// </summary>
        public EvolutionConfig EvolutionConfig { get; set; } = new EvolutionConfig();
    }
    
    /// <summary>
    /// Configuration settings for the genetic algorithm
    /// </summary>
    public class EvolutionConfig
    {
        /// <summary>
        /// Size of the population
        /// </summary>
        public int PopulationSize { get; set; } = 100;
        
        /// <summary>
        /// Maximum number of generations to run
        /// </summary>
        public int MaxGenerations { get; set; } = 50;
        
        /// <summary>
        /// Probability of mutation
        /// </summary>
        public double MutationRate { get; set; } = 0.1;
        
        /// <summary>
        /// Probability of crossover
        /// </summary>
        public double CrossoverRate { get; set; } = 0.8;
        
        /// <summary>
        /// Number of elite individuals to preserve
        /// </summary>
        public int ElitismCount { get; set; } = 5;
        
        /// <summary>
        /// Tournament size for selection
        /// </summary>
        public int TournamentSize { get; set; } = 3;
        
        /// <summary>
        /// Convergence threshold
        /// </summary>
        public double ConvergenceThreshold { get; set; } = 0.001;
        
        /// <summary>
        /// Number of generations with no improvement to consider converged
        /// </summary>
        public int ConvergenceGenerations { get; set; } = 10;
        
        /// <summary>
        /// Random seed for reproducibility
        /// </summary>
        public int? RandomSeed { get; set; }
        
        /// <summary>
        /// Additional settings
        /// </summary>
        public Dictionary<string, object> AdditionalSettings { get; set; } = new Dictionary<string, object>();
    }
}
