using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GeneticSpecOptimizer.Common.Models;
using GeneticSpecOptimizer.GeneticAlgorithm.Models;
using GeneticSpecOptimizer.GeneticAlgorithm.Interfaces;

namespace GeneticSpecOptimizer.GeneticAlgorithm.Evolution
{
    /// <summary>
    /// Implementation of the evolution engine
    /// </summary>
    public class EvolutionEngine : IEvolutionEngine
    {
        private readonly IGeneticOperators _geneticOperators;
        private readonly IFitnessEvaluator _fitnessEvaluator;
        private readonly Random _random;

        /// <summary>
        /// Event for progress updates
        /// </summary>
        public event EventHandler<EvolutionProgressEventArgs>? ProgressUpdated;

        /// <summary>
        /// Creates a new evolution engine
        /// </summary>
        public EvolutionEngine(IGeneticOperators geneticOperators, IFitnessEvaluator fitnessEvaluator)
        {
            _geneticOperators = geneticOperators;
            _fitnessEvaluator = fitnessEvaluator;
            _random = new Random();
        }

        /// <summary>
        /// Runs the genetic algorithm
        /// </summary>
        public async Task<EvolutionResult> EvolveAsync(SpecificationGenome initialGenome, CodeHierarchy codeHierarchy, Common.Models.EvolutionConfig config, CancellationToken cancellationToken = default)
        {
            // Initialize random with seed if provided
            Random random = config.RandomSeed.HasValue ? new Random(config.RandomSeed.Value) : new Random();
            
            // Initialize population
            var population = await InitializePopulationAsync(initialGenome, config.PopulationSize, codeHierarchy, config, cancellationToken);
            
            // Track best fitness over generations
            var fitnessHistory = new List<double>();
            var bestGenome = population.OrderByDescending(g => g.FitnessScore).First();
            fitnessHistory.Add(bestGenome.FitnessScore);
            
            // Track convergence
            int generationsWithoutImprovement = 0;
            double previousBestFitness = bestGenome.FitnessScore;
            
            // Start timer
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            // Evolve for specified number of generations
            for (int generation = 1; generation <= config.MaxGenerations; generation++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                
                // Select parents
                var parents = await _geneticOperators.SelectAsync(population, config.PopulationSize, config.TournamentSize);
                
                // Create new population through crossover and mutation
                var newPopulation = new List<SpecificationGenome>();
                
                // Add elite individuals
                newPopulation.AddRange(population.OrderByDescending(g => g.FitnessScore).Take(config.ElitismCount));
                
                // Fill rest of population with offspring
                while (newPopulation.Count < config.PopulationSize)
                {
                    // Select two parents
                    var parent1 = parents[random.Next(parents.Count)];
                    var parent2 = parents[random.Next(parents.Count)];
                    
                    // Perform crossover
                    var (offspring1, offspring2) = await _geneticOperators.CrossoverAsync(parent1, parent2, config.CrossoverRate);
                    
                    // Perform mutation
                    offspring1 = await _geneticOperators.MutateAsync(offspring1, config.MutationRate);
                    offspring2 = await _geneticOperators.MutateAsync(offspring2, config.MutationRate);
                    
                    // Set generation
                    offspring1.Generation = generation;
                    offspring2.Generation = generation;
                    
                    // Evaluate fitness
                    offspring1.FitnessScore = await _fitnessEvaluator.EvaluateAsync(offspring1, codeHierarchy, null);
                    offspring2.FitnessScore = await _fitnessEvaluator.EvaluateAsync(offspring2, codeHierarchy, null);
                    
                    // Add to new population
                    newPopulation.Add(offspring1);
                    if (newPopulation.Count < config.PopulationSize)
                    {
                        newPopulation.Add(offspring2);
                    }
                }
                
                // Replace population
                population = newPopulation;
                
                // Find best genome
                bestGenome = population.OrderByDescending(g => g.FitnessScore).First();
                fitnessHistory.Add(bestGenome.FitnessScore);
                
                // Calculate average fitness
                double averageFitness = population.Average(g => g.FitnessScore);
                
                // Report progress
                ProgressUpdated?.Invoke(this, new EvolutionProgressEventArgs
                {
                    Generation = generation,
                    BestFitness = bestGenome.FitnessScore,
                    AverageFitness = averageFitness,
                    ElapsedTime = stopwatch.Elapsed
                });
                
                // Check for convergence
                if (bestGenome.FitnessScore - previousBestFitness < config.ConvergenceThreshold)
                {
                    generationsWithoutImprovement++;
                    if (generationsWithoutImprovement >= config.ConvergenceGenerations)
                    {
                        // Converged
                        return new EvolutionResult
                        {
                            BestGenome = bestGenome,
                            FinalPopulation = population,
                            FitnessHistory = fitnessHistory,
                            Generations = generation,
                            ElapsedTime = stopwatch.Elapsed,
                            Converged = true
                        };
                    }
                }
                else
                {
                    // Reset counter
                    generationsWithoutImprovement = 0;
                    previousBestFitness = bestGenome.FitnessScore;
                }
            }
            
            // Stop timer
            stopwatch.Stop();
            
            // Return result
            return new EvolutionResult
            {
                BestGenome = bestGenome,
                FinalPopulation = population,
                FitnessHistory = fitnessHistory,
                Generations = config.MaxGenerations,
                ElapsedTime = stopwatch.Elapsed,
                Converged = false
            };
        }
        
        private async Task<List<SpecificationGenome>> InitializePopulationAsync(SpecificationGenome initialGenome, int populationSize, CodeHierarchy codeHierarchy, Common.Models.EvolutionConfig config, CancellationToken cancellationToken)
        {
            var population = new List<SpecificationGenome>();
            
            // Add initial genome
            initialGenome.Generation = 0;
            initialGenome.FitnessScore = await _fitnessEvaluator.EvaluateAsync(initialGenome, codeHierarchy, null);
            population.Add(initialGenome);
            
            // Create variations
            for (int i = 1; i < populationSize; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                
                // Clone and mutate
                var genome = initialGenome.Clone();
                genome = await _geneticOperators.MutateAsync(genome, config.MutationRate * 2); // Higher mutation rate for initialization
                genome.Generation = 0;
                genome.FitnessScore = await _fitnessEvaluator.EvaluateAsync(genome, codeHierarchy, null);
                
                population.Add(genome);
            }
            
            return population;
        }
    }
}
