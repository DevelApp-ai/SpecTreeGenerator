using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using GeneticSpecOptimizer.Common.Models;
using GeneticSpecOptimizer.GeneticAlgorithm.Interfaces;
using GeneticSpecOptimizer.GeneticAlgorithm.Models;

namespace GeneticSpecOptimizer.GeneticAlgorithm.Operators
{
    /// <summary>
    /// Implementation of genetic operators
    /// </summary>
    public class GeneticOperators : IGeneticOperators
    {
        private readonly Random _random;

        /// <summary>
        /// Creates a new instance of genetic operators
        /// </summary>
        public GeneticOperators(int? seed = null)
        {
            _random = seed.HasValue ? new Random(seed.Value) : new Random();
        }

        /// <summary>
        /// Performs mutation on a genome
        /// </summary>
        public async Task<SpecificationGenome> MutateAsync(SpecificationGenome genome, double mutationRate)
        {
            // Clone genome
            var mutatedGenome = genome.Clone();

            // Apply mutations
            if (_random.NextDouble() < mutationRate)
            {
                await MutateSpecificationsAsync(mutatedGenome, mutationRate);
            }

            if (_random.NextDouble() < mutationRate)
            {
                await MutateRelationshipsAsync(mutatedGenome, mutationRate);
            }

            return mutatedGenome;
        }

        /// <summary>
        /// Performs crossover between two genomes
        /// </summary>
        public async Task<(SpecificationGenome, SpecificationGenome)> CrossoverAsync(SpecificationGenome parent1, SpecificationGenome parent2, double crossoverRate)
        {
            // Check if crossover should occur
            if (_random.NextDouble() > crossoverRate)
            {
                // No crossover, return clones of parents
                return (parent1.Clone(), parent2.Clone());
            }

            // Clone parents
            var offspring1 = parent1.Clone();
            var offspring2 = parent2.Clone();

            // Perform crossover on specifications
            await CrossoverSpecificationsAsync(offspring1, offspring2);

            // Perform crossover on relationships
            await CrossoverRelationshipsAsync(offspring1, offspring2);

            return (offspring1, offspring2);
        }

        /// <summary>
        /// Selects parents from a population
        /// </summary>
        public async Task<List<SpecificationGenome>> SelectAsync(List<SpecificationGenome> population, int count, int tournamentSize)
        {
            await Task.CompletedTask;
            var selected = new List<SpecificationGenome>();

            // Tournament selection
            for (int i = 0; i < count; i++)
            {
                // Select random individuals for tournament
                var tournament = new List<SpecificationGenome>();
                for (int j = 0; j < tournamentSize; j++)
                {
                    tournament.Add(population[_random.Next(population.Count)]);
                }

                // Select winner (highest fitness)
                var winner = tournament.OrderByDescending(g => g.FitnessScore).First();
                selected.Add(winner);
            }

            return selected;
        }

        private async Task MutateSpecificationsAsync(SpecificationGenome genome, double mutationRate)
        {
            // Mutate each specification with probability mutationRate
            foreach (var spec in genome.SpecificationFiles)
            {
                if (_random.NextDouble() < mutationRate)
                {
                    // Mutate content
                    await MutateContentAsync(spec, mutationRate);
                }
            }

            // Add or remove specifications with low probability
            if (_random.NextDouble() < mutationRate * 0.1)
            {
                if (_random.NextDouble() < 0.5 && genome.SpecificationFiles.Count > 1)
                {
                    // Remove random specification
                    int indexToRemove = _random.Next(genome.SpecificationFiles.Count);
                    var specToRemove = genome.SpecificationFiles[indexToRemove];
                    genome.SpecificationFiles.RemoveAt(indexToRemove);

                    // Remove relationships
                    var keysToRemove = genome.ParentChildRelationships.Where(kvp => kvp.Key == specToRemove.Id || kvp.Value == specToRemove.Id).Select(kvp => kvp.Key).ToList();
                    foreach (var key in keysToRemove)
                    {
                        genome.ParentChildRelationships.Remove(key);
                    }
                }
                else
                {
                    // Duplicate random specification with mutations
                    int indexToDuplicate = _random.Next(genome.SpecificationFiles.Count);
                    var specToDuplicate = genome.SpecificationFiles[indexToDuplicate];
                    var newSpec = specToDuplicate.Clone();
                    newSpec.Id = Guid.NewGuid().ToString();
                    newSpec.Name = $"{specToDuplicate.Name}_variant";

                    // Mutate content
                    await MutateContentAsync(newSpec, mutationRate * 2);

                    genome.SpecificationFiles.Add(newSpec);
                }
            }
        }

        private async Task MutateContentAsync(SpecificationFile spec, double mutationRate)
        {
            await Task.CompletedTask;
            // Parse content into sections
            var sections = ParseContentSections(spec.Content);

            // Mutate sections
            foreach (var section in sections)
            {
                if (_random.NextDouble() < mutationRate)
                {
                    // Apply random mutation to section
                    switch (_random.Next(3))
                    {
                        case 0:
                            // Modify section title
                            section.Title = MutateText(section.Title);
                            break;
                        case 1:
                            // Modify section content
                            section.Content = MutateText(section.Content);
                            break;
                        case 2:
                            // Reorder subsections
                            if (section.Subsections.Count > 1)
                            {
                                section.Subsections = section.Subsections.OrderBy(_ => _random.Next()).ToList();
                            }
                            break;
                    }
                }
            }

            // Rebuild content
            spec.Content = RebuildContent(sections);
        }

        private async Task MutateRelationshipsAsync(SpecificationGenome genome, double mutationRate)
        {
            await Task.CompletedTask;
            // Clone relationships
            var relationships = new Dictionary<string, string>(genome.ParentChildRelationships);

            // Mutate each relationship with probability mutationRate
            foreach (var key in relationships.Keys.ToList())
            {
                if (_random.NextDouble() < mutationRate)
                {
                    // Remove relationship
                    genome.ParentChildRelationships.Remove(key);
                }
            }

            // Add new relationships with probability mutationRate
            foreach (var spec in genome.SpecificationFiles)
            {
                if (_random.NextDouble() < mutationRate && !genome.ParentChildRelationships.ContainsKey(spec.Id))
                {
                    // Find potential parent
                    var potentialParents = genome.SpecificationFiles
                        .Where(s => s.Id != spec.Id && !IsAncestor(genome, spec.Id, s.Id))
                        .ToList();

                    if (potentialParents.Count > 0)
                    {
                        // Add relationship
                        var parent = potentialParents[_random.Next(potentialParents.Count)];
                        genome.ParentChildRelationships[spec.Id] = parent.Id;
                    }
                }
            }
        }

        private static bool IsAncestor(SpecificationGenome genome, string ancestorId, string descendantId)
        {
            // Check if ancestorId is an ancestor of descendantId
            string currentId = descendantId;
            while (genome.ParentChildRelationships.TryGetValue(currentId, out string? parentId))
            {
                if (parentId == ancestorId)
                {
                    return true;
                }
                currentId = parentId;
            }
            return false;
        }

        private async Task CrossoverSpecificationsAsync(SpecificationGenome offspring1, SpecificationGenome offspring2)
        {
            await Task.CompletedTask;
            // Create mapping of specification IDs between parents
            var idMapping1to2 = new Dictionary<string, string>();
            var idMapping2to1 = new Dictionary<string, string>();

            // Map specifications with same name
            foreach (var spec1 in offspring1.SpecificationFiles)
            {
                var matchingSpec2 = offspring2.SpecificationFiles.FirstOrDefault(s => s.Name == spec1.Name);
                if (matchingSpec2 != null)
                {
                    idMapping1to2[spec1.Id] = matchingSpec2.Id;
                    idMapping2to1[matchingSpec2.Id] = spec1.Id;
                }
            }

            // Crossover point
            int crossoverPoint1 = _random.Next(offspring1.SpecificationFiles.Count);
            int crossoverPoint2 = _random.Next(offspring2.SpecificationFiles.Count);

            // Swap specifications
            var temp1 = offspring1.SpecificationFiles.Skip(crossoverPoint1).ToList();
            var temp2 = offspring2.SpecificationFiles.Skip(crossoverPoint2).ToList();

            offspring1.SpecificationFiles.RemoveRange(crossoverPoint1, offspring1.SpecificationFiles.Count - crossoverPoint1);
            offspring2.SpecificationFiles.RemoveRange(crossoverPoint2, offspring2.SpecificationFiles.Count - crossoverPoint2);

            offspring1.SpecificationFiles.AddRange(temp2);
            offspring2.SpecificationFiles.AddRange(temp1);

            // Update relationships
            UpdateRelationships(offspring1, idMapping2to1);
            UpdateRelationships(offspring2, idMapping1to2);
        }

        private static void UpdateRelationships(SpecificationGenome genome, Dictionary<string, string> idMapping)
        {
            // Update parent-child relationships
            var newRelationships = new Dictionary<string, string>();

            foreach (var kvp in genome.ParentChildRelationships)
            {
                string childId = kvp.Key;
                string parentId = kvp.Value;

                // Check if child exists
                if (genome.SpecificationFiles.Any(s => s.Id == childId) || idMapping.TryGetValue(childId, out string? mappedChildId))
                {
                    // Check if parent exists
                    if (genome.SpecificationFiles.Any(s => s.Id == parentId) || idMapping.TryGetValue(parentId, out string? mappedParentId))
                    {
                        // Add relationship
                        string actualChildId = idMapping.TryGetValue(childId, out mappedChildId) ? mappedChildId! : childId;
                        string actualParentId = idMapping.TryGetValue(parentId, out mappedParentId) ? mappedParentId! : parentId;

                        newRelationships[actualChildId] = actualParentId;
                    }
                }
            }

            genome.ParentChildRelationships = newRelationships;
        }

        private async Task CrossoverRelationshipsAsync(SpecificationGenome offspring1, SpecificationGenome offspring2)
        {
            await Task.CompletedTask;
            // Crossover point
            int crossoverPoint = _random.Next(Math.Max(offspring1.ParentChildRelationships.Count, offspring2.ParentChildRelationships.Count) + 1);

            // Get keys
            var keys1 = offspring1.ParentChildRelationships.Keys.ToList();
            var keys2 = offspring2.ParentChildRelationships.Keys.ToList();

            // Swap relationships
            for (int i = 0; i < crossoverPoint && i < keys1.Count && i < keys2.Count; i++)
            {
                string key1 = keys1[i];
                string key2 = keys2[i];

                if (offspring1.SpecificationFiles.Any(s => s.Id == key2) &&
                    offspring1.SpecificationFiles.Any(s => s.Id == offspring2.ParentChildRelationships[key2]) &&
                    offspring2.SpecificationFiles.Any(s => s.Id == key1) &&
                    offspring2.SpecificationFiles.Any(s => s.Id == offspring1.ParentChildRelationships[key1]))
                {
                    string value1 = offspring1.ParentChildRelationships[key1];
                    string value2 = offspring2.ParentChildRelationships[key2];

                    offspring1.ParentChildRelationships[key2] = value2;
                    offspring2.ParentChildRelationships[key1] = value1;

                    if (key1 != key2)
                    {
                        offspring1.ParentChildRelationships.Remove(key1);
                        offspring2.ParentChildRelationships.Remove(key2);
                    }
                }
            }
        }

        private string MutateText(string text)
        {
            // Simple text mutation
            if (string.IsNullOrEmpty(text))
            {
                return text;
            }

            // Split into words
            var words = text.Split(' ').ToList();

            // Apply random mutation
            switch (_random.Next(4))
            {
                case 0:
                    // Remove random word
                    if (words.Count > 1)
                    {
                        words.RemoveAt(_random.Next(words.Count));
                    }
                    break;
                case 1:
                    // Duplicate random word
                    if (words.Count > 0)
                    {
                        int index = _random.Next(words.Count);
                        words.Insert(index, words[index]);
                    }
                    break;
                case 2:
                    // Swap random words
                    if (words.Count > 1)
                    {
                        int index1 = _random.Next(words.Count);
                        int index2 = _random.Next(words.Count);
                        string temp = words[index1];
                        words[index1] = words[index2];
                        words[index2] = temp;
                    }
                    break;
                case 3:
                    // Add random word
                    string[] randomWords = { "the", "a", "an", "and", "or", "but", "if", "then", "else", "while", "for", "each", "all", "some", "any" };
                    words.Insert(_random.Next(words.Count + 1), randomWords[_random.Next(randomWords.Length)]);
                    break;
            }

            // Join words
            return string.Join(" ", words);
        }

        private sealed class ContentSection
        {
            public string Title { get; set; } = string.Empty;
            public string Content { get; set; } = string.Empty;
            public int Level { get; set; }
            public List<ContentSection> Subsections { get; set; } = new List<ContentSection>();
        }

        private static List<ContentSection> ParseContentSections(string content)
        {
            // Simple parsing of markdown sections
            var sections = new List<ContentSection>();
            ContentSection? currentSection = null;

            foreach (var line in content.Split('\n'))
            {
                if (line.StartsWith('#'))
                {
                    // Count heading level
                    int level = 0;
                    while (level < line.Length && line[level] == '#')
                    {
                        level++;
                    }

                    // Create new section
                    var section = new ContentSection
                    {
                        Title = line.Substring(level).Trim(),
                        Level = level
                    };

                    // Add to appropriate parent
                    if (level == 1)
                    {
                        sections.Add(section);
                        currentSection = section;
                    }
                    else if (currentSection != null)
                    {
                        currentSection.Subsections.Add(section);
                    }
                }
                else if (currentSection != null)
                {
                    // Add to current section content
                    currentSection.Content += line + "\n";
                }
            }

            return sections;
        }

        private static string RebuildContent(List<ContentSection> sections)
        {
            // Rebuild content from sections
            var content = new System.Text.StringBuilder();

            foreach (var section in sections)
            {
                // Add heading
                content.AppendLine(new string('#', section.Level) + " " + section.Title);

                // Add content
                content.AppendLine(section.Content);

                // Add subsections
                foreach (var subsection in section.Subsections)
                {
                    content.AppendLine(new string('#', subsection.Level) + " " + subsection.Title);
                    content.AppendLine(subsection.Content);
                }
            }

            return content.ToString();
        }
    }
}