using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using SpecTreeGenerator.Common.Models;
using SpecTreeGenerator.GeneticAlgorithm.Models;

namespace SpecTreeGenerator.GeneticAlgorithm.Interfaces
{
    /// <summary>
    /// Interface for specification genome encoders
    /// </summary>
    public interface ISpecificationGenomeEncoder
    {
        /// <summary>
        /// Encodes specifications into a genome
        /// </summary>
        Task<SpecificationGenome> EncodeAsync(IEnumerable<SpecificationFile> specifications);

        /// <summary>
        /// Decodes a genome into specifications
        /// </summary>
        Task<IEnumerable<SpecificationFile>> DecodeAsync(SpecificationGenome genome);
    }

    /// <summary>
    /// Interface for genetic operators
    /// </summary>
    public interface IGeneticOperators
    {
        /// <summary>
        /// Performs mutation on a genome
        /// </summary>
        Task<SpecificationGenome> MutateAsync(SpecificationGenome genome, double mutationRate);

        /// <summary>
        /// Performs crossover between two genomes
        /// </summary>
        Task<(SpecificationGenome, SpecificationGenome)> CrossoverAsync(SpecificationGenome parent1, SpecificationGenome parent2, double crossoverRate);

        /// <summary>
        /// Selects parents from a population
        /// </summary>
        Task<List<SpecificationGenome>> SelectAsync(List<SpecificationGenome> population, int count, int tournamentSize);
    }

    /// <summary>
    /// Interface for fitness evaluators
    /// </summary>
    public interface IFitnessEvaluator
    {
        /// <summary>
        /// Evaluates the fitness of a genome
        /// </summary>
        Task<double> EvaluateAsync(SpecificationGenome genome, CodeHierarchy codeHierarchy, HierarchyOptimizerConfig? config);

        /// <summary>
        /// Calculates detailed fitness metrics for a genome
        /// </summary>
        Task<FitnessMetrics> CalculateMetricsAsync(SpecificationGenome genome, CodeHierarchy codeHierarchy);
    }

    /// <summary>
    /// Interface for evolution engines
    /// </summary>
    public interface IEvolutionEngine
    {
        /// <summary>
        /// Runs the genetic algorithm
        /// </summary>
        Task<EvolutionResult> EvolveAsync(SpecificationGenome initialGenome, CodeHierarchy codeHierarchy, Common.Models.EvolutionConfig config, CancellationToken cancellationToken = default);

        /// <summary>
        /// Event for progress updates
        /// </summary>
        event EventHandler<EvolutionProgressEventArgs> ProgressUpdated;
    }

    /// <summary>
    /// Event arguments for evolution progress
    /// </summary>
    public class EvolutionProgressEventArgs : EventArgs
    {
        public int Generation { get; set; }
        public double BestFitness { get; set; }
        public double AverageFitness { get; set; }
        public TimeSpan ElapsedTime { get; set; }
    }
}