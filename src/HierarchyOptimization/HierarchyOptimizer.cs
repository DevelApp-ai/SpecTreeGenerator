using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GeneticSpecOptimizer.Common.Models;
using GeneticSpecOptimizer.GeneticAlgorithm.Encoding;
using GeneticSpecOptimizer.GeneticAlgorithm.Evolution;
using GeneticSpecOptimizer.GeneticAlgorithm.Fitness;
using GeneticSpecOptimizer.GeneticAlgorithm.Operators;
using GeneticSpecOptimizer.GeneticAlgorithm.Models;
using GeneticSpecOptimizer.GeneticAlgorithm.Interfaces;

namespace GeneticSpecOptimizer.HierarchyOptimization
{
        // HierarchyOptimizerConfig has been moved to Common/Models/OptimizerConfigs.cs
    
    /// <summary>
    /// Result of hierarchy optimization
    /// </summary>
    public class HierarchyOptimizationResult
    {
        /// <summary>
        /// Optimized specifications
        /// </summary>
        public List<SpecificationFile> OptimizedSpecifications { get; set; }
        
        /// <summary>
        /// Evolution result
        /// </summary>
        public EvolutionResult EvolutionResult { get; set; }
        
        /// <summary>
        /// Optimization metrics
        /// </summary>
        public OptimizationMetrics Metrics { get; set; }
    }
    
    /// <summary>
    /// Metrics for optimization
    /// </summary>
    public class OptimizationMetrics
    {
        /// <summary>
        /// Total size of specifications before optimization
        /// </summary>
        public int OriginalSize { get; set; }
        
        /// <summary>
        /// Total size of specifications after optimization
        /// </summary>
        public int OptimizedSize { get; set; }
        
        /// <summary>
        /// Size reduction percentage
        /// </summary>
        public double SizeReductionPercentage => 
            OriginalSize > 0 ? 100.0 * (OriginalSize - OptimizedSize) / OriginalSize : 0;
        
        /// <summary>
        /// Number of inherited sections
        /// </summary>
        public int InheritedSections { get; set; }
        
        /// <summary>
        /// Total number of sections
        /// </summary>
        public int TotalSections { get; set; }
        
        /// <summary>
        /// Inheritance percentage
        /// </summary>
        public double InheritancePercentage => 
            TotalSections > 0 ? 100.0 * InheritedSections / TotalSections : 0;
        
        /// <summary>
        /// Hierarchy depth
        /// </summary>
        public int HierarchyDepth { get; set; }
        
        /// <summary>
        /// Time taken for optimization
        /// </summary>
        public TimeSpan OptimizationTime { get; set; }
    }
    
    /// <summary>
    /// Interface for hierarchy optimizer
    /// </summary>
    public interface IHierarchyOptimizer
    {
        /// <summary>
        /// Optimizes a specification hierarchy
        /// </summary>
        Task<HierarchyOptimizationResult> OptimizeHierarchyAsync(
            IEnumerable<SpecificationFile> specifications,
            HierarchyOptimizerConfig config = null);
        
        /// <summary>
        /// Event raised when optimization progress is updated
        /// </summary>
        event EventHandler<OptimizationProgressEventArgs> OptimizationProgressUpdated;
    }
    
    /// <summary>
    /// Event arguments for optimization progress
    /// </summary>
    public class OptimizationProgressEventArgs : EventArgs
    {
        /// <summary>
        /// Current generation
        /// </summary>
        public int CurrentGeneration { get; set; }
        
        /// <summary>
        /// Maximum generations
        /// </summary>
        public int MaxGenerations { get; set; }
        
        /// <summary>
        /// Current best fitness
        /// </summary>
        public double BestFitness { get; set; }
        
        /// <summary>
        /// Progress percentage
        /// </summary>
        public double ProgressPercentage => 
            MaxGenerations > 0 ? 100.0 * CurrentGeneration / MaxGenerations : 0;
        
        /// <summary>
        /// Time elapsed
        /// </summary>
        public TimeSpan TimeElapsed { get; set; }
    }
    
    /// <summary>
    /// Implementation of hierarchy optimizer
    /// </summary>
    public class HierarchyOptimizer : IHierarchyOptimizer
    {
        private readonly ISpecificationGenomeEncoder _genomeEncoder;
        
        public event EventHandler<OptimizationProgressEventArgs> OptimizationProgressUpdated;
        
        public HierarchyOptimizer(ISpecificationGenomeEncoder genomeEncoder)
        {
            _genomeEncoder = genomeEncoder ?? throw new ArgumentNullException(nameof(genomeEncoder));
        }
        
        public async Task<HierarchyOptimizationResult> OptimizeHierarchyAsync(
            IEnumerable<SpecificationFile> specifications,
            HierarchyOptimizerConfig config = null)
        {
            // Use default config if none provided
            config ??= new HierarchyOptimizerConfig();
            
            // Start timing
            var startTime = DateTime.Now;
            
            // Calculate original size
            var originalSize = CalculateTotalSize(specifications);
            
            // Encode specifications to genome
            var initialGenome = await _genomeEncoder.EncodeAsync(specifications);
            
            // Create fitness evaluator
            var fitnessEvaluator = new FitnessEvaluator();
            
            // Get code hierarchy (placeholder - in a real implementation, this would be passed in)
            var codeHierarchy = new CodeHierarchy();
            
            // Create evolution engine
            var evolutionEngine = new EvolutionEngine(new GeneticOperators(), fitnessEvaluator);
            
            // Subscribe to progress updates
            evolutionEngine.ProgressUpdated += (sender, e) =>
            {
                OnOptimizationProgressUpdated(new OptimizationProgressEventArgs
                {
                    CurrentGeneration = e.Generation,
                    MaxGenerations = config.EvolutionConfig.MaxGenerations,
                    BestFitness = e.BestFitness,
                    TimeElapsed = DateTime.Now - startTime
                });
            };
            
            // Set up cancellation if max time is specified
            var cancellationTokenSource = config.MaxOptimizationTimeMinutes > 0
                ? new System.Threading.CancellationTokenSource(TimeSpan.FromMinutes(config.MaxOptimizationTimeMinutes))
                : new System.Threading.CancellationTokenSource();
            
            // Run evolution
            EvolutionResult evolutionResult;
            try
            {
                evolutionResult = await evolutionEngine.EvolveAsync(initialGenome, codeHierarchy, config.EvolutionConfig, cancellationTokenSource.Token);
            }
            catch (OperationCanceledException)
            {
                // Evolution was cancelled due to time limit
                // We'll use the best genome found so far
                evolutionResult = new EvolutionResult
                {
                    BestGenome = initialGenome, // This would be updated by the evolution engine
                    Generations = 0,
                    Converged = false,
                    ElapsedTime = DateTime.Now - startTime
                };
            }
            
            // Decode the best genome to specifications
            var optimizedSpecifications = (await _genomeEncoder.DecodeAsync(evolutionResult.BestGenome)).ToList();
            
            // Calculate optimization metrics
            var metrics = CalculateOptimizationMetrics(
                specifications.ToList(),
                optimizedSpecifications,
                evolutionResult.BestGenome,
                DateTime.Now - startTime);
            
            // Return result
            return new HierarchyOptimizationResult
            {
                OptimizedSpecifications = optimizedSpecifications,
                EvolutionResult = evolutionResult,
                Metrics = metrics
            };
        }
        
        private int CalculateTotalSize(IEnumerable<SpecificationFile> specifications)
        {
            return specifications.Sum(s => s.Content?.Length ?? 0);
        }
        
        private OptimizationMetrics CalculateOptimizationMetrics(
            List<SpecificationFile> originalSpecifications,
            List<SpecificationFile> optimizedSpecifications,
            SpecificationGenome bestGenome,
            TimeSpan optimizationTime)
        {
            // Calculate sizes
            int originalSize = CalculateTotalSize(originalSpecifications);
            int optimizedSize = CalculateTotalSize(optimizedSpecifications);
            
            // Calculate inherited sections
            int inheritedSections = 0;
            int totalSections = 0;
            
            // Convert specification files to genes for analysis
            var genes = new List<SpecificationGene>();
            foreach (var spec in optimizedSpecifications)
            {
                genes.Add(SpecificationGene.FromSpecificationFile(spec));
            }
            
            foreach (var gene in genes)
            {
                foreach (var section in gene.ContentSections)
                {
                    if (section.Type != ContentSectionType.Header)
                    {
                        totalSections++;
                        
                        if (section.IsInherited)
                        {
                            inheritedSections++;
                        }
                    }
                }
            }
            
            // Calculate hierarchy depth
            int hierarchyDepth = CalculateHierarchyDepth(bestGenome);
            
            return new OptimizationMetrics
            {
                OriginalSize = originalSize,
                OptimizedSize = optimizedSize,
                InheritedSections = inheritedSections,
                TotalSections = totalSections,
                HierarchyDepth = hierarchyDepth,
                OptimizationTime = optimizationTime
            };
        }
        
        private int CalculateHierarchyDepth(SpecificationGenome genome)
        {
            // Build a map of parent-child relationships
            var childToParent = genome.ParentChildRelationships;
            
            // Calculate the depth of each specification
            var depths = new Dictionary<string, int>();
            
            foreach (var specId in genome.SpecificationFiles.Select(s => s.Id))
            {
                CalculateSpecDepth(specId, childToParent, depths);
            }
            
            // Return the maximum depth
            return depths.Values.DefaultIfEmpty(0).Max();
        }
        
        private int CalculateSpecDepth(
            string specId,
            Dictionary<string, string> childToParent,
            Dictionary<string, int> depths)
        {
            // Check if we've already calculated the depth for this specification
            if (depths.TryGetValue(specId, out int depth))
            {
                return depth;
            }
            
            // Base case: no parent
            if (!childToParent.TryGetValue(specId, out string parentId) || string.IsNullOrEmpty(parentId))
            {
                depths[specId] = 0;
                return 0;
            }
            
            // Recursive case: calculate parent depth first
            int parentDepth = CalculateSpecDepth(parentId, childToParent, depths);
            depths[specId] = parentDepth + 1;
            return parentDepth + 1;
        }
        
        protected virtual void OnOptimizationProgressUpdated(OptimizationProgressEventArgs e)
        {
            OptimizationProgressUpdated?.Invoke(this, e);
        }
    }
}
