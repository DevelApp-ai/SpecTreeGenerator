using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GeneticSpecOptimizer.Common.Models;
using GeneticSpecOptimizer.GeneticAlgorithm.Models;

namespace GeneticSpecOptimizer.GeneticAlgorithm.Encoding
{
    /// <summary>
    /// Implementation of the specification genome encoder
    /// </summary>
    public class SpecificationGenomeEncoder : Interfaces.ISpecificationGenomeEncoder
    {
        /// <summary>
        /// Encodes specifications into a genome
        /// </summary>
        public async Task<SpecificationGenome> EncodeAsync(IEnumerable<SpecificationFile> specifications)
        {
            var genome = new SpecificationGenome();
            
            // Add specifications
            genome.SpecificationFiles.AddRange(specifications.Select(s => s.Clone()));
            
            // Extract parent-child relationships
            foreach (var spec in genome.SpecificationFiles)
            {
                if (!string.IsNullOrEmpty(spec.ParentSpecificationId))
                {
                    genome.ParentChildRelationships[spec.Id] = spec.ParentSpecificationId;
                }
            }
            
            return genome;
        }
        
        /// <summary>
        /// Decodes a genome into specifications
        /// </summary>
        public async Task<IEnumerable<SpecificationFile>> DecodeAsync(SpecificationGenome genome)
        {
            var specifications = new List<SpecificationFile>();
            
            // Clone specifications
            foreach (var spec in genome.SpecificationFiles)
            {
                var clone = spec.Clone();
                
                // Update parent-child relationships
                if (genome.ParentChildRelationships.TryGetValue(spec.Id, out var parentId))
                {
                    clone.ParentSpecificationId = parentId;
                }
                else
                {
                    clone.ParentSpecificationId = null;
                }
                
                specifications.Add(clone);
            }
            
            return specifications;
        }
        
        /// <summary>
        /// Generates a new specification file
        /// </summary>
        public SpecificationFile GenerateSpecificationFile(string name, string content, string componentId, string? parentSpecificationId = null)
        {
            return new SpecificationFile
            {
                Name = name,
                Content = content,
                ComponentId = componentId,
                ParentSpecificationId = parentSpecificationId,
                FilePath = $"{name}.ai"
            };
        }
    }
}
