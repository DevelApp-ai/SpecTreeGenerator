using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GeneticSpecOptimizer.Common.Models;
using GeneticSpecOptimizer.GeneticAlgorithm.Models;

namespace GeneticSpecOptimizer.GeneticAlgorithm.Models
{
    /// <summary>
    /// Represents a genome in the genetic algorithm
    /// </summary>
    public class SpecificationGenome
    {
        /// <summary>
        /// Unique identifier for this genome
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();
        
        /// <summary>
        /// Specification files in this genome
        /// </summary>
        public List<SpecificationFile> SpecificationFiles { get; set; } = new List<SpecificationFile>();
        
        /// <summary>
        /// Parent-child relationships between specifications
        /// </summary>
        public Dictionary<string, string> ParentChildRelationships { get; set; } = new Dictionary<string, string>();
        
        /// <summary>
        /// Fitness score of this genome
        /// </summary>
        public double FitnessScore { get; set; }
        
        /// <summary>
        /// Generation this genome belongs to
        /// </summary>
        public int Generation { get; set; }
        
        /// <summary>
        /// Additional metadata
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
        
        /// <summary>
        /// Creates a deep clone of this genome
        /// </summary>
        public SpecificationGenome Clone()
        {
            var clone = new SpecificationGenome
            {
                Id = Guid.NewGuid().ToString(),
                FitnessScore = this.FitnessScore,
                Generation = this.Generation,
                Metadata = new Dictionary<string, object>(this.Metadata),
                ParentChildRelationships = new Dictionary<string, string>(this.ParentChildRelationships)
            };
            
            foreach (var spec in this.SpecificationFiles)
            {
                clone.SpecificationFiles.Add(spec.Clone());
            }
            
            return clone;
        }
        
        /// <summary>
        /// Returns a string representation of the genome
        /// </summary>
        public override string ToString()
        {
            return $"Genome: {Id} (Fitness: {FitnessScore}, Generation: {Generation}, Specs: {SpecificationFiles.Count})";
        }
    }
    
    /// <summary>
    /// Represents the result of running the genetic algorithm
    /// </summary>
    public class EvolutionResult
    {
        /// <summary>
        /// Best genome found
        /// </summary>
        public SpecificationGenome BestGenome { get; set; } = null!;
        
        /// <summary>
        /// All genomes in the final population
        /// </summary>
        public List<SpecificationGenome> FinalPopulation { get; set; } = new List<SpecificationGenome>();
        
        /// <summary>
        /// Fitness history over generations
        /// </summary>
        public List<double> FitnessHistory { get; set; } = new List<double>();
        
        /// <summary>
        /// Number of generations run
        /// </summary>
        public int Generations { get; set; }
        
        /// <summary>
        /// Time taken for evolution
        /// </summary>
        public TimeSpan ElapsedTime { get; set; }
        
        /// <summary>
        /// Whether evolution converged
        /// </summary>
        public bool Converged { get; set; }
        
        /// <summary>
        /// Additional metadata
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
        
        /// <summary>
        /// Returns a string representation of the evolution result
        /// </summary>
        public override string ToString()
        {
            return $"Evolution Result: {Generations} generations, Best Fitness: {BestGenome?.FitnessScore}, Time: {ElapsedTime}";
        }
    }
    
    // FitnessMetrics class has been moved to a dedicated file: FitnessMetrics.cs
}
